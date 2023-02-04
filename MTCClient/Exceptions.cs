using System;

namespace MTConnectSharp
{
    public class GetCurrentStateFailedException : Exception
    {
        public GetCurrentStateFailedException() : base() { }
        public GetCurrentStateFailedException(string message) : base(message) { }
        public GetCurrentStateFailedException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class StartSamplingFailedException : System.Exception
    {
        public StartSamplingFailedException() : base() { }
        public StartSamplingFailedException(string message) : base(message) { }
        public StartSamplingFailedException(string message, Exception innerException) : base(message, innerException) { }

    }
}