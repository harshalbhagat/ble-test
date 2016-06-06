using System;
using BTLE.Types;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToAutoProperty
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UnusedMember.Global

namespace BTLE.Misc
    {
    public class RawPacket
        {
        private bool _isComplete;

        public bool IsComplete
            {
            get
                {
                return _isComplete;
                }
            set
                {
                _isComplete = value;
                }
            }

        private int _payloadSize;
        public int PayloadSize
            {
            get
                {
                return _payloadSize;
                }
            set
                {
                _payloadSize = value;
                }
            }   

        private int _payloadIndex;
        public int PayloadIndex
            {
            get
                {
                return _payloadIndex;
                }
            set
                {
                _payloadIndex = value;
                }
            }

        private int _payloadMissing;
        public int PayloadMissing
            {
            get
                {
                return _payloadMissing;
                }
            set
                {
                _payloadMissing = value;
                }
            }      

        private byte[] _data;
        public byte[ ] Data
            {
            get
                {
                return _data;
                }
            set
                {
                value = value ?? new byte[ 0 ];
                _data = new byte[ value.Length ];
                Buffer.BlockCopy( value, 0, _data, 0, value.Length );
                }
            }

        private byte[] _reminder;
        public byte[ ] Reminder
            {
            get
                {
                return _reminder;
                }
            set
                {
                value = value ?? new byte[ 0 ];
                _reminder = new byte[ value.Length ];
                Buffer.BlockCopy( value, 0, _reminder, 0, value.Length );
                }
            }

        public RawPacket()
            {
            //SequenceNo = -1;
            IsComplete = false;
            PayloadSize = -1;
            PayloadIndex = 0;
            PayloadMissing = -1;
            Data = null;
            Reminder = null;
            }

        public RawPacket( byte[ ] buffer, byte[ ] reminder )
            {
            byte[ ] newBuffer;
            if ( reminder != null )
                {
                newBuffer = new byte[ buffer.Length + reminder.Length ];
                Buffer.BlockCopy( reminder, 0, newBuffer, 0, reminder.Length );
                Buffer.BlockCopy( buffer, 0, newBuffer, reminder.Length, buffer.Length );
                }
            else
                {
                newBuffer = new byte[ buffer.Length ];
                Buffer.BlockCopy( buffer, 0, newBuffer, 0, buffer.Length );
                }

            PayloadSize = newBuffer[ (int)HdrOffset.PayloadSize ];

            // We got the whole packet
            if ( PayloadSize + BtleLinkTypes.RESPONSE_HEADER_SIZE == newBuffer.Length )
                {
                IsComplete = true;
                PayloadMissing = 0;
                PayloadIndex = 0;
                Data = new byte[ BtleLinkTypes.RESPONSE_HEADER_SIZE + PayloadSize ];
                Buffer.BlockCopy( newBuffer, 0, Data, 0, BtleLinkTypes.RESPONSE_HEADER_SIZE + PayloadSize );

                // No extra bytes
                Reminder = null;
                return;
                }

            // We got more data than one packet, we will have a Reminder
            if ( PayloadSize + BtleLinkTypes.RESPONSE_HEADER_SIZE < newBuffer.Length )
                {
                IsComplete = true;
                PayloadMissing = 0;
                PayloadIndex = 0;
                Data = new byte[ PayloadSize + BtleLinkTypes.RESPONSE_HEADER_SIZE ];
                Buffer.BlockCopy( newBuffer, 0, Data, 0, PayloadSize + BtleLinkTypes.RESPONSE_HEADER_SIZE );

                // There are extra bytes, move them into the Reminder
                Reminder = new byte[ newBuffer.Length - ( BtleLinkTypes.RESPONSE_HEADER_SIZE + PayloadSize ) ];
                Buffer.BlockCopy( newBuffer, PayloadSize + BtleLinkTypes.RESPONSE_HEADER_SIZE, Reminder, 0, newBuffer.Length - ( BtleLinkTypes.RESPONSE_HEADER_SIZE + PayloadSize ) );
                return;
                }

            // We did not get the whole payload
            if ( PayloadSize + BtleLinkTypes.RESPONSE_HEADER_SIZE > newBuffer.Length )
                {
                IsComplete = false;
                PayloadMissing = PayloadSize - ( newBuffer.Length - BtleLinkTypes.RESPONSE_HEADER_SIZE );
                PayloadIndex = newBuffer.Length;
                Data = new byte[ BtleLinkTypes.RESPONSE_HEADER_SIZE + PayloadSize ];
                Buffer.BlockCopy( newBuffer, 0, Data, 0, newBuffer.Length );

                // No extra bytes
                Reminder = null;
                }
            }

        public void Add( byte[ ] buffer )
            {
            var size = Math.Min( PayloadMissing, buffer.Length - 1 );
            Buffer.BlockCopy( buffer, 1, _data, PayloadIndex, size );
            PayloadMissing -= size;
            PayloadIndex += size;
            IsComplete = PayloadMissing == 0;

            if ( size < buffer.Length - 1 )
                {
                // There are extra bytes, move them into the Reminder
                var len = buffer.Length - 1 - size;
                Reminder = new byte[ len ];
                Buffer.BlockCopy( buffer, size + 1, Reminder, 0, len );
                return;
                }

            if ( size == buffer.Length - 1 )
                {
                Reminder = null;
                return;
                }

            if ( size > buffer.Length - 1 )
                {
                Reminder = null;
                }
            }
        }
    }
