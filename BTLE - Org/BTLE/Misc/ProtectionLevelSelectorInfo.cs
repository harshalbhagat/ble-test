using Windows.Devices.Enumeration;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace BTLE.Misc
    {
    public struct ProtectionLevelSelectorInfo
        {
        public string DisplayName
            {
            get;
            set;
            }

        public DevicePairingProtectionLevel ProtectionLevel
            {
            get;
            set;
            }
        }
    }