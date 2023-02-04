using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MTConnectSharp
{
   public interface IMTConnectClient
	{
        event EventHandler ProbeCompleted;
        event EventHandler DataItemsChanged;
        string AgentUri { get; set; }
		TimeSpan UpdateInterval { get; set; }
		int SampleRequestSize { get; set; }
		ReadOnlyObservableCollection<Device> Devices { get; }
		ReadOnlyDictionary<string, DataItem> DataItemsDictionary { get; }
		long FirstSequence { get; }
		long LastSequence { get; }
		long NextSequence { get; }
		bool SamplingActive{ get; }
		int SamplingErrorCounter { get; }
		int MaxSamplingErrorCount { get; set; }
        Task ProbeAsync();
		Task StartStreamingAsync();
		void StopStreaming();
		Task GetCurrentStateAsync();

        Task StartSamplingAsync();
        void StopSampling();
    }
}
