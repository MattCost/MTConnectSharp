using System;

namespace MTConnectSharp
{
    public class ProbeFailedException : Exception
    {
        public ProbeFailedException() : base() { }
        public ProbeFailedException(string message) : base(message) { }
        public ProbeFailedException(string message, Exception innerException) : base(message, innerException) { }
    }    
    public class GetCurrentStateFailedException : Exception
    {
        public GetCurrentStateFailedException() : base() { }
        public GetCurrentStateFailedException(string message) : base(message) { }
        public GetCurrentStateFailedException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class StartSamplingFailedException : Exception
    {
        public StartSamplingFailedException() : base() { }
        public StartSamplingFailedException(string message) : base(message) { }
        public StartSamplingFailedException(string message, Exception innerException) : base(message, innerException) { }

    }
}