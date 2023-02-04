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
        public event EventHandler ProbeCompleted;

        /// <summary>
        /// A Current or Sample response has been parsed, and new DataItems are available.
        /// </summary>
        public event EventHandler DataItemsChanged;
        
        /// <summary>
        /// The base uri of the agent
        /// </summary>
        public string AgentUri
        {
            get { return _agentUri; }
            set
            {
                if(string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }
                if(string.IsNullOrEmpty(_agentUri) || (!string.Equals(_agentUri, value)))
                {
                    _agentUri = value;
                    CreateRestClient();
                }
            }
        }

        private string _agentUri;

        /// <summary>
        /// Time between sample queries when issuing sample requests. The default is 2 seconds.
        /// </summary>
        public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// How many sequence numbers to request from the MTConnect stream. Default is 100.
        /// </summary>
        public int SampleRequestSize {get;set;} = 100;

        /// <summary>
        /// Devices on the connected agent
        /// </summary>
        public ReadOnlyObservableCollection<Device> Devices
        {
            get
            {
                return _readOnlyDevices ??= new ReadOnlyObservableCollection<Device>(_devices);
            }
        }

        private ReadOnlyObservableCollection<Device> _readOnlyDevices = null;

        private ObservableCollection<Device> _devices = new ObservableCollection<Device>();

        /// <summary>
        /// Dictionary Reference to all data items by id for better performance when streaming
        /// </summary>
        private Dictionary<string, DataItem> _dataItemsDictionary = new Dictionary<string, DataItem>();

        private ReadOnlyDictionary<string, DataItem> _readOnlyDataItemsDictionary = null;

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
        private RestClient _restClient = null;

        private void CreateRestClient()
        {
            var options = new RestClientOptions(_agentUri) {
                ThrowOnAnyError = false,
                MaxTimeout = 5000
            };
            _restClient = new RestClient(options);
        }
        
        /// <summary>
        /// Timer fires when time to query for new samples
        /// </summary>
        private Timer _samplingTimer;

        /// <summary>
        /// First sequence number available from the MTConnect stream
        /// </summary>
        public long FirstSequence { get; private set; }

        /// <summary>
        /// Last sequence number available from the MTConnect stream
        /// </summary>
        public long LastSequence { get; private set; }

        /// <summary>
        /// Next sequence number to read, or that will be generated?
        /// </summary>
        public long NextSequence { get; private set; }

        /// <summary>
        /// Indicates if the client is actively sampling from an MTConnect stream. True if connected to mtconnect agent, false otherwise.
        /// </summary>
        public bool SamplingActive { get; private set; } = false;
        /// <summary>
        /// How many sample requests have errored out, in a row. Resets to 0 after a successful request.
        /// </summary>
        public int SamplingErrorCounter { get; private set; }

        /// <summary>
        /// How many errors to tolerate, before stopping sampling.
        /// </summary>
        public int MaxSamplingErrorCount { get; set; } = 3;

        private bool _probeStarted = false;
        private bool _probeCompleted = false;

        /// <summary>
        /// Initializes a new Client 
        /// </summary>
        public MTConnectClient()
        {
            // UpdateInterval = TimeSpan.FromMilliseconds(2000);

            // _devices = new ObservableCollection<Device>();
            // Devices = new ReadOnlyObservableCollection<Device>(_devices);
        }

        /// <summary>
        /// Starts streaming from the MTConnect agent in real time. Not yet implemented.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public Task StartStreamingAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stops streaming from the MTConnect agent if active. Not yet implemented.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void StopStreaming()
        {
            throw new NotImplementedException();

        }
        /// <summary>
        /// Starts sample polling and updating DataItem values as they change
        /// </summary>
        public async Task StartSamplingAsync()
        {
            if (SamplingActive)
            {
                return;
            }

            // Timer.enabled replaced with SamplingActive bool			
            // if (_samplingTimer?.Enabled == true)
            // {
            //     return;
            // }

            try
            {
                await GetCurrentStateAsync();
            }
            catch (GetCurrentStateFailedException ex)
            {
                //todo logger
                throw new StartSamplingFailedException("Unable to start sampling, current request failed. See inner exception for details", ex);
            }
            catch (Exception ex)
            {
                //todo logger
                throw new StartSamplingFailedException("Unable to start sampling. See inner exception for details", ex);
            }


            _samplingTimer = new Timer(UpdateInterval.TotalMilliseconds);
            _samplingTimer.Elapsed += SamplingTimerElapsed;
            _samplingTimer.AutoReset = false;
            SamplingActive = true;
            SampleRequest(); // Fire off sample request right now, instead of waiting for the timer to elapse
        }

        /// <summary>
        /// Stops sample polling
        /// </summary>
        public void StopSampling()
        {
            if (SamplingActive)
            {
                _samplingTimer.Stop();
                SamplingActive = false;
            }
        }

        /// <summary>
        /// Gets current response and updates DataItems
        /// </summary>
        public async Task GetCurrentStateAsync()
        {
            if (!_probeCompleted)
            {
                throw new InvalidOperationException("Cannot get DataItem values. Agent has not been probed yet.");
            }

            var request = new RestRequest
            {
                Resource = "current",
        };

            //TODO add support for mtconnect json
            request.AddHeader("Accept", "application/accepts=xml");

            var response = await _restClient.ExecuteAsync(request).ConfigureAwait(false);

            //TODO figure out if new RestSharp has better response error handling
            switch(response.ResponseStatus)
            {
                case ResponseStatus.Completed:
                    ParseStream(response);
                    break;
                
                case ResponseStatus.None:
                case ResponseStatus.Error:
                case ResponseStatus.TimedOut:
                case ResponseStatus.Aborted:
                default:
                    throw new GetCurrentStateFailedException();
            }
        }

        /// <summary>
        /// Gets probe response from the agent and populates the devices collection
        /// </summary>
        public async Task ProbeAsync()
        {

            if(_restClient == null)
            {
                throw new InvalidOperationException("Must set AgentUri");
            }

            if (_probeStarted && !_probeCompleted)
            {
                throw new InvalidOperationException("Cannot start a new Probe when one is still running.");
            }

            // _restClient = new RestClient(AgentUri);
            
            var request = new RestRequest
            {
                Resource = "probe",
                Method = Method.Get
            };
            // request.Timeout = 100;

            //TODO add support for mtconnect json
            request.AddHeader("Accept", "application/accepts=xml");

            try
            {
                _probeStarted = true;
                var response = await _restClient.ExecuteAsync(request).ConfigureAwait(false);
                switch(response.ResponseStatus)
                {
                    case ResponseStatus.Completed:
                        ParseProbeResponse(response);
                        break;
                    
                    default:
                        _probeStarted = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                _probeStarted = false;
                throw new Exception("Probe request failed.\nAgent Uri: " + AgentUri, ex);
            }
        }

        /// <summary>
        /// Parses IRestResponse from a probe command into a Device collection
        /// </summary>
        /// <param name="response">An IRestResponse from a probe command</param>
        private void ParseProbeResponse(RestResponse response)
        {
            var xdoc = XDocument.Load(new StringReader(response.Content));
            if (_devices.Any())
                _devices.Clear();

            _devices.AddRange(xdoc.Descendants()
               .Where(d => d.Name.LocalName == "Devices")
               .Take(1) // needed? 
               .SelectMany(d => d.Elements())
               .Select(d => new Device(d)));

            BuildDataItemDictionary();

            _probeCompleted = true;
            _probeStarted = false;
            ProbeCompletedHandler();
        }

        /// <summary>
        /// Loads DataItemRefList with all data items from all devices
        /// </summary>
        private void BuildDataItemDictionary()
        {
            _dataItemsDictionary = _devices.SelectMany(d =>
               d.DataItems.Concat(GetAllDataItems(d.Components))
            ).ToDictionary(i => i.Id, i => i);
            
            // Set to null to force rebuild on next get.
            _readOnlyDataItemsDictionary = null;

            DataItemsChangedHandler();
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

        private void SamplingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            SampleRequest();
        }
        private void SampleRequest()
        {
            var request = new RestRequest
            {
                Resource = "sample"
            };
            //not in mtconnect spec
            // request.AddParameter("at", _lastSequence + 1);
            
            //TODO add support for mtconnect json
            request.AddHeader("Accept", "application/accepts=xml");

            request.AddParameter("from", NextSequence);
            request.AddParameter("count", SampleRequestSize);

            // _restClient.ExecuteAsync(request, r => ParseStream(r));
            var response = _restClient.ExecuteAsync(request).Result;
            switch (response.ResponseStatus)
            {
                case ResponseStatus.Completed:
                    ParseStream(response);
                    SamplingErrorCounter = 0;
                    break;

                case ResponseStatus.TimedOut:
                case ResponseStatus.Error:
                case ResponseStatus.Aborted:
                case ResponseStatus.None:
                default:
                    if(++SamplingErrorCounter >= MaxSamplingErrorCount)
                    {
                        SamplingActive = false;
                    }
                    break;
            }
            
            if(SamplingActive)
            {
                _samplingTimer.Start();
            }
        }

        /// <summary>
        /// Parses response from a current or sample request, updates changed data items and fires events
        /// </summary>
        /// <param name="response">IRestResponse from the MTConnect request</param>
        private void ParseStream(RestResponse response)
        {
            //TODO test if newest RestSharp fixed bugs that returned null/empty response without an error code response
            if(response == null)
                return;

            if(string.IsNullOrEmpty(response.Content))
                return;


            using (StringReader sr = new StringReader(response.Content))
            {
                var xdoc = XDocument.Load(sr);

                FirstSequence = Convert.ToInt64(xdoc.Descendants().First(e => e.Name.LocalName == "Header").Attribute("firstSequence").Value);
                LastSequence = Convert.ToInt64(xdoc.Descendants().First(e => e.Name.LocalName == "Header").Attribute("lastSequence").Value);
                NextSequence = Convert.ToInt64(xdoc.Descendants().First(e => e.Name.LocalName == "Header").Attribute("nextSequence").Value);

                var xmlDataItems = xdoc.Descendants()
                   .Where(e => e.Attributes().Any(a => a.Name.LocalName == "dataItemId"));
                if (xmlDataItems.Any())
                {
                    var dataItems = xmlDataItems.Select(e => new
                    {
                        id = e.Attribute("dataItemId").Value,
                        timestamp = DateTime.Parse(e.Attribute("timestamp").Value, null,
                          System.Globalization.DateTimeStyles.RoundtripKind),
                        value = e.Value
                    })
                    .OrderBy(i => i.timestamp)
                    .ToList();

                    foreach (var item in dataItems)
                    {
                        var dataItem = _dataItemsDictionary[item.id];
                        var sample = new DataItemSample(item.value.ToString(), item.timestamp);
                        dataItem.AddSample(sample);
                    }

                    DataItemsChangedHandler(); //we have new samples somewhere
                }
            }
        }

        private void ProbeCompletedHandler()
        {
            ProbeCompleted?.Invoke(this, new EventArgs());
        }


        private void DataItemsChangedHandler()
        {
            DataItemsChanged?.Invoke(this, new EventArgs());
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
