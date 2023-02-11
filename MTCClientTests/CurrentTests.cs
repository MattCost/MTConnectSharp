
using System;
using System.Xml;
using MTConnectSharp;
using RestSharp;
using RichardSzalay.MockHttp;
using Xunit;

namespace MTCClientTests;

public class CurrentTests : IClassFixture<MTCClientFixture>
{
    private MTCClientFixture _fixture;
    public CurrentTests(MTCClientFixture fixture)
    {
        _fixture = fixture;
    }
    [Fact]
    public async void CurrentWorks()
    {
        var client = _fixture.ClientBuilder.Build(MTCClientFixture.ProbeAndCurrentWorkUrl);

        client.ProbeCompleted += (sender, e) =>
        {
            var me = sender as MTConnectUnitTestClient;
            
            Assert.NotNull(me);
            Assert.Equal(8, me.Devices.Count);
            Assert.Equal(242, me.DataItemsDictionary.Count);
        };

        await client.ProbeAsync();
        await client.GetCurrentStateAsync();
        Assert.NotEmpty(client.DataItemsDictionary);
        Assert.True(client.DataItemsDictionary.ContainsKey("GFAgie01-dtop_1"));
        Assert.Equal("Hello World", client.DataItemsDictionary["GFAgie01-dtop_1"].CurrentSample.Value);
    }

}