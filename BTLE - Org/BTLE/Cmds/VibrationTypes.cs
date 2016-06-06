using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace BTLE.Cmds
    {
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct VibrationPatternRequest
        {
        public VibrationPatternRequest( int vibrationPatternSlotMax = VibrationTypes.VIBRATION_PATTERN_SLOT_MAX ) : this()
            {
            IdCount = (byte)vibrationPatternSlotMax;
            Id = new byte[ vibrationPatternSlotMax ];
            }

        public byte IdCount;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = VibrationTypes.VIBRATION_PATTERN_SLOT_MAX )]
        public byte [] Id;
        }

    public static class VibrationTypes
        {
        public const int VIBRATION_PATTERN_SLOT_MAX = 8;
        }
    }
