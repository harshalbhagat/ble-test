using BTLE.Cmds;
using BTLE.Exceptions;
using BTLE.Misc;
using BTLE.Types;
using BTLE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Buffer = System.Buffer;

// ReSharper disable UnusedParameter.Local
// ReSharper disable NotAccessedField.Local
// ReSharper disable RedundantEmptyDefaultSwitchBranch
// ReSharper disable RedundantExtendsListEntry
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedVariable
// ReSharper disable RedundantStringFormatCall
// ReSharper disable RedundantJumpStatement
// ReSharper disable UnusedMember.Global
// ReSharper disable TooWideLocalVariableScope
// ReSharper disable RedundantVerbatimPrefix          

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

// Bluetooth  - https://msdn.microsoft.com/en-us/library/windows/apps/mt270288.aspx
// GATT       - https://msdn.microsoft.com/en-us/library/windows/apps/mt185618.aspx
// RFCOMM     - https://msdn.microsoft.com/en-us/library/windows/apps/mt270289.aspx    

// frame.Navigate
// https://channel9.msdn.com/Series/Windows-10-development-for-absolute-beginners/UWP-019-Working-with-Navigation     

//
//
// Needs to be developed:
// ======================
// 1. Develop Connect/Reconnect when the band is out of range                                   - Done
// 2. User Events                                                                               -
// 3. Display the Ticks on the UI                                                               - Done
// 4. OTA code                                                                                  -
// 5. Save the LemondTickRecord into a Tick Records Database                                    - Done
// 6. Fill Device State (LemondDevice.cs)                                                       - Partial
// 7. Device Information Service (DEVICE_INFORMATION_SERVICE_UUID, DeviceInformationService.cs) - Done
//
// 

namespace BTLE
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly App _app = Application.Current as App;

        private static MainPage _rootPage;

        private IAsyncAction _workItemTransaction;
        private IAsyncAction _workItemDecrypt;
        private readonly int _threadPoolSleepTime = 10;

        private static StreamEncryptor _streamEncryptor;

        private readonly JawboneErrorCodes errorCode = JawboneErrorCodes.BLUETOOTH_NOT_ENABLED;

        private BluetoothLEDevice btleDevice2;
        private BluetoothLEDevice btleDevice3;
        private BluetoothLEDevice btleDevice4;

        private DeviceWatcher _deviceWatcher1;
        private BtleWatcher _watcher2;
        private BtleWatcher _watcher3;
        private BtleWatcher _watcher4;

        private BluetoothLEAdvertisementWatcher _advertisementWatcher;


        private DeviceInformationDisplay _deviceInformationDisplayPair;
        private DeviceInformationDisplay _deviceInformationDisplayUnpair;
        private DeviceInformationDisplay _deviceInformationDisplayConnect;


        private bool _isDisconnected;                                                                                                                                   // ** Needed for Re-Connect ** //

        private int _simpleSendBusy;                                                                                                                                    // ** Needed for Re-Connect ** //
        private int _multiSendBusy;                                                                                                                                     // ** Needed for Re-Connect ** //
        private int _decodeResponseBusy;                                                                                                                                // ** Needed for Re-Connect ** //

        private byte _messageType;
        private byte _flags;
        private byte _transactionId = 0xF1;
        private byte _payloadSize;

        private static byte _sequenceNumberIn = 0xFF;                                                                                                                   // ** Needed for Re-Connect ** //
        private static byte _sequenceNumberOut;


        private DispatcherTimer _timer;


        private static PairingMsgTypes _pairingSequenceState = PairingMsgTypes.BandNotConnected;                                                                        // ** Needed for Re-Connect ** //
        private static bool _isConnecting;                                                                                                                              // ** Needed for Re-Connect ** //

        private static bool _isLongTransaction;

        private byte[] _phoneChallenge = new byte[0];
        private byte[] _bandResponse = new byte[0];
        private byte[] _phoneResponse = new byte[0];
        private byte[] _bandChallenge = new byte[0];
        private byte[] _key = new byte[0];
        private byte[] _phoneSeed = new byte[0];
        private byte[] _deviceSeed = new byte[0];
        private byte[] _maskedPhoneChallengeZ = new byte[0];


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


        private TypedEventHandler<DeviceWatcher, DeviceInformation> _handlerAdded1;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> _handlerUpdated1;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> _handlerRemoved1;
        private TypedEventHandler<DeviceWatcher, object> _handlerEnumCompleted1;
        private TypedEventHandler<DeviceWatcher, object> _handlerStopped1;


        private static readonly ObservableCollection<Transaction> QueueTransactions = new ObservableCollection<Transaction>();                                    // ** Needed for Re-Connect ** //
        private static readonly ObservableCollection<RawPacket> QueueResponsePackets = new ObservableCollection<RawPacket>();                                           // ** Needed for Re-Connect ** //

        private object lockQueueTransactions = new object();                                                                                                            // ** Needed for Re-Connect ** //
        private object lockQueueResponsePackets = new object();                                                                                                         // ** Needed for Re-Connect ** //


        private RawPacket _rawPacket;                                                                                                                                   // ** Needed for Re-Connect ** //
        private byte[] _tmpBuffer;                                                                                                                                      // ** Needed for Re-Connect ** //
        private byte[] _reminder;                                                                                                                                       // ** Needed for Re-Connect ** //

        readonly ArrayList _characteristicsTxDataIn = new ArrayList();                                                                                                  // ** Needed for Re-Connect ** //
        private int _neededBytes;                                                                                                                                       // ** Needed for Re-Connect ** //

        private int _isBusyCharacteristicsTxValueChanged;                                                                                                               // ** Needed for Re-Connect ** //

        private ConnectionSpeed _inProgressSpeedChange = ConnectionSpeed.None;


        // Dictionary definitions
        private readonly Dictionary<uint, DateTime> _dictionaryEpochTimes = new Dictionary<uint, DateTime>();
        private readonly Dictionary<byte, byte> _dictionaryTransactionIds = new Dictionary<byte, byte>();


        private static readonly ObservableCollection<TickRecord> TickRecords = new ObservableCollection<TickRecord>();

        private readonly ObservableCollection<DeviceInformationDisplay> ResultCollection = new ObservableCollection<DeviceInformationDisplay>();


        private LemondSettingsSyncVersions _lemondSettingsSyncVersions;
        private TickRequestBeginTransaction _tickRequestBeginTransaction;
        private bool _isTickRequestBeginTransaction;


        private const byte MAX_EPOCH_COUNT = 5;
        private const byte TICK_RECORD_BATCH_MIN_COUNT = 1;
        private const byte TICK_RECORD_BATCH_MAX_COUNT = 8;


        private readonly BandData bandData = new BandData();


        private readonly string[] _bandType =
            {
                "NA", "Lemond", "Spitz", "Thorpe", "Phelps"
            };


        private static DeviceSelectorInfo BluetoothLE => new DeviceSelectorInfo
        {
            // Enumeration Definitions for selector:
            // https://msdn.microsoft.com/en-us/library/windows/apps/mt187356.aspx
            // https://msdn.microsoft.com/en-us/library/windows/apps/mt187348.aspx
            DisplayName = "Bluetooth LE",
            Selector = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"", // Indicates the protocol used to discover this AssocationEndpoint device.
            Kind = DeviceInformationKind.AssociationEndpoint
        };


        public MainPage()
        {
            InitializeComponent();

            _rootPage = this;

            // All the BLTE device around
            //ResultCollection = new ObservableCollection<DeviceInformationDisplay>();

            // A thread to send the commands to the Band (Stream Service)
            // Manjit Commented it for time-being
            CreateThreadPoolSendStreamTransactionWorkItem();

            // A thread to decode the messages coming from the Band (Stream Service)
            // Manjit Commented it for time-being
            CreateThreadPoolReceiveStreamDecryptWorkItem();
        }


        #region Navigation Routines
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var rootFrame = Window.Current.Content as Frame;
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = rootFrame != null && rootFrame.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;

            // This works but return 0. A Microsoft problem
            //var uuid = new Guid( DEVICE_INFORMATION_SERVICE_UUID );
            //var deviceInfoCollection = await DeviceInformation.FindAllAsync( GattDeviceService.GetDeviceSelectorFromUuid( uuid ) );                           // return 0 devices

            // Eitan, Microsoft version - works fine
            //var deviceInfoCollection = await DeviceInformation.FindAllAsync( DeviceClass.All );                                                               // returns 615 devices

            // Eitan, this code is running fine
            //var deviceInfoCollection = await DeviceInformation.FindAllAsync( BluetoothDevice.GetDeviceSelectorFromPairingState( false ) );                    // returns 4 devices

            //var deviceInfoCollection = await DeviceInformation.FindAllAsync( GattDeviceService.GetDeviceSelectorFromUuid( GattServiceUuids.GenericAccess ) ); // return 0 devices
            //var deviceInfoCollection = await DeviceInformation.FindAllAsync( BluetoothLEDevice.GetDeviceSelector() );                                         // return 0 devices

            //var selector = BluetoothDevice.GetDeviceSelector();
            //var deviceInfoCollection = await DeviceInformation.FindAllAsync( selector );                                                                      // return 0 devices

            //BluetoothLEAppearance appearance = BluetoothLEAppearance.FromParts( BluetoothLEAppearanceCategories.OutdoorSportActivity, BluetoothLEAppearanceSubcategories.Generic);
            //var deviceInfoCollection = await DeviceInformation.FindAllAsync( BluetoothLEDevice.GetDeviceSelectorFromAppearance( appearance ) );               // return 0 devices

            if (e.NavigationMode == NavigationMode.New)
            {
                // Eitan, this commands has problem 
                //await Debug_EnumerateDevices();

                // Eitan, this command works fine
                //InitializeAdvertisementWatcher();

                AppBarButtonStart.IsEnabled = true;
                AppBarButtonStop.IsEnabled = false;


                // Set the time of day timer 
                _timer = new DispatcherTimer();
                _timer.Tick += _timer_Tick;

                // Every 200 miliseconds
                _timer.Interval = new TimeSpan(0, 0, 0, 0, 200);

                _timer.Start();
            }

            _rootPage = this;
            DataContext = this;
            GridBandData.DataContext = bandData;
            TotalSteps.DataContext = bandData;
            Distance.DataContext = bandData;
            ResultsListView.ItemsSource = ResultCollection;

            // Eitan, just for debug we re-create the database
            //Database.Database.DeleteDatabase();

            File.Delete(Database.Database.DbPath);
            Database.Database.CreateDatabaseForTickRecords();
            Database.Database.DeleteAllDatabaseTables(true);

            // Eitan, Just for testing
            //Debug.WriteLine( "Count= " + Database.Database.CountTickRecords() );
            //var isExist = Database.Database.DoesRecordExist( 10 );

            //Debug_CreateFakeTickRecords();

            // Eitan, this get an ObservableCollection of TickRecord's
            //var tickRecords = Database.Database.GetAllTickRecords();
        }

        private void _timer_Tick(object sender, object e)
        {
            Time.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            StopDeviceWatchers();

            //_advertisementWatcher.Received -= OnAdvertisementWatcherReceived;
            //_advertisementWatcher.Stopped -= OnAdvertisementWatcherStopped;

            _characteristicsStreamTx[0].ValueChanged -= CharacteristicsTx_ValueChanged;

            // Cancel the ThreadPool
            _workItemTransaction?.Cancel();
            _workItemDecrypt?.Cancel();
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            var rootFrame = Window.Current.Content as Frame;

            if (rootFrame != null && rootFrame.CanGoBack)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }
        #endregion


        #region Create, Initialize, Start, Stop Watchers
        private void InitializeBtleWatcher1(string aqsFilter)
        {
            if (_deviceWatcher1 != null)
            {
                return;
            }

            ResultCollection.Clear();

            Debug.WriteLine("Watcher1 aqsFilter: " + aqsFilter);


            // https://channel9.msdn.com/coding4fun/blog/Powering-up-with-BLE-in-Windows-81
            // var uuid = new Guid( JAWBONE_CONTROL_SERVICE_UUID );
            // string aqsFilter1 = GattDeviceService.GetDeviceSelectorFromUuid( uuid );
            // System.Devices.InterfaceClassGuid:= "{6E3BB679-4372-40C8-9EAA-4509DF260CD8}" AND
            // System.DeviceInterface.Bluetooth.ServiceGuid:= "{151C0000-4580-4111-9CA1-5056F3454FBandChallenge}" AND
            // System.Devices.InterfaceEnabled:= System.StructuredQueryType.Boolean#True


            /////////////////////////////////////
            // Create the Watcher for the BTLE //
            /////////////////////////////////////
            // List of additional properties 
            // https://msdn.microsoft.com/en-us/windows/uwp/devices-sensors/device-information-properties
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            //requestedProperties = null;
            _deviceWatcher1 = DeviceInformation.CreateWatcher(
                    aqsFilter,                                      // An AQS string that filters the DeviceInformation objects to enumerate
                    requestedProperties,                            // An iterable list of additional properties to include in the Properties property of the DeviceInformation objects in the enumeration results
                    DeviceInformationKind.AssociationEndpoint);    // The specific types of devices the DeviceWatcher is interested in


            /////////////////////////////////////////////////////////////////////////
            // Hook up handlers for the watcher events before starting the watcher //
            /////////////////////////////////////////////////////////////////////////

            ///////////
            // Added //
            ///////////
            _handlerAdded1 = async (watcher, deviceInfo1) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await _rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
               {
                   // Check for duplicate entry
                   var isDuplicate = ResultCollection.Any(bleInfoDisp => deviceInfo1.Id == bleInfoDisp.Id);
                   if (isDuplicate)
                   {
                       return;
                   }

                   //DeviceID: "BluetoothLE#BluetoothLEb4:e1:c4:7c:5b:b1-f7:a3:08:13:de:8a" 
                   ResultCollection.Add(new DeviceInformationDisplay(deviceInfo1));

                   ////////////////////////////////////////////////////////////////////////
                   //var btleDevice1 = await BluetoothLEDevice.FromIdAsync( deviceInfo1.Id );
                   //if ( btleDevice1 != null )
                   //    {
                   //    // BT_Alert: GattServices returns a list of all the supported services of the device. If the services supported by the device are expected to change
                   //    // during BT usage, make sure to implement the GattServicesChanged event
                   //    Debug.WriteLine( "Services: " );
                   //    Debug.WriteLine( "========= " );

                   //    var ServiceCollection = new ObservableCollection<BluetoothLEAttributeeDisplay>();
                   //    foreach ( var service in btleDevice1.GattServices )
                   //        {
                   //        ServiceCollection.Add( new BluetoothLEAttributeeDisplay( service ) );
                   //        if ( BluetoothLEAttributeeDisplay.IsSigDefinedUuid( service.Uuid ) )
                   //            {
                   //            GattNativeServiceUuid serviceName;
                   //            if ( Enum.TryParse( Utility.ConvertUuidToShortId( service.Uuid ).ToString(), out serviceName ) )
                   //                {
                   //                Debug.WriteLine( "    " + serviceName.ToString() );
                   //                }
                   //            }
                   //        else
                   //            {
                   //            Debug.WriteLine( "    Custom Service: " + service.Uuid );
                   //            }
                   //        }
                   //    }      
                   ////////////////////////////////////////////////////////////////////////

                   //Debug_DisplayDeviceParams( deviceInfo1, ResultCollection1.Count );

                   _rootPage.NotifyUser(string.Format("W1-{0} devices found.", ResultCollection.Count), NotifyType.StatusMessage);
               });
            };
            _deviceWatcher1.Added += _handlerAdded1;

            /////////////
            // Updated //
            /////////////
            _handlerUpdated1 = async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await _rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
               {
                   // Find the corresponding updated DeviceInformation in the collection and pass the update object
                   // to the Update method of the existing DeviceInformation.This automatically updates the object for us.
                   foreach (DeviceInformationDisplay deviceInformationDisplay in ResultCollection)
                   {
                       if (deviceInformationDisplay.Id != deviceInfoUpdate.Id)
                       {
                           continue;
                       }

                       deviceInformationDisplay.Update(deviceInfoUpdate, "W1");
                       break;
                   }
               });
            };
            _deviceWatcher1.Updated += _handlerUpdated1;

            /////////////
            // Removed //
            /////////////
            _handlerRemoved1 = async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await _rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
               {
                   // Find the corresponding DeviceInformation in the collection and remove it
                   foreach (DeviceInformationDisplay deviceInformationDisplay in ResultCollection)
                   {
                       if (deviceInformationDisplay.Id != deviceInfoUpdate.Id)
                       {
                           continue;
                       }

                       //ResultCollection1.Remove( deviceInformationDisplay );  

                       if (ResultCollection.Contains(deviceInformationDisplay))
                       {
                           ResultCollection.Remove(deviceInformationDisplay);
                       }
                       break;
                   }

                   _rootPage.NotifyUser(string.Format("W1-{0} devices found.", ResultCollection.Count), NotifyType.StatusMessage);
               });
            };
            _deviceWatcher1.Removed += _handlerRemoved1;

            //////////////////////////
            // EnumerationCompleted //
            //////////////////////////
            _handlerEnumCompleted1 = async (watcher, obj) =>
            {
                await _rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
               {
                   Debug.WriteLine("W1 Enumeration completed: {0} devices found .... Watching for updates...", ResultCollection.Count);
                   _rootPage.NotifyUser(string.Format("W1-{0} devices found.\nW1-Enumeration completed.\nW1-Watching for updates...", ResultCollection.Count), NotifyType.StatusMessage);

                   AppBarButtonPair.IsEnabled = true;
                   Utility.VibratePhone();
               });
            };
            _deviceWatcher1.EnumerationCompleted += _handlerEnumCompleted1;

            /////////////
            // Stopped //
            /////////////
            _handlerStopped1 = async (watcher, obj) =>
            {
                await _rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
               {
                   _rootPage.NotifyUser(string.Format("W1-{0} devices found. Watcher {1}.",
                                         ResultCollection.Count, DeviceWatcherStatus.Aborted == watcher.Status ? "aborted" : "stopped"),
                                         NotifyType.StatusMessage);
               });
            };
            _deviceWatcher1.Stopped += _handlerStopped1;
        }

        private void StartWatchers()
        {
            AppBarButtonStart.IsEnabled = false;

            // Start the first watcher for all the BluetoothLE devices First get the device selector for the BTLE
            DeviceSelectorInfo deviceSelectorInfo = BluetoothLE;
            string aqsFilter = deviceSelectorInfo.Selector;
            //Manjit Temp initilized it to ""
            //aqsFilter = deviceSelectorInfo.Selector;
            // System.Devices.Aep.ProtocolId:="{bb7bb05e-5972-42b5-94fc-76eaa7084d49}"
            // association endpoint (AEP)
            InitializeBtleWatcher1(aqsFilter);
            _deviceWatcher1.Start();

            // Start the second watcher for the UP Band Stream Service: GATT_STREAM_SERVICE_UUID 
            aqsFilter = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(UuidDefs.GATT_STREAM_SERVICE_UUID));
            _watcher2 = new BtleWatcher(this, "W2");
            _watcher2.InitializeBtleWatcher(aqsFilter);

            // Hook a function to the watcher events
            _watcher2.WacherEvent += Wacher2EventFired;

            // Start the third watcher for the UP Band Control Service: JAWBONE_CONTROL_SERVICE_UUID
            aqsFilter = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(UuidDefs.JAWBONE_CONTROL_SERVICE_UUID));
            _watcher3 = new BtleWatcher(this, "W3");
            _watcher3.InitializeBtleWatcher(aqsFilter);

            // Start the third watcher for the UP Band Control Service: DEVICE_INFORMATION_SERVICE_UUID
            aqsFilter = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(UuidDefs.DEVICE_INFORMATION_SERVICE_UUID));
            _watcher4 = new BtleWatcher(this, "W4");
            _watcher4.InitializeBtleWatcher(aqsFilter);

            _rootPage.NotifyUser("Starting Watchers Done...", NotifyType.StatusMessage);

            AppBarButtonStop.IsEnabled = true;
        }

        static void Wacher2EventFired(Object sender, BtleWatcherEventsArgs e)
        {
            Debug.WriteLine("Wacher2EventFired: " + Enum.GetName(typeof(WatcherEvents), e.Event));
        }

        private void StopDeviceWatchers()
        {
            AppBarButtonStop.IsEnabled = false;

            StopDeviceWatcher1();
            _watcher2.StopDeviceWatcher();
            _watcher3.StopDeviceWatcher();
            _watcher4.StopDeviceWatcher();
        }

        private void StopDeviceWatcher1()
        {
            if (_deviceWatcher1 != null)
            {
                // First unhook all event handlers except the stopped handler. This ensures our
                // event handlers don't get called after stop, as stop won't block for any "in flight" 
                // event handler calls.  We leave the stopped handler as it's message guaranteed to only be called
                // once and we'll use it to know when the query is completely stopped. 
                _deviceWatcher1.Added -= _handlerAdded1;
                _deviceWatcher1.Updated -= _handlerUpdated1;
                _deviceWatcher1.Removed -= _handlerRemoved1;
                _deviceWatcher1.EnumerationCompleted -= _handlerEnumCompleted1;

                if (DeviceWatcherStatus.Started == _deviceWatcher1.Status || DeviceWatcherStatus.EnumerationCompleted == _deviceWatcher1.Status)
                {
                    _deviceWatcher1.Stop();
                }

                _deviceWatcher1 = null;
            }
        }

        private IReadOnlyList<GattCharacteristic> GetCharacteristics(GattDeviceService gattService, string characteristicUuid)
        {
            var characteristics = gattService.GetCharacteristics(new Guid(characteristicUuid));
            return characteristics;
        }
        #endregion


        //        #region Advertisement Watcher
        ////        private void InitializeAdvertisementWatcher()
        ////        {
        ////            //
        ////            // http://stackoverflow.com/questions/32892815/windows-10-bluetooth-low-energy-connection-c-sharp
        ////            //
        ////            _advertisementWatcher = new BluetoothLEAdvertisementWatcher
        ////            {
        ////                ScanningMode = BluetoothLEScanningMode.Active
        ////            };

        ////            // Two Events: 
        ////            // Received - Notification for new Bluetooth LE advertisement events received
        ////            // Stopped  - Notification to the app that the Bluetooth LE scanning for advertisements has been cancelled or aborted either by the app or due to an error.
        ////            _advertisementWatcher.Received += OnAdvertisementWatcherReceived;
        ////            _advertisementWatcher.Stopped += OnAdvertisementWatcherStopped;

        ////            // Configure the signal strength filter to only propagate events when in-range
        ////            // Please adjust these values if you cannot receive any advertisement 
        ////            // Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
        ////            // will start to be considered "in-range".
        ////            _advertisementWatcher.SignalStrengthFilter.InRangeThresholdInDBm = -70;

        ////            // Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction with OutOfRangeTimeout
        ////            // to determine when an advertisement is no longer considered "in-range"
        ////            _advertisementWatcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -75;

        ////            // Set the out-of-range timeout to be 3 seconds. Used in conjunction with OutOfRangeThresholdInDBm
        ////            // to determine when an advertisement is no longer considered "in-range"
        ////            _advertisementWatcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000);

        ////            // By default, the sampling interval is set to zero, which means there is no sampling and all
        ////            // the advertisement received is returned in the Received event

        ////            // End of watcher configuration. There is no need to comment out any code beyond this point.

        ////            _advertisementWatcher.Start();
        ////        }

        ////        private void OnAdvertisementWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        ////        {
        ////#if never
        ////            @BitField(offset = 0) public byte preamble;
        ////            @BitField(offset = 1) public int address;
        ////        *** @BitField(offset = 5) public short header;  ***
        ////            @BitField(offset = 7) public byte[] payload;
        ////            @BitField(offset = 7) public byte type = -1;
        ////            @BitField(offset = 8, swap = true) public int id;
        ////            @BitField(offset = 12) public byte color;
        ////            @BitField(offset = 13) public byte size;
        ////            @BitField(offset = 14) public byte isPairable; 

        ////            3/4/2016 11:40:23 AM -08:00 - Address: F7-A3-08-13-DE-8A    - Adver  tisementType: ConnectableUndirected    - Signal: -127  
        ////            Length: 1    - Capacity: 1    - Data: 06
        ////            Length: 10   - Capacity: 10   - Data: 0A-18-03-A5-28-50-50-03-0F-00    
        ////            ---------------------------------------------------------------------------------------------------------------------------
        ////            0A-18 - Header
        ////            03 - Band Type - which is Thorpe
        ////            A5-28-50-50 - device id = device hash
        ////            03 - Color which is black
        ////            0F - Size
        ////            01 - is pairable
        ////#endif

        ////            ulong btleAddress = eventArgs.BluetoothAddress;
        ////            string btleAddressStr = Utility.BtleAddress2String(btleAddress);

        ////            // Eitan, here we decode the band we are interested in...
        ////            //if ( btleAddressStr != "F7-A3-08-13-DE-8A" )
        ////            //if ( btleAddressStr != "D1-95-E1-37-9A-2E" )
        ////            //    {
        ////            //    return;
        ////            //    }

        ////            BluetoothLEAdvertisement advertisement = eventArgs.Advertisement;
        ////            IList<BluetoothLEAdvertisementDataSection> dataSections = advertisement.DataSections;
        ////            IList<Guid> serviceUuids = advertisement.ServiceUuids;
        ////            IList<BluetoothLEManufacturerData> manufactureData = advertisement.ManufacturerData;
        ////            BluetoothLEAdvertisementType advertisementType = eventArgs.AdvertisementType;
        ////            DateTimeOffset timeStamp = eventArgs.Timestamp;
        ////            string localName = advertisement.LocalName;

        ////            Debug.WriteLine("{0,-24} - Address: {1,-20} - AdvertisementType: {2,-24} - Signal: {3,-6} - LocalName: {4,-16}", timeStamp, btleAddressStr, advertisementType, eventArgs.RawSignalStrengthInDBm, localName);

        ////            foreach (BluetoothLEAdvertisementDataSection dataSection in dataSections)
        ////            {
        ////                var bytes = Utility.ReadBuffer(dataSection.Data);
        ////                string data = Utility.ByteArray2String(bytes);

        ////                if ((advertisementType == BluetoothLEAdvertisementType.ScanResponse) && (bytes.Length == 6))
        ////                {
        ////                    string name = new string(Encoding.UTF8.GetString(bytes).ToCharArray());
        ////#if DEBUG
        ////                    Debug.WriteLine("Length: {0,-4} - Capacity: {1,-4} - Data: {2} - Name: {3}", dataSection.Data.Length, dataSection.Data.Capacity, data, name);
        ////                    Debug_ListManufactureData(manufactureData);
        ////                    Debug_ListServiceUuids(serviceUuids);
        ////#endif
        ////                    continue;
        ////                }

        ////                if ((advertisementType == BluetoothLEAdvertisementType.ScanResponse) && (bytes.Length == 16))
        ////                {
        ////                    //Array.Reverse( bytes );
        ////                    // 151c1000-4580-4111-9ca1-5056f3454fbc
        ////                    // 15-1C-10-00-45-80-41-11-9C-A1-50-56-F3-45-4F-BandChallenge
        ////                    //uuid = uuid.Substring(0, 4) + "-" + uuid.Substring(4, 2) + "-" + uuid.Substring(6, 2) + "-" + uuid.Substring(8, 2 ) + "-" + uuid.Substring( 10 );
        ////#if DEBUG
        ////                    Debug.WriteLine("Length: {0,-4} - Capacity: {1,-4} - Data: {2}", dataSection.Data.Length, dataSection.Data.Capacity, data);
        ////                    Debug_ListManufactureData(manufactureData);
        ////                    Debug_ListServiceUuids(serviceUuids);
        ////#endif
        ////                    continue;
        ////                }

        ////                if ((advertisementType == BluetoothLEAdvertisementType.ConnectableUndirected) && (bytes.Length == 1))
        ////                {
        ////#if DEBUG
        ////                    Debug.WriteLine("Length: {0,-4} - Capacity: {1,-4} - Data: {2}", dataSection.Data.Length, dataSection.Data.Capacity, data);
        ////                    Debug_ListManufactureData(manufactureData);
        ////                    Debug_ListServiceUuids(serviceUuids);
        ////#endif
        ////                    continue;
        ////                }

        ////                if ((advertisementType == BluetoothLEAdvertisementType.ConnectableUndirected) && (bytes.Length == 10))
        ////                {
        ////                    var bytes1 = new byte[2];                                                     // is it the AttribueHandle
        ////                    bytes1[0] = bytes[0];
        ////                    bytes1[1] = bytes[1];
        ////                    string header = Utility.ByteArray2String(bytes1);

        ////                    string bandtype = _bandType[bytes[2]];

        ////                    bytes1 = new byte[4];
        ////                    bytes1[0] = bytes[3];
        ////                    bytes1[1] = bytes[4];
        ////                    bytes1[2] = bytes[5];
        ////                    bytes1[3] = bytes[6];
        ////                    string deviceid = Utility.ByteArray2String(bytes1);

        ////                    byte color = bytes[7];

        ////                    byte size = bytes[8];

        ////                    string ispairable = bytes[9] == 1 ? "Yes" : "No";

        ////#if DEBUG
        ////                    // Print the header data
        ////                    Debug.WriteLine(string.Format("      Header:      {0,-4}", header));            // is it the AttribueHandle
        ////                    Debug.WriteLine(string.Format("      Band Type:   {0,-10}", bandtype));
        ////                    Debug.WriteLine(string.Format("      Device ID:   {0,-10}", deviceid));
        ////                    Debug.WriteLine(string.Format("      Color:       {0}", color));
        ////                    Debug.WriteLine(string.Format("      Size:        {0}", size));
        ////                    Debug.WriteLine(string.Format("      IsPairable:  {0,-4}", ispairable));
        ////                    Debug_ListManufactureData(manufactureData);
        ////                    Debug_ListServiceUuids(serviceUuids);
        ////#endif
        ////                }
        ////            }

        ////            Debug.WriteLine("----------------------------------------------------------------------------------------------------------------------------------------------------" + "\n");
        ////        }

        ////        private void OnAdvertisementWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        ////        {
        ////        }
        //        #endregion


        #region ThreadPool tasks
        private void CreateThreadPoolSendStreamTransactionWorkItem()
        {
            // Create the work item with the specified priority.
            IAsyncAction asyncAction = ThreadPool.RunAsync(async workItem =>
               {
                   try
                   {
                       if (workItem != null)
                       {
                           await SendStreamTransactionsAsync();
                       }
                   }
                   catch (Exception ex)
                   {
                       Debug.WriteLine("** Error ** - CreateThreadPoolSendStreamTransactionWorkItem: " + ex.Message);
                   }
               }, WorkItemPriority.Normal);

            // A reference to the work item is cached so that we can trigger a cancellation when the user presses the Cancel button.
            _workItemTransaction = asyncAction;

            // Register a completed-event handler to run when the work item finishes or is canceled.
            asyncAction.Completed = (asyncInfo, asyncStatus) =>
            {
                // CancellationHandler
                if (asyncStatus == AsyncStatus.Canceled)
                {
                    return;
                }

                var updateString = "CreateThreadPoolSendStreamTransactionWorkItem - AsyncActionCompletedHandler message: Completed";
                DispatcherUpdateUI(updateString);
            };
        }

        private void CreateThreadPoolReceiveStreamDecryptWorkItem()
        {
            // Create the work item with the specified priority.
            IAsyncAction asyncAction = ThreadPool.RunAsync(async workItem =>
           {
               try
               {
                   if (workItem != null)
                   {
                       await DecodeBandResponse();
                   }
               }
               catch (Exception ex)
               {
                   Debug.WriteLine("** Error ** - CreateThreadPoolReceiveStreamDecryptWorkItem: " + ex.Message);
               }
           }, WorkItemPriority.Normal);

            // A reference to the work item is cached so that we can trigger a cancellation when the user presses the Cancel button.
            _workItemDecrypt = asyncAction;

            // Register a completed-event handler to run when the work item finishes or is canceled.
            asyncAction.Completed = (asyncInfo, asyncStatus) =>
            {
                // CancellationHandler
                if (asyncStatus == AsyncStatus.Canceled)
                {
                    return;
                }

                var updateString = "CreateThreadPoolReceiveStreamDecryptWorkItem - AsyncActionCompletedHandler message: Completed";
                DispatcherUpdateUI(updateString);
            };
        }
        #endregion


        #region Toolbar commands
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton b = sender as AppBarButton;

            if (b != null)
            {
                string label = b.Label;

                switch (label)
                {
                    case "Scan":
                        StartWatchers();
                        Debug.WriteLine("You clicked: " + label);
                        break;

                    case "Stop":
                        StopDeviceWatchers();
                        Debug.WriteLine("You clicked: " + label);
                        break;

                    case "Edit":
                        Debug.WriteLine("You clicked: " + label);
                        break;

                    case "Remove":
                        Debug.WriteLine("You clicked: " + label);
                        break;

                    case "Add":
                        Debug.WriteLine("You clicked: " + label);
                        break;
                }
            }
        }

        private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ResultsListView.SelectedItem as DeviceInformationDisplay;
            if ((item != null) && (_deviceWatcher1 != null))
            {
                AppBarButtonPair.IsEnabled = true;
            }

            if ((ResultsListView.ItemsSource == _watcher2.ResultCollection) && (AppBarButtonPair.IsEnabled == false))
            {
                AppBarButtonConnect.IsEnabled = true;
            }
        }

        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            _deviceInformationDisplayPair = ResultsListView.SelectedItem as DeviceInformationDisplay;
            if (_deviceInformationDisplayPair == null)
            {
                _rootPage.NotifyUser("Pairing result = Item Error", NotifyType.ErrorMessage);
                return;
            }

            // Stop Watcher1
            StopDeviceWatcher1();

            _watcher2.ResultCollection.Clear();
            ResultsListView.ItemsSource = _watcher2.ResultCollection;

            // Gray out the pair button and results view while pairing is in progress.
            ResultsListView.IsEnabled = false;
            AppBarButtonPair.IsEnabled = false;

            _rootPage.NotifyUser(string.Format("Pairing started. Please wait...\nCanPair: {0}\nIsPaired: {1}", _deviceInformationDisplayPair.CanPair, _deviceInformationDisplayPair.IsPaired), NotifyType.StatusMessage);

            // Do the pairing
            DevicePairingResult devicePairingResult = await _deviceInformationDisplayPair.DeviceInformation.Pairing.PairAsync();
            Debug.WriteLine("After Pairing: DecodeProperties");
#if DEBUG
            //Debug_DecodeProperties( deviceInformationDisplay.Properties );
#endif

            _rootPage.NotifyUser("Pairing result = " + devicePairingResult.Status, devicePairingResult.Status == DevicePairingResultStatus.Paired ? NotifyType.StatusMessage : NotifyType.ErrorMessage);

            // Once we are paired start the watchers for the GATT_STREAM_SERVICE_UUID and the JAWBONE_CONTROL_SERVICE_UUID

            // _watcher2
            _watcher2.Watcher.Start();

            // _watcher3
            _watcher3.Watcher.Start();

            // _watcher4
            _watcher4.Watcher.Start();


            ResultsListView.IsEnabled = true;
            AppBarButtonUnpair.IsEnabled = true;
            AppBarButtonConnect.IsEnabled = true;
        }

        private async void UnpairButton_Click(object sender, RoutedEventArgs e)
        {
            // Gray out the unpair button and results view while unpairing is in progress.
            ResultsListView.IsEnabled = false;
            AppBarButtonUnpair.IsEnabled = false;
            _rootPage.NotifyUser("Unpairing started. Please wait...", NotifyType.StatusMessage);

            _deviceInformationDisplayUnpair = ResultsListView.SelectedItem as DeviceInformationDisplay;

            if (_deviceInformationDisplayUnpair != null)
            {
                DeviceUnpairingResult deviceUnpairingResult = await _deviceInformationDisplayUnpair.DeviceInformation.Pairing.UnpairAsync();
                _rootPage.NotifyUser("Unpairing result = " + deviceUnpairingResult.Status, deviceUnpairingResult.Status == DeviceUnpairingResultStatus.Unpaired ? NotifyType.StatusMessage : NotifyType.ErrorMessage);
            }
            else
            {
                AppBarButtonUnpair.IsEnabled = true;
                return;
            }

            ResultsListView.IsEnabled = true;
            AppBarButtonPair.IsEnabled = true;
        }

        private async void AppBarButtonConnect_OnClick(object sender, RoutedEventArgs e)
        {
            _deviceInformationDisplayConnect = ResultsListView.SelectedItem as DeviceInformationDisplay;
            if (_deviceInformationDisplayConnect == null)
            {
                _rootPage.NotifyUser("Pair result = Item Error", NotifyType.ErrorMessage);
                return;
            }


            // At this point stop the Watchers and disable the toolbar button
            AppBarButtonConnect.IsEnabled = false;
            StopDeviceWatchers();


            btleDevice2 = await BluetoothLEDevice.FromIdAsync(_deviceInformationDisplayConnect.Id);
            Debug.WriteLine("\n" + btleDevice2.BluetoothAddress + "\n");
            _deviceInformationDisplayConnect.BtleAddress = btleDevice2.BluetoothAddress;


#if DEBUG
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
#endif


            // Occurs when the connection status for the device has changed.
            btleDevice2.ConnectionStatusChanged += BtleDevice2_ConnectionStatusChanged;


            //
            // Get the GATT_STREAM_SERVICE and Characteristics
            //
            await InitializeGattStreamServiceCharateristics(_deviceInformationDisplayConnect);


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
            _rootPage.NotifyUser("Starting Pairing Sequence", NotifyType.StatusMessage);
        }

        private async Task InitializeGattStreamServiceCharateristics(DeviceInformationDisplay deviceInfoDisp)
        {
            _gattStreamService = await GattDeviceService.FromIdAsync(deviceInfoDisp.Id);
            Debug.WriteLine("\n" + _gattStreamService + ":   " + _gattStreamService.Device.Name + "\n");
            _rootPage.NotifyUser("Getting GATT Services", NotifyType.StatusMessage);

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

        private async void BtleDevice2_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            var msg = "Connection Status Changed - " + btleDevice2.ConnectionStatus;
            Debug.WriteLine(msg);
            _rootPage.NotifyUser(msg, NotifyType.StatusMessage);

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

                Utility.VibratePhone();
            }

            // We are connected and were disconnected before.... Start a connection sequence 
            if ((btleDevice2.ConnectionStatus == BluetoothConnectionStatus.Connected) && _isDisconnected)
            {
                _isDisconnected = false;
                Utility.VibratePhone();

                // Initiate the re-connection
                await InitializeGattStreamServiceCharateristics(_deviceInformationDisplayConnect);
                await InitializeControlService(btleDevice2);          //
                await InitializeInformationService(btleDevice2);      //

                // Start a connection sequence 
                //StartConnectionSequence();                            // 1. - Works
                Connecting_EstablishSecureChannel_5();                  // 5. - Works
            }
        }

        private void AppBarButtonAlert_OnClick(object sender, RoutedEventArgs e)
        {
            // Send an Alert Command here...
            SendAlertCommand();
        }

        private void AppBarButtonSetting_OnClick(object sender, RoutedEventArgs e)
        {
            // Send an Sync Versions Query here...
            SendSyncVersionsQuery();

            // Send an Epoch Request here...
            SendEpochTimeRequest();
        }

        private void AppBarButtonTest_OnClick(object sender, RoutedEventArgs e)
        {
            // Send a Set Person Data Command here...
            var dob = new DateTime(1955, 11, 4);
            SetPersonData((ushort)Math.Round(215.0 / 2.2), 178, Gender.Male, new DateTimeOffset(dob));

            // Send a Tick Record Rate Command here...
            SendTickRecordsRate();

            // Clear the TickRecords list
            TickRecords.Clear();

            //var start = DateTime.Now.AddMinutes( -15 );                   // Set start time to 15 minutes back
            //var start = DateTime.Now.AddHours( -3 );                      // Set start time to 3 hours back
            var start = DateTime.Now.Date;                                  // Set start time to Midnight
            var end = DateTime.Now;

            var ReplayMinutes = new TimeSpan(end.Ticks - start.Ticks).TotalMinutes;
            var noRecords = (byte)Math.Min(TICK_RECORD_BATCH_MAX_COUNT, ReplayMinutes);

            // Long Replay here
            SendTickRequestReplay(start, end, noRecords);
        }

        private async void AppBarButtonDateTime_OnClick(object sender, RoutedEventArgs e)
        {
            // Send a Set DateTime Command here...
            await SendSetDateTimeCommand();
        }
        #endregion      


        #region Band Toolbar Commands
        private void SendSyncVersionsQuery()
        {
            Debug.WriteLine(string.Format("\n\n== {0} - Sync Versions Query", "0x" + _transactionId.ToString("X2")));
            _messageType = (byte)SettingsIds.GetSyncVersions;               // 0xC2
            _flags = 0x00;
            _payloadSize = 0x00;
            byte[] payload = new byte[0];
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, payload);
            SaveTransaction(transaction);

            _rootPage.NotifyUser("Sync Versions Query", NotifyType.StatusMessage);
        }

        private void SendEpochTimeRequest()
        {
            Debug.WriteLine(string.Format("\n\n== {0} - Request Epoch Time", "0x" + _transactionId.ToString("X2")));
            _messageType = (byte)TickMessageType.QueryEpochs;               // 0x35
            _flags = 0x00;
            _payloadSize = 0x02;
            byte[] payload = { 0x00, 0x00 };
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, payload);
            SaveTransaction(transaction);

            _rootPage.NotifyUser("Epoch Time Request", NotifyType.StatusMessage);
        }

        private void SendTickRecordsRate(byte recordsPerSend = 1)
        {
            if (recordsPerSend < TICK_RECORD_BATCH_MIN_COUNT || recordsPerSend > TICK_RECORD_BATCH_MAX_COUNT)
            {
                throw new JawboneException(JawboneErrorCodes.INVALID_TICK_RECORD_RATE);
            }

            Debug.WriteLine(string.Format("\n\n== {0} - Send Tick Records Rate", "0x" + _transactionId.ToString("X2")));
            _messageType = (byte)TickMessageType.SetTickRecordRate;         // 0x38
            _flags = 0x00;
            _payloadSize = 0x01;
            byte[] payload = { recordsPerSend };
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, payload);
            SaveTransaction(transaction);

            _rootPage.NotifyUser("Tick Records Rate", NotifyType.StatusMessage);
        }

        private void SendTickRequestReplay(DateTime start, DateTime end, byte numRecords = TICK_RECORD_BATCH_MAX_COUNT)
        {
            Debug.WriteLine(string.Format("\n\n== {0} - Tick Request Replay", "0x" + _transactionId.ToString("X2")));

            var tickRequestReplayPayload = new TickRequestReplayPayload
            {
                NoRecords = numRecords,

                TimestampStart = start.ToUniversalTime().ToSecondsSince1970(),
                TimestampEnd = end.ToUniversalTime().ToSecondsSince1970(),


                // Use the extension function, ToSecondsSince1970, to find seconds since: DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc )
                //TimestampStart = DateTime.UtcNow.AddMinutes( -60 ).ToSecondsSince1970(),
                //TimestampEnd = DateTime.UtcNow.ToSecondsSince1970()
            };

            _messageType = (byte)TickMessageType.Replay;                    // 0x34
            _flags = 0x00;
            _payloadSize = (byte)Marshal.SizeOf<TickRequestReplayPayload>();
            byte[] payload = Utility.StructToBytes(tickRequestReplayPayload);
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, payload);
            SaveTransaction(transaction);

            _rootPage.NotifyUser("Tick Request Replay", NotifyType.StatusMessage);
        }

        private void SetPersonData(ushort weight, ushort heightInCentiMeter, Gender gender, DateTimeOffset birthday)
        {
            PersonCommand personCommand = new PersonCommand(weight, heightInCentiMeter, gender, birthday);

            Debug.WriteLine(string.Format("\n\n== {0} - Set Person Data", "0x" + _transactionId.ToString("X2")));
            _messageType = (byte)SettingsIds.SetPersonData;                 // 0xC0
            _flags = 0x00;
            _payloadSize = (byte)Marshal.SizeOf<PersonCommand>();
            byte[] payload = Utility.StructToBytes(personCommand);
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, payload);
            SaveTransaction(transaction);

            _rootPage.NotifyUser("Set Person Data", NotifyType.StatusMessage);
        }

        private void SendAlertCommand()
        {
            Debug.WriteLine(string.Format("\n\n== {0} - Send Alert Message", "0x" + _transactionId.ToString("X2")));
            _messageType = (byte)AlertRequestIds.AlertNow;                  // 0x40
            _flags = 0x00;
            _payloadSize = 0x01;
            byte[] payload = new byte[_payloadSize];
            payload[0] = 0x00;
            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, payload);
            SaveTransaction(transaction);

            _rootPage.NotifyUser("Alert Command", NotifyType.StatusMessage);
        }

        public void RequestConnectionSpeed(ConnectionSpeed speed)
        {
            Debug.Assert(speed != ConnectionSpeed.None);

            //CheckErrorCode();

            SetConnectionSpeedRequest speedRequest = new SetConnectionSpeedRequest((byte)speed);
            if (_inProgressSpeedChange != ConnectionSpeed.None)
            {
                throw new JawboneException(JawboneErrorCodes.SPEED_CHANGE_ALREADY_IN_PROGRESS);
            }

            // mark speed change is in progress
            _inProgressSpeedChange = speed;

            byte[] requestBytes = Utility.StructToBytes(speedRequest);

            Debug.WriteLine(string.Format("\n\n== {0} - Request Connection Speed", "0x" + _transactionId.ToString("X2")));
            _messageType = (byte)PairingMsgTypes.SetConnectionSpeed;        // 0x01
            _flags = 0x00;
            _payloadSize = (byte)requestBytes.Length;
            byte[] payload = new byte[_payloadSize];
            Buffer.BlockCopy(payload, 0, requestBytes, 0, _payloadSize);

            var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
            var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, payload);
            SaveTransaction(transaction);

            _rootPage.NotifyUser("Connection Speed", NotifyType.StatusMessage);

#if never
            // Eitan, this is the response from the SetConnectionSpeed Command
            byte[ ] responseBytes = await messageLayer.SendMessageAsync( (byte)PairingMsgTypes.SetConnectionSpeed, requestBytes );
            if ( responseBytes.Length < Marshal.SizeOf<SetConnectionSpeedResponse>() )
                {
                throw new JawboneException( JawboneErrorCodes.INVALID_SET_SPEED_CONNECTION_RESPONSE );
                }

            SetConnectionSpeedResponse response = Utility.BytesToStruct<SetConnectionSpeedResponse>( responseBytes );
            if ( speedRequest.RequestedSpeed == response.CurrentSpeed )
                {
                // speed change request has been completed
                _inProgressSpeedChange = ConnectionSpeed.None;
                return response.CurrentConnectionIntervalInMilliSecs;
                }
#endif
        }

        private void CheckErrorCode(bool connectionOnly = false)
        {
            if (connectionOnly)
            {
                if ((errorCode == JawboneErrorCodes.DEVICE_DISCONNECTED) || (errorCode == JawboneErrorCodes.PROTOCOL_VERSION_MISMATCH))
                {
                    throw new JawboneException(errorCode);
                }
            }
            else if (errorCode != 0)
            {
                throw new JawboneException(errorCode);
            }
        }
        #endregion


        #region Connection Sequence
        // 1. Get Protocol Version          // 0x00
        // 2. Key Exchange                  // 0x03
        // 3. Authenticate                  // 0x04
        // 4. RespondToChallenge            // 0x05
        // 5. EstablishSecureChannel        // 0x06
        // 6. Initialize Stream Encryptor

        private void StartConnectionSequence()
        {
            _isConnecting = true;

            // 1. Get Protocol Version
            Connecting_GetProtocolVersion_1();
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

            _rootPage.NotifyUser("Protocol Version", NotifyType.StatusMessage);
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

            _rootPage.NotifyUser("Key Exchange", NotifyType.StatusMessage);
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

            _rootPage.NotifyUser("Authenticate", NotifyType.StatusMessage);
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

            _rootPage.NotifyUser("Respond To Challenge", NotifyType.StatusMessage);
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

            _rootPage.NotifyUser("Establish Secure Channel", NotifyType.StatusMessage);
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
                    throw new JawboneException(JawboneErrorCodes.DECRYPTION_FAILED);
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

                await _rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
               {
                   AppBarButtonAlert.IsEnabled = true;
                   AppBarButtonDateTime.IsEnabled = true;
                   AppBarButtonTest.IsEnabled = true;
                   AppBarButtonSetting.IsEnabled = true;
               });
            }
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
        #endregion


        #region UI update
        private async void DispatcherUpdateUI(string message)
        {
            // Update the UI thread with the CoreDispatcher.
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => UpdateUI(message));
        }

        private void UpdateUI(string message)
        {
            Debug.WriteLine(message);
        }
        #endregion


        #region Send data to Bluetooth
        private async Task SendStreamTransactionsAsync()
        {
            while ((_workItemTransaction != null) && (_workItemTransaction.Status != AsyncStatus.Canceled))
            {
                if ((QueueTransactions != null) &&
                     (QueueTransactions.Any()) &&
                     (_simpleSendBusy == 0) &&
                     (_multiSendBusy == 0) &&
                     (QueueTransactions[0].Status == TransactionStatus.TransactionWait))
                {
                    // Doing work here......
                    if (_isConnecting)
                    {
                        lock (lockQueueTransactions)
                        {
                            // Set the current machine state
                            _pairingSequenceState = QueueTransactions[0].Header.MessageType;
                        }
                    }

                    // Send the Transaction to the BAND
                    if (_isLongTransaction == false)
                    {
                        lock (lockQueueTransactions)
                        {
                            // Update the Debug screen
                            var updateString = "-- Send Command ThreadPool: SendTransactionAsync: " + QueueTransactions[0].Header.MessageType;
                        }

                        await SendTransactionAsync();
                    }
                    else
                    {
                        // Sleep and wait here for new transactions to the Band
                        await Task.Delay(_threadPoolSleepTime);
                    }
                }
                else
                {
                    // Sleep and wait here for new transactions to the Band
                    await Task.Delay(_threadPoolSleepTime);
                }
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

        private async Task SendTransactionAsync()
        {
            Transaction transaction;
            lock (lockQueueTransactions)
            {
                if ((QueueTransactions?[0]?.Status != TransactionStatus.TransactionWait) || (btleDevice2.ConnectionStatus != BluetoothConnectionStatus.Connected))
                {
                    return;
                }

                QueueTransactions[0].Status = TransactionStatus.TransactionSent;
                transaction = new Transaction(QueueTransactions[0]);
            }

            if (transaction.NoPackets == 1)
            {
                await SendSimpleTransaction(transaction);
            }
            else
            {
                await SendMultiMsgTransaction(transaction);
            }

            lock (lockQueueTransactions)
            {
                QueueTransactions.RemoveAt(0);
            }
        }

        private async Task SendSimpleTransaction(Transaction transaction)
        {
            _simpleSendBusy = Interlocked.Increment(ref _simpleSendBusy);

            // Prepare the command
            var payLoadSize = transaction.Payload.Length > BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE ? BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE : transaction.Payload.Length;
            byte[] header = transaction.Header.Hdr2Bytes();

            var command = new byte[1 + 4 + payLoadSize];
            command[0] = _sequenceNumberOut++;

            Buffer.BlockCopy(header, 0, command, 1, header.Length);

            if (transaction.Payload.Length > 0)
            {
                Buffer.BlockCopy(transaction.Payload, 0, command, 1 + header.Length, payLoadSize);
            }

            try
            {
                // Send the packet to the Band
                await SendPacketAsync(transaction.Characteristic, command, transaction.IsEncrypt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("** Error ** - SendSimpleTransaction: " + ex.Message);
            }

            _simpleSendBusy = Interlocked.Decrement(ref _simpleSendBusy);
        }

        private async Task SendMultiMsgTransaction(Transaction transaction)
        {
            _multiSendBusy = Interlocked.Increment(ref _multiSendBusy);

            // Send the first command
            await SendSimpleTransaction(transaction);

            // Send the rest of the commands
            for (var i = 0; i < transaction.NoPackets - 1; i++)
            {
                // Prepare the command
                var payloadSize = transaction.Payload.Length - (BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE + (i * BtleLinkTypes.MULTI_TRANSACTION_MAX_PAYLOAD_SIZE));
                payloadSize = payloadSize > BtleLinkTypes.MULTI_TRANSACTION_MAX_PAYLOAD_SIZE ? BtleLinkTypes.MULTI_TRANSACTION_MAX_PAYLOAD_SIZE : payloadSize;

                var command = new byte[1 + payloadSize];
                command[0] = _sequenceNumberOut++;

                Buffer.BlockCopy(transaction.Payload, BtleLinkTypes.SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE + (i * BtleLinkTypes.MULTI_TRANSACTION_MAX_PAYLOAD_SIZE), command, 1, payloadSize);

                try
                {
                    // Send the packet to the Band
                    await SendPacketAsync(transaction.Characteristic, command, transaction.IsEncrypt);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("** Error ** - SendMultiMsgTransaction: " + ex.Message);
                }
            }

            _multiSendBusy = Interlocked.Decrement(ref _multiSendBusy);
        }

        private static async Task SendPacketAsync(GattCharacteristic characteristic, byte[] command, bool isEncrypt)
        {
            Debug.Assert(characteristic != null);
            Debug.Assert(command != null);

#if DEBUG
            var msg = new StringBuilder();
            msg.AppendFormat(string.Format("    SendPacketAsync - Before Encrypt: [{0}] - {1}\n", _sequenceNumberOut - 1, Utility.ByteArray2String(command)));
#endif

            if ((_isConnecting == false) && isEncrypt)
            {
                // Here we Encrypt the data
                var before = new byte[command.Length - 1];
                Buffer.BlockCopy(command, 1, before, 0, command.Length - 1);
                var after = Encrypt(before);
                Buffer.BlockCopy(after, 0, command, 1, command.Length - 1);

#if DEBUG
                msg.AppendFormat(string.Format("                    - After Encrypt:  [{0}] - {1}\n", _sequenceNumberOut - 1, Utility.ByteArray2String(command)));
#endif
            }
            else
            {
#if DEBUG
                msg.AppendFormat("                    - No Encrypt!\n");
#endif
            }

            // At this point the command is ready
            var writer = new DataWriter();
            writer.WriteBytes(command);

            try
            {
                GattCommunicationStatus status = await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse);
                if (status == GattCommunicationStatus.Unreachable)
                {
                    Debug.WriteLine("** Error ** - JawboneErrorCodes.GATT_COMMUNICATION_FAILED");
                    //throw new JawboneException( JawboneErrorCodes.GATT_COMMUNICATION_FAILED );
#if DEBUG
                    msg.AppendFormat(status == GattCommunicationStatus.Unreachable ? "Writing to the band failed" : "Writing to the band succeeded");

                    Debug.WriteLine(msg);
#endif
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("** Error ** - " + ex.Message);
            }
        }
        #endregion


        #region Decode Band Response
        private async Task DecodeBandResponse()
        {
            while (_workItemDecrypt != null && _workItemDecrypt.Status != AsyncStatus.Canceled)
            {
                int count;
                lock (lockQueueResponsePackets)
                {
                    count = QueueResponsePackets.Count;
                }

                if ((QueueResponsePackets != null) && (count > 0) && (_isConnecting == false) && (_decodeResponseBusy == 0))
                {
                    // Doing work here......
                    await DecodeResponsePacket();
                }
                else
                {
                    // Sleep and wait here for new Packets from the Band
                    await Task.Delay(_threadPoolSleepTime);
                }
            }
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
#if DEBUG
            var msg = new StringBuilder();

            msg.AppendFormat("\n");
            msg.AppendFormat("Read Packet In:\n");
            msg.AppendFormat("===============\n");
            msg.AppendFormat("    Packet Data:     " + Utility.ByteArray2String(buffer, true) + "\n");
            msg.AppendFormat("    Sequence Number: " + buffer[0].ToString("00") + "\n");
#endif

            var packet = new byte[buffer.Length - 1];
            Buffer.BlockCopy(buffer, 1, packet, 0, buffer.Length - 1);

            // Here we do the Decryption of the data
#if DEBUG
            msg.AppendFormat("    Before Decrypt:  " + Utility.ByteArray2String(packet, true) + "\n");
#endif
            Decrypt(packet, 0, packet.Length);

#if DEBUG
            msg.AppendFormat("    After Decrypt:   " + Utility.ByteArray2String(packet, true) + "\n");
            Debug.WriteLine(msg);
#endif

            Buffer.BlockCopy(packet, 0, buffer, 1, buffer.Length - 1);

            if (_rawPacket == null)
            {
                _rawPacket = new RawPacket(packet, _reminder);
            }
            else
            {
                _rawPacket.Add(buffer);
            }

            if (_rawPacket.Reminder != null)
            {
                _reminder = new byte[_rawPacket.Reminder.Length];
                Buffer.BlockCopy(_rawPacket.Reminder, 0, _reminder, 0, _rawPacket.Reminder.Length);
            }
            else
            {
                _reminder = null;
            }

            if (_rawPacket.IsComplete)
            {
                lock (lockQueueResponsePackets)
                {
                    QueueResponsePackets.Add(_rawPacket);
                }
                _rawPacket = null;
            }
        }

        private async Task DecodeResponsePacket()
        {
            _decodeResponseBusy = Interlocked.Increment(ref _decodeResponseBusy);

            if (QueueResponsePackets != null)
            {
                RawPacket rawPacket;

                lock (lockQueueResponsePackets)
                {
                    rawPacket = QueueResponsePackets[0];
                }

                Debug.Assert(rawPacket != null && rawPacket.Data.Length > 0);

#if DEBUG
                var msg = new StringBuilder();

                msg.AppendFormat("\n\n");
                msg.AppendFormat("Decode Packet:\n");
                msg.AppendFormat("++++++++++++++\n");
                //DumpPacket( rawPacket );
#endif

                var bandResponse = new Response(rawPacket.Data);

#if DEBUG
                msg.AppendFormat("    Header:\n");
                msg.AppendFormat("        ResponseType:    " + "0x" + bandResponse.Header.ResponseType.ToString("X2") + "\n");
                msg.AppendFormat("        Flags:           " + Enum.GetName(typeof(ResponseStatus), bandResponse.Header.Flags) + "\n");
                msg.AppendFormat("        TransactionId:   " + "0x" + bandResponse.Header.TransactionId.ToString("X2") + "\n");
                msg.AppendFormat("        PayloadSize:     " + bandResponse.Header.PayloadSize + "\n");
                msg.AppendFormat("    Payload:\n");
                msg.AppendFormat("        Payload:         " + Utility.ByteArray2String(rawPacket.Data, true) + "\n");
                msg.AppendFormat("\n");
#endif

                switch (bandResponse.Header.ResponseType)
                {
                    // 0x84 - New Battery Reading 
                    case (byte)DeviceMaintenanceIds.NewBatteryReading:
                        BatteryReading batteryReading = Utility.BytesToStruct<BatteryReading>(bandResponse.Payload);
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => bandData.Battery = batteryReading.Percentage);

#if DEBUG
                        msg.AppendFormat("    New Battery Reading\n");
                        msg.AppendFormat("        Flags:      " + "0x" + batteryReading.Flags.ToString("X2") + "\n");
                        msg.AppendFormat("        Percentage: " + batteryReading.Percentage.ToString("##0") + "%" + "\n");
                        msg.AppendFormat("        Voltage:    " + batteryReading.Voltage.ToString("#,##0") + "mV" + "\n");

                        Debug.WriteLine(msg);
#endif
                        break;

                    // 0x71 - Notification from the device that walking started 
                    case (byte)MotionRequestIds.BeginWalking:
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => bandData.IsWalking = true);

#if DEBUG
                        msg.AppendFormat("    Start Walking\n");
                        Debug.WriteLine(msg);
#endif
                        break;

                    // 0x72 - Notification from the device that walking stopped 
                    case (byte)MotionRequestIds.EndWalking:
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => bandData.IsWalking = false);

#if DEBUG
                        msg.AppendFormat("    End Walking\n");
                        Debug.WriteLine(msg);
#endif
                        break;

                    // 0x31 - Tick Record (Broadcast)
                    case (byte)TickMessageType.TickRecord:
                        var rawTickRecord = Utility.BytesToStruct<RawTickRecord>(bandResponse.Payload);

#if DEBUG
                        msg.AppendFormat("    Tick Record\n");
                        msg.AppendFormat("        DurationInSeconds:     " + rawTickRecord.DurationInSeconds + "\n");
                        msg.AppendFormat("        Tick:                  " + Utility.Ulong2String(rawTickRecord.Tick) + "\n");
                        msg.AppendFormat("        EPochTime:\n");
                        msg.AppendFormat("            EpochId:           " + rawTickRecord.Timestamp.EpochId + "\n");
                        msg.AppendFormat("            SecondsSinceEpoch: " + rawTickRecord.Timestamp.SecondsSinceEpoch + " - (" + Utility.Seconds2String(rawTickRecord.Timestamp.SecondsSinceEpoch) + ")" + "\n");
#endif

                        // Make sure the EpochID already exist in the Dictionary
                        if (_dictionaryEpochTimes != null & _dictionaryEpochTimes.Any() && !_dictionaryEpochTimes.ContainsKey(rawTickRecord.Timestamp.EpochId))
                        {
#if DEBUG
                            msg.AppendFormat("    Tick Record Error, EpochID does not exist\n");
                            Debug.WriteLine(msg);
#endif

                            // 1. Request the Epoch IDs
                            SendEpochTimeRequest();

                            // 2. Request the Tick for the last 3 minutes...
                            SendTickRequestReplay(DateTime.Now.AddMinutes(-3), DateTime.Now);
                            break;
                        }

                        var tickRecord = new TickRecord(rawTickRecord, _dictionaryEpochTimes);

#if DEBUG
                        msg.AppendFormat(tickRecord.ToString());

                        //UpdateTickRecordsDictionary( tickRecord );

                        // Here we save the TickRecord into a Tick Records Database
                        msg.AppendFormat("    SQLITE Database Count (1): " + Database.Database.CountTickRecords() + "\n");
                        msg.AppendFormat(Database.Database.DoesRecordExist(tickRecord.Key) == false ? "       +Add New Record\n" : "       =Update Existing Record\n");
#endif

                        // Save the Tick Record into the database
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => Database.Database.AddOrUpdateTickRecord(tickRecord));
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => UpdateBandData(tickRecord));

#if DEBUG
                        msg.AppendFormat(bandData.ToString());
                        Debug.WriteLine(msg);
#endif
                        break;

                    // 0x32 - Epoch
                    case (byte)TickMessageType.Epoch:
#if DEBUG
                        Debug.WriteLine(msg);
#endif

                        DecodeEpoch(bandResponse);
                        break;

                    // 0xFD - BeginTransaction
                    case (byte)MessageReponseTypes.BeginTransaction:
#if DEBUG
                        Debug.WriteLine(msg);
#endif

                        _isLongTransaction = true;
                        DecodeBeginTransaction(bandResponse);
                        break;

                    // 0xFC - Continue Transaction
                    case (byte)MessageReponseTypes.ContinueTransaction:
#if DEBUG
                        Debug.WriteLine(msg);
#endif

                        await DecodeContinueTransaction(bandResponse);
                        break;

                    // 0xFE - MessageResponse
                    case (byte)MessageReponseTypes.EndTransaction:
#if DEBUG
                        Debug.WriteLine(msg);
#endif

                        await DecodeMessageResponse(bandResponse);
                        _isLongTransaction = false;
                        break;

                    // 0x01 - SetConnectionSpeed
                    case (byte)PairingMsgTypes.SetConnectionSpeed:
#if DEBUG
                        Debug.WriteLine(msg);
#endif

                        DecodeConnectionSpeedResponse(bandResponse);
                        break;

                    default:
                        //_characteristicsTxDataIn.Clear();
                        //_neededBytes = 0;
                        //_rawPacket = null;

#if DEBUG
                        msg.AppendFormat("Unkown Band Response:  0x" + bandResponse.Header.ResponseType.ToString("X2") + "\n");
                        //msg.AppendFormat( "Initializing Parameters" + "\n" );
                        Debug.WriteLine(msg);
#endif
                        break;
                }
            }

            lock (lockQueueResponsePackets)
            {
                QueueResponsePackets?.RemoveAt(0);
            }

            _decodeResponseBusy = Interlocked.Decrement(ref _decodeResponseBusy);
        }

        private async Task DecodeMessageResponse(Response bandResponse)
        {
            if (!_dictionaryTransactionIds.ContainsKey(bandResponse.Header.TransactionId))
            {
                return;
            }

#if DEBUG
            var msg = new StringBuilder();

            msg.AppendFormat("    DecodeMessgeResponse\n");
            msg.AppendFormat("    ~~~~~~~~~~~~~~~~~~~~\n");
#endif

            var messageType = _dictionaryTransactionIds[bandResponse.Header.TransactionId];
            if (messageType == (byte)TickMessageType.QueryEpochs)             // 0x35
            {
#if DEBUG
                msg.AppendFormat("        Message Type:         " + "0x" + messageType.ToString("X2") + " - " + Enum.GetName(typeof(TickMessageType), messageType) + "\n");
#endif

                DecodeEpoch(bandResponse);
                _dictionaryTransactionIds.Remove(bandResponse.Header.TransactionId);

#if DEBUG
                Debug.WriteLine(msg);
#endif
                return;
            }

            // We got the Transaction Complete Message
            if (messageType == (byte)TickMessageType.Replay)                  // 0x34
            {
#if DEBUG
                msg.AppendFormat("        Message Type:         " + "0x" + messageType.ToString("X2") + " - " + Enum.GetName(typeof(TickMessageType), messageType) + "\n");
#endif

                if (_isTickRequestBeginTransaction)
                {
                    // Here we should save the data into the database
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                       {
                           if (TickRecords != null && TickRecords.Any())
                           {
                               Database.Database.AddAllTickRecords(TickRecords);

                               if (TickRecords.Last() != null)
                               {
                                   UpdateBandData(TickRecords.Last());
                               }

                               // Clear the TickRecords list
                               TickRecords.Clear();
                           }
                       });

#if DEBUG
                    msg.AppendFormat("            Long Replay Complete!! - " + TickRecords.Count.ToString("##,###,###") + "\n");
#endif

                    _dictionaryTransactionIds.Remove(bandResponse.Header.TransactionId);

                    _isLongTransaction = false;
                    _isTickRequestBeginTransaction = false;
                }

#if DEBUG
                Debug.WriteLine(msg);
#endif
                return;
            }

            // We got the the SetTickRecordRate response
            if (messageType == (byte)TickMessageType.SetTickRecordRate)       // 0x38
            {
#if DEBUG
                msg.AppendFormat("        Message Type:         " + "0x" + messageType.ToString("X2") + " - " + Enum.GetName(typeof(TickMessageType), messageType) + "\n");
#endif

                _dictionaryTransactionIds.Remove(bandResponse.Header.TransactionId);

#if DEBUG
                Debug.WriteLine(msg);
#endif
                return;
            }

            // We got the the SetPersonData response
            if (messageType == (byte)SettingsIds.SetPersonData)               // 0xC0
            {
#if DEBUG
                msg.AppendFormat("        Message Type:         " + "0x" + messageType.ToString("X2") + " - " + Enum.GetName(typeof(SettingsIds), messageType) + "\n");
#endif

                int responseSize = Marshal.SizeOf<SettingsSyncVersionResponse>();
                if (bandResponse.Payload == null || bandResponse.Payload.Length < responseSize)
                {
                    throw new JawboneException(JawboneErrorCodes.INVALID_PERSON_DATA_REPONSE);
                }

                SettingsSyncVersionResponse personDataResponse = Utility.BytesToStruct<SettingsSyncVersionResponse>(bandResponse.Payload);

#if DEBUG
                msg.AppendFormat("                Sync Version: " + personDataResponse.SyncVersion + "\n");
                Debug.WriteLine(msg);
#endif
                return;
            }


            if (messageType == (byte)SettingsIds.GetSyncVersions)             // 0xC2
            {
#if DEBUG
                msg.AppendFormat("        Message Type:         " + "0x" + messageType.ToString("X2") + " - " + Enum.GetName(typeof(SettingsIds), messageType) + "\n");
                msg.AppendFormat("            Sync Versions:    " + Utility.ByteArray2String(bandResponse.Payload, true) + "\n");
#endif

                // Validate the response header size
                int responseSize = Marshal.SizeOf<AllSettingsSyncVersionsResponse>();
                if (bandResponse.Payload == null || bandResponse.Payload.Length < responseSize)
                {
                    throw new JawboneException(JawboneErrorCodes.SETTINGS_SYNC_VERSIONS_INCOMPLETE_RESPONSE);
                }

                AllSettingsSyncVersionsResponse versionsResponse = Utility.BytesToStruct<AllSettingsSyncVersionsResponse>(bandResponse.Payload);
                _lemondSettingsSyncVersions = new LemondSettingsSyncVersions((int)versionsResponse.SmartAlarmSyncVersion,
                                                                              (int)versionsResponse.IdleAlertSyncVersion,
                                                                              (int)versionsResponse.DemographicsSyncVersion,
                                                                              (int)versionsResponse.GoalsSyncVersion);

#if DEBUG
                msg.AppendFormat("                SmartAlarm:   " + _lemondSettingsSyncVersions.SmartAlarmSyncVersion + "\n");
                msg.AppendFormat("                IdleAlert:    " + _lemondSettingsSyncVersions.IdleAlertSyncVersion + "\n");
                msg.AppendFormat("                Demographics: " + _lemondSettingsSyncVersions.DemographicsSyncVersion + "\n");
                msg.AppendFormat("                Goals:        " + _lemondSettingsSyncVersions.GoalsSyncVersion + "\n");
#endif

                _dictionaryTransactionIds.Remove(bandResponse.Header.TransactionId);

#if DEBUG
                Debug.WriteLine(msg);
#endif
                return;
            }

#if DEBUG
            msg.AppendFormat("        Message Type Unkonwn: " + "0x" + messageType.ToString("X2") + "\n");
            Debug.WriteLine(msg);
#endif
        }

        private void DecodeEpoch(Response bandResponse)
        {
            var epochsCount = (bandResponse.Payload[1] << 8) + bandResponse.Payload[0];
            var epochEntrySize = Marshal.SizeOf<EpochEntry>();

#if DEBUG
            var msg = new StringBuilder();
#endif

            int index = Marshal.SizeOf<ushort>();
            for (int i = 0; i < epochsCount; i++)
            {
                byte[] epochEntryBytes = new byte[epochEntrySize];
                Buffer.BlockCopy(bandResponse.Payload, index, epochEntryBytes, 0, epochEntrySize);

                EpochEntry epochEntry = Utility.BytesToStruct<EpochEntry>(epochEntryBytes);
                var epochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(epochEntry.StartTime);


                if (_dictionaryEpochTimes.ContainsKey(epochEntry.EpochId))
                {
                    _dictionaryEpochTimes[epochEntry.EpochId] = epochDateTime;
#if DEBUG
                    msg.AppendFormat("\n           -Update Epoch Key: " + epochEntry.EpochId + " - " + epochDateTime.ToLocalTime() + "\n");
#endif
                }
                else
                {
                    _dictionaryEpochTimes.Add(epochEntry.EpochId, epochDateTime);
#if DEBUG
                    msg.AppendFormat("\n           +Add Epoch Key:    " + epochEntry.EpochId + " - " + epochDateTime.ToLocalTime() + "\n");
#endif
                }

                //UpdateEpochDictionary( epochEntry.EpochId, epochDateTime );

                index += epochEntrySize;

#if DEBUG
                msg.AppendFormat("            Epoch Index:      " + (i + 1) + "\n");
                msg.AppendFormat("                EpochId:      " + epochEntry.EpochId + "\n");
                msg.AppendFormat("                StartTime:    " + epochEntry.StartTime + "\n");
                msg.AppendFormat("            EpochDateTime:    " + epochDateTime + "\n\n");
#endif
            }

#if DEBUG
            Debug.WriteLine(msg);
#endif
        }

        private void DecodeConnectionSpeedResponse(Response bandResponse)
        {
            var payloadSize = Marshal.SizeOf<SetConnectionSpeedResponse>();
            if (bandResponse.Header.PayloadSize == payloadSize)
            {
                SetConnectionSpeedResponse connectionSpeed = Utility.BytesToStruct<SetConnectionSpeedResponse>(bandResponse.Payload);

                Debug.WriteLine("");
                Debug.WriteLine("    Connection Speed");
                Debug.WriteLine("        CurrentConnectionInterval:    " + connectionSpeed.CurrentConnectionIntervalInMilliSecs + "(MilliSecs)");
                Debug.WriteLine("        CurrentSpeed:                 " + connectionSpeed.CurrentSpeed);
            }
        }

        private void DecodeBeginTransaction(Response bandResponse)
        {
            if (!_dictionaryTransactionIds.ContainsKey(bandResponse.Header.TransactionId))
            {
                return;
            }

            var messageType = _dictionaryTransactionIds[bandResponse.Header.TransactionId];
            Debug.WriteLine("        messageType:               " + "0x" + messageType.ToString("X2"));

            if (messageType == (byte)TickMessageType.Replay)              // 0x34
            {
                if (bandResponse.Header.PayloadSize == Marshal.SizeOf<TickRequestBeginTransaction>())
                {
                    _tickRequestBeginTransaction = Utility.BytesToStruct<TickRequestBeginTransaction>(bandResponse.Payload);

#if DEBUG
                    var msg = new StringBuilder();

                    msg.AppendFormat("        Tick Record Begin Transaction\n");
                    msg.AppendFormat("            TotalNoRecords:        " + _tickRequestBeginTransaction.TotalNoRecords + "\n");
                    msg.AppendFormat("            Timestamp:             " + _tickRequestBeginTransaction.Timestamp + "\n");
                    msg.AppendFormat("            Time:                  " + Extensions.FromUnixTime(_tickRequestBeginTransaction.Timestamp) + "\n");

                    Debug.WriteLine(msg);
#endif

                    _isTickRequestBeginTransaction = true;
                }
            }
        }

        private async Task DecodeContinueTransaction(Response bandResponse)
        {
            if (!_dictionaryTransactionIds.ContainsKey(bandResponse.Header.TransactionId))
            {
                return;
            }
            var messageType = _dictionaryTransactionIds[bandResponse.Header.TransactionId];

#if DEBUG
            var msg = new StringBuilder();
            msg.AppendFormat("    MessageType:               " + "0x" + messageType.ToString("X2") + "\n");
#endif

            // This is the Long Replay Continue
            if (messageType == (byte)TickMessageType.Replay)              // 0x34
            {
#if DEBUG
                msg.AppendFormat("    Tick Record Continue Transaction\n");
                msg.AppendFormat("        Payload:               " + Utility.ByteArray2String(bandResponse.Payload, true) + "\n");
                msg.AppendFormat("\n");
#endif

                // Decode the ticks here
                var size = (byte)Marshal.SizeOf<RawTickRecord>();
                byte[] data = new byte[size];
                var count = bandResponse.Payload.Length / size;

                if (bandResponse.Payload.Length % size != 0)
                {
                    Debug.Assert(bandResponse.Payload.Length % size == 0, "DecodeContinueTransaction - Tick Record Size Error");
#if DEBUG
                    Debug.WriteLine(msg);
#endif
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    Buffer.BlockCopy(bandResponse.Payload, i * size, data, 0, size);
                    var rawTickRecord = Utility.BytesToStruct<RawTickRecord>(data);

                    var tickRecord = new TickRecord(rawTickRecord, _dictionaryEpochTimes);

#if DEBUG
                    msg.AppendFormat("    Tick Record:               " + (i + 1) + "\n");
                    msg.AppendFormat("        DurationInSeconds:     " + rawTickRecord.DurationInSeconds + "\n");
                    msg.AppendFormat("        Tick:                  " + Utility.Ulong2String(rawTickRecord.Tick) + "\n");
                    msg.AppendFormat("        EPochTime:\n");
                    msg.AppendFormat("            EpochId:           " + rawTickRecord.Timestamp.EpochId + "\n");
                    msg.AppendFormat("            SecondsSinceEpoch: " + rawTickRecord.Timestamp.SecondsSinceEpoch + " - (" + Utility.Seconds2String(rawTickRecord.Timestamp.SecondsSinceEpoch) + ")" + "\n");
                    msg.AppendFormat(tickRecord.ToString());
#endif

                    // Here we save the data temporaritly into an ObservableCollection
                    TickRecords.Add(tickRecord);

#if DEBUG
                    msg.AppendFormat("    SQLITE Database Count (2): " + TickRecords.Count.ToString("##,###,###") + "\n");
                    msg.AppendFormat(bandData.ToString());
#endif
                }

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                   {
                       if (TickRecords != null && TickRecords.Any())
                       {
                           Database.Database.AddAllTickRecords(TickRecords);

                           if (TickRecords.Last() != null)
                           {
                               UpdateBandData(TickRecords.Last());
                           }

                           // Clear the TickRecords list
                           TickRecords.Clear();
                       }
                   });

#if DEBUG
                Debug.WriteLine(msg);
#endif
            }
        }

        private void UpdateBandData(TickRecord tickRecord)
        {
            bandData.Steps = Database.Database.CountSteps(DateTime.Today, DateTime.Now);
            bandData.DistanceInMeters = Database.Database.SumDistance(DateTime.Today, DateTime.Now);

            bandData.MeanHeartRate = tickRecord.MeanHeartRate;

            bandData.Count = Database.Database.CountTickRecords();

            bandData.IsPossibleBandRemoved = tickRecord.IsPossibleBandRemoved;
            bandData.IsBatteryCharging = tickRecord.IsBatteryCharging;

            bandData.LastTick = string.Format("{0}", tickRecord.TickEndDate.ToLocalTime());

            GridBandData.DataContext = bandData;
            TotalSteps.DataContext = bandData;
            Distance.DataContext = bandData;
        }

        private async void UpdateBandData(int Count)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () => LastTick.Text = Count.ToString("#,###,###"));
        }

        private void DecodeCommandResponse(byte[] buffer)
        {
            if (buffer[1] != 0xFE)
            {
                return;
            }

            byte requestStatus = buffer[1 + 0x01];
            byte trasactionId = buffer[1 + 0x02];
            byte payloadSize = buffer[1 + 0x03];
            var vendorId = (buffer[1 + 0x04] << 8) + buffer[1 + 0x05];
            var protocolNo = (buffer[1 + 0x06] << 8) + buffer[1 + 0x07];
            byte majorProtocolVer = buffer[1 + 0x08];
            byte minorProtocolVar = buffer[1 + 0x09];

            if (payloadSize != 9)
            {
                return;
            }

            byte authenticationProtocolVer = buffer[1 + 0x0A];
            byte otaMajorVer = buffer[1 + 0x0B];
            byte otaMinorVer = buffer[1 + 0x0C];
        }
        #endregion


        #region Update Dictionaries
        private void UpdateEpochDictionary(uint key, DateTime dateTime)
        {
            if (_dictionaryEpochTimes.ContainsKey(key))
            {
                _dictionaryEpochTimes[key] = dateTime;
            }
            else
            {
                _dictionaryEpochTimes.Add(key, dateTime);
            }
        }
        #endregion


        #region Encryption/Decription
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // _key:                Secret key, random 16 bytes shared by phone and band                              //
        // IV:                  Initialization vector, 16 bytes                                                   //
        // AES_ECB:             Block encryption for 16 bytes, no chaining                                        //
        // AES_OFB:             Output feedback encryption for 16 bytes, chaining with an initial IV              //
        // PhoneChallenge:      Phone challenge, random non-zero 8 bytes generated by phone                       //
        // BandChallenge:       Band challenge, random non-zero 8 bytes generated by band                         //
        // Z:                   Zero 8 bytes { 0, 0, 0, 0, 0, 0, 0, 0 }                                           //
        // PhoneResponse:       Phone response, 16 bytes                                                          //
        // BandResponse:        Band response, 16 bytes                                                           //
        // [8 bytes, 8 bytes]:  Byte concatenation                                                                //
        //                      Example: Given PhoneChallenge = {1,2,3,4,5,6,7,8} and Z = {0,0,0,0,0,0,0,0}       //
        //                                   [PhoneChallenge,Z]    = {1,2,3,4,5,6,7,8,0,0,0,0,0,0,0,0}            //
        //                                   [PhoneChallenge,Z][0] = 1 and [PhoneChallenge,Z][15] = 0             //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////


        private static byte[] Encrypt(byte[] data)
        {
            if ((_streamEncryptor != null) && (_streamEncryptor.IsEnabled) && (data.Length > 0))
            {
                return _streamEncryptor.EncryptAsync(data);
            }

            return data;
        }

        private static void Decrypt(byte[] data, int index, int len)
        {
            if ((_streamEncryptor != null) && (_streamEncryptor.IsEnabled) && (len > 0))
            {
                _streamEncryptor.DecryptAsync(data, index, len);
            }
        }

        private void EnableEncryption(bool isEnabled)
        {
            if (_streamEncryptor != null)
            {
                _streamEncryptor.IsEnabled = isEnabled;
            }
        }
        #endregion


        #region Control Service - JAWBONE_CONTROL_SERVICE_UUID
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

        private async Task SendSetDateTimeCommand()
        {
            if (btleDevice2.ConnectionStatus != BluetoothConnectionStatus.Connected)
            {
                return;
            }

            // Send a Set DateTime Command here...
            Debug.Assert(_characteristicsControlDatetime != null);

            Debug.WriteLine(string.Format("\n\n== {0} - Set DateTime Message", "0x" + _transactionId.ToString("X2")));

            var command = SetDateCommandData();
            try
            {
                // Send the packet to the Band
                await SendPacketAsync(_characteristicsControlDatetime[0], command, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("** Error ** - SendSetDateTimeCommand: " + ex.Message);
            }

            _rootPage.NotifyUser("Set Date and Time Command", NotifyType.StatusMessage);
        }

        private byte[] SetDateCommandData()
        {
            DateTime now = DateTime.UtcNow;
            LemondDateTime deviceDateTime = new LemondDateTime
            {
                Hour = (byte)now.Hour,
                Minutes = (byte)now.Minute,
                Seconds = (byte)now.Second,
                Month = (byte)now.Month,
                DayOfMonth = (byte)now.Day,
                Pad = 0,
                Year = (ushort)now.Year,
                SecondsOffsetToLocalTime = (int)now.ToLocalTime().Ticks
            };

            byte[] writeData = Utility.StructToBytes(deviceDateTime);
            return writeData;
        }

        private async Task SetConnectModeAsync(ConnectModes connectMode)
        {
            Debug.Assert(_characteristicsControlConnectMode != null);

            byte[] writeData = { (byte)connectMode };
            GattCommunicationStatus status = await _characteristicsControlConnectMode[0].WriteValueAsync(writeData.AsBuffer(), GattWriteOption.WriteWithResponse);
            if (status != GattCommunicationStatus.Success)
            {
                throw new JawboneException(JawboneErrorCodes.GATT_COMMUNICATION_FAILED);
            }
        }

        public async Task DisconnectAsync()
        {
            await SetConnectModeAsync(ConnectModes.Connect);
        }

        public async Task ResetToPairingModeAsync()
        {
            await SetConnectModeAsync(ConnectModes.Bonding);
        }
        #endregion


        #region Information Service - DEVICE_INFORMATION_SERVICE_UUID
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
        #endregion


        #region Phone Utilities
        public async void NotifyUser(string strMessage, NotifyType type)
        {
            await _rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
           {
               switch (type)
               {
                   case NotifyType.StatusMessage:
                       StatusBorder.Background = new SolidColorBrush(Colors.Green);
                       break;

                   case NotifyType.ErrorMessage:
                       StatusBorder.Background = new SolidColorBrush(Colors.Red);
                       break;
               }

               StatusBlock.Text = strMessage;

               // Collapse the StatusBlock if it has no text to conserve real estate.
               StatusBorder.Visibility = StatusBlock.Text != string.Empty ? Visibility.Visible : Visibility.Collapsed;
               if (StatusBlock.Text != string.Empty)
               {
                   StatusBorder.Visibility = Visibility.Visible;
                   StatusPanel.Visibility = Visibility.Visible;
               }
               else
               {
                   StatusBorder.Visibility = Visibility.Collapsed;
                   StatusPanel.Visibility = Visibility.Collapsed;
               }
           });
        }

        //public void PlayVibrationPattern(LemondVibrationPattern pattern)
        //{
        //    //CheckErrorCode();

        //    VibrationPatternRequest request = new VibrationPatternRequest();

        //    //memset(request.Id, 0, sizeof(request.IdCount)); ???
        //    Buffer.SetByte(request.Id, 0, 0x00);

        //    int i = 0;
        //    foreach (LemondVibrationPatternSlot slot in pattern.VibrationSlots)
        //    {
        //        if (slot.Specifier == VibrationPatternSpecifier.WaveFormID)
        //        {
        //            request.Id[i] = (byte)slot.Value;
        //        }
        //        else
        //        {
        //            //test the explicit cast
        //            request.Id[i] = LemondVibrationPatternSlot.CorrectBitFromDelayIn10MilliSec((int)slot.Value);
        //        }

        //        i++;

        //        if (i >= VibrationTypes.VIBRATION_PATTERN_SLOT_MAX)
        //        {
        //            Debug.WriteLine("Vibration pattern is too long");
        //            break;
        //        }
        //    }

        //    request.IdCount = (byte)i;
        //    byte[] requestBytes = Utility.StructToBytes(request);

        //    //                                     Message ID,                             Payload
        //    //await messageLayer.SendMessageAsync( (byte)SettingsIds.VibrationPlayPattern, requestBytes );    

        //    Debug.WriteLine(string.Format("\n\n== {0} - Play Vibration Pattern", "0x" + _transactionId.ToString("X2")));
        //    _messageType = (byte)SettingsIds.VibrationPlayPattern;
        //    _flags = 0x00;
        //    _payloadSize = (byte)requestBytes.Length;
        //    byte[] payload = new byte[_payloadSize];
        //    Buffer.BlockCopy(payload, 0, requestBytes, 0, _payloadSize);

        //    var msgHeader = new TransactionHeader((PairingMsgTypes)_messageType, _flags, _transactionId, _payloadSize);
        //    var transaction = new Transaction(_characteristicsStreamRx[0], msgHeader, payload);
        //    SaveTransaction(transaction);

        //    _rootPage.NotifyUser("Play Vibration Pattern", NotifyType.StatusMessage);
        //}
        #endregion


        #region Debug Utilities
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

        //        private void DumpPacket(RawPacket rawPacket)
        //        {
        //#if DEBUG
        //            var msg = new StringBuilder();

        //            msg.AppendFormat("\n");
        //            msg.AppendFormat("    Raw Packet: " + Utility.ByteArray2String((byte[])_characteristicsTxDataIn.ToArray(typeof(byte)), true) + "\n");
        //            msg.AppendFormat("        Header:\n");
        //            msg.AppendFormat("            ResponseType:     " + Enum.GetName(typeof(MessageReponseTypes), (MessageReponseTypes)rawPacket.Data[(int)HdrOffset.MessageType]) + "\n");
        //            msg.AppendFormat("            RequestStatus:    " + Enum.GetName(typeof(ResponseStatus), (ResponseStatus)rawPacket.Data[(int)HdrOffset.Flags]) + "\n");
        //            msg.AppendFormat("            TrasactionId:     " + "0x" + rawPacket.Data[(int)HdrOffset.TransactionID].ToString("X2") + "\n");
        //            msg.AppendFormat("            PayloadSize:      " + rawPacket.Data[(int)HdrOffset.PayloadSize] + "\n");
        //            msg.AppendFormat("        Payload:\n");
        //            msg.AppendFormat("            Payload:          " + Utility.ByteArray2String(rawPacket.Data, true) + "\n");

        //            Debug.WriteLine(msg);
        //#endif
        //        }

        //        private async Task Debug_EnumerateDevices()
        //        {
        //            // Code runs fine on this call
        //            DeviceInformationCollection deviceInfoCollection = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelectorFromPairingState(false));

        //            // Get the address of the BluetoothLE device
        //            foreach (DeviceInformation deviceInfo in deviceInfoCollection)
        //            {
        //                // Code is crashing on this call
        //                BluetoothDevice bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
        //                // bluetoothDevice is a BluetoothDevice object and has RfcommServices and bd.BluetoothAddress properties

        //                Debug.WriteLine("Address: " + bluetoothDevice.BluetoothAddress + "  --  " + Utility.BtleAddress2String(bluetoothDevice.BluetoothAddress));
        //            }

        //            int index = 1;
        //            Debug.WriteLine("devices.Count: " + deviceInfoCollection.Count);
        //            foreach (DeviceInformation deviceInfo in deviceInfoCollection)
        //            {
        //                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
        //               {
        //                   var btleDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
        //                   Debug.WriteLine("btleDevice.DeviceId: " + btleDevice.DeviceId);
        //               });


        //#if DEBUG
        //                Debug_DisplayDeviceParams(deviceInfo, index);
        //#endif
        //                index++;
        //            }
        //        }

        //private static void Debug_ListManufactureData(IList<BluetoothLEManufacturerData> manufactureData)
        //{
        //    foreach (BluetoothLEManufacturerData manData in manufactureData)
        //    {
        //        IBuffer buffer1 = manData.Data;
        //        var strData = Utility.ReadIBuffer2Str(buffer1);
        //        Debug.WriteLine(string.Format("                                CompanyId: {0} - Data: {1}", manData.CompanyId, strData));
        //    }
        //}

        //private static void Debug_ListServiceUuids(IList<Guid> serviceUuids)
        //{
        //    foreach (Guid uuid in serviceUuids)
        //    {
        //        Debug.WriteLine("                                UUID: " + uuid);
        //    }
        //}

        private static void Debug_DisplayDeviceParams(DeviceInformation deviceInfo, int index)
        {
            string name = deviceInfo.Name;

            // Bluetooth / Specifications / Assigned Numbers
            // https://www.bluetooth.com/specifications/assigned-numbers
            // 0x18xx are GATT Specifications / Services
            // https://developer.bluetooth.org/gatt/services/Pages/ServicesHome.aspx    


            // 1800 - The generic_access service contains generic information about the device. All available Characteristics are readonly. 
            // 1801 - The generic_attribute service. 
            // 180a - The Device Information Service exposes manufacturer and/or vendor information about a device. 


            // Decode the Type
            int end = deviceInfo.Id.IndexOf('#');
            string type = deviceInfo.Id.Substring(0, end);

            // Decode the ID
            string id = deviceInfo.Id;

            // Decode the Address
            ulong btleAddress = DeviceInformationDisplay.DecodeBtleAddress(deviceInfo);

            //var btleDevice = await BluetoothLEDevice.FromBluetoothAddressAsync( btleAddress );       

            Debug.WriteLine("index: {0,-4} - Name: {1,-20} \n    Type:     {2,-10} \n    Address:  {3,-20} \n    ID:       {4}", index, name, type, Utility.BtleAddress2String(btleAddress), id);

            int j = 1;
            foreach (KeyValuePair<string, object> property in deviceInfo.Properties)
            {
                Debug.WriteLine("   - {0, -6}   {1,-60}   {2,-40}  ", j++, property.Key, property.Value);
            }

            Debug.WriteLine("---------------------------------------------------------------" + "\n");
        }

        //private static void Debug_ListCharacteristics(IReadOnlyList<GattCharacteristic> characteristics, string msg)
        //{
        //    // ObjectDumper 

        //    Debug.WriteLine("********************************************************************************************************************" + "\n");
        //    Debug.WriteLine(msg + "\n");

        //    Debug.WriteLine("============================================================================");
        //    var count1 = 1;
        //    foreach (GattCharacteristic characteristic in characteristics)
        //    {
        //        Debug.WriteLine("Count1: " + count1++);

        //        //foreach ( var format in characteristic.PresentationFormats )
        //        //    {
        //        //    Debug.WriteLine( "        " + format.Description );
        //        //    Debug.WriteLine( "        " + format.Exponent );
        //        //    Debug.WriteLine( "        " + format.FormatType );
        //        //    Debug.WriteLine( "        " + format.Namespace );
        //        //    Debug.WriteLine( "        " + format.Unit );
        //        //    }

        //        Debug.WriteLine("       characteristic.AttributeHandle:          " + characteristic.AttributeHandle);
        //        Debug.WriteLine("       characteristic.ProtectionLevel:          " + characteristic.ProtectionLevel);
        //        Debug.WriteLine("       characteristic.CharacteristicProperties: " + characteristic.CharacteristicProperties);
        //        Debug.WriteLine("       characteristic.UserDescription:          " + characteristic.UserDescription);
        //        Debug.WriteLine("       characteristic.Uuid:                     " + characteristic.Uuid);
        //        Debug.WriteLine("       characteristic.Service:                  " + characteristic.Service);           // GattDeviceService

        //        ObjectDumper.Write(characteristic.Service, 0);

        //        Debug.WriteLine("       ");

        //        // https://msdn.microsoft.com/en-us/library/windows/apps/windows.devices.bluetooth.genericattributeprofile.gattcharacteristic.aspx
        //        IReadOnlyList<GattDescriptor> descriptors = characteristic.GetAllDescriptors();
        //        Debug.WriteLine("       " + "Descriptors");
        //        Debug.WriteLine("       " + "-----------");
        //        int count2 = 1;
        //        foreach (var descriptor in descriptors)
        //        {
        //            Debug.WriteLine("       Count2: " + count2++);

        //            Debug.WriteLine("           descriptor.AttributeHandle: " + descriptor.AttributeHandle);
        //            Debug.WriteLine("           descriptor.ProtectionLevel: " + descriptor.ProtectionLevel);
        //            Debug.WriteLine("           descriptor.Uuid:            " + descriptor.Uuid);
        //            Debug.WriteLine("           " + "----------------------------------");
        //        }
        //        Debug.WriteLine("       ");

        //        //Debug.WriteLine( "       " + "Value" );
        //        //Debug.WriteLine( "       " + "-----" );
        //        //GattReadResult value = await characteristic.ReadValueAsync();
        //        //Debug.WriteLine( "       value:                                   " + value );
        //        //Debug.WriteLine( "       value.Status:                            " + value.Status );
        //        //Debug.WriteLine( "       value.Value:                             " + ReadIBuffer2Str( value.Value ) );
        //        //Debug.WriteLine( "============================================================================" );
        //    }
        //}

        //private static void Debug_DecodeProperties(IReadOnlyDictionary<string, object> properties)
        //{
        //    IEnumerable<string> keys = properties.Keys;
        //    IEnumerable<object> values = properties.Values;

        //    Debug.WriteLine("\n" + "    {0,-50}{1,-50}", "Key", "Value");

        //    string[] enumerable1 = keys as string[] ?? keys.ToArray();
        //    object[] enumerable2 = values as string[] ?? values.ToArray();
        //    int count = enumerable1.Length;

        //    for (int i = 0; i < count; i++)
        //    {
        //        Debug.WriteLine("    {0,-50}{1,-50}", enumerable1[i] ?? "null", enumerable2[i] ?? "null");
        //    }
        //}

        //private static void Debug_DumpServicesCharacteristics(GattDeviceService gattService)
        //{
        //    IReadOnlyList<GattDeviceService> services = gattService.GetAllIncludedServices();
        //    foreach (var service in services)
        //    {
        //        ObjectDumper.Write(service, 0);
        //        Debug.WriteLine("\n\n\n");
        //    }


        //    IReadOnlyList<GattCharacteristic> characteristics = gattService.GetAllCharacteristics();
        //    foreach (var characteristic in characteristics)
        //    {
        //        ObjectDumper.Write(characteristic, 0);
        //        Debug.WriteLine("\n\n\n");
        //    }
        //}

        //private static void Debug_DumpDescriptors(GattCharacteristic characteristic)
        //{
        //    // Client Characteristic Configuration - "00002902-0000-1000-8000-00805f9b34fb"

        //    Debug.WriteLine(nameof(characteristic));

        //    IReadOnlyList<GattDescriptor> descriptors = characteristic.GetAllDescriptors();
        //    foreach (var descriptor in descriptors)
        //    {
        //        ObjectDumper.Write(descriptor, 0);
        //        Debug.WriteLine("\n\n\n");
        //    }
        //}

        //        private static void Debug_ListCharacteristics()
        //        {
        //#if DEBUG
        //            Debug_ListCharacteristics(_characteristicsStreamTx, "characteristicsTx");
        //            Debug_ListCharacteristics(_characteristicsStreamRx, "characteristicsRx");
        //#endif
        //        }

        //private void Debug_CreateFakeTickRecords()
        //{
        //    Database.Database.DeleteAllDatabaseTables(true);
        //    //Database.Database.ClearAllDatabase();

        //    if (Database.Database.CountTickRecords() > 0)
        //    {
        //        return;
        //    }

        //    for (int i = 0; i < (60 * 24 * 14); i++)
        //    {
        //        var tickRecord = new TickRecord(i);
        //        TickRecords.Add(tickRecord);
        //    }


        //    // diff = 0:3:122
        //    // this does Bulk Insert
        //    var start = DateTime.Now;
        //    Database.Database.AddAllTickRecords(TickRecords);
        //    var end = DateTime.Now;
        //    TimeSpan diff = new TimeSpan(end.Ticks - start.Ticks);
        //    Debug.WriteLine("Count= " + Database.Database.CountTickRecords());
        //    Debug.WriteLine("diff = " + diff.Minutes + ":" + diff.Seconds + ":" + diff.Milliseconds);


        //    // This will add one record at a time, very slow
        //    //foreach ( var tickRecord in TickRecords )
        //    //    {
        //    //    Database.Database.AddOrUpdateTickRecord( tickRecord );
        //    //    }
        //}
        #endregion
    }
}
