using BALE_watcher.Type;
using System;
using System.ComponentModel;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using static BALE_watcher.Type.Enums;

namespace BALE_watcher.Misc
{
    public class Transaction : INotifyPropertyChanged
    {
        private int _noPackets;
        public int NoPackets
        {
            get
            {
                return _noPackets;
            }
            set
            {
                _noPackets = value;
                OnPropertyChanged("NoPackets");
            }
        }

        private bool _isEncrypt;
        public bool IsEncrypt
        {
            get
            {
                return _isEncrypt;
            }
            set
            {
                _isEncrypt = value;
                OnPropertyChanged("IsEncrypt");
            }
        }

        private TransactionStatus _status;

        public TransactionStatus Status
        {
            get
            {
                return _status;
            }
            private set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        private TransactionHeader _header = new TransactionHeader();
        public TransactionHeader Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
                OnPropertyChanged("Header");
            }
        }

        private byte[] _payload = new byte[0];
        public byte[] Payload
        {
            get
            {
                return _payload;
            }
            set
            {
                _payload = value;
                OnPropertyChanged("Payload");
            }
        }

        private GattCharacteristic _characteristic;
        public GattCharacteristic Characteristic
        {
            get
            {
                return _characteristic;
            }
            set
            {
                _characteristic = value;
                OnPropertyChanged("Characteristic");
            }
        }

        public Transaction(Transaction transaction)
        {
            NoPackets = transaction.NoPackets;
            IsEncrypt = transaction.IsEncrypt;
            Status = transaction.Status;
            Header = new TransactionHeader(transaction.Header.MessageType, transaction.Header.Flags, transaction.Header.TransactionId, transaction.Header.PayloadSize);
            if (transaction.Payload != null)
            {
                Payload = new byte[transaction.Header.PayloadSize];
                Buffer.BlockCopy(transaction.Payload, 0, Payload, 0, transaction.Header.PayloadSize);
            }
            else
            {
                Payload = null;
            }
            Characteristic = transaction.Characteristic;
        }

        public Transaction(GattCharacteristic characteristic, TransactionHeader header, byte[] payload, bool isEncrypt = true)
        {
            IsEncrypt = isEncrypt;
            Characteristic = characteristic;
            Status = TransactionStatus.TransactionWait;
            Header = new TransactionHeader(header.MessageType, header.Flags, header.TransactionId, header.PayloadSize);
            if (payload != null)
            {
                Payload = new byte[header.PayloadSize];
                Buffer.BlockCopy(payload, 0, Payload, 0, header.PayloadSize);
            }
            else
            {
                Payload = null;
            }
        }

        public int CalcNoPackets()
        {
            int noPackets;
            if (Payload.Length <= BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE)
            {
                noPackets = 1;
            }
            else
            {
                noPackets = 1 + (Payload.Length - BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE) / BtleLinkTypes.MULTI_TRANSACTION_MAX_PAYLOAD_SIZE +
                                ((Payload.Length - BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE) % BtleLinkTypes.MULTI_TRANSACTION_MAX_PAYLOAD_SIZE > 0 ? 1 : 0);
            }
            return noPackets;

        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class TransactionHeader : INotifyPropertyChanged
    {
        private PairingMsgTypes _messageType;
        public PairingMsgTypes MessageType
        {
            get
            {
                return _messageType;
            }
            set
            {
                _messageType = value;
                OnPropertyChanged("MessageType");
            }
        }

        private byte _flags;
        public byte Flags
        {
            get
            {
                return _flags;
            }
            set
            {
                _flags = value;
                OnPropertyChanged("Flags");
            }
        }

        private byte _transactionId;
        public byte TransactionId
        {
            get
            {
                return _transactionId;
            }
            set
            {
                _transactionId = value;
                OnPropertyChanged("TransactionId");
            }
        }

        private byte _payloadSize;
        public byte PayloadSize
        {
            get
            {
                return _payloadSize;
            }
            set
            {
                _payloadSize = value;
                OnPropertyChanged("PayloadSize");
            }
        }

        public TransactionHeader()
        {
        }

        public TransactionHeader(PairingMsgTypes messageType, byte flags, byte transactionId, byte payloadSize)
        {
            MessageType = messageType;
            Flags = flags;
            TransactionId = transactionId;
            PayloadSize = payloadSize;
        }

        public byte[] Hdr2Bytes()
        {
            byte[] buffer = new byte[4];
            buffer[(int)HdrOffset.MessageType] = (byte)MessageType;
            buffer[(int)HdrOffset.Flags] = Flags;
            buffer[(int)HdrOffset.TransactionID] = TransactionId;
            buffer[(int)HdrOffset.PayloadSize] = PayloadSize;
            return buffer;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}