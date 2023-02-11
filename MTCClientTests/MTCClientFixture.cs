using System;
using System.Collections.Generic;
using System.Xml;
using MTConnectSharp;
using RichardSzalay.MockHttp;

namespace MTCClientTests
{
    public class MTCClientFixture
    {
        public MTConnectClientBuilder ClientBuilder = new MTConnectClientBuilder();
        public MTCClientFixture()
        {
            XmlDocument probeDoc = new XmlDocument();
            probeDoc.Load("TestData/testBedProbe.xml");

            XmlDocument currentDoc = new XmlDocument();
            currentDoc.Load("TestData/testBedCurrent.xml");

            // Probe Works
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When($"{ProbeAndCurrentWorkUrl}/probe")
                .Respond("application/xml", probeDoc.InnerXml);

            mockHttp.When($"{ProbeAndCurrentWorkUrl}/current")
                .Respond("application/xml", currentDoc.InnerXml);


            ClientBuilder.AddHttpMessageHandler(ProbeAndCurrentWorkUrl, mockHttp);

            // Probe Throws
            mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{ProbeThrowsUrl}/probe")
                .Throw(new Exception("probe failed!"));

            ClientBuilder.AddHttpMessageHandler(ProbeThrowsUrl, mockHttp);

            //Probe Not Found
            mockHttp = new MockHttpMessageHandler();
            // if you dont setup a mock, the client returns not found
            // mockHttp.When($"{ProbeNotFoundUrl}/probe")
            //     .Respond("application/xml", probeDoc.InnerXml);
            ClientBuilder.AddHttpMessageHandler(ProbeNotFoundUrl, mockHttp);

            //Probe Non Success Code
            mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{ProbeNonSuccessUrl}/probe")
                .Respond(System.Net.HttpStatusCode.BadGateway);
            ClientBuilder.AddHttpMessageHandler(ProbeNonSuccessUrl, mockHttp);

            mockHttp = new MockHttpMessageHandler();
            mockHttp.When($"{ProbeEmptyContentUrl}/probe")
                .Respond("application/xml", string.Empty);
            ClientBuilder.AddHttpMessageHandler(ProbeEmptyContentUrl, mockHttp);
        }

        public const string ProbeAndCurrentWorkUrl = "http://probe-and-current-work";
        public const string ProbeThrowsUrl = "http://probe-throws";
        public const string ProbeNotFoundUrl = "http://probe-not-found";
        public const string ProbeNonSuccessUrl = "http://probe-400";
        public const string ProbeEmptyContentUrl = "http://probe-empty-content";
    }
}