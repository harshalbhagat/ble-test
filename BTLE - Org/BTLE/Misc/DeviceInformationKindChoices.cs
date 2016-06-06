using System.Collections.Generic;
using Windows.Devices.Enumeration;
using BTLE.Cmds;

// ReSharper disable UnusedMember.Global

namespace BTLE.Misc
    {
    public static class DeviceInformationKindChoices
        {
        public static List<DeviceInformationKindChoice> Choices
            {
            get
                {
                var choices = new List<DeviceInformationKindChoice>
                    {
                    new DeviceInformationKindChoice
                        {
                        DisplayName = "DeviceContainer",
                        DeviceInformationKinds = new[] {DeviceInformationKind.DeviceContainer}
                        },
                    new DeviceInformationKindChoice
                        {
                        DisplayName = "Device",
                        DeviceInformationKinds = new[] {DeviceInformationKind.Device}
                        },
                    new DeviceInformationKindChoice
                        {
                        DisplayName = "DeviceInterface (Default)",
                        DeviceInformationKinds = new[] {DeviceInformationKind.DeviceInterface}
                        },
                    new DeviceInformationKindChoice
                        {
                        DisplayName = "DeviceInterfaceClass",
                        DeviceInformationKinds = new[] {DeviceInformationKind.DeviceInterfaceClass}
                        },
                    new DeviceInformationKindChoice
                        {
                        DisplayName = "AssociationEndpointContainer",
                        DeviceInformationKinds = new[] {DeviceInformationKind.AssociationEndpointContainer}
                        },
                    new DeviceInformationKindChoice
                        {
                        DisplayName = "AssociationEndpoint",
                        DeviceInformationKinds = new[] {DeviceInformationKind.AssociationEndpoint}
                        },
                    new DeviceInformationKindChoice
                        {
                        DisplayName = "AssociationEndpointService",
                        DeviceInformationKinds = new[] {DeviceInformationKind.AssociationEndpointService}
                        },
                    new DeviceInformationKindChoice
                        {
                        DisplayName = "AssociationEndpointService and DeviceInterface",
                        DeviceInformationKinds =
                        new[] {DeviceInformationKind.AssociationEndpointService, DeviceInformationKind.DeviceInterface}
                        }
                    };


                return choices;
                }
            }
        }
    }