using System;

namespace MTConnectSharp
{
   /// <summary>
   /// A single value from a current or sample response
   /// </summary>
   public class DataItemSample : MTConnectSharp.IDataItemSample
	{
		/// <summary>
		/// The value of the sample
		/// </summary>
		public string Value { get; private set; }

		/// <summary>
		/// The timestamp of the sample
		/// </summary>
		public DateTime TimeStamp { get; private set; }

        /// <summary>
        /// Flag for client application to use to track if sample has been processed.
        /// </summary>
		public bool Processed { get; set; } = false;

		/// <summary>
        /// Sequence number from the MTConnect Stream
        /// </summary>
		public string Sequence { get; private set; }

		/// <summary>
		/// Creates a new sample
		/// </summary>
		/// <param name="value">Value of the sample</param>
		/// <param name="timestamp">Timestamp of the sample</param>
		internal DataItemSample(string value, DateTime timestamp, string sequence)
		{
			TimeStamp = timestamp;
			Value = value;
            Sequence = sequence;
        }

		/// <summary>
		/// Returns the Value
		/// </summary>
		public override string ToString()
		{
			return Value;
		}
	}

}

