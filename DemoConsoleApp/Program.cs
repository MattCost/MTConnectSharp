using System.Threading.Tasks;
using MTConnectSharp;
internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        IMTConnectClient client = new MTConnectClient()
        {
            AgentUri = "http://agent.mtconnect.org",
            // AgentUri = "http://mtconnect.mazakcorp.com:5610",
            UpdateInterval = TimeSpan.FromSeconds(.5)
        };

        client.ProbeCompleted += async (sender, info) =>
        {
            Console.WriteLine("Probe Complete!");
            var itemsDict = client.DataItemsDictionary;

            Console.WriteLine($"We have {itemsDict.Keys.Count} items available");

            Console.WriteLine("Starting Sampling");
            await client.StartSamplingAsync();
        };

        client.DataItemsChanged += (sender, info) =>
        {
            Console.WriteLine("New Samples!");
        };

        await client.ProbeAsync();

        await Task.Delay(TimeSpan.FromSeconds(30));

        Console.WriteLine("Goodbye");
    }
}