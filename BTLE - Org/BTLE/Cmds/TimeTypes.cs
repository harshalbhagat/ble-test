using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace BTLE.Cmds
    {
    // Defines the timestamp of a record. Because the band's calendar is not 
    // guaranteed to be up to date, record time is defined by the time since 
    // an epoch event, which may be boot, a time sync event or other synchronization points.
    [StructLayout( LayoutKind.Explicit, Pack = 1 )]
    public struct EpochTime
        {
        static EpochTime()
            {
            Max = new EpochTime
                {
                EpochId = ushort.MaxValue,
                SecondsSinceEpoch = short.MaxValue
                };
            }

        // Count of seconds since this beginning of the epoch. A negative value
        // indicates a time **before** the epoch started.
        // 2 bytes
        [FieldOffset(0)]
        public short SecondsSinceEpoch;

        // A full timestamp is recovered by looking up the epoch's
        // start time and adding SecondsSinceEpoch.        
        // 2 bytes
        [FieldOffset(2)]
        public ushort EpochId;

        // 4 bytes
        [FieldOffset(0)]
        public uint Timestamp;

        // Eitan, This is not clear what it is used for?!
        public static int Compare( EpochTime left, EpochTime right )
            {
            if ( left.EpochId > right.EpochId )
                {
                return 1;
                }

            if ( left.EpochId < right.EpochId )
                {
                return -1;
                }

            if ( left.SecondsSinceEpoch > right.SecondsSinceEpoch )
                {
                return 1;
                }

            if ( left.SecondsSinceEpoch < right.SecondsSinceEpoch )
                {
                return -1;
                }

            return 0;
            }

        public static EpochTime Max;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct Date                          // 4
        {
        public byte Month;                      // 0
        public byte DayOfMonth;                 // 1
        public ushort Year;                     // 2, 3
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct LemondDateTime
        {
        public byte Hour;                       // 0
        public byte Minutes;                    // 1
        public byte Seconds;                    // 2
        public byte Month;                      // 3
        public byte DayOfMonth;                 // 4
        public byte Pad;                        // 5
        public ushort Year;                     // 6, 7
        public int SecondsOffsetToLocalTime;    // 8, 9, 10, 11
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct TickRequestReplayPayload
        {
        public uint TimestampStart;
        public uint TimestampEnd;
        public byte NoRecords;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct TickRequestBeginTransaction
        {
        public uint TotalNoRecords;
        public uint Timestamp;
        }
    }
