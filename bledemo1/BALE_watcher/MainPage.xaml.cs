using BALE_watcher.Events;
using BALE_watcher.Misc;
using BALE_watcher.Type;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BALE_watcher
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

        //------------------------ Custome Watcher --------------------------------------------------//
        private BtleWatcher _watcher2;

        //============================= Events =====================================================//
        EnumberationCompletedEventHandler enumberationCompletedEvent = new EnumberationCompletedEventHandler();



        private static readonly ObservableCollection<Transaction> QueueTransactions = new ObservableCollection<Transaction>();                                    // ** Needed for Re-Connect ** //
        private static readonly ObservableCollection<RawPacket> QueueResponsePackets = new ObservableCollection<RawPacket>();


        private ObservableCollection<DeviceInformationDisplay> ResultCollection = null;

        public MainPage()
        {
            this.InitializeComponent();
            ResultCollection = new ObservableCollection<DeviceInformationDisplay>();
        }

        private void button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

        }


        private void StartWatchers()
        {
            // aqsFilter = System.Devices.Aep.ProtocolId:="{bb7bb05e-5972-42b5-94fc-76eaa7084d49}"
            // association endpoint (AEP)
            string aqsFilter = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"";

            deviceWatcher = DeviceInformation.CreateWatcher(
               aqsFilter,
               null, // don't request additional properties for this sample
               DeviceInformationKind.AssociationEndpoint);
            InitializeBtleWatcher(aqsFilter);
            deviceWatcher.Start();

            // Start the second watcher for the UP Band Stream Service: GATT_STREAM_SERVICE_UUID 
            aqsFilter = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(UuidDefs.GATT_STREAM_SERVICE_UUID));
            _watcher2 = new BtleWatcher(this, "W2");
            _watcher2.InitializeBtleWatcher(aqsFilter);

            // Hook a function to the watcher events
            _watcher2.WacherEvent += Wacher2EventFired;

            //// Start the third watcher for the UP Band Control Service: JAWBONE_CONTROL_SERVICE_UUID
            //aqsFilter = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(UuidDefs.JAWBONE_CONTROL_SERVICE_UUID));
            //_watcher3 = new BtleWatcher(this, "W3");
            //_watcher3.InitializeBtleWatcher(aqsFilter);

            //// Start the third watcher for the UP Band Control Service: DEVICE_INFORMATION_SERVICE_UUID
            //aqsFilter = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(UuidDefs.DEVICE_INFORMATION_SERVICE_UUID));
            //_watcher4 = new BtleWatcher(this, "W4");
            //_watcher4.InitializeBtleWatcher(aqsFilter);

            //_rootPage.NotifyUser("Starting Watchers Done...", NotifyType.StatusMessage);

            //AppBarButtonStop.IsEnabled = true;
        }

        private void Wacher2EventFired(object sender, BtleWatcherEventsArgs e)
        {



        }



        //public void testc()
        //{
        //    List<string> serviceList = new List<string>();
        //    foreach (var service in currentDevice.GattServices)
        //    {
        //        switch (service.Uuid.ToString())
        //        {
        //            case "00001811-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("AlertNotification");
        //                break;
        //            case "0000180f-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("Battery");
        //                break;
        //            case "00001810-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("BloodPressure");
        //                break;
        //            case "00001805-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("CurrentTime");
        //                break;
        //            case "00001818-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("CyclingPower");
        //                break;
        //            case "00001816-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("CyclingSpeedAndCadence");
        //                break;
        //            case "0000180a-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("DeviceInformation");
        //                break;
        //            case "00001800-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("GenericAccess");
        //                break;
        //            case "00001801-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("GenericAttribute");
        //                break;
        //            case "00001808-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("Glucose");
        //                break;
        //            case "00001809-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("HealthThermometer");
        //                break;
        //            case "0000180d-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("HeartRate");
        //                break;
        //            case "00001812-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("HumanInterfaceDevice");
        //                break;
        //            case "00001802-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("ImmediateAlert");
        //                break;
        //            case "00001803-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("LinkLoss");
        //                break;
        //            case "00001819-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("LocationAndNavigation");
        //                break;
        //            case "00001807-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("NextDstChange");
        //                break;
        //            case "0000180e-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("PhoneAlertStatus");
        //                break;
        //            case "00001806-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("ReferenceTimeUpdate");
        //                break;
        //            case "00001814-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("RunningSpeedAndCadence");
        //                break;
        //            case "00001813-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("ScanParameters");
        //                break;
        //            case "00001804-0000-1000-8000-00805f9b34fb":
        //                serviceList.Add("TxPower");
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //    MessageDialog md = new MessageDialog(String.Join("\r\n", serviceList));
        //    md.ShowAsync();
        //}

        private void InitializeBtleWatcher(string aqsFilter)
        {
            handlerAdded = new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    var info = new DeviceInformationDisplay(deviceInfo);
                    if (!info.IsPaired && info.CanPair)
                        ResultCollection.Add(info);

                    //rootPage.NotifyUser(
                    //    String.Format("{0} devices found.", ResultCollection.Count),
                    //    NotifyType.StatusMessage);
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

                    Debug.WriteLine(String.Format("Handler Removed  :-  {0} devices found.", ResultCollection.Count));

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
                    args.TextBlock = lblComplete;
                    enumberationCompletedEvent.EnumberationCompleted(args);
                });
            });
            deviceWatcher.EnumerationCompleted += handlerEnumCompleted;

            handlerStopped = new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    Debug.WriteLine(String.Format(" Handler Stopped  -  {0} devices found. Watcher {1}.", ResultCollection.Count, DeviceWatcherStatus.Aborted == watcher.Status ? "aborted" : "stopped"));
                });
            });
            deviceWatcher.Stopped += handlerStopped;
        }

        private void EnumberationCompletedEvent_StopEventHandle(object sender, EnumberationCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
