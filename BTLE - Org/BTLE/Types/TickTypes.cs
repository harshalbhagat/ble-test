using System.Runtime.InteropServices;
using BTLE.Utils;
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

namespace BTLE.Types
    {
    /**
     * This defines the tick data format exchanged between the band and the
     * phone. This data format is used when sending a tick to the phone, or when
     * replaying a set of records.
     *
     * N.B - The tick data is tightly packed within 8 bytes.
     *       There are still 5 unused bit if additional data is to be included.
     *
     * The bit field layout from TICK_DATA_MEAN_HEART_RATE to TICK_DATA_BAND_REMOVED (left to right).
     * 8 | 3 | 3 | 5 | 5 | 6 | 10 | 9 | 5 | 1 | 1
     */

    /** F(n)_OFFSET := F(n-1)_OFFSET + F(n-1)_LEN */

    /*
      Defines the different sleep stages. They are mutually exclusive.
      N.B - SLEEP_STAGE_DEEP is used when BioImpedance circuit is enabled.
      SLEEP_STAGE_SOUND is used when only accelerometer is enabled.
    */

    public enum SleepStage
        {
        Invalid = 0,
        Wake = 1,
        Light = 2,
        Rem = 3,
        Deep = 4,
        Sound = 5
        }

    public enum EventTypes
        {
        Timestamp = 0,
        ActivityStart = 1,
        ActivityStop = 2,
        SleepStart = 3,
        SleepStop = 4,
        IdleAlert = 5,
        Max = 6
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct ReplayRequest
        {
        public ReplayRequest( uint startTime, uint endTime ) : this()
            {
            StartTime = startTime;
            EndTime = endTime;
            }

        // Specifies the oldest record to start from
        public readonly uint StartTime;

        //Specifies the newest record to end with
        public readonly uint EndTime;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct ReplayResponse
        {
        // Specifies the number of records in the replay set
        public readonly uint Count;

        //Specifies the effective end of the replay - the start time of the first record that is not included in the replay
        public readonly uint EffectiveEnd;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct EpochRequest
        {
        public EpochRequest( ushort start ) : this()
            {
            Start = start;
            }

        // Specifies the starting epoch number to retrieve. All epochs beginning at this number will be sent
        public readonly ushort Start;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct EpochEntry
        {
        // Specifies the index of the epoch
        public readonly uint EpochId;

        // Specifies the UTC time of the epoch start
        public readonly uint StartTime;

        // Specifies any flags for this epoch EPOCH_ENTRY_FLAG_UNVERIFIED
        public readonly uint Flags;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct EpochResponseHeader
        {
        // Specifies the number of epoch records in the reply
        public readonly ushort Count;
        }

    // This structure specifies the format of a record sent from the band to the phone
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct TickRecord
        {
        // Specifies the timestamp of *end* of the period represented by the record
        public EpochTime Timestamp;

        // Specifies the length of time the record represents. The period of time 
        // represented by the record is [Timestamp - DurationInSeconds, Timestamp]
        public readonly byte DurationInSeconds;

        // Specifies tick data for activity and/or sleep.
        // The bit fields are defined at the top of this header file.
        public readonly ulong Tick;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct TickRecordRate
        {
        public TickRecordRate( byte rate ) : this()
            {
            NumRecordsPerSend = rate;
            }

        // Specifies the number of records per send. This is bounded by
        // TICK_RECORD_BATCH_COUNT_MAX and TICK_RECORD_BATCH_COUNT_MIN
        private readonly byte NumRecordsPerSend;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct ActivitySummary
        {
        public readonly uint Steps;
        public readonly uint ActivityTimeSecs;
        public readonly uint DistanceInMeters;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct RealTimeSummaryStateRequest
        {
        public RealTimeSummaryStateRequest( byte enable = 1 ) : this()
            {
            Enable = enable;
            Since = 0;
            }

        // Set to true to enable, false to disable, real-time summary reporting
        public readonly byte Enable;

        // Set the baseline timestamp for the summary 
        public readonly uint Since;
        }

    public static class TickTypes
        {
        // Tick record types, as specified by bits 2..7 of TickData.
        public const int TICK_DATA_RECORD_TYPE_ACTIVITY = 0;
        public const int TICK_DATA_RECORD_TYPE_EVENT = 1;

        // This indicates the band was possibly removed during the record */
        public const int TICK_DATA_BAND_REMOVED_LEN = 1;
        public const int TICK_DATA_BAND_REMOVED_OFFSET = 0;

        // The battery was charging during the record 
        public const int TICK_DATA_BATTERY_CHARGING_LEN = 1;
        public const int TICK_DATA_BATTERY_CHARGING_OFFSET = 1;

        // Record type bits. 
        public const int TICK_DATA_RECORD_TYPE_LEN = 5;
        public const int TICK_DATA_RECORD_TYPE_OFFSET = 2;

        // The number of steps post processed. Max 360/minute. 
        public const int TICK_DATA_POST_PROCESSED_STEP_COUNT_LEN = 9;
        public const int TICK_DATA_POST_PROCESSED_STEP_COUNT_OFFSET = 7;

        // The distance traveled in the record in meters 
        public const int TICK_DATA_DISTANCE_IN_METERS_LEN = 10;
        public const int TICK_DATA_DISTANCE_IN_METERS_OFFSET = 16;

        // The amount of time actively moving during the record in seconds. Max is 60. 
        public const int TICK_DATA_ACTIVE_TIME_IN_SECONDS_LEN = 6;
        public const int TICK_DATA_ACTIVE_TIME_IN_SECONDS_OFFSET = 26;


        // Locomotion METs/calorie burned.
        // MET comes per .5 tick. MET0 for the 1st 30 sec and MET1 for 2nd 30 sec.
        public const int TICK_DATA_LOCOMOTION_MET0_LEN = 5;
        public const int TICK_DATA_LOCOMOTION_MET0_OFFSET = 32;
        public const int TICK_DATA_LOCOMOTION_MET1_LEN = 5;
        public const int TICK_DATA_LOCOMOTION_MET1_OFFSET = 37;

        // Sleep tick stages.
        // SleepStage0 for the 1st 30 sec and SleepStage1 for 2nd 30 sec. 
        public const int TICK_DATA_SLEEP_STAGE0_LEN = 3;
        public const int TICK_DATA_SLEEP_STAGE0_OFFSET = 42;
        public const int TICK_DATA_SLEEP_STAGE1_LEN = 3;
        public const int TICK_DATA_SLEEP_STAGE1_OFFSET = 45;

        // Mean heart rate in beats/minute.
        public const int TICK_DATA_MEAN_HEART_RATE_LEN = 8;
        public const int TICK_DATA_MEAN_HEART_RATE_OFFSET = 48;

        /**
         * EVENT bit fields.
         * Events have the TICK_DATA_RECORD_TYPE field set to
         * TICK_DATA_RECORD_TYPE_EVENT (1)
         * Events waste a ton of space, as there are only 5 event types today. But
         * events are expected to be infrequent.
        */

        public const int EVENT_DATA_FLAGS_LEN = 2;
        public const int EVENT_DATA_FLAGS_OFFSET = 0;

        public const int EVENT_DATA_TYPE_LEN = 3;
        public const int EVENT_DATA_TYPE_OFFSET = 8;

        /* Enforce type for tick data 
           typedef uint64_t TickData_t; */

        // deprecated
        // Specifies the amount of signal the accelerometer buffers.
        public const int RECORD_TICK_PERIOD = 6;

        // Specifies the flag values for EpochEntry_t::Flags
        public const int EPOCH_ENTRY_FLAG_UNVERIFIED = 1;

        // max number of epoch entries that will be returned from a query
        public const int MAX_EPOCH_ENTRIES = 5;

        // Specifies the lower limit of tick record send rate
        public const int TICK_RECORD_BATCH_COUNT_MIN = 1;

        // Specifies the upper limit of tick record send rate
        public const int TICK_RECORD_BATCH_COUNT_MAX = 15;

        public static void SetTickData( ref ulong tickData, int offset, int len, ulong value )
            {
            //while loop does not make sense ???

            do
                {
                tickData &= ~Utility.CreateULongBitfieldMask( offset, len );
                tickData |= ( ( value << offset ) & Utility.CreateULongBitfieldMask( offset, len ) );
                } while ( false );
            }
        }
    }
