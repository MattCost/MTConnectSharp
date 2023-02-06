using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;

namespace MTConnectSharp
{
    /// <summary>
    /// Connects to a single agent and streams data from it.
    /// </summary>
    public class MTConnectClient : IMTConnectClient, IDisposable
    {
        /// <summary>
        /// The probe response has been received and parsed
        /// </summary>
        public event EventHandler? ProbeCompleted;

        /// <summary>
        /// A Current or Sample response has been parsed, and new DataItems are available.
        /// </summary>
        public event EventHandler? DataItemsChanged;

        private string _agentUri = string.Empty;
        /// <summary>
        /// The base uri of the agent
        /// </summary>
        public string AgentUri
        {
            get { return _agentUri; }
            set
            {
                // TODO make setter private, and add SetAgentUri method? 
                // TODO throw if AgentUri is changed while sampling/streaming are active
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }
                if (string.IsNullOrEmpty(_agentUri) || (!string.Equals(_agentUri, value)))
                {
                    _agentUri = value;
                    _restClient?.Dispose();
                    _restClient = CreateRestClient();
                }
            }
        }
        /// <summary>
        /// Time between sample queries when using Sampling mode. Default is 2.0 seconds.
        /// </summary>
        public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Devices on the connected agent
        /// </summary>
        public ReadOnlyObservableCollection<Device> Devices
        {
            get;
            private set;
        }
        private ObservableCollection<Device> _devices;

        /// <summary>
        /// Dictionary Reference to all data items by id for better performance when streaming
        /// </summary>
        private Dictionary<string, DataItem> _dataItemsDictionary = new Dictionary<string, DataItem>();

        private ReadOnlyDictionary<string, DataItem>? _readOnlyDataItemsDictionary = null;

        /// <summary>
        /// Dictionary containing all Data Items available from the MTConnect stream.
        /// </summary>
        public ReadOnlyDictionary<string, DataItem> DataItemsDictionary
        {
            get
            {
                return _readOnlyDataItemsDictionary ??= new ReadOnlyDictionary<string, DataItem>(_dataItemsDictionary);
            }
        }


        /// <summary>
        /// RestSharp RestClient
        /// </summary>
        private RestClient? _restClient;

        protected virtual RestClient CreateRestClient()
        {
            var options = new RestClientOptions(_agentUri)
            {
                ThrowOnAnyError = false,
                MaxTimeout = 5000
            };
            return new RestClient(options);
        }

        /// <summary>
        /// Not actually parsing multipart stream - this timer fires sample queries to simulate streaming
        /// </summary>
        private Timer? _samplingTimer;

        /// <summary>
        /// Last sequence number read from current or sample
        /// </summary>
        private long _lastSequence;

        private bool _probeStarted = false;
        private bool _probeCompleted = false;

        /// <summary>
        /// Initializes a new Client 
        /// </summary>
        public MTConnectClient()
        {
            _devices = new ObservableCollection<Device>();
            Devices = new ReadOnlyObservableCollection<Device>(_devices);
        }

        /// <summary>
        /// Start MTConnect Streaming mode, not yet implemented. Will throw NotImplementedException
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown always</exception>
        public Task StartStreamingAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stop MTConnect Streaming mode, not yet implemented. Will throw NotImplementedException
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown always</exception>
        public void StopStreaming()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts sample polling and updating DataItem values as they change
        /// </summary>
        public async Task StartSamplingAsync()
        {
            if (_samplingTimer?.Enabled == true)
            {
                return;
            }

            await GetCurrentStateAsync();

            _samplingTimer = new Timer(UpdateInterval.TotalMilliseconds);
            _samplingTimer.Elapsed += StreamingTimerElapsed;
            _samplingTimer.Start();
        }

        /// <summary>
        /// Stops sample polling
        /// </summary>
        public void StopSampling()
        {
            _samplingTimer?.Stop();
        }

        /// <summary>
        /// Gets current response and updates DataItems
        /// </summary>
        public async Task GetCurrentStateAsync()
        {
            if (!_probeCompleted || _restClient == null)
            {
                throw new InvalidOperationException("Cannot get DataItem values. Agent has not been probed yet. Set AgentUri, and call ProbeAsync first");
            }

            var request = new RestRequest
            {
                Resource = "current"
            };

            //TODO add support for mtconnect json
            request.AddHeader("Accept", "application/accepts=xml");

            var response = await _restClient.ExecuteAsync(request).ConfigureAwait(false);

            if (response.ResponseStatus == ResponseStatus.Completed)
            {
                throw new GetCurrentStateFailedException($"get /current request failed to complete for {AgentUri}.");
            }

            if (response.IsSuccessStatusCode)
            {
                throw new GetCurrentStateFailedException($"get /current request completed, but status code did not indicate success. Status Code: {response.StatusCode}");
            }

            ParseStream(response);
        }

        /// <summary>
        /// Gets probe response from the agent and populates the devices collection
        /// </summary>
        /// <remarks>
        /// AgentUri must be set before calling ProbeAsync. 
        /// Only 1 Probe may be active a time.
        /// If ProbeAsync is called without an AgentUri, or multiple times, an InvalidOperationException will be thrown
        /// Set an Event Handler on ProbeComplete() to be notified when the Probe Operation finishes.
        /// A ProbeFailedException will be thrown if there are any problems with the request, or a non-success response is received from the agent.
        /// A ParseResultException will be thrown if there are any problems parsing the xml response.
        /// </remarks>
        /// <returns>A Task representing the async Probe request</returns>
        /// <exception cref="InvalidOperationException">Thrown if client is in invalid state for probing.</exception>
        /// <exception cref="ProbeFailedException">Thrown if the client is unable to connect to the agent</exception>
        /// <exception cref="ParseXMLException">Thrown if the client is unable to parse the response from the agent</exception>
        public async Task ProbeAsync()
        {
            if (_restClient == null)
            {
                throw new InvalidOperationException("Must set AgentUri before calling ProbeAsync");
            }
            if (_probeStarted && !_probeCompleted)
            {
                throw new InvalidOperationException("Cannot start a new Probe when one is still running.");
            }

            var request = new RestRequest
            {
                Resource = "probe"
            };

            _probeStarted = true;
            _probeCompleted = false;
            //clear _devices
            //clear dictionary

            var response = await _restClient.ExecuteAsync(request).ConfigureAwait(false);

            if (response.ResponseStatus != ResponseStatus.Completed)
            {
                _probeStarted = false;
                throw new ProbeFailedException($"Probe request failed to complete for Agent Uri: {AgentUri}.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _probeStarted = false;
                throw new ProbeFailedException($"Probe request completed, but response did not indicate success. Response code: {response.StatusCode}");
            }

            if (response.Content == null || string.IsNullOrEmpty(response.Content))
            {
                _probeStarted = false;
                throw new ProbeFailedException($"Probe request completed, but response Content was empty");
            }

            try
            {
                var xmlDoc = XDocument.Load(new StringReader(response.Content));
                if (_devices.Any())
                    _devices.Clear();

                _devices.AddRange(xmlDoc.Descendants()
                   .Where(d => d.Name.LocalName == "Devices")
                   .Take(1) // needed? 
                   .SelectMany(d => d.Elements())
                   .Select(d => new Device(d)));

                // BuildDataItemDictionary();
                _dataItemsDictionary = _devices.SelectMany(d =>
                    d.DataItems.Concat(GetAllDataItems(d.Components))).ToDictionary(i => i.Id, i => i);

                 // Set to null to force rebuild on next get.
                _readOnlyDataItemsDictionary = null;

                _probeCompleted = true;
                _probeStarted = false;
                ProbeCompletedHandler();
            }
            catch (Exception ex)
            {
                _probeStarted = false;
                throw new ParseXMLException("Unexpected error while parsing Probe response. See inner exception for details", ex);
            }
        }

        /// <summary>
        /// Recursive function to get DataItems list from a Component collection
        /// </summary>
        /// <param name="components">Collection of Components</param>
        /// <returns>Collection of DataItems from passed Component collection</returns>
        private static List<DataItem> GetAllDataItems(IReadOnlyList<Component> components)
        {
            var queue = new Queue<Component>(components);
            var dataItems = new List<DataItem>();
            while (queue.Count > 0)
            {
                var component = queue.Dequeue();
                foreach (var c in component.Components)
                    queue.Enqueue(c);
                dataItems.AddRange(component.DataItems);
            }
            return dataItems;
        }

        private void StreamingTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_restClient == null)
            {
                throw new InvalidOperationException("Probe must be completed before streaming can occur");
            }
            var request = new RestRequest
            {
                Resource = "sample"
            };
            request.AddParameter("at", _lastSequence + 1);
            var response = _restClient.ExecuteAsync(request).Result;
            ParseStream(response);
        }

        /// <summary>
        /// Parses response from a current or sample request, updates changed data items and fires events
        /// </summary>
        /// <param name="response">IRestResponse from the MTConnect request</param>
        private void ParseStream(RestResponse response)
        {
            if (response == null || string.IsNullOrEmpty(response.Content))
            {
                throw new ParseXMLException("response or response.Content was null. Nothing to parse");
            }

            try
            {
                using (StringReader sr = new StringReader(response.Content))
                {
                    var xDoc = XDocument.Load(sr);

                    var header = xDoc.Descendants().First(e => e.Name.LocalName == "Header");
                    _lastSequence = Convert.ToInt64(header.GetAttribute("lastSequence"));

                    var xmlDataItems = xDoc.Descendants()
                       .Where(e => e.Attributes().Any(a => a.Name.LocalName == "dataItemId"));
                    if (xmlDataItems.Any())
                    {
                        var dataItems = xmlDataItems.Select(e => new
                        {
                            id = e.GetAttribute("dataItemId"),
                            timestamp = DateTime.Parse(e.GetAttribute("timestamp"), null, System.Globalization.DateTimeStyles.RoundtripKind),
                            value = e.Value,
                            sequence = e.GetAttribute("sequence")
                        })
                        .OrderBy(i => i.timestamp)
                        .ToList();

                        foreach (var item in dataItems)
                        {
                            var dataItem = _dataItemsDictionary[item.id];
                            var sample = new DataItemSample(item.value.ToString(), item.timestamp, item.sequence);
                            dataItem.AddSample(sample);
                        }

                        DataItemsChangedHandler();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ParseXMLException("Unexpected error while parsing response. See inner exception for details.", ex);
            }
        }
        private void DataItemsChangedHandler()
        {
            DataItemsChanged?.Invoke(this, new EventArgs());
        }
        private void ProbeCompletedHandler()
        {
            ProbeCompleted?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Disposes unmanaged resources
        /// </summary>
        public void Dispose()
        {
            _samplingTimer?.Dispose();
        }
    }
}
