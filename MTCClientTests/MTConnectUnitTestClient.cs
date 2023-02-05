using System.Net.Http;
using MTConnectSharp;
using RestSharp;

namespace MTCClientTests
{
    /// <summary>
    /// A unit test version of the MTConnectClient. Mock Http Handlers are passed on to the RestClient used internally by MTConnectClient.
    /// </summary>
    /// <remarks>
    /// This pattern can also be used to unit test code that uses MTConnectClient. Copy this UnitTestClient, along with ClientBuilder. 
    /// Use the ClientBuilder in code to get an MTConnectClient. In unit test setup, add custom handlers to the Client Builder with a known url, then use that url in the unit test.
    /// See usage of this class for examples.
    /// </remarks>
    public class MTConnectUnitTestClient : MTConnectClient
    {
        private HttpMessageHandler _handler;
        private string _uri;
        /// <summary>
        /// Create an MTConnectClient, with custom HttpMessageHandling, for a given uri
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="uri"></param>
        public MTConnectUnitTestClient(HttpMessageHandler handler, string uri)
        {
            _handler = handler;
            _uri = uri;
            AgentUri = _uri;
        }
        
        /// <summary>
        /// Override the default CreateRestClient method to provide any custom options to the RestClient.
        /// </summary>
        /// <returns>A RestClient that will be used for all operations by the MTConnectClient</returns>
        protected override RestClient CreateRestClient()
        {
            var options = new RestClientOptions(_uri)
            {
                ConfigureMessageHandler = _ => _handler
            };

            return new RestClient(options);
        }

    }
}