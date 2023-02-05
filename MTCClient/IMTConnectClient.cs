using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MTConnectSharp
{
   public interface IMTConnectClient
	{
		string AgentUri { get; set; }
		Task ProbeAsync();
		Task GetCurrentStateAsync();
        Task StartSamplingAsync();
        void StopSampling();
        Task StartStreamingAsync();
		void StopStreaming();
		ReadOnlyObservableCollection<Device> Devices { get; }
		TimeSpan UpdateInterval { get; set; }
	}
}
