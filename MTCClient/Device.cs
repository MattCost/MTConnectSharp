﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace MTConnectSharp
{
    /// <summary>
    /// Represents a device in the MTConnect probe response
    /// </summary>
    public class Device : MTCItemBase, IDevice
    {
        /// <summary>
        /// Description of the device
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Manufacturer of the device
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// Serial number of the device
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// The DataItems which are direct children of the device
        /// </summary>
        private ObservableCollection<DataItem> _dataItems = new ObservableCollection<DataItem>();

        /// <summary>
        /// The components which are direct children of the device
        /// </summary>
        private ObservableCollection<Component> _components = new ObservableCollection<Component>();

        /// <summary>
        /// Array of the DataItems collection for COM Interop
        /// </summary>
        public ReadOnlyObservableCollection<DataItem> DataItems
        {
            get;
            private set;
        }

        /// <summary>
        /// Array of the Components collection for COM Interop
        /// </summary>
        public ReadOnlyObservableCollection<Component> Components
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates a new device from an MTConnect XML device node
        /// </summary>
        /// <param name="xElem">The MTConnect XML node which defines the device</param>
        internal Device(XElement? xElem = null)
        {
            DataItems = new ReadOnlyObservableCollection<DataItem>(_dataItems);
            Components = new ReadOnlyObservableCollection<Component>(_components);

            if (xElem?.Name.LocalName == "Device")
            {
                // Populate basic fields
                Id = xElem.GetAttribute("id");
                Name = xElem.GetAttribute("name");

                var descXml = xElem.Descendants().First(x => x.Name.LocalName == "Description");
                Description = descXml.Value ?? string.Empty;
                Manufacturer = descXml.GetAttribute("manufacturer");
                SerialNumber = descXml.GetAttribute("serialNumber");

                _dataItems.AddRange(xElem.GetDataItems());
                _components.AddRange(xElem.GetComponents());
            }
        }
    }
}
