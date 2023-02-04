using System.Xml;
using RestSharp;
using RichardSzalay.MockHttp;
using Xunit;

namespace MTCClientTests;

public class UnitTest1
{
    [Fact]
    public async void ProbeWorks()
    {

        XmlDocument probeDoc = new XmlDocument();
        probeDoc.Load("TestData/testBedProbe.xml");

        XmlDocument currentDoc = new XmlDocument();
        currentDoc.Load("TestData/testBedCurrent.xml");
        
        var mockHttp = new MockHttpMessageHandler();

        var url = "http://dummy";

        mockHttp.When($"{url}/probe")
            .Respond("application/xml", probeDoc.InnerXml);

        mockHttp.When($"{url}/current")
            .Respond("application/xml", currentDoc.InnerXml);

        var mtconnectClient = new MTConnectClientTest(mockHttp, url);

        mtconnectClient.ProbeCompleted += (sender, e) =>
        {
            var me = sender as MTConnectClientTest;
            
            Assert.NotNull(me);
            Assert.Equal(8, me.Devices.Count);
            Assert.Equal(242, me.DataItemsDictionary.Count);
        };

        await mtconnectClient.ProbeAsync();


    }
}