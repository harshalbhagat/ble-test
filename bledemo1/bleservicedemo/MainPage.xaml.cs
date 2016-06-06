using bleservicedemo.Helper_Classes;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace bleservicedemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        //-------------------- Event Watcher --------------------------------------------------//
        private TypedEventHandler<DeviceWatcher, DeviceInformation> handlerAdded;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerUpdated;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerRemoved;
        private TypedEventHandler<DeviceWatcher, object> handlerEnumCompleted;
        private TypedEventHandler<DeviceWatcher, object> handlerStopped;

        //------------------------ Default Watcher --------------------------------------------------//
        private DeviceWatcher deviceWatcher = null;

        //---------------------- Device Info Hoder
        public ObservableCollection<DeviceInformationDisplay> ResultCollection
        {
            get;
            private set;
        }


        //============================= Events =====================================================//
        EnumberationCompletedEventHandler enumberationCompletedEvent = new EnumberationCompletedEventHandler();

        public MainPage()
        {

            this.InitializeComponent();
            ResultCollection = new ObservableCollection<DeviceInformationDisplay>();

            listView.SelectionChanged += ComboBox_SelectionChanged;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string val = listView.SelectedValue.ToString();

            TryPair(val);
        }

        private async void TryPair(string val)
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

            var bleDevice = await BluetoothLEDevice.FromIdAsync(_deviceInformationDisplayConnect.Id);



            var accService = await GattDeviceService.FromIdAsync(_deviceInformationDisplayConnect.Id);

            //Get the accelerometer data characteristic  
            var accData = accService.GetCharacteristics(new Guid("151c0000-4580-4111-9ca1-5056f3454fbc"))[0];
            //Subcribe value changed  

            //accData.ValueChanged += AccData_ValueChanged;
            accData.ValueChanged += test;

            //Set configuration to notify  
            await accData.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

            //Get the accelerometer configuration characteristic  
            var accConfig = accService.GetCharacteristics(new Guid("151c0000-4580-4111-9ca1-5056f3454fbc"))[0];

            GattReadResult Resultat = await accConfig.ReadValueAsync();
            var Output = Resultat.Value.ToArray();

            Debug.WriteLine("Acc: " + Output.Count());
            Debug.WriteLine("Registre 0:" + Output[0].ToString());
            Debug.WriteLine("Registre 1:" + Output[1].ToString());

            Output[0] = 0x7F;

            await accConfig.WriteValueAsync(Output.AsBuffer());
        }

        private void test(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private void PairingRequestedHandler(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            args.Accept();
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

        private void EnumberationCompletedEvent_StopEventHandle(object sender, EnumberationCompletedEventArgs e)
        {
            e.Button.IsEnabled = true;
            e.ProgressRing.IsActive = false;
            //e.ProgressRing.Visibility = 
            e.TextBlock.Text = "--Scan Completed--";
        }

        BluetoothLEDevice currentDevice { get; set; }
        string deviceName = "UP3";
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {



        }

        private void btnStart_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            progressRing.IsActive = true;
            lblComplete.Text = "";
            progressRing.Visibility = 0;
            StartWatcher();
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
                            deviceInfoDisp.Update(deviceInfoUpdate);

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
    }
}
