using System;
using System.Xml.Linq;
using MTConnectSharp;
using Xunit;

namespace MTCClientTests;

public class DataItemTests
{

    [Fact]
    public void DataItem()
    {
        XElement xmlTree1 = new XElement("Root",
            new XAttribute("id", "myId"),
            new XAttribute("name", "myName"),
            new XAttribute("category", "aCategory"),
            new XAttribute("type", "mainType"),
            new XAttribute("subType", "SubType"),
            new XAttribute("units", "inches"),
            new XAttribute("nativeUnits", "americanInches")
        );

        var dataItem = new DataItem(xmlTree1);

        Assert.Equal("myId", dataItem.Id);
        Assert.Equal("myName", dataItem.Name);
        Assert.Equal("aCategory", dataItem.Category);
        Assert.Equal("mainType", dataItem.Type);
        Assert.Equal("SubType", dataItem.SubType);
        Assert.Equal("inches", dataItem.Units);
        Assert.Equal("americanInches", dataItem.NativeUnits);

        Assert.Empty(dataItem.SampleHistory);

        Assert.Throws<InvalidOperationException>(() => dataItem.CurrentSample.Value);

        dataItem.AddSample(new DataItemSample("42", System.DateTime.UtcNow, "1"));

        Assert.Single(dataItem.SampleHistory);
        Assert.Equal("42", dataItem.SampleHistory[0].Value);
        Assert.Equal("1", dataItem.SampleHistory[0].Sequence);

        Assert.Equal("42", dataItem.CurrentSample.Value);
        Assert.Equal("1", dataItem.CurrentSample.Sequence);
        Assert.False(dataItem.CurrentSample.Processed);
        dataItem.CurrentSample.Processed = true;
        Assert.True(dataItem.CurrentSample.Processed);
        
        Assert.Throws<InvalidOperationException>(() => dataItem.PreviousSample.Value);


        dataItem.AddSample(new DataItemSample("123", System.DateTime.UtcNow, "2"));

        Assert.Equal(2, dataItem.SampleHistory.Count);
        Assert.Equal("123", dataItem.SampleHistory[1].Value);
        Assert.Equal("2", dataItem.SampleHistory[1].Sequence);

        Assert.Equal("123", dataItem.CurrentSample.Value);
        Assert.Equal("2", dataItem.CurrentSample.Sequence);


        Assert.Equal("42", dataItem.PreviousSample.Value);
        Assert.True(dataItem.PreviousSample.Processed);
        Assert.False(dataItem.CurrentSample.Processed);

        



    }
}