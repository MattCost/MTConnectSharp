using System.Net.Http;
using MTConnectSharp;
using RestSharp;

namespace MTCClientTests
{
    public class MTConnectClientTest : MTConnectClient
    {
        private HttpMessageHandler _handler;
        private string _url;
        public MTConnectClientTest(HttpMessageHandler handler, string url)
        {
            _handler = handler;
            _url = url;
            AgentUri = _url;
        }
        
        protected override RestClient CreateRestClient()
        {
            var options = new RestClientOptions(_url)
            {
                ConfigureMessageHandler = _ => _handler
            };

            return new RestClient(options);
        }

    }
}