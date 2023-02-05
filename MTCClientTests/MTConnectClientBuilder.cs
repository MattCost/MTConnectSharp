using System.Collections.Generic;
using System.Net.Http;
using MTConnectSharp;

namespace MTCClientTests;

public class MTConnectClientBuilder
{
    Dictionary<string, HttpMessageHandler> _handlers = new Dictionary<string, HttpMessageHandler>();
    public MTConnectClientBuilder()
    {
    }
    public void AddHttpMessageHandler(string url, HttpMessageHandler handler)
    {
        _handlers.Add(url, handler);
    }

    public MTConnectClient Build(string uri)
    {
        if (_handlers.ContainsKey(uri))
        {
            return new MTConnectUnitTestClient(_handlers[uri], uri);
        }
        else
        {
            return new MTConnectClient()
            {
                AgentUri = uri
            };
        }
    }

}