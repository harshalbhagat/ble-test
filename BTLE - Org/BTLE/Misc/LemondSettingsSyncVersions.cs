using System.ComponentModel;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global

namespace BTLE.Misc
    {
    public class LemondSettingsSyncVersions : INotifyPropertyChanged
        {
        // Specifies the sync version of the alarm settings
        private int _smartAlarmSyncVersion;
        public int SmartAlarmSyncVersion
            {
            get
                {
                return _smartAlarmSyncVersion;
                }
            set
                {
                _smartAlarmSyncVersion = value;
                OnPropertyChanged( "SmartAlarmSyncVersion" );
                }
            }

        // Specifies the sync version of the idle alert settings
        private int _idleAlertSyncVersion;
        public int IdleAlertSyncVersion
            {
            get
                {
                return _idleAlertSyncVersion;
                }
            set
                {
                _idleAlertSyncVersion = value;
                OnPropertyChanged( "IdleAlertSyncVersion" );
                }
            }

        // Specifies the sync version of the demographics information
        private int _demographicsSyncVersion;
        public int DemographicsSyncVersion
            {
            get
                {
                return _demographicsSyncVersion;
                }
            set
                {
                _demographicsSyncVersion = value;
                OnPropertyChanged( "DemographicsSyncVersion" );
                }
            }

        // Specifies the sync version of the daily goals settings
        private int _goalsSyncVersion;
        public int GoalsSyncVersion
            {
            get
                {
                return _goalsSyncVersion;
                }
            set
                {
                _goalsSyncVersion = value;
                OnPropertyChanged( "GoalsSyncVersion" );
                }
            }

        public LemondSettingsSyncVersions( int smartAlarmSyncVersion, int idleAlertSyncVersion, int demographicsSyncVersion, int goalsSyncVersion )
            {
            SmartAlarmSyncVersion = smartAlarmSyncVersion;
            IdleAlertSyncVersion = idleAlertSyncVersion;
            DemographicsSyncVersion = demographicsSyncVersion;
            GoalsSyncVersion = goalsSyncVersion;
            }

        public override string ToString()
            {
            StringBuilder description = new StringBuilder();

            description.AppendFormat( "DemographicsSyncVersion = {0},\n", DemographicsSyncVersion );
            description.AppendFormat( "GoalsSyncVersion = {0},\n", GoalsSyncVersion );
            description.AppendFormat( "IdleAlertSyncVersion = {0},\n", IdleAlertSyncVersion );
            description.AppendFormat( "SmartAlarmSyncVersion = {0},\n", SmartAlarmSyncVersion );

            return description.ToString();
            }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged( string name )
            {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke( this, new PropertyChangedEventArgs( name ) );
            }
        }
    }
