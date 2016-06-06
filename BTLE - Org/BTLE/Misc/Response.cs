using System;
using System.ComponentModel;
using BTLE.Types;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace BTLE.Misc
    {
    public class Response : INotifyPropertyChanged
        {
        private int _cmdTotal;
        public int CmdTotal
            {
            get
                {
                return _cmdTotal;
                }
            set
                {
                _cmdTotal = value;
                OnPropertyChanged( "NoPackets" );
                }
            }

        private ResponseHeader _header = new ResponseHeader();
        public ResponseHeader Header
            {
            get
                {
                return _header;
                }
            set
                {
                _header = value;
                OnPropertyChanged( "Header" );
                }
            }

        private byte[] _payload = new byte[0];
        public byte[ ] Payload
            {
            get
                {
                return _payload;
                }
            set
                {
                _payload = value;
                OnPropertyChanged( "Payload" );
                }
            }

        public Response()
            {
            }

        public Response( ResponseHeader header, byte[ ] payload )
            {
            Header = new ResponseHeader( (MessageReponseTypes)header.ResponseType, header.Flags, header.TransactionId, header.PayloadSize );
            Payload = new byte[ header.PayloadSize ];
            Buffer.BlockCopy( payload, 0, Payload, 0, header.PayloadSize );
            }

        public Response( byte[ ] buffer )
            {
            Header = new ResponseHeader( buffer[ (int)HdrOffset.MessageType ],
                                         buffer[ (int)HdrOffset.Flags ],
                                         buffer[ (int)HdrOffset.TransactionID ],
                                         buffer[ (int)HdrOffset.PayloadSize ] );
            Payload = new byte[ Header.PayloadSize ];
            Buffer.BlockCopy( buffer, BtleLinkTypes.RESPONSE_HEADER_SIZE, Payload, 0, Header.PayloadSize );
            }

        private int _calcCmdTotal;
        public int CalcCmdTotal
            {
            get
                {
                if ( Payload.Length <= BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE )
                    {
                    _calcCmdTotal = 1;
                    }
                else
                    {
                    _calcCmdTotal = 1 + ( ( Payload.Length - BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE ) / BtleLinkTypes.MULTI_TRANSACTION_MAX_PAYLOAD_SIZE ) + ( ( ( Payload.Length - BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE ) % BtleLinkTypes.MULTI_TRANSACTION_MAX_PAYLOAD_SIZE ) > 0 ? 1 : 0 );
                    }
                return _calcCmdTotal;
                }
            set
                {
                _calcCmdTotal = value;
                OnPropertyChanged( "CalcCmdTotat" );
                }
            }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged( string name )
            {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke( this, new PropertyChangedEventArgs( name ) );
            }
        }

    public class ResponseHeader
        {
        public byte ResponseType
            {
            get; set;
            }

        public ResponseStatus Flags
            {
            get; set;
            }

        public byte TransactionId
            {
            get; set;
            }

        public byte PayloadSize
            {
            get; set;
            }

        public ResponseHeader()
            {
            }

        public ResponseHeader( MessageReponseTypes messageType, ResponseStatus flags, byte transactionId, byte payloadSize )
            {
            ResponseType = (byte)messageType;
            Flags = flags;
            TransactionId = transactionId;
            PayloadSize = payloadSize;
            }

        public ResponseHeader( byte messageType, byte flags, byte transactionId, byte payloadSize )
            {
            ResponseType = messageType;
            Flags = (ResponseStatus)flags;
            TransactionId = transactionId;
            PayloadSize = payloadSize;
            }

        public byte[ ] Hdr2Bytes()
            {
            byte[ ] buffer = new byte[ 4 ];
            buffer[ (int)HdrOffset.MessageType ] = ResponseType;
            buffer[ (int)HdrOffset.Flags ] = (byte)Flags;
            buffer[ (int)HdrOffset.TransactionID ] = TransactionId;
            buffer[ (int)HdrOffset.PayloadSize ] = PayloadSize;
            return buffer;
            }
        }
    }
