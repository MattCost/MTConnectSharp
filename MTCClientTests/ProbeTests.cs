using System.Xml;
using MTConnectSharp;
using RestSharp;
using RichardSzalay.MockHttp;
using Xunit;

namespace MTCClientTests;

public class ProbeTests : IClassFixture<MTCClientFixture>
{
    private MTCClientFixture _fixture;
    public ProbeTests(MTCClientFixture fixture)
    {
        _fixture = fixture;
    }
    [Fact]
    public async void ProbeWorks()
    {
        var client = _fixture.ClientBuilder.Build(MTCClientFixture.ProbeWorksUrl);

        client.ProbeCompleted += (sender, e) =>
        {
            var me = sender as MTConnectUnitTestClient;
            
            Assert.NotNull(me);
            Assert.Equal(8, me.Devices.Count);
            // Assert.Equal(242, me.DataItemsDictionary.Count);
        };

        await client.ProbeAsync();

    }

    [Theory]
    [InlineData(MTCClientFixture.ProbeThrowsUrl)]
    [InlineData(MTCClientFixture.ProbeNotFoundUrl)]
    [InlineData(MTCClientFixture.ProbeNonSuccessUrl)]
    [InlineData(MTCClientFixture.ProbeEmptyContentUrl)]
    public async void ProbeThrows(string url)
    {
        var client = _fixture.ClientBuilder.Build(url);

        await Assert.ThrowsAsync<ProbeFailedException>(async () => await client.ProbeAsync());
    }
}