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
    }
}