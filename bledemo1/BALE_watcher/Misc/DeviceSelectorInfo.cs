using Windows.Devices.Enumeration;

namespace BALE_watcher.Misc
{
    public class DeviceSelectorInfo
    {
        public DeviceSelectorInfo()
        {
            Kind = DeviceInformationKind.Unknown;
            DeviceClassSelector = DeviceClass.All;
        }

        public string DisplayName
        {
            get;
            set;
        }

        public DeviceClass DeviceClassSelector
        {
            get;
            set;
        }

        public DeviceInformationKind Kind
        {
            get;
            set;
        }

        public string Selector
        {
            get;
            set;
        }
    }
}
