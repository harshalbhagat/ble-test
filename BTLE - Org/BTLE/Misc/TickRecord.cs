using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BTLE.Cmds;
using BTLE.Types;
using BTLE.Utils;
using SQLite.Net.Attributes;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace BTLE.Misc
    {
    public class TickRecord : INotifyPropertyChanged
        {
        //
        // Primary Key
        //
        private long _key;
        [PrimaryKey]
        public long Key
            {
            get
                {
                return _key;
                }
            set
                {
                _key = value;
                OnPropertyChanged( "Key" );
                }
            }


        //
        // Timestamp
        //
        private DateTime _tickStartDate;
        public DateTime TickStartDate
            {
            get
                {
                return _tickStartDate;
                }
            set
                {
                _tickStartDate = value;
                OnPropertyChanged( "TickStartDate" );
                }
            }

        private DateTime _tickEndDate;
        public DateTime TickEndDate
            {
            get
                {
                return _tickEndDate;
                }
            set
                {
                _tickEndDate = value;
                OnPropertyChanged( "TickEndDate" );
                }
            }


        //
        // Record Duration in Seconds
        //
        private byte _durationInSeconds;
        public byte DurationInSeconds
            {
            get
                {
                return _durationInSeconds;
                }
            set
                {
                _durationInSeconds = value;
                OnPropertyChanged( "DurationInSeconds" );
                }
            }


        //
        // User Event Format
        //
        private bool _isPossibleBandRemoved;
        public bool IsPossibleBandRemoved
            {
            get
                {
                return _isPossibleBandRemoved;
                }
            set
                {
                _isPossibleBandRemoved = value;
                OnPropertyChanged( "IsPossibleBandRemoved" );
                }
            }

        private bool _isBatteryCharging;
        public bool IsBatteryCharging
            {
            get
                {
                return _isBatteryCharging;
                }
            set
                {
                _isBatteryCharging = value;
                OnPropertyChanged( "IsBatteryCharging" );
                }
            }

        private byte _eventSpecificFlags;
        public byte EventSpecificFlags
            {
            get
                {
                return _eventSpecificFlags;
                }
            set
                {
                _eventSpecificFlags = value;
                OnPropertyChanged( "EventSpecificFlags" );
                }
            }


        private byte _tickEventType;
        public byte TickEventType
            {
            get
                {
                return _tickEventType;
                }
            set
                {
                _tickEventType = value;
                OnPropertyChanged( "TickEventType" );
                }
            }

        private int _postProcessedStepCount;
        public int PostProcessedStepCount
            {
            get
                {
                return _postProcessedStepCount;
                }
            set
                {
                _postProcessedStepCount = value;
                OnPropertyChanged( "PostProcessedStepCount" );
                }
            }

        private double _distanceInMeters;
        public double DistanceInMeters
            {
            get
                {
                return _distanceInMeters;
                }
            set
                {
                _distanceInMeters = value;
                OnPropertyChanged( "DistanceInMeters" );
                }
            }

        private byte _activeTimeInSeconds;
        public byte ActiveTimeInSeconds
            {
            get
                {
                return _activeTimeInSeconds;
                }
            set
                {
                _activeTimeInSeconds = value;
                OnPropertyChanged( "ActiveTimeInSeconds" );
                }
            }


        private double _locomotionMET0;
        public double LocomotionMET0
            {
            get
                {
                return _locomotionMET0;
                }
            set
                {
                _locomotionMET0 = value;
                OnPropertyChanged( "LocomotionMET0" );
                }
            }

        private double _locomotionMET1;
        public double LocomotionMET1
            {
            get
                {
                return _locomotionMET1;
                }
            set
                {
                _locomotionMET1 = value;
                OnPropertyChanged( "LocomotionMET1" );
                }
            }

        private SleepStage _sleepStage0;
        public SleepStage SleepStage0
            {
            get
                {
                return _sleepStage0;
                }
            set
                {
                _sleepStage0 = value;
                OnPropertyChanged( "SleepStage0" );
                }
            }

        private SleepStage _sleepStage1;
        public SleepStage SleepStage1
            {
            get
                {
                return _sleepStage1;
                }
            set
                {
                _sleepStage1 = value;
                OnPropertyChanged( "SleepStage1" );
                }
            }

        private byte _meanHeartRate;
        public byte MeanHeartRate
            {
            get
                {
                return _meanHeartRate;
                }
            set
                {
                _meanHeartRate = value;
                OnPropertyChanged( "MeanHeartRate" );
                }
            }


        //
        // User Events
        //
        private byte _eventType;
        public byte EventType
            {
            get
                {
                return _eventType;
                }
            set
                {
                _eventType = value;
                OnPropertyChanged( "EventType" );
                }
            }


        //
        // Local variables
        //
        private bool _isReplayRecord;
        public bool IsReplayRecord
            {
            get
                {
                return _isReplayRecord;
                }
            set
                {
                _isReplayRecord = value;
                OnPropertyChanged( "IsReplayRecord" );
                }
            }

        private byte[] _rawTickRecord;
        public byte[ ] RawTickRecord
            {
            get
                {
                return _rawTickRecord;
                }
            set
                {
                value = value ?? new byte[ 0 ];
                _rawTickRecord = new byte[ value.Length ];
                Buffer.BlockCopy( value, 0, _rawTickRecord, 0, value.Length );

                OnPropertyChanged( "RawTickRecord" );
                }
            }


        public TickRecord()
            {
            }


        // Just for testing
        public TickRecord( long key )
            {
            TickStartDate = DateTime.Now.AddSeconds( -60 );
            TickEndDate = DateTime.Now;
            DurationInSeconds = 60;
            IsPossibleBandRemoved = false;
            IsBatteryCharging = false;
            EventSpecificFlags = 0x00;
            TickEventType = 0x00;
            PostProcessedStepCount = 1;
            DistanceInMeters = 1;
            ActiveTimeInSeconds = 1;
            LocomotionMET0 = 0.0;
            LocomotionMET1 = 0.0;
            SleepStage0 = SleepStage.Deep;
            SleepStage1 = SleepStage.Deep;
            MeanHeartRate = 75;
            EventType = 0x00;
            Key = key;
            IsReplayRecord = false;
            RawTickRecord = new byte[ ] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C };
            }

        public TickRecord( RawTickRecord rawTickRecord, IDictionary<uint, DateTime> epochTimes )
            {
            // Eitan, here we need to save the LemondTickRecord into a Database 
            PopulateWithTickRecord( rawTickRecord, epochTimes );
            }


        public override string ToString()
            {
            var msg = new StringBuilder();

            if ( TickEventType == 0x00 )
                {
                // Primary Key
                msg.AppendFormat( "        Tick Primary Key         = {0},\n", string.Format( "{0:###,###,###,##0} - {1}", Key, "(Seconds since 1/1/1970)" ) );

                // Timestamp
                msg.AppendFormat( "        Tick start date          = {0},\n", string.Format( "UTC: {0} - Local: {1}", TickStartDate.ToUniversalTime(), TickStartDate.ToLocalTime() ) );
                msg.AppendFormat( "        Tick end date            = {0},\n", string.Format( "UTC: {0} - Local: {1}", TickEndDate.ToUniversalTime(), TickEndDate.ToLocalTime() ) );

                // Record Duration
                msg.AppendFormat( "        Duration in seconds      = {0},\n", DurationInSeconds );

                // Event Data
                msg.AppendFormat( "        IsPossibleBandRemoved    = {0},\n", IsPossibleBandRemoved );
                msg.AppendFormat( "        IsBatteryCharging        = {0},\n", IsBatteryCharging );
                msg.AppendFormat( "        TickEventType            = {0},\n", string.Format( "0x{0}", Convert.ToString( TickEventType, 2 ).PadLeft( 8, '0' ) ) );
                msg.AppendFormat( "        PostProcessedStepCount   = {0},\n", PostProcessedStepCount );
                msg.AppendFormat( "        Distance in meters       = {0},\n", DistanceInMeters );
                msg.AppendFormat( "        Active time in secs      = {0},\n", string.Format( "{0} - ({1})", ActiveTimeInSeconds, Utility.Seconds2String( ActiveTimeInSeconds ) ) );
                msg.AppendFormat( "        LocomotionMET0           = {0},\n", LocomotionMET0 );
                msg.AppendFormat( "        LocomotionMET1           = {0},\n", LocomotionMET1 );
                msg.AppendFormat( "        SleepStage0              = {0},\n", SleepStage0 );
                msg.AppendFormat( "        SleepStage1              = {0},\n", SleepStage1 );
                msg.AppendFormat( "        Mean heart rate          = {0},\n", MeanHeartRate );

                // Local Variables
                msg.AppendFormat( "        IsReplayRecord           = {0},\n", IsReplayRecord );
                msg.AppendFormat( "\n" );
                }
            else if ( TickEventType == 0x01 )
                {
                msg.AppendFormat( "        Event Type               = {0},\n", EventType );
                }
            else if ( TickEventType == 0x02 )
                {
                }

            return msg.ToString();
            }

        public TickRecord( byte[ ] data, IDictionary<uint, DateTime> epochTimes ) : this()
            {
            Debug.Assert( data != null );
            Debug.Assert( epochTimes != null );

            RawTickRecord rawTickRecord = Utility.BytesToStruct<RawTickRecord>( data );

            // Eitan, here we need to save the LemondTickRecord into a Database
            PopulateWithTickRecord( rawTickRecord, epochTimes );
            }

        // Returns tick data size (excluding the header)
        public void PopulateWithTickRecord( RawTickRecord rawTickRecord, IDictionary<uint, DateTime> epochTimes )
            {
            int rawTickRecordSize = Marshal.SizeOf<RawTickRecord>();
            Debug.Assert( rawTickRecordSize == 13, "Tick Record Size Error" );

            ulong tickData = rawTickRecord.Tick;

            EventSpecificFlags = (byte)Utility.GetULongBitfieldData( tickData, TickTypes.EVENT_DATA_FLAGS_OFFSET, TickTypes.EVENT_DATA_FLAGS_LEN );
            TickEventType = (byte)Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_RECORD_TYPE_OFFSET, TickTypes.TICK_DATA_RECORD_TYPE_LEN );

            // Activity Data
            switch ( TickEventType )
                {
                case 0x00:
                    IsPossibleBandRemoved = Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_BAND_REMOVED_OFFSET, TickTypes.TICK_DATA_BAND_REMOVED_LEN ) != 0;
                    IsBatteryCharging = Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_BATTERY_CHARGING_OFFSET, TickTypes.TICK_DATA_BATTERY_CHARGING_LEN ) != 0;
                    TickEventType = (byte)Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_RECORD_TYPE_OFFSET, TickTypes.TICK_DATA_RECORD_TYPE_LEN );
                    PostProcessedStepCount = (int)Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_POST_PROCESSED_STEP_COUNT_OFFSET, TickTypes.TICK_DATA_POST_PROCESSED_STEP_COUNT_LEN );
                    DistanceInMeters = Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_DISTANCE_IN_METERS_OFFSET, TickTypes.TICK_DATA_DISTANCE_IN_METERS_LEN );
                    ActiveTimeInSeconds = (byte)Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_ACTIVE_TIME_IN_SECONDS_OFFSET, TickTypes.TICK_DATA_ACTIVE_TIME_IN_SECONDS_LEN );
                    LocomotionMET0 = Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_LOCOMOTION_MET0_OFFSET, TickTypes.TICK_DATA_LOCOMOTION_MET0_LEN );
                    LocomotionMET1 = Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_LOCOMOTION_MET1_OFFSET, TickTypes.TICK_DATA_LOCOMOTION_MET1_LEN );
                    SleepStage0 = (SleepStage)Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_SLEEP_STAGE0_OFFSET, TickTypes.TICK_DATA_SLEEP_STAGE0_LEN );
                    SleepStage1 = (SleepStage)Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_SLEEP_STAGE1_OFFSET, TickTypes.TICK_DATA_SLEEP_STAGE1_LEN );
                    MeanHeartRate = (byte)Utility.GetULongBitfieldData( tickData, TickTypes.TICK_DATA_MEAN_HEART_RATE_OFFSET, TickTypes.TICK_DATA_MEAN_HEART_RATE_LEN );
                    break;

                case 0x01:
                    EventType = (byte)Utility.GetULongBitfieldData( tickData, TickTypes.EVENT_DATA_TYPE_OFFSET, TickTypes.EVENT_DATA_TYPE_LEN );
                    break;

                case 0x02:
                    break;
                }

            DurationInSeconds = rawTickRecord.DurationInSeconds;
            RawTickRecord = Utility.StructToBytes( rawTickRecord );

            Key = -1;
            uint epochKey = rawTickRecord.Timestamp.EpochId;
            if ( epochTimes != null & epochTimes.Any() && epochTimes.ContainsKey( epochKey ) )
                {
                TickEndDate = epochTimes[ epochKey ].AddSeconds( rawTickRecord.Timestamp.SecondsSinceEpoch );
                TickStartDate = TickEndDate.Subtract( new TimeSpan( 0, 0, 0, rawTickRecord.DurationInSeconds ) );

                Key = TickStartDate.ToUnixTime();
                }
            else
                {
                Debug.Assert( Key == -1, "Key Error" );
                }
            }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged( string name )
            {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke( this, new PropertyChangedEventArgs( name ) );
            }
        }
    }
