using System;
using System.Diagnostics;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace BTLE.Misc
    {
    public sealed class LemondVibrationPattern
        {
        public LemondVibrationPattern( string name, LemondVibrationPatternSlot[ ] vibrationSlots, bool isRepeat )
            {
            Debug.Assert( !string.IsNullOrWhiteSpace( name ) );
            Debug.Assert( vibrationSlots != null );

            _name = name;
            _isRepeat = isRepeat;

            VibrationSlots = new LemondVibrationPatternSlot[ vibrationSlots.Length ];
            Array.Copy( vibrationSlots, VibrationSlots, vibrationSlots.Length );
            }

        public LemondVibrationPatternSlot[ ] VibrationSlots
            {
            get;
            }

        public override string ToString()
            {
            StringBuilder description = new StringBuilder();

            description.AppendFormat( "Name = {0}, \n", _name );
            description.AppendFormat( "IsRepeat = {0}, \n", _isRepeat );

            if ( VibrationSlots != null )
                {
                foreach ( LemondVibrationPatternSlot slot in VibrationSlots )
                    {
                    description.AppendFormat( "Slot. Values = {0}, \n\n", slot );
                    }
                }

            return description.ToString();
            }

        //todo: IsEqual 

        private readonly string _name;
        private readonly bool _isRepeat;
        }
    }
