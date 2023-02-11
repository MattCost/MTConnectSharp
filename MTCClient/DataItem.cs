using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace MTConnectSharp
{
    /// <summary>
    /// Represents a DataItem in the MTConnect probe response
    /// </summary>
    public class DataItem : MTCItemBase, IDataItem
    {
        /// <summary>
        /// DataItemSample collection as a Queue for correct circular buffer behavior
        /// </summary>
        private ObservableCollection<DataItemSample> _dataItemSamples = new ObservableCollection<DataItemSample>();

        /// <summary>
        /// Value of the category attribute
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Value of the type attribute
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Value of the subType attribute
        /// </summary>
        public string SubType { get; set; }

        /// <summary>
        /// Value of the units attribute
        /// </summary>
        public string Units { get; set; }

        /// <summary>
        /// Value of the nativeUnits attribute
        /// </summary>
        public string NativeUnits { get; set; }

        /// <summary>
        /// The maximum number of samples to keep in the value buffer
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// The value immediately before the value
        /// </summary>
        public DataItemSample PreviousSample
        {
            get
            {
                if (_dataItemSamples.Count >= 2)
                    return _dataItemSamples.TakeLast(2).First();
                throw new InvalidOperationException("SampleHistory only has 1 sample.");
            }
        }

        /// <summary>
        /// The current value of this DataItem
        /// </summary>
        public DataItemSample CurrentSample
        {
            get
            {
                if (_dataItemSamples.Any())
                    return _dataItemSamples.Last();
                throw new InvalidOperationException("SampleHistory is empty");
            }
        }

        /// <summary>
        /// Every value in the buffer
        /// </summary>
        public ReadOnlyObservableCollection<DataItemSample> SampleHistory
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates a new DataItem
        /// </summary>
        /// <param name="xmlDataItem">The XElement which defines the DataItem</param>
        internal DataItem(XElement xmlDataItem)
        {
            SampleHistory = new ReadOnlyObservableCollection<DataItemSample>(_dataItemSamples);

            BufferSize = 100;
            Id = xmlDataItem.GetAttribute("id");
            Name = xmlDataItem.GetAttribute("name");
            Category = xmlDataItem.GetAttribute("category");
            Type = xmlDataItem.GetAttribute("type");
            SubType = xmlDataItem.GetAttribute("subType");
            Units = xmlDataItem.GetAttribute("units");
            NativeUnits = xmlDataItem.GetAttribute("nativeUnits");
        }

        /// <summary>
        /// Adds a sample to the value buffer and removes the oldest value if the buffer is full
        /// </summary>
        /// <param name="newSample">The new sample to add</param>
        internal void AddSample(DataItemSample newSample)
        {
            _dataItemSamples.Add(newSample);
            _dataItemSamples.RemoveRange(0, Math.Max(0, _dataItemSamples.Count - BufferSize));
        }
    }
}
