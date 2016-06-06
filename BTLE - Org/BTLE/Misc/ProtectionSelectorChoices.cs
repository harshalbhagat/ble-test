using System.Collections.Generic;
using Windows.Devices.Enumeration;

// ReSharper disable UnusedMember.Global

namespace BTLE.Misc
    {
    // ReSharper disable once UnusedMember.Global
    public static class ProtectionSelectorChoices
        {
        // ReSharper disable once UnusedMember.Global
        public static List<ProtectionLevelSelectorInfo> Selectors
            {
            get
                {
                var selectors = new List<ProtectionLevelSelectorInfo>
                    {
                    new ProtectionLevelSelectorInfo
                        {
                        DisplayName = "Default",
                        ProtectionLevel = DevicePairingProtectionLevel.Default
                        },
                    new ProtectionLevelSelectorInfo
                        {
                        DisplayName = "None",
                        ProtectionLevel = DevicePairingProtectionLevel.None
                        },
                    new ProtectionLevelSelectorInfo
                        {
                        DisplayName = "Encryption",
                        ProtectionLevel = DevicePairingProtectionLevel.Encryption
                        },
                    new ProtectionLevelSelectorInfo
                        {
                        DisplayName = "Encryption and authentication",
                        ProtectionLevel = DevicePairingProtectionLevel.EncryptionAndAuthentication
                        }
                    };

                return selectors;
                }
            }
        }
    }