using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Devices.PointOfService;
using Windows.Devices.Sensors;
using Windows.Devices.WiFiDirect;
using Windows.Media.Casting;
using Windows.Media.DialProtocol;
using Windows.Networking.Proximity;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace BTLE.Misc
    {
    public static class DeviceSelectorChoices
        {
        private static List<DeviceSelectorInfo> CommonDeviceSelectors
            {
            get
                {
                var selectors = new List<DeviceSelectorInfo>
                    {
                    new DeviceSelectorInfo
                        {
                        DisplayName = "All Device Interfaces (default)",
                        DeviceClassSelector = DeviceClass.All,
                        Selector = null
                        },
                    new DeviceSelectorInfo
                        {
                        DisplayName = "Audio Capture",
                        DeviceClassSelector = DeviceClass.AudioCapture,
                        Selector = null
                        },
                    new DeviceSelectorInfo
                        {
                        DisplayName = "Audio Render",
                        DeviceClassSelector = DeviceClass.AudioRender,
                        Selector = null
                        },
                    new DeviceSelectorInfo
                        {
                        DisplayName = "Image Scanner",
                        DeviceClassSelector = DeviceClass.ImageScanner,
                        Selector = null
                        },
                    new DeviceSelectorInfo
                        {
                        DisplayName = "Location",
                        DeviceClassSelector = DeviceClass.Location,
                        Selector = null
                        },
                    new DeviceSelectorInfo
                        {
                        DisplayName = "Portable Storage",
                        DeviceClassSelector = DeviceClass.PortableStorageDevice,
                        Selector = null
                        },
                    new DeviceSelectorInfo
                        {
                        DisplayName = "Video Capture",
                        DeviceClassSelector = DeviceClass.VideoCapture,
                        Selector = null
                        },
                    new DeviceSelectorInfo
                        {
                        DisplayName = "Human Interface (HID)",
                        Selector = HidDevice.GetDeviceSelector(0, 0)
                        },
                    new DeviceSelectorInfo
                        {
                        DisplayName = "Activity Sensor",
                        Selector = ActivitySensor.GetDeviceSelector()
                        },
                    new DeviceSelectorInfo {DisplayName = "Pedometer", Selector = Pedometer.GetDeviceSelector()},
                    new DeviceSelectorInfo {DisplayName = "Proximity", Selector = ProximityDevice.GetDeviceSelector()},
                    new DeviceSelectorInfo
                        {
                        DisplayName = "Proximity Sensor",
                        Selector = ProximitySensor.GetDeviceSelector()
                        }
                    };

                // Pre-canned device class selectors

                // A few examples of selectors built dynamically by windows runtime APIs. 

                return selectors;
                }
            }

        private static DeviceSelectorInfo Bluetooth => new DeviceSelectorInfo
            {
            DisplayName = "Bluetooth",
            Selector = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\"",
            Kind = DeviceInformationKind.AssociationEndpoint
            };

        private static DeviceSelectorInfo BluetoothUnpairedOnly => new DeviceSelectorInfo
            {
            DisplayName = "Bluetooth (unpaired)",
            Selector = BluetoothDevice.GetDeviceSelectorFromPairingState( false )
            };

        private static DeviceSelectorInfo BluetoothPairedOnly => new DeviceSelectorInfo
            {
            DisplayName = "Bluetooth (paired)",
            Selector = BluetoothDevice.GetDeviceSelectorFromPairingState( true )
            };

        private static DeviceSelectorInfo BluetoothLE => new DeviceSelectorInfo
            {
            DisplayName = "Bluetooth LE",
            Selector = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"",
            Kind = DeviceInformationKind.AssociationEndpoint
            };

        private static DeviceSelectorInfo BluetoothLEUnpairedOnly => new DeviceSelectorInfo
            {
            DisplayName = "Bluetooth LE (unpaired)",
            Selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState( false )
            };

        private static DeviceSelectorInfo BluetoothLEPairedOnly => new DeviceSelectorInfo
            {
            DisplayName = "Bluetooth LE (paired)",
            Selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState( true )
            };

        private static DeviceSelectorInfo WiFiDirect => new DeviceSelectorInfo
            {
            DisplayName = "Wi-Fi Direct",
            Selector = WiFiDirectDevice.GetDeviceSelector( WiFiDirectDeviceSelectorType.AssociationEndpoint )
            };

        private static DeviceSelectorInfo WiFiDirectPairedOnly => new DeviceSelectorInfo
            {
            DisplayName = "Wi-Fi Direct (paired)",
            Selector = WiFiDirectDevice.GetDeviceSelector()
            };

        private static DeviceSelectorInfo PointOfServicePrinter => new DeviceSelectorInfo
            {
            DisplayName = "Point of Service Printer",
            Selector = PosPrinter.GetDeviceSelector()
            };

        private static DeviceSelectorInfo VideoCasting => new DeviceSelectorInfo
            {
            DisplayName = "Video Casting",
            Selector = CastingDevice.GetDeviceSelector( CastingPlaybackTypes.Video )
            };

        private static DeviceSelectorInfo DialAllApps => new DeviceSelectorInfo
            {
            DisplayName = "DIAL (All apps)",
            Selector = DialDevice.GetDeviceSelector( "" )
            };

        // WSD and UPnP are unique in that there are currently no general WSD or UPnP APIs to USE the devices once you've discovered them. 
        // You can pair the devices using DeviceInformation.Pairing.PairAsync etc, and you can USE them with the sockets APIs. However, since
        // there's no specific API right now, there's no *.GetDeviceSelector available.  That's why we just simply build the device selector
        // ourselves and specify the correct DeviceInformationKind (AssociationEndpoint). 
        private static DeviceSelectorInfo Wsd => new DeviceSelectorInfo
            {
            DisplayName = "Web Services on Devices (WSD)",
            Selector = "System.Devices.Aep.ProtocolId:=\"{782232aa-a2f9-4993-971b-aedc551346b0}\"",
            Kind = DeviceInformationKind.AssociationEndpoint
            };

        private static DeviceSelectorInfo Upnp => new DeviceSelectorInfo
            {
            DisplayName = "UPnP",
            Selector = "System.Devices.Aep.ProtocolId:=\"{0e261de4-12f0-46e6-91ba-428607ccef64}\"",
            Kind = DeviceInformationKind.AssociationEndpoint
            };

        public static List<DeviceSelectorInfo> DevicePickerSelectors
            {
            get
                {
                // Add all selectors that can be used with the DevicePicker
                var selectors = new List<DeviceSelectorInfo>( CommonDeviceSelectors )
                    {
                    BluetoothPairedOnly,
                    BluetoothUnpairedOnly,
                    BluetoothLEPairedOnly,
                    BluetoothLEUnpairedOnly,
                    WiFiDirect,
                    PointOfServicePrinter,
                    VideoCasting,
                    DialAllApps
                    };

                return selectors;
                }
            }

        public static List<DeviceSelectorInfo> FindAllAsyncSelectors
            {
            get
                {
                // Add all selectors that are reasonable to use with FindAllAsync
                var selectors = new List<DeviceSelectorInfo>( CommonDeviceSelectors )
                    {
                    BluetoothPairedOnly,
                    BluetoothLEPairedOnly,
                    WiFiDirectPairedOnly
                    };

                return selectors;
                }
            }

        public static List<DeviceSelectorInfo> DeviceWatcherSelectors
            {
            get
                {
                // Add all selectors that can be used with the DeviceWatcher
                var selectors = new List<DeviceSelectorInfo>( CommonDeviceSelectors )
                    {
                    Bluetooth,
                    BluetoothLE,
                    WiFiDirect,
                    PointOfServicePrinter,
                    VideoCasting,
                    DialAllApps,
                    Wsd,
                    Upnp
                    };

                return selectors;
                }
            }

        public static List<DeviceSelectorInfo> BackgroundDeviceWatcherSelectors
            {
            get
                {
                // Add all selectors that can be used with the BackgroundDeviceWatcher
                var selectors = new List<DeviceSelectorInfo>( CommonDeviceSelectors )
                    {
                    BluetoothPairedOnly,
                    BluetoothLEPairedOnly,
                    WiFiDirectPairedOnly,
                    PointOfServicePrinter,
                    VideoCasting,
                    DialAllApps,
                    Wsd,
                    Upnp
                    };

                return selectors;
                }
            }

        /// <summary>
        /// Selectors for use in the pairing scenarios
        /// </summary>
        public static List<DeviceSelectorInfo> PairingSelectors
            {
            get
                {
                // Add selectors that can be used in pairing scenarios
                var selectors = new List<DeviceSelectorInfo>
                    {
                    Bluetooth,
                    BluetoothLE,
                    WiFiDirect,
                    PointOfServicePrinter,
                    VideoCasting,
                    Wsd,
                    Upnp
                    };

                return selectors;
                }
            }
        }
    }