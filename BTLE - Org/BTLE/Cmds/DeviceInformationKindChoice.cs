using Windows.Devices.Enumeration;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace BTLE.Cmds
    {
    public struct DeviceInformationKindChoice
        {
        public string DisplayName
            {
            get;
            set;
            }

        public DeviceInformationKind[ ] DeviceInformationKinds
            {
            get;
            set;
            }
        }
    }