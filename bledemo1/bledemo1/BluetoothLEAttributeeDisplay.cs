using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace bledemo1
{
    internal class BluetoothLEAttributeeDisplay
    {
        private readonly AttributeType attributeType;

        public readonly GattDeviceService _service;
        public readonly GattCharacteristic _characteristic;
        public GattDescriptor _descriptor;

        public BluetoothLEAttributeeDisplay(GattDeviceService service)
        {
            _service = service;
            attributeType = AttributeType.Service;
        }

        public BluetoothLEAttributeeDisplay(GattCharacteristic characteristic)
        {
            _characteristic = characteristic;
            attributeType = AttributeType.Characteristic;
        }

        public string Name
        {
            get
            {
                switch (attributeType)
                {
                    case AttributeType.Service:
                        {
                            if (IsSigDefinedUuid(_service.Uuid))
                            {
                                GattNativeServiceUuid serviceName;
                                if (Enum.TryParse(Utility.ConvertUuidToShortId(_service.Uuid).ToString(), out serviceName))
                                {
                                    return serviceName.ToString();
                                }
                            }
                            else
                            {
                                return "Custom Service: " + _service.Uuid;
                            }
                            break;
                        }
                    case AttributeType.Characteristic:
                        {
                            if (IsSigDefinedUuid(_characteristic.Uuid))
                            {
                                GattNativeCharacteristicUuid characteristicName;
                                if (Enum.TryParse(Utility.ConvertUuidToShortId(_characteristic.Uuid).ToString(), out characteristicName))
                                {
                                    return characteristicName.ToString();
                                }
                            }
                            else
                            {
                                return "Custom Characteristic: " + _characteristic.Uuid;
                            }
                            break;
                        }
                }
                return "Invalid";
            }
        }


        public AttributeType AttributeDisplayType => attributeType;

        public static bool IsSigDefinedUuid(Guid uuid)
        {
            var bluetoothBaseUuid = new Guid("00000000-0000-1000-8000-00805F9B34FB");

            byte[] bytes = uuid.ToByteArray();
            // Zero out the first and second bytes
            // Note how each byte gets flipped in a section - 1234 becomes 34 12
            // Example Guid: 35918bc9-1234-40ea-9779-889d79b753f0
            //                   ^^^^
            // bytes output = C9 8B 91 35 34 12 EA 40 97 79 88 9D 79 B7 53 F0
            //                ^^ ^^
            bytes[0] = 0;
            bytes[1] = 0;
            Guid baseUuid = new Guid(bytes);
            return (baseUuid == bluetoothBaseUuid);
        }
    }

   
}