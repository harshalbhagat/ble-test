using bledemo1.Type;
using bledemo1.Util;
using BTLE.Misc;
using BTLE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Buffer = System.Buffer;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace bledemo1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DeviceWatcher deviceWatcher = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformation> handlerAdded = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerUpdated = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerRemoved = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerEnumCompleted = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerStopped = null;
        private EnumberationCompletedEventHandler enumberationCompletedEvent = new EnumberationCompletedEventHandler();
        private BluetoothLEDevice btleDevice2;

        public ObservableCollection<DeviceInformationDisplay> ResultCollection
        {
            get;
            private set;
        }


        //------------------------------------------

        private static bool _isConnecting;


        // GATT Device Services
        private GattDeviceService _gattStreamService;
        private GattDeviceService _gattControlService;
        private GattDeviceService _gattInformationService;

        // GATT_STREAM_SERVICE_UUID
        private static IReadOnlyList<GattCharacteristic> _characteristicsStreamTx;                      // GATT Stream Service - Stream Data from the band              // ** Needed for Re-Connect ** //
        private static IReadOnlyList<GattCharacteristic> _characteristicsStreamRx;                      // GATT Stream Service - Stream Data to the band

        // JAWBONE_CONTROL_SERVICE_UUID
        private static IReadOnlyList<GattCharacteristic> _characteristicsControlDatetime;               // JAWBONE Control Service Data - DateTime to/from the band
        private static IReadOnlyList<GattCharacteristic> _characteristicsControlConnectMode;            // JAWBONE Control Service Data - ConnectMode to/from the band

        // DEVICE_INFORMATION_SERVICE_UUID
        private static IReadOnlyList<GattCharacteristic> _characteristicsInformationModelNo;            // Device Information Service - Model Number
        private static IReadOnlyList<GattCharacteristic> _characteristicsInformationSerialNo;           // Device Information Service - Serial Number
        private static IReadOnlyList<GattCharacteristic> _characteristicsInformationFirmwareVer;        // Device Information Service - Firmware Version
        private static IReadOnlyList<GattCharacteristic> _characteristicsInformationSoftwareVer;        // Device Information Service - Software Version
        private static IReadOnlyList<GattCharacteristic> _characteristicsInformationlManufacturer;      // Device Information Service - Manufacturer Name

        //-----------------------------
        private RawPacket _rawPacket;                                                                                                                                   // ** Needed for Re-Connect ** //
        private byte[] _tmpBuffer;                                                                                                                                      // ** Needed for Re-Connect ** //
        private byte[] _reminder;

        private bool _isDisconnected;                                                                                                                                   // ** Needed for Re-Connect ** //

        private int _simpleSendBusy;                                                                             // ** Needed for Re-Connect ** //
        private int _multiSendBusy;                                                                              // ** Needed for Re-Connect ** //
        private int _decodeResponseBusy;                                                                         // ** Needed for Re-Connect ** //

        private byte _messageType;
        private byte _flags;
        private byte _transactionId = 0xF1;
        private byte _payloadSize;

        private static byte _sequenceNumberIn = 0xFF;                                                            // ** Needed for Re-Connect ** //
        private static byte _sequenceNumberOut;


        private DispatcherTimer _timer;
        private int _neededBytes;


        readonly ArrayList _characteristicsTxDataIn = new ArrayList();

        private int _isBusyCharacteristicsTxValueChanged;

        private static PairingMsgTypes _pairingSequenceState = PairingMsgTypes.BandNotConnected;                 // ** Needed for Re-Connect ** //
                                                                                                                 // ** Needed for Re-Connect ** //

        private static bool _isLongTransaction;

        private byte[] _phoneChallenge = new byte[0];
        private byte[] _bandResponse = new byte[0];
        private byte[] _phoneResponse = new byte[0];
        private byte[] _bandChallenge = new byte[0];
        private byte[] _key = new byte[0];
        private byte[] _phoneSeed = new byte[0];
        private byte[] _deviceSeed = new byte[0];
        private byte[] _maskedPhoneChallengeZ = new byte[0];


        private object lockQueueTransactions = new object();                                                                                                            // ** Needed for Re-Connect ** //
        private object lockQueueResponsePackets = new object();


        private static readonly ObservableCollection<Transaction> QueueTransactions = new ObservableCollection<Transaction>();                                    // ** Needed for Re-Connect ** //
        private static readonly ObservableCollection<RawPacket> QueueResponsePackets = new ObservableCollection<RawPacket>();

        private readonly BandData bandData = new BandData();


        private BtleWatcher _watcher4;
        private BluetoothLEDevice btleDevice4;
        private BtleWatcher _watcher3;
        private BluetoothLEDevice btleDevice3;

        private DeviceInformationDisplay _deviceInformationDisplayConnect;
        // Dictionary definitions
        private readonly Dictionary<uint, DateTime> _dictionaryEpochTimes = new Dictionary<uint, DateTime>();
        private readonly Dictionary<byte, byte> _dictionaryTransactionIds = new Dictionary<byte, byte>();
        private StreamEncryptor _streamEncryptor;

        public MainPage()
        {
            this.InitializeComponent();
            ResultCollection = new ObservableCollection<DeviceInformationDisplay>();

            listView.SelectionChanged += ComboBox_SelectionChanged;

            //var selector = BluetoothDevice.GetDeviceSelector();
            //var devices = DeviceInformation.FindAllAsync(selector);
            //var unpair = DeviceInformationPairing.TryRegisterForAllInboundPairingRequests(DevicePairingKinds.None);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string val = listView.SelectedValue.ToString();


            TryPair(val);

        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            progressRing.IsActive = true;
            lblComplete.Text = "";
            progressRing.Visibility = Visibility.Visible;
            StartWatcher();
        }

        private async Task TryPair(string val)
        {
            DeviceInformationDisplay _deviceInformationDisplayConnect = ResultCollection.Where(r => r.Id == val).FirstOrDefault();

            DevicePairingKinds ceremoniesSelected = DevicePairingKinds.ConfirmOnly;
            //  ProtectionLevelSelectorInfo protectionLevelInfo = (ProtectionLevelSelectorInfo)protectionLevelComboBox.SelectedItem;

            DevicePairingProtectionLevel protectionLevel = DevicePairingProtectionLevel.Default;

            DeviceInformationCustomPairing customPairing = _deviceInformationDisplayConnect.DeviceInformation.Pairing.Custom;


            customPairing.PairingRequested += PairingRequestedHandler;

            DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);

            customPairing.PairingRequested -= PairingRequestedHandler;


            StopWatcher();

            btleDevice2 = await BluetoothLEDevice.FromIdAsync(_deviceInformationDisplayConnect.Id);

            Debug.WriteLine("\n" + btleDevice2.BluetoothAddress + "\n");

            _deviceInformationDisplayConnect.BtleAddress = btleDevice2.BluetoothAddress;


            var service1 = await GattDeviceService.FromIdAsync(_deviceInformationDisplayConnect.Id);

            var gapData = service1.GetCharacteristics(new Guid("00002A04-0000-1000-8000-00805f9b34fb"))[0];
            var raw = await gapData.ReadValueAsync();

            byte[] conParas = new byte[raw.Value.Length];

            DataReader.FromBuffer(raw.Value).ReadBytes(conParas);

            var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromShortId(0x1800));

            var status = await gapData.WriteValueAsync(conParas.AsBuffer());

            //GattDeviceService Service = await GattDeviceService.FromIdAsync(deviceInfoDisp.Id);




            //I can breakpoint and verify that the read works fine


            //I can breakpoint and verify that the read works fine


            if (btleDevice2 != null)
            {
                // BT_Alert: GattServices returns a list of all the supported services of the device. If the services supported by the device are expected to change
                // during BT usage, make sure to implement the GattServicesChanged event
                Debug.WriteLine("Services: ");
                Debug.WriteLine("========= ");

                var ServiceCollection = new ObservableCollection<BluetoothLEAttributeeDisplay>();
                foreach (var service in btleDevice2.GattServices)
                {
                    ServiceCollection.Add(new BluetoothLEAttributeeDisplay(service));
                    if (BluetoothLEAttributeeDisplay.IsSigDefinedUuid(service.Uuid))
                    {
                        GattNativeServiceUuid serviceName;
                        if (Enum.TryParse(Utility.ConvertUuidToShortId(service.Uuid).ToString(), out serviceName))
                        {
                            Debug.WriteLine("    " + serviceName);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("    Custom Service: " + service.Uuid);
                    }
                }
            }

            //  btleDevice2.ConnectionStatusChanged += BtleDevice2_ConnectionStatusChanged;

            //
            // Get the GATT_STREAM_SERVICE and Characteristics
            //
            //  await InitializeGattStreamServiceCharateristics(_deviceInformationDisplayConnect);

            // _watcher3
            //
            // Get the JAWBONE_CONTROL_SERVICE and Characteristics
            //
            await InitializeControlService(btleDevice2);


            // _watcher4
            //
            // Get the DEVICE_INFORMATION_SERVICE_UUID and Characteristics
            //
            await InitializeInformationService(btleDevice2);

            //Debug_DumpDescriptors( _characteristicsStreamRx[ 0 ] );
            //Debug_DumpDescriptors( _characteristicsStreamTx[ 0 ] );

            //Debug_DumpDescriptors( _characteristicsControlDatetime[ 0 ] );
            //Debug_DumpDescriptors( _characteristicsControlConnectMode[ 0 ] );


            Debug.WriteLine("Characteristics Properties:");
            Debug.WriteLine("===========================");
            Debug.WriteLine("    _characteristicsStreamTx:                 " + _characteristicsStreamTx[0].CharacteristicProperties);
            Debug.WriteLine("    _characteristicsStreamRx:                 " + _characteristicsStreamRx[0].CharacteristicProperties);
            Debug.WriteLine("    _characteristicsControlDatetime:          " + _characteristicsControlDatetime[0].CharacteristicProperties);
            Debug.WriteLine("    _characteristicsControlConnectMode:       " + _characteristicsControlConnectMode[0].CharacteristicProperties);
            Debug.WriteLine("    _characteristicsInformationModelNo:       " + _characteristicsInformationModelNo[0].CharacteristicProperties);
            Debug.WriteLine("    _characteristicsInformationSerialNo:      " + _characteristicsInformationSerialNo[0].CharacteristicProperties);
            Debug.WriteLine("    _characteristicsInformationFirmwareVer:   " + _characteristicsInformationFirmwareVer[0].CharacteristicProperties);
            Debug.WriteLine("    _characteristicsInformationSoftwareVer:   " + _characteristicsInformationSoftwareVer[0].CharacteristicProperties);
            Debug.WriteLine("    _characteristicsInformationlManufacturer: " + _characteristicsInformationlManufacturer[0].CharacteristicProperties);


            // Create the PairingSequence transactions and submit them to the Band
            StartConnectionSequence();
            Debug.WriteLine("Starting Pairing Sequence", NotifyType.StatusMessage);

        }

        private void StartConnectionSequence()
        {
            _isConnecting = true;

            // 1. Get Protocol Version
            Connecting_GetProtocolVersion_1();
        }

        private async Task InitializeInformationService(BluetoothLEDevice btleDevice)
        {
            if (_watcher4.ResultCollection.Any() && _watcher4.EnumCompleted)
            {
                //foreach ( DeviceInformationDisplay entry in ResultCollection4 )
                foreach (DeviceInformationDisplay entry in _watcher4.ResultCollection)
                {
                    btleDevice4 = await BluetoothLEDevice.FromIdAsync(entry.Id);

                    // Here we check if we are using the same BTLE address as btleDevice2: btleDevice2.BluetoothAddress == btleDevice3.BluetoothAddress
                    if (btleDevice4.BluetoothAddress == btleDevice.BluetoothAddress)
                    {
                        Guid guid = new Guid(UuidDefs.DEVICE_INFORMATION_SERVICE_UUID);
                        _gattInformationService = btleDevice4.GetGattService(guid);

                        // Get the Model Number characteristics
                        _characteristicsInformationModelNo = GetCharacteristics(_gattInformationService, UuidDefs.DEVICE_INFORMATION_MODEL_NUMBER_CHARACTERISTIC_UUID);
                        Debug.Assert(_characteristicsInformationModelNo != null);
                        GattReadResult modelNo = await _characteristicsInformationModelNo[0].ReadValueAsync();
                        Debug.WriteLine("   Characteristics:                             " + "_characteristicsInformationModelNo");
                        Debug.WriteLine("       Status:                                  " + modelNo.Status);
                        Debug.WriteLine("       Value:                                   " + Utility.ReadIBuffer2Str(modelNo.Value, true));
                        using (DataReader dataReader = DataReader.FromBuffer(modelNo.Value))
                        {
                            var modelNumber = dataReader.ReadString(modelNo.Value.Length);
                            Debug.WriteLine("       Model Number:                            " + modelNumber);
                        }
                        Debug.WriteLine("");

                        // Get the Serial Number characteristics
                        _characteristicsInformationSerialNo = GetCharacteristics(_gattInformationService, UuidDefs.DEVICE_INFORMATION_SERIAL_NUMBER_CHARACTERISTIC_UUID);
                        Debug.Assert(_characteristicsInformationSerialNo != null);
                        GattReadResult serialNo = await _characteristicsInformationSerialNo[0].ReadValueAsync();
                        Debug.WriteLine("   Characteristics:                             " + "_characteristicsInformationSerialNo");
                        Debug.WriteLine("       Status:                                  " + serialNo.Status);
                        Debug.WriteLine("       Value:                                   " + Utility.ReadIBuffer2Str(serialNo.Value, true));
                        using (DataReader dataReader = DataReader.FromBuffer(serialNo.Value))
                        {
                            byte[] serialNumBytes = new byte[serialNo.Value.Length];
                            dataReader.ReadBytes(serialNumBytes);
                            var serialNumber = serialNumBytes.BytesToString(serialNumBytes.Length);
                            Debug.WriteLine("       Serial Number:                           " + serialNumber);

                            var buffer = new byte[2];
                            Buffer.BlockCopy(serialNumBytes, 0, buffer, 0, 2);
                            var patternConfig = buffer.BytesToString(buffer.Length);
                            Debug.WriteLine("          Pattern Config:                       " + patternConfig);

                            buffer = new byte[1];
                            Buffer.BlockCopy(serialNumBytes, 2, buffer, 0, 1);
                            var pcbVersion = buffer.BytesToString(buffer.Length);
                            Debug.WriteLine("          PCB Version:                          " + pcbVersion);

                            buffer = new byte[1];
                            Buffer.BlockCopy(serialNumBytes, 3, buffer, 0, 1);
                            var yearCode = buffer.BytesToString(buffer.Length);
                            Debug.WriteLine("          Year Code:                            " + yearCode);

                            buffer = new byte[2];
                            Buffer.BlockCopy(serialNumBytes, 4, buffer, 0, 2);
                            var weekCode = buffer.BytesToString(buffer.Length);
                            Debug.WriteLine("          Week Code:                            " + weekCode);

                            buffer = new byte[5];
                            Buffer.BlockCopy(serialNumBytes, 6, buffer, 0, 5);
                            var serial = buffer.BytesToString(buffer.Length);
                            Debug.WriteLine("          Serial:                               " + serial);

                            buffer = new byte[1];
                            Buffer.BlockCopy(serialNumBytes, 11, buffer, 0, 1);
                            var fwCode = buffer.BytesToString(buffer.Length);
                            Debug.WriteLine("          FW Code:                              " + fwCode);

                            buffer = new byte[1];
                            Buffer.BlockCopy(serialNumBytes, 12, buffer, 0, 1);
                            var size = buffer.BytesToString(buffer.Length);
                            Debug.WriteLine("          Size:                                 " + size);

                            buffer = new byte[2];
                            Buffer.BlockCopy(serialNumBytes, 13, buffer, 0, 2);
                            var color = buffer.BytesToString(buffer.Length);
                            Debug.WriteLine("          Color:                                " + color);

                            buffer = new byte[1];
                            Buffer.BlockCopy(serialNumBytes, 15, buffer, 0, 1);
                            var productCode = buffer.BytesToString(buffer.Length);
                            Debug.WriteLine("          Product Code:                         " + productCode);
                        }
                        Debug.WriteLine("");

                        // Get the Firmware Version characteristics
                        _characteristicsInformationFirmwareVer = GetCharacteristics(_gattInformationService, UuidDefs.DEVICE_INFORMATION_FIRMWARE_REVISION_CHARACTERISTIC_UUID);
                        Debug.Assert(_characteristicsInformationFirmwareVer != null);
                        GattReadResult firmwareVer = await _characteristicsInformationFirmwareVer[0].ReadValueAsync();
                        Debug.WriteLine("   Characteristics:                             " + "_characteristicsInformationFirmwareVer");
                        Debug.WriteLine("       Status:                                  " + firmwareVer.Status);
                        Debug.WriteLine("       Value:                                   " + Utility.ReadIBuffer2Str(firmwareVer.Value, true));
                        using (DataReader dataReader = DataReader.FromBuffer(firmwareVer.Value))
                        {
                            var firmwareVersion = dataReader.ReadString(firmwareVer.Value.Length);
                            Debug.WriteLine("       Firmware Version:                        " + firmwareVersion);
                        }
                        Debug.WriteLine("");

                        // Get the Software Version characteristics
                        _characteristicsInformationSoftwareVer = GetCharacteristics(_gattInformationService, UuidDefs.DEVICE_INFORMATION_SOFTWARE_REVISION_CHARACTERISTIC_UUID);
                        Debug.Assert(_characteristicsInformationSoftwareVer != null);
                        GattReadResult softwareVer = await _characteristicsInformationSoftwareVer[0].ReadValueAsync();
                        Debug.WriteLine("   Characteristics:                             " + "_characteristicsInformationSoftwareVer");
                        Debug.WriteLine("       Status:                                  " + softwareVer.Status);
                        Debug.WriteLine("       Value:                                   " + Utility.ReadIBuffer2Str(softwareVer.Value, true));
                        using (DataReader dataReader = DataReader.FromBuffer(softwareVer.Value))
                        {
                            var softwareVersion = dataReader.ReadString(softwareVer.Value.Length);
                            Debug.WriteLine("       Software Version:                        " + softwareVersion);
                        }
                        Debug.WriteLine("");

                        // Get the Connect Mode characteristics
                        _characteristicsInformationlManufacturer = GetCharacteristics(_gattInformationService, UuidDefs.DEVICE_INFORMATION_MANUFACTURER_NAME_CHARACTERISTIC_UUID);
                        Debug.Assert(_characteristicsInformationlManufacturer != null);
                        GattReadResult manufacturer = await _characteristicsInformationlManufacturer[0].ReadValueAsync();
                        Debug.WriteLine("   Characteristics:                             " + "_characteristicsInformationlManufacturer");
                        Debug.WriteLine("       Status:                                  " + manufacturer.Status);
                        Debug.WriteLine("       Value:                                   " + Utility.ReadIBuffer2Str(manufacturer.Value, true));
                        using (DataReader dataReader = DataReader.FromBuffer(manufacturer.Value))
                        {
                            var manufacture = dataReader.ReadString(manufacturer.Value.Length - 1);
                            Debug.WriteLine("       Manufacturer:                            " + manufacture);
                        }
                        Debug.WriteLine("");
                        break;
                    }
                }
            }
        }

        private async Task InitializeControlService(BluetoothLEDevice btleDevice)
        {
            if (_watcher3.ResultCollection.Any() && _watcher3.EnumCompleted)

            {
                foreach (DeviceInformationDisplay entry in _watcher3.ResultCollection)
                {
                    btleDevice3 = await BluetoothLEDevice.FromIdAsync(entry.Id);

                    // Here we check if we are using the same BTLE address as btleDevice: btleDevice2.BluetoothAddress == btleDevice3.BluetoothAddress
                    if (btleDevice3.BluetoothAddress != btleDevice.BluetoothAddress)
                    {
                        continue;
                    }

                    var guid = new Guid(UuidDefs.JAWBONE_CONTROL_SERVICE_UUID);
                    _gattControlService = btleDevice3.GetGattService(guid);

                    // Get the DateTime characteristics
                    _characteristicsControlDatetime = GetCharacteristics(_gattControlService, UuidDefs.JAWBONE_CONTROL_DATETIME_CHARACTERISTIC_UUID);
                    Debug.Assert(_characteristicsControlDatetime != null);

                    // Get the Connect Mode characteristics
                    _characteristicsControlConnectMode = GetCharacteristics(_gattControlService, UuidDefs.JAWBONE_CONTROL_CONNECT_MODE_CHARACTERISTIC_UUID);
                    Debug.Assert(_characteristicsControlConnectMode != null);

                    break;
                }
            }
        }

        private async Task InitializeGattStreamServiceCharateristics(DeviceInformationDisplay deviceInfoDisp)
        {
            _gattStreamService = await GattDeviceService.FromIdAsync(deviceInfoDisp.Id);
            Debug.WriteLine("\n" + _gattStreamService + ":   " + _gattStreamService.Device.Name + "\n");
            Debug.WriteLine("Getting GATT Services", NotifyType.StatusMessage);

            // Get the Tx characteristic - We will get data from this Characteristics
            _characteristicsStreamTx = GetCharacteristics(_gattStreamService, UuidDefs.GATT_STREAM_TX_CHARACTERISTIC_UUID);
            Debug.Assert(_characteristicsStreamTx != null);


            // Set the Client Characteristic Configuration Descriptor to "Indicate"
            GattCommunicationStatus status = await _characteristicsStreamTx[0].WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);

            // Get the Rx characteristic - We will send data to this Characteristics
            _characteristicsStreamRx = GetCharacteristics(_gattStreamService, UuidDefs.GATT_STREAM_RX_CHARACTERISTIC_UUID);
            Debug.Assert(_characteristicsStreamRx != null);

            //Debug_ListCharacteristics();

            // This is the ValueChanged handler: Set an handler to the characteristicsTx
            _characteristicsStreamTx[0].ValueChanged += CharacteristicsTx_ValueChanged;
        }

        private void CharacteristicsTx_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            if (_isBusyCharacteristicsTxValueChanged > 0)
            {
                return;
            }
            _isBusyCharacteristicsTxValueChanged = Interlocked.Increment(ref _isBusyCharacteristicsTxValueChanged);

            byte[] buffer = Utility.ReadBuffer(args.CharacteristicValue);

            // Check out of sequence
            if ((byte)(_sequenceNumberIn + 1) != buffer[0])
            {
                Debug.WriteLine("!!! Error, out of sequence packet !!!");
                Debug.WriteLine("        Expected: " + string.Format("0x" + (_sequenceNumberIn + 1).ToString("X2")));
                Debug.WriteLine("        Recieved: " + string.Format("0x" + buffer[0].ToString("X2")));
            }
            _sequenceNumberIn = buffer[0];

            //
            // Still pairing - Data not Encrypted
            //
            if (_isConnecting)
            {
                _characteristicsTxDataIn?.AddRange(buffer);

                // Here we need to decode the data 
                DecodePairingResponse(buffer);

                _isBusyCharacteristicsTxValueChanged = Interlocked.Decrement(ref _isBusyCharacteristicsTxValueChanged);
                return;
            }

            //
            // Done pairing, store packets into _queuePackets queue - Data Encryped
            //
            if (_isConnecting == false)
            {
                // Here we need first to glue packets together so we have one full packet
                //Debug.WriteLine( "CharacteristicsTx_ValueChanged\n" );
                ReadPacketIn(buffer);

                _isBusyCharacteristicsTxValueChanged = Interlocked.Decrement(ref _isBusyCharacteristicsTxValueChanged);
            }
        }

        private void ReadPacketIn(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        private async void DecodePairingResponse(byte[] buffer)
        {
            if (_characteristicsTxDataIn.Count < 1 + BtleLinkTypes.RESPONSE_HEADER_SIZE)
            {
                _characteristicsTxDataIn.Clear();
                return;
            }

            Debug.WriteLine("\nPairing - DecodePairingResponse: " + Utility.ByteArray2String(buffer, true));


            ///////////////////////
            // Decode the Header //
            /////////////////////// 
            byte messageType = (byte)_characteristicsTxDataIn[1 + (int)HdrOffset.MessageType];
            byte requestStatus = (byte)_characteristicsTxDataIn[1 + (int)HdrOffset.Flags];
            byte trasactionId = (byte)_characteristicsTxDataIn[1 + (int)HdrOffset.TransactionID];
            byte payloadSize = (byte)_characteristicsTxDataIn[1 + (int)HdrOffset.PayloadSize];

            var response = new Response
            {
                Payload = new byte[payloadSize],
                Header =
                    {
                    ResponseType = messageType,
                    Flags = (ResponseStatus)requestStatus,
                    TransactionId = trasactionId,
                    PayloadSize = payloadSize
                    }
            };


            /////////////////////
            // Simple Response //
            /////////////////////
            if (payloadSize <= BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE)
            {
                Buffer.BlockCopy(_characteristicsTxDataIn.ToArray(typeof(byte)), 1 + BtleLinkTypes.RESPONSE_HEADER_SIZE, response.Payload, 0, payloadSize);

                // dump the data
#if DEBUG
                DumpResponse(response);
#endif

                //dataIn.Clear();
                _characteristicsTxDataIn.RemoveRange(0, 1 + BtleLinkTypes.RESPONSE_HEADER_SIZE + payloadSize);

                lock (lockQueueTransactions)
                {
                    if (QueueTransactions.Any())
                    {
                        QueueTransactions.RemoveAt(0);
                    }
                }

                if (_pairingSequenceState == PairingMsgTypes.GetProtocolVersion)
                {
                    // 2. Key Exchange
                    Connecting_KeyExchange_2();
                }

                if (_pairingSequenceState == PairingMsgTypes.KeyExchange)
                {
                    // 3. Authenticate
                    Connecting_Authenticate_3();
                }

                if (_pairingSequenceState == PairingMsgTypes.RespondToChallenge)
                {
                    // 5. EstablishSecureChannel
                    Connecting_EstablishSecureChannel_5();
                }

                _neededBytes = 0;
                return;
            }


            ////////////////////
            // Multi Response //
            ////////////////////
            if (_characteristicsTxDataIn.Count <= BtleLinkTypes.BTLE_PACKET_SIZE)
            {
                return;
            }

            _neededBytes = payloadSize - BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE;
            if (_neededBytes != buffer.Length - 1)
            {
                Debug.WriteLine("!!!!!  Error in the second packet  !!!!!    Expected: " + _neededBytes + "  -- Actual: " + (buffer.Length - 1));

                Buffer.BlockCopy(_characteristicsTxDataIn.ToArray(typeof(byte)), 1 + BtleLinkTypes.RESPONSE_HEADER_SIZE, response.Payload, 0, BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE);
                Buffer.BlockCopy(buffer, 1, response.Payload, BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE, Math.Min(_neededBytes, buffer.Length - 1));

                _tmpBuffer = new byte[buffer.Length - _neededBytes];
                _tmpBuffer[0] = buffer[0];
                Buffer.BlockCopy(buffer, 1 + _neededBytes, _tmpBuffer, 1, buffer.Length - 2);

                _characteristicsTxDataIn.Clear();
            }
            else
            {
                Buffer.BlockCopy(_characteristicsTxDataIn.ToArray(typeof(byte)), 1 + BtleLinkTypes.RESPONSE_HEADER_SIZE, response.Payload, 0, BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE);
                Buffer.BlockCopy(_characteristicsTxDataIn.ToArray(typeof(byte)), BtleLinkTypes.BTLE_PACKET_SIZE + 1, response.Payload, BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE, _neededBytes);
                _characteristicsTxDataIn.RemoveRange(0, BtleLinkTypes.BTLE_PACKET_SIZE + 1 + _neededBytes);
            }

#if DEBUG
            DumpResponse(response);
#endif

            // Response for: Authenticate
            if (_pairingSequenceState == PairingMsgTypes.Authenticate)
            {
                Debug.Assert(_key != null);

                IBuffer keyBuffer = CryptographicBuffer.CreateFromByteArray(_key);
                _bandResponse = response.Payload;
                IBuffer BR_Buffer = CryptographicBuffer.CreateFromByteArray(_bandResponse);
                SymmetricKeyAlgorithmProvider aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);

                // Creates a symmetric key
                // https://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k(Windows.Security.Cryptography.Core.SymmetricKeyAlgorithmProvider.CreateSymmetricKey);k(TargetFrameworkMoniker-.NETCore,Version%3Dv5.0);k(DevLang-csharp)&rd=true 
                // https://msdn.microsoft.com/en-us/library/windows/apps/xaml/br241541(v=win.10).aspx?appid=dev14idef1&l=en-us&k=k(windows.security.cryptography.core.symmetrickeyalgorithmprovider.createsymmetrickey)%3bk(targetframeworkmoniker-.netcore,version%3dv5.0)%3bk(devlang-csharp)&rd=true
                CryptographicKey cryptographicKey = aesProvider.CreateSymmetricKey(keyBuffer);

                // Decrypt the data
                IBuffer phoneChallengeBandChallengeBuffer = CryptographicEngine.Decrypt(cryptographicKey, BR_Buffer, null);

                byte[] phoneChallengeBandChallenge;
                CryptographicBuffer.CopyToByteArray(phoneChallengeBandChallengeBuffer, out phoneChallengeBandChallenge);
                Debug.WriteLine("\nBand response to PhoneChallenge is: ");
                Debug.WriteLine("\n    PhoneChallenge_BandChallenge = " + Utility.ByteArray2String(phoneChallengeBandChallenge, true));

                byte[] phoneChallengeReceived = new byte[BtleLinkTypes.ENCR_CHALLENGE_SIZE];
                Buffer.BlockCopy(phoneChallengeBandChallenge, 0, phoneChallengeReceived, 0, BtleLinkTypes.ENCR_CHALLENGE_SIZE);
                if (!_phoneChallenge.SequenceEqual(phoneChallengeReceived))
                {
                    Debug.WriteLine("PhoneChallenges not matched. PhoneChallenge = {0}, PhoneChallenge_Received = {1}\n",
                                            Utility.ByteArray2String(_phoneChallenge),
                                            Utility.ByteArray2String(phoneChallengeReceived));
                    throw new Exception("JawboneErrorCodes.DECRYPTION_FAILED");
                }
                Debug.WriteLine("  +++ PhoneChallenges matched! +++\n");

                // BandChallenge: Band challenge, random non-zero 8 bytes generated by the band
                _bandChallenge = new byte[BtleLinkTypes.ENCR_CHALLENGE_SIZE];
                Buffer.BlockCopy(phoneChallengeBandChallenge, BtleLinkTypes.ENCR_CHALLENGE_SIZE, _bandChallenge, 0, BtleLinkTypes.ENCR_CHALLENGE_SIZE);

                lock (lockQueueTransactions)
                {
                    if (QueueTransactions.Any())
                    {
                        QueueTransactions.RemoveAt(0);
                    }
                }

                // 4. Prepare the next command: RespondToChallenge
                Connecting_RespondToChallenge_4(keyBuffer, phoneChallengeBandChallengeBuffer);

                return;
            }

            if (_pairingSequenceState == PairingMsgTypes.EstablishSecureChannel)
            {
                _deviceSeed = new byte[BtleLinkTypes.ENCRYPTED_BLOCK_SIZE];
                Buffer.BlockCopy(response.Payload, 0, _deviceSeed, 0, BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);

                // 6. Initialize Stream Encryptor
                Connecting_InitStreamEncryptor_6();

                // We don't need the dataIn anymore
                _characteristicsTxDataIn.Clear();

                _pairingSequenceState = PairingMsgTypes.BandConnected;
                _isConnecting = false;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => bandData.IsConnected = true);

                if (_tmpBuffer != null)
                {
                    //Debug.WriteLine( "  +++ PairingMsgTypes.EstablishSecureChannel +++\n" );
                    ReadPacketIn(_tmpBuffer);
                    _tmpBuffer = null;
                }

                //await _rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                //{
                //    AppBarButtonAlert.IsEnabled = true;
                //    AppBarButtonDateTime.IsEnabled = true;
                //    AppBarButtonTest.IsEnabled = true;
                //    AppBarButtonSetting.IsEnabled = true;
                //});
            }
        }

        private void Connecting_GetProtocolVersion_1()
        {
            Debug.WriteLine("\n\n== 1. Get Protocol Version");
            _messageType = (byte)PairingMsgTypes.GetProtocolVersion;        // 0x00
            _flags = 0x00;
            _payloadSize = 0x00;
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, new byte[0], false);
            SaveTransaction(transaction);

            Debug.WriteLine(string.Format("Protocol Version --- {0}", NotifyType.StatusMessage.ToString()));
        }

        // 2. Save Key Exchange
        private void Connecting_KeyExchange_2()
        {
            Debug.WriteLine("\n\n== 2. Key Exchange");
            _messageType = (byte)PairingMsgTypes.KeyExchange;               // 0x03
            _flags = 0x00;
            _key = CryptographicBuffer.GenerateRandom(BtleLinkTypes.ENCRYPTED_BLOCK_SIZE).ToArray();

            Debug.Assert(_key != null);

            byte[] protocolKeyExchangePayload = new byte[_key.Length];
            Buffer.BlockCopy(_key, 0, protocolKeyExchangePayload, 0, _key.Length);
            _payloadSize = (byte)protocolKeyExchangePayload.Length;
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, protocolKeyExchangePayload, false);
            SaveTransaction(transaction);

            Debug.WriteLine(string.Format("Key Exchange --- {0} ", NotifyType.StatusMessage.ToString()));
        }

        // 3. Authentication
        private void Connecting_Authenticate_3()
        {
            Debug.WriteLine("\n\n== 3. Authentication");
            _phoneChallenge = CryptographicBuffer.GenerateRandom(BtleLinkTypes.ENCR_CHALLENGE_SIZE).ToArray();

            byte[] phoneChallengeZ = new byte[_phoneChallenge.Length + BtleLinkTypes.ENCR_CHALLENGE_SIZE];
            Buffer.BlockCopy(_phoneChallenge, 0, phoneChallengeZ, 0, _phoneChallenge.Length);

            IBuffer phoneChallengeZBuffer = CryptographicBuffer.CreateFromByteArray(phoneChallengeZ);
            SymmetricKeyAlgorithmProvider aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);

            // Creates a symmetric key
            // https://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k(Windows.Security.Cryptography.Core.SymmetricKeyAlgorithmProvider.CreateSymmetricKey);k(TargetFrameworkMoniker-.NETCore,Version%3Dv5.0);k(DevLang-csharp)&rd=true 
            // https://msdn.microsoft.com/en-us/library/windows/apps/xaml/br241541(v=win.10).aspx?appid=dev14idef1&l=en-us&k=k(windows.security.cryptography.core.symmetrickeyalgorithmprovider.createsymmetrickey)%3bk(targetframeworkmoniker-.netcore,version%3dv5.0)%3bk(devlang-csharp)&rd=true
            CryptographicKey cryptographicKey = aesProvider.CreateSymmetricKey(CryptographicBuffer.CreateFromByteArray(_key));

            // Encrypt the data
            IBuffer phoneChallengeZEncrypted = CryptographicEngine.Encrypt(cryptographicKey, phoneChallengeZBuffer, null);

            byte[] phoneChallengeZBytes;
            CryptographicBuffer.CopyToByteArray(phoneChallengeZEncrypted, out phoneChallengeZBytes);

            _maskedPhoneChallengeZ = new byte[BtleLinkTypes.ENCRYPTED_BLOCK_SIZE];
            Buffer.BlockCopy(phoneChallengeZBytes, 0, _maskedPhoneChallengeZ, 0, BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);

            SendAuthenticateCmd();
        }

        private void SendAuthenticateCmd()
        {
            _messageType = (byte)PairingMsgTypes.Authenticate;              // 0x04
            _flags = 0x00;
            byte[] authenticationRequestPayload = _maskedPhoneChallengeZ;
            _payloadSize = (byte)authenticationRequestPayload.Length;
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, authenticationRequestPayload, false);
            SaveTransaction(transaction);

            Debug.WriteLine(string.Format("Authenticate  {0}", NotifyType.StatusMessage.ToString()));
        }

        // 4. RespondToChallenge
        private void Connecting_RespondToChallenge_4(IBuffer keyBuffer, IBuffer phoneChallengeBandChallengeBuffer)
        {
            Debug.WriteLine("\n\n== 4. Respond To Challenge");

            //CheckErrorCode( true );

            Debug.WriteLine("Sending response to BandResponse");
            Debug.WriteLine("    BandChallenge = " + Utility.ByteArray2String(_bandChallenge, true));

            Debug.Assert(keyBuffer != null && keyBuffer.Length > 0);
            Debug.Assert(phoneChallengeBandChallengeBuffer != null && phoneChallengeBandChallengeBuffer.Length > 0);
            Debug.Assert(_bandChallenge != null && _bandChallenge.Length > 0);

            byte[] zBandChallenge = new byte[BtleLinkTypes.ENCR_CHALLENGE_SIZE + _bandChallenge.Length];
            Buffer.BlockCopy(_bandChallenge, 0, zBandChallenge, BtleLinkTypes.ENCR_CHALLENGE_SIZE, _bandChallenge.Length);
            Debug.WriteLine("    Z_BandChallenge = " + Utility.ByteArray2String(zBandChallenge, true));

            SymmetricKeyAlgorithmProvider aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);

            // Creates a symmetric key
            // https://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k(Windows.Security.Cryptography.Core.SymmetricKeyAlgorithmProvider.CreateSymmetricKey);k(TargetFrameworkMoniker-.NETCore,Version%3Dv5.0);k(DevLang-csharp)&rd=true 
            // https://msdn.microsoft.com/en-us/library/windows/apps/xaml/br241541(v=win.10).aspx?appid=dev14idef1&l=en-us&k=k(windows.security.cryptography.core.symmetrickeyalgorithmprovider.createsymmetrickey)%3bk(targetframeworkmoniker-.netcore,version%3dv5.0)%3bk(devlang-csharp)&rd=true
            CryptographicKey cryptographicKey = aesProvider.CreateSymmetricKey(keyBuffer);

            // Encrypt the data
            IBuffer PR_Buffer = CryptographicEngine.Encrypt(cryptographicKey, phoneChallengeBandChallengeBuffer, null);
            _phoneResponse = Utility.Xor(PR_Buffer.ToArray(), zBandChallenge, zBandChallenge.Length);
            Debug.WriteLine("    PhoneResponse = " + Utility.ByteArray2String(_phoneResponse, true));

            _messageType = (byte)PairingMsgTypes.RespondToChallenge;        // 0x05
            _flags = 0x00;
            byte[] challengePayload = _phoneResponse;
            _payloadSize = (byte)challengePayload.Length;
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, challengePayload, false);
            SaveTransaction(transaction);


            Debug.WriteLine(string.Format("Respond To Challenge -   {0}", NotifyType.StatusMessage.ToString()));
        }

        // 5. EstablishSecureChannel
        private void Connecting_EstablishSecureChannel_5()
        {
            Debug.WriteLine("\n\n== 5. Establish Secure Channel");
            Debug.WriteLine("\nEstablishing secure channel... Key = " + Utility.ByteArray2String(_key, true));

            _phoneSeed = CryptographicBuffer.GenerateRandom(BtleLinkTypes.ENCRYPTED_BLOCK_SIZE).ToArray();

            _messageType = (byte)PairingMsgTypes.EstablishSecureChannel;    // 0x06
            _flags = 0x00;
            byte[] protocolEstablishSecureChannel = new byte[BtleLinkTypes.ENCRYPTED_BLOCK_SIZE];
            Buffer.BlockCopy(_phoneSeed, 0, protocolEstablishSecureChannel, 0, BtleLinkTypes.ENCRYPTED_BLOCK_SIZE);
            _payloadSize = (byte)protocolEstablishSecureChannel.Length;
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, protocolEstablishSecureChannel, false);
            SaveTransaction(transaction);

            Debug.WriteLine(string.Format("Establish Secure Channel  --  {0}", NotifyType.StatusMessage.ToString()));
        }


        // 6. Initialize Stream Encryptor
        private void Connecting_InitStreamEncryptor_6()
        {
#if DEBUG
            var msg = new StringBuilder();

            msg.AppendFormat("\n\n== 6. Init Stream Encryptor\n");

            Debug.Assert(_key != null && _key.Length > 0);
            Debug.Assert(_deviceSeed != null && _deviceSeed.Length > 0);
            Debug.Assert(_phoneSeed != null && _phoneSeed.Length > 0);
#endif

            _streamEncryptor = new StreamEncryptor(_key, _deviceSeed, _phoneSeed);

#if DEBUG
            msg.AppendFormat("    END - Connecting_InitStreamEncryptor_6:\n");
            msg.AppendFormat("        key:        " + Utility.ByteArray2String(_key) + "\n");
            msg.AppendFormat("        DeviceSeed: " + Utility.ByteArray2String(_deviceSeed) + "\n");
            msg.AppendFormat("        PhoneSeed:  " + Utility.ByteArray2String(_phoneSeed) + "\n");
            msg.AppendFormat("\n\n");

            Debug.WriteLine(msg);
#endif
        }
        private IReadOnlyList<GattCharacteristic> GetCharacteristics(GattDeviceService _gattStreamService, string GATT_STREAM_TX_CHARACTERISTIC_UUID)
        {
            var characteristics = _gattStreamService.GetCharacteristics(new Guid(GATT_STREAM_TX_CHARACTERISTIC_UUID));
            return characteristics;
        }

        private async void BtleDevice2_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            var msg = "Connection Status Changed - " + btleDevice2.ConnectionStatus;
            Debug.WriteLine(msg);
            /*rootPage.NotifyUser(msg, NotifyType.StatusMessage);*/

            // Set the BandData IsConnected status
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => bandData.IsConnected = btleDevice2.ConnectionStatus == BluetoothConnectionStatus.Disconnected);

            // We are disconnected, intialize the variables
            if (btleDevice2.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                _isDisconnected = true;

                _characteristicsStreamTx[0].ValueChanged -= CharacteristicsTx_ValueChanged;

                //
                // Initialize parameters for Encrypted communication
                //
                _isConnecting = true;
                _characteristicsTxDataIn.Clear();
                _neededBytes = 0;
                _rawPacket = null;

                _tmpBuffer = null;
                _reminder = null;

                _sequenceNumberIn = 0xFF;
                _sequenceNumberOut = 0x00;                              //

                _isBusyCharacteristicsTxValueChanged = 0;
                _multiSendBusy = 0;
                _simpleSendBusy = 0;
                _decodeResponseBusy = 0;

                QueueTransactions.Clear();
                QueueResponsePackets.Clear();

                _pairingSequenceState = PairingMsgTypes.BandNotConnected;

                lockQueueTransactions = new object();                   //
                lockQueueResponsePackets = new object();                //

                //   Utility.VibratePhone();
            }

            // We are connected and were disconnected before.... Start a connection sequence 
            if ((btleDevice2.ConnectionStatus == BluetoothConnectionStatus.Connected) && _isDisconnected)
            {
                _isDisconnected = false;
                //Utility.VibratePhone();

                // Initiate the re-connection
                await InitializeGattStreamServiceCharateristics(_deviceInformationDisplayConnect);
                await InitializeControlService(btleDevice2);          //
                await InitializeInformationService(btleDevice2);      //

                // Start a connection sequence 
                //StartConnectionSequence();                            // 1. - Works
                Connecting_EstablishSecureChannel_5();                  // 5. - Works
            }
        }



        private void SaveTransaction(Transaction transaction)
        {
            transaction.NoPackets = transaction.Payload.Length <= BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE ? 1 : transaction.CalcNoPackets();
            transaction.Status = TransactionStatus.TransactionWait;

            _dictionaryTransactionIds.Add(transaction.Header.TransactionId, (byte)transaction.Header.MessageType);
            _transactionId++;

#if DEBUG
            // For debug
            DumpTransaction(transaction);
#endif

            lock (lockQueueTransactions)
            {
                QueueTransactions.Add(transaction);
            }
        }
        private void DumpTransaction(Transaction transaction)
        {
#if DEBUG
            var msg = new StringBuilder();

            msg.AppendFormat("    Transaction Command: \n");
            msg.AppendFormat("        Header:\n");
            msg.AppendFormat("            NoPackets:        " + transaction.NoPackets + "\n");
            msg.AppendFormat("            MessageType:      " + "0x" + ((byte)transaction.Header.MessageType).ToString("X2") + "\n");
            msg.AppendFormat("            Flags:            " + "0x" + transaction.Header.Flags.ToString("X2") + "\n");
            msg.AppendFormat("            TrasactionId:     " + "0x" + transaction.Header.TransactionId.ToString("X2") + "\n");
            msg.AppendFormat("            PayloadSize:      " + transaction.Header.PayloadSize + "\n");
            msg.AppendFormat("        Payload:\n");
            msg.AppendFormat("            Data:             " + Utility.ByteArray2String(transaction.Payload, true) + "\n");

            Debug.WriteLine(msg);
#endif
        }

        private void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            args.Accept();
        }



        private void StartWatcher()
        {
            ResultCollection.Clear();

            // Kind is specified in the selector info
            deviceWatcher = DeviceInformation.CreateWatcher(
                "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"",
                null, // don't request additional properties for this sample
                DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher

            InitilizedWatcher();

            deviceWatcher.Start();


            //var aqsFilter = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(UuidDefs.DEVICE_INFORMATION_SERVICE_UUID));
            //_watcher4 = new BtleWatcher(this, "W4");
            //_watcher4.InitializeBtleWatcher(aqsFilter);

            var aqsFilter = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(UuidDefs.JAWBONE_CONTROL_SERVICE_UUID));
            _watcher3 = new BtleWatcher(this, "W3");
            _watcher3.InitializeBtleWatcher(aqsFilter);
        }

        private void Wacher2EventFired(object sender, BtleWatcherEventsArgs e)
        {
            Debug.WriteLine("Wacher2EventFired: " + Enum.GetName(typeof(WatcherEvents), e.Event));
        }

        private void InitilizedWatcher()
        {
            handlerAdded = new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    var info = new DeviceInformationDisplay(deviceInfo);
                    if (!info.IsPaired && info.CanPair)
                        ResultCollection.Add(info);
                });
            });
            deviceWatcher.Added += handlerAdded;

            handlerUpdated = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    // Find the corresponding updated DeviceInformation in the collection and pass the update object
                    // to the Update method of the existing DeviceInformation. This automatically updates the object
                    // for us.
                    foreach (DeviceInformationDisplay deviceInfoDisp in ResultCollection)
                    {
                        if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            deviceInfoDisp.Update(deviceInfoUpdate, "W1");

                            //// If the item being updated is currently "selected", then update the pairing buttons
                            ////DeviceInformationDisplay selectedDeviceInfoDisp = (DeviceInformationDisplay)resultsListView.SelectedItem;
                            //if (deviceInfoDisp == selectedDeviceInfoDisp)
                            //{
                            //    //UpdatePairingButtons();
                            //}
                            break;
                        }
                    }
                });
            });
            deviceWatcher.Updated += handlerUpdated;

            handlerRemoved = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    // Find the corresponding DeviceInformation in the collection and remove it
                    foreach (DeviceInformationDisplay deviceInfoDisp in ResultCollection)
                    {
                        if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            ResultCollection.Remove(deviceInfoDisp);
                            break;
                        }
                    }
                });
            });
            deviceWatcher.Removed += handlerRemoved;

            handlerEnumCompleted = new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    foreach (var item in ResultCollection)
                    {
                        Debug.WriteLine(String.Format("Name         = {0}", item.Name));
                        Debug.WriteLine(String.Format("Id           = {0}", item.Id));
                        Debug.WriteLine(String.Format("NameIsPaired = {0}", item.IsPaired));
                        listView.ItemsSource = ResultCollection.Select(s => new { s.Name, s.Id });
                        listView.DisplayMemberPath = "Name";
                        listView.SelectedValuePath = "Id";
                    }


                    enumberationCompletedEvent.StopEventHandle += EnumberationCompletedEvent_StopEventHandle;
                    EnumberationCompletedEventArgs args = new EnumberationCompletedEventArgs();
                    args.Button = btnStart;
                    args.ProgressRing = progressRing;   
                    args.TextBlock = lblComplete;
                    enumberationCompletedEvent.EnumberationCompleted(args);
                });
            });
            deviceWatcher.EnumerationCompleted += handlerEnumCompleted;

            handlerStopped = new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    //rootPage.NotifyUser(
                    //    String.Format("{0} devices found. Watcher {1}.",
                    //        ResultCollection.Count,
                    //        DeviceWatcherStatus.Aborted == watcher.Status ? "aborted" : "stopped"),
                    //    NotifyType.StatusMessage);
                });
            });
            deviceWatcher.Stopped += handlerStopped;
        }

        private void EnumberationCompletedEvent_StopEventHandle(object sender, EnumberationCompletedEventArgs e)
        {
            e.Button.IsEnabled = true;
            e.ProgressRing.IsActive = false;
            e.ProgressRing.Visibility = Visibility.Collapsed;
            e.TextBlock.Text = "--Scan Completed--";
        }

        private void DumpResponse(Response response)
        {
#if DEBUG
            var msg = new StringBuilder();

            msg.AppendFormat("    Response Data: " + Utility.ByteArray2String((byte[])_characteristicsTxDataIn.ToArray(typeof(byte)), true) + "\n");
            msg.AppendFormat("        Header:\n");
            msg.AppendFormat("            ResponseType:     " + Enum.GetName(typeof(MessageReponseTypes), response.Header.ResponseType) + "\n");
            msg.AppendFormat("            RequestStatus:    " + Enum.GetName(typeof(ResponseStatus), response.Header.Flags) + "\n");
            msg.AppendFormat("            TrasactionId:     " + "0x" + response.Header.TransactionId.ToString("X2") + "\n");
            msg.AppendFormat("            PayloadSize:      " + response.Header.PayloadSize + "\n");
            msg.AppendFormat("        Payload:\n");
            msg.AppendFormat("            Data:             " + Utility.ByteArray2String(response.Payload, true) + "\n");

            Debug.WriteLine(msg);
#endif
        }

        private void StopWatcher()
        {
            if (null != deviceWatcher)
            {
                // First unhook all event handlers except the stopped handler. This ensures our
                // event handlers don't get called after stop, as stop won't block for any "in flight" 
                // event handler calls.  We leave the stopped handler as it's guaranteed to only be called
                // once and we'll use it to know when the query is completely stopped. 
                deviceWatcher.Added -= handlerAdded;
                deviceWatcher.Updated -= handlerUpdated;
                deviceWatcher.Removed -= handlerRemoved;
                deviceWatcher.EnumerationCompleted -= handlerEnumCompleted;

                if (DeviceWatcherStatus.Started == deviceWatcher.Status ||
                    DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status)
                {
                    deviceWatcher.Stop();
                }
            }
        }
    }

    public static class Utility
    {
        public static ushort ConvertUuidToShortId(Guid uuid)
        {
            // Get the short Uuid
            byte[] bytes = uuid.ToByteArray();
            ushort shortUuid = (ushort)(bytes[0] | (bytes[1] << 8));
            return shortUuid;
        }

        public static string ByteArray2String(byte[] byteArray, bool isLen = false)
        {
            var message = String.Empty;
            if (isLen)
            {
                message = "[" + byteArray.Length + "] => ";
            }

            for (var i = 0; i < byteArray.Length; i++)
            {
                message += byteArray[i].ToString("X2");
                if (i != byteArray.Length - 1)
                {
                    message += "-";
                }
            }

            return message;
        }

        public static byte[] ReadBuffer(IBuffer buffer)
        {
            byte[] bytes = new byte[buffer.Length];
            DataReader.FromBuffer(buffer).ReadBytes(bytes);
            return bytes;
        }
        public static byte[] Xor(byte[] data1, byte[] data2, int len)
        {
            Debug.Assert(data1 != null && data1.Length >= len);
            Debug.Assert(data2 != null && data2.Length >= len);

            byte[] response = new byte[len];

            for (int i = 0; i < len; i++)
            {
                response[i] = (byte)(data1[i] ^ data2[i]);
            }

            return response;
        }
        public static string ReadIBuffer2Str(IBuffer buffer, bool isLen = false)
        {
            byte[] bytes = ReadBuffer(buffer);
            string strData = ByteArray2String(bytes, isLen);
            return strData;
        }
    }
}
