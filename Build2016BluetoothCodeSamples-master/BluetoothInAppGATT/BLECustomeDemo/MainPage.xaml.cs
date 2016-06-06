using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BLECustomeDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public ObservableCollection<BluetoothLEDeviceDisplay> ResultCollection
        {
            get;
            private set;
        }

        private DeviceWatcher deviceWatcher = null;
        private DeviceWatcher gattServiceWatcher = null;
        private static Guid IoServiceUuid = new Guid("151c0000-4580-4111-9ca1-5056f3454fbc");
        private static Guid OutputCommandCharacteristicGuid = new Guid("00001565-1212-efde-1523-785feabcd123");
        private GattDeviceService weDoIoService = null;
        private GattCharacteristic outputCommandCharacteristic = null;
        BluetoothLEDeviceDisplay deviceInfoDisp;

        public MainPage()
        {
            this.InitializeComponent();

            ResultCollection = new ObservableCollection<BluetoothLEDeviceDisplay>();
            resultsListView.SelectionChanged += ResultsListView_SelectionChanged;

            btnUnPair.IsEnabled = false;
            btnStop.IsEnabled = false;
        }

        private async void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            resultsListView.IsEnabled = false;


            string val = resultsListView.SelectedValue.ToString();

            deviceInfoDisp = ResultCollection.Where(r => r.Id == val).FirstOrDefault();

            this.NotifyUser("Unpairing started. Please wait...", NotifyType.StatusMessage);

            DevicePairingKinds ceremoniesSelected = DevicePairingKinds.ConfirmOnly;
            //  ProtectionLevelSelectorInfo protectionLevelInfo = (ProtectionLevelSelectorInfo)protectionLevelComboBox.SelectedItem;

            DevicePairingProtectionLevel protectionLevel = DevicePairingProtectionLevel.Default;

            DeviceInformationCustomPairing customPairing = deviceInfoDisp.DeviceInformation.Pairing.Custom;

            Debug.WriteLine("Is Paired -- " + deviceInfoDisp.IsPaired);
            customPairing.PairingRequested += PairingRequestedHandler;

            DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);

            customPairing.PairingRequested -= PairingRequestedHandler;

            if (result.Status == DevicePairingResultStatus.Paired || result.Status == DevicePairingResultStatus.AlreadyPaired)
            {
                Debug.WriteLine("Paired..");
            }
            deviceInfoDisp = ResultCollection.Where(r => r.Id == val).FirstOrDefault();
            Debug.WriteLine("Is Paired -- " + deviceInfoDisp.IsPaired);
            //this.NotifyUser(
            //    "Unpairing result = " + result.Status.ToString(),
            //    result.Status == DeviceUnpairingResultStatus.Unpaired ? NotifyType.StatusMessage : NotifyType.ErrorMessage);

            UpdateButtons(deviceInfoDisp);
            resultsListView.IsEnabled = true;
        }

        private void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            args.Accept();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.IsEnabled = false;
            btnStop.IsEnabled = true;

            // Clear any previous messages
            Debug.WriteLine("Start Watcher");

            // Enumerate all Bluetooth LE devices and display them in a list
            StartBleDeviceWatcher();
        }

        private void StartBleDeviceWatcher()
        {
            //Reset displayed results
            ResultCollection.Clear();

            // Request additional properties
            string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

            deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")",
                                                            requestedProperties,
                                                            DeviceInformationKind.AssociationEndpoint);

            // Hook up handlers for the watcher events before starting the watcher
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ResultCollection.Add(new BluetoothLEDeviceDisplay(deviceInfo));

                    this.NotifyUser(
                        String.Format("{0} devices found.", ResultCollection.Count),
                        NotifyType.StatusMessage);
                });
            });

            deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    foreach (BluetoothLEDeviceDisplay bleInfoDisp in ResultCollection)
                    {
                        if (bleInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            bleInfoDisp.Update(deviceInfoUpdate);

                            // If the item being updated is currently "selected", then update the pairing buttons
                            //BluetoothLEDeviceDisplay selectedDeviceInfoDisp = (BluetoothLEDeviceDisplay)resultsListView.SelectedItem;
                            //if (bleInfoDisp == selectedDeviceInfoDisp)
                            //{
                            //    deviceInfoDisp.Update(deviceInfoUpdate);

                            //}

                            break;
                        }
                    }
                });
            });

            deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    this.NotifyUser(
                        String.Format("{0} devices found. Enumeration completed. Watching for updates...", ResultCollection.Count),
                        NotifyType.StatusMessage);
                    resultsListView.ItemsSource = ResultCollection.Select(s => new { s.Name, s.Id });
                    resultsListView.SelectedValuePath = "Id";
                    resultsListView.DisplayMemberPath = "Name";
                });
            });

            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    // Find the corresponding DeviceInformation in the collection and remove it
                    foreach (BluetoothLEDeviceDisplay bleInfoDisp in ResultCollection)
                    {
                        if (bleInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            ResultCollection.Remove(bleInfoDisp);
                            break;
                        }
                    }

                    this.NotifyUser(
                        String.Format("{0} devices found.", ResultCollection.Count),
                        NotifyType.StatusMessage);
                });
            });

            deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    this.NotifyUser(
                        String.Format("{0} devices found. Watcher {1}.",
                            ResultCollection.Count,
                            DeviceWatcherStatus.Aborted == watcher.Status ? "aborted" : "stopped"),
                        NotifyType.StatusMessage);
                });
            });

            deviceWatcher.Start();
        }

        private void NotifyUser(string v, NotifyType type)
        {
            Debug.WriteLine(v);
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopBleDeviceWatcher();
        }

        //private async void btnPair_Click(object sender, RoutedEventArgs e)
        //{
        //    resultsListView.IsEnabled = false;
        //    (sender as Button).IsEnabled = false;
        //    this.NotifyUser("Pairing started. Please wait...", NotifyType.StatusMessage);

        //    // Get the device selected for pairing
        //    BluetoothLEDeviceDisplay deviceInfoDisp = resultsListView.SelectedItem as BluetoothLEDeviceDisplay;
        //    DevicePairingResult result = null;

        //    result = await deviceInfoDisp.DeviceInformation.Pairing.PairAsync();

        //    this.NotifyUser(
        //        "Pairing result = " + result.Status.ToString(),
        //        result.Status == DevicePairingResultStatus.Paired ? NotifyType.StatusMessage : NotifyType.ErrorMessage);

        //    //   UpdateButtons();
        //    resultsListView.IsEnabled = true;

        //}

        private async void btnUnPair_Click(object sender, RoutedEventArgs e)
        {
            resultsListView.IsEnabled = false;
            (sender as Button).IsEnabled = false;
            this.NotifyUser("Unpairing started. Please wait...", NotifyType.StatusMessage);

            DeviceUnpairingResult dupr = await deviceInfoDisp.DeviceInformation.Pairing.UnpairAsync();

            this.NotifyUser(
                "Unpairing result = " + dupr.Status.ToString(),
                dupr.Status == DeviceUnpairingResultStatus.Unpaired ? NotifyType.StatusMessage : NotifyType.ErrorMessage);

            // UpdateButtons();
            resultsListView.IsEnabled = true;
        }

        private void UpdateButtons(BluetoothLEDeviceDisplay deviceInfoDisp)
        {


            btnStop.IsEnabled = false;





            //Stop any existing service watcher
            if (gattServiceWatcher != null)
            {
                StopGattServiceWatcher();
            }

            //If there is a paired device selected, look for the WeDo service and enable controls if found
            if (deviceInfoDisp != null)
            {
                if (deviceInfoDisp.IsPaired == true)
                {
                    StartGattServiceWatcher(deviceInfoDisp);
                }
            }
        }

        private async void StartGattServiceWatcher(BluetoothLEDeviceDisplay deviceInfoDisp)
        {
            //Get the Bluetooth address for filtering the watcher

            BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(deviceInfoDisp.Id);
            string selector = "(" + GattDeviceService.GetDeviceSelectorFromUuid(IoServiceUuid) + ")"
                                + " AND (System.DeviceInterface.Bluetooth.DeviceAddress:=\""
                                + bleDevice.BluetoothAddress.ToString("X") + "\")";

            gattServiceWatcher = DeviceInformation.CreateWatcher(selector);

            // Hook up handlers for the watcher events before starting the watcher
            gattServiceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                // If the selected device is a WeDo device, enable the controls
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    weDoIoService = await GattDeviceService.FromIdAsync(deviceInfo.Id);
                    outputCommandCharacteristic = weDoIoService.GetCharacteristics(OutputCommandCharacteristicGuid)[0];
                    InitializeGattStreamServiceCharateristics(deviceInfoDisp);
                    btnStop.IsEnabled = true;

                });
            });

            gattServiceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    //Do nothing
                });
            });

            gattServiceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    //Do nothing
                });
            });

            gattServiceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    //Do nothing
                });
            });

            gattServiceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    //Do nothing
                });
            });

            gattServiceWatcher.Start();
        }

        private IReadOnlyList<GattCharacteristic> GetCharacteristics(GattDeviceService gattService, string characteristicUuid)
        {
            var characteristics = gattService.GetCharacteristics(new Guid(characteristicUuid));
            return characteristics;
        }

        private async Task InitializeGattStreamServiceCharateristics(BluetoothLEDeviceDisplay deviceInfoDisp)
        {
            var _gattStreamService = await GattDeviceService.FromIdAsync(deviceInfoDisp.Id);
            Debug.WriteLine("\n" + _gattStreamService + ":   " + _gattStreamService.Device.Name + "\n");
            this.NotifyUser("Getting GATT Services", NotifyType.StatusMessage);



            // Get the Tx characteristic - We will get data from this Characteristics
            var _characteristicsStreamTx = GetCharacteristics(_gattStreamService, UuidDefs.GATT_STREAM_TX_CHARACTERISTIC_UUID);
            Debug.Assert(_characteristicsStreamTx != null);


            // Set the Client Characteristic Configuration Descriptor to "Indicate"
            GattCommunicationStatus status = await _characteristicsStreamTx[0].WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);

            // Get the Rx characteristic - We will send data to this Characteristics
            var _characteristicsStreamRx = GetCharacteristics(_gattStreamService, UuidDefs.GATT_STREAM_RX_CHARACTERISTIC_UUID);
            Debug.Assert(_characteristicsStreamRx != null);

            //Debug_ListCharacteristics();

            // This is the ValueChanged handler: Set an handler to the characteristicsTx
            // _characteristicsStreamTx[0].ValueChanged += CharacteristicsTx_ValueChanged;
        }





        private void StopGattServiceWatcher()
        {
            if (null != gattServiceWatcher)
            {
                if (DeviceWatcherStatus.Started == gattServiceWatcher.Status ||
                    DeviceWatcherStatus.EnumerationCompleted == gattServiceWatcher.Status)
                {
                    gattServiceWatcher.Stop();
                }
            }

        }
        enum NotifyType { StatusMessage, ErrorMessage }

        private void StopBleDeviceWatcher()
        {
            btnStop.IsEnabled = false;

            if (null != deviceWatcher)
            {
                if (DeviceWatcherStatus.Started == deviceWatcher.Status ||
                    DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status)
                {
                    deviceWatcher.Stop();
                }
            }

            btnStart.IsEnabled = true;
        }
    }


    public static class UuidDefs
    {
        // Device Information Service
        public const string DEVICE_INFORMATION_SERVICE_UUID = @"0000180A-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_MODEL_NUMBER_CHARACTERISTIC_UUID = @"00002A24-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_SERIAL_NUMBER_CHARACTERISTIC_UUID = @"00002A25-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_FIRMWARE_REVISION_CHARACTERISTIC_UUID = @"00002A26-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_SOFTWARE_REVISION_CHARACTERISTIC_UUID = @"00002A28-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_MANUFACTURER_NAME_CHARACTERISTIC_UUID = @"00002A29-0000-1000-8000-00805F9B34FB";      // Used

        // Jawbone Control Service                                                      
        public const string JAWBONE_CONTROL_SERVICE_UUID = @"151c0000-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string JAWBONE_CONTROL_DATETIME_CHARACTERISTIC_UUID = @"151c0001-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string JAWBONE_CONTROL_CONNECT_MODE_CHARACTERISTIC_UUID = @"151c0002-4580-4111-9ca1-5056f3454fbc";      // Used

        // GATT Stream Service                                                          
        public const string GATT_STREAM_SERVICE_UUID = @"151c1000-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string GATT_STREAM_RX_CHARACTERISTIC_UUID = @"151c1001-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string GATT_STREAM_TX_CHARACTERISTIC_UUID = @"151c1002-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string GATT_STREAM_HBTX_CHARACTERISTIC_UUID = @"151c1003-4580-4111-9ca1-5056f3454fbc";      // Used ??

        // Jawbone Log Stream Service                                                   
        public const string JAWBONE_LOG_STREAM_SERVICE_UUID = @"151c2000-4580-4111-9ca1-5056f3454fbc";
        public const string JAWBONE_LOG_STREAM_RX_UUID = @"151c2001-4580-4111-9ca1-5056f3454fbc";
        public const string JAWBONE_LOG_STREAM_TX_UUID = @"151c2002-4580-4111-9ca1-5056f3454fbc";

        // Jawbone OTA Service                                                          
        public const string JAWBONE_OTA_SERVICE_UUID = @"151c3000-4580-4111-9ca1-5056f3454fbc";
        public const string JAWBONE_OTA_IN_CHARACTERISTIC_UUID = @"151c3001-4580-4111-9ca1-5056f3454fbc";
        public const string JAWBONE_OTA_OUT_CHARACTERISTIC_UUID = @"151c3002-4580-4111-9ca1-5056f3454fbc";      // Used
    }


}
