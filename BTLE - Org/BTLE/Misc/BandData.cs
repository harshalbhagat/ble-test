// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

using System.ComponentModel;
using System.Text;

namespace BTLE.Misc
    {
    public class BandData : INotifyPropertyChanged
        {
        #region Definitions for the UI display
        private int _steps;
        public int Steps
            {
            get
                {
                return _steps;
                }

            set
                {
                _steps = value;
                OnPropertyChanged( "Steps" );
                }
            }

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

        private int _count;
        public int Count
            {
            get
                {
                return _count;
                }
            set
                {
                _count = value;
                OnPropertyChanged( "Count" );
                }
            } 

        private string _lastTick;
        public string LastTick
            {
            get
                {
                return _lastTick;
                }
            set
                {
                _lastTick = value;
                OnPropertyChanged( "LastTick" );
                }
            }

        private bool _isWalking;
        public bool IsWalking
            {
            get
                {
                return _isWalking;
                }

            set
                {
                _isWalking = value;
                OnPropertyChanged( "IsWalking" );
                }
            }

        private bool _isConnected;
        public bool IsConnected
            {
            get
                {
                return _isConnected;
                }

            set
                {
                _isConnected = value;
                OnPropertyChanged( "IsConnected" );
                }
            }

        private byte _battery;
        public byte Battery
            {
            get
                {
                return _battery;
                }

            set
                {
                _battery = value;
                OnPropertyChanged( "Battery" );
                }
            }

        public override string ToString()
            {
            var msg = new StringBuilder();

            msg.AppendFormat( "\n" );
            msg.AppendFormat( "    Band Data\n" );

            msg.AppendFormat( "        Steps:                 " + Steps + "\n" );
            msg.AppendFormat( "        Distance:              " + DistanceInMeters + "\n" );

            msg.AppendFormat( "        Mean HR:               " + MeanHeartRate + "\n" );

            msg.AppendFormat( "        Is Removed:            " + IsPossibleBandRemoved + "\n" );
            msg.AppendFormat( "        Is Charging:           " + IsBatteryCharging + "\n" );

            msg.AppendFormat( "        Last Tick:             " + LastTick + "\n" );

            msg.AppendFormat( "        Count:                 " + Count + "\n" );

            msg.AppendFormat( "        Is Walking:            " + IsWalking + "\n" );
            msg.AppendFormat( "        Is Connected:          " + IsConnected + "\n" );

            return msg.ToString();
            }


        public BandData()
            {
            Steps = 0;
            DistanceInMeters = 0.0;

            MeanHeartRate = 0;

            IsPossibleBandRemoved = false;
            IsBatteryCharging = false;

            Count = 0;

            LastTick = string.Empty;

            IsWalking = false;  
            IsConnected = false;
            }
        #endregion  

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged( string propertyName )
            {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
            }
        #endregion      
        }
    }
