using System.Runtime.InteropServices;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace BTLE.Cmds
    {
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BatteryReading
        {
        public BatteryReading( byte flags, byte percentage, ushort voltage ) : this()
            {
            Flags = flags;
            Percentage = percentage;
            Voltage = voltage;
            }

        public byte Flags;
        public byte Percentage;
        public ushort Voltage;
        }
    }
