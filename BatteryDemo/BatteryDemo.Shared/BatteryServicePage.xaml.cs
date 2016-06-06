using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace BatteryDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BatteryServicePage : Page,INotifyPropertyChanged
    {
        public BatteryServicePage()
        {
            if (!DesignMode.DesignModeEnabled)
            {
                _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            }
            this.InitializeComponent();
        }

        public DeviceInformation Device { get; set; }

        private int _BatteryLevel;
        public int BatteryLevel
        {
            get
            {
                return _BatteryLevel;
            }
            set
            {
                _BatteryLevel = value;
                OnPropertyChanged();
            }
        }


        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Device = e.Parameter as DeviceInformation;
            BatteryLevel = Convert.ToInt32((await GetValue(GattCharacteristicUuids.BatteryLevel))[0]);
            base.OnNavigatedTo(e);
        }




        public async Task<byte[]> GetValue(Guid gattCharacteristicUuids)
        {
            try
            {
                var gattDeviceService = await GattDeviceService.FromIdAsync(Device.Id);
                if (gattDeviceService != null)
                {
                    var characteristics = gattDeviceService.GetCharacteristics(gattCharacteristicUuids).First();

                    //If the characteristic supports Notify then tell it to notify us.
                    try
                    {
                        if (characteristics.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                        {
                            characteristics.ValueChanged += characteristics_ValueChanged;
                            await characteristics.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        }
                    }
                    catch { }

                    //Read
                    if (characteristics.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Read))
                    {
                        var result = await characteristics.ReadValueAsync(BluetoothCacheMode.Uncached);

                        if (result.Status == GattCommunicationStatus.Success)
                        {
                            byte[] forceData = new byte[result.Value.Length];
                            DataReader.FromBuffer(result.Value).ReadBytes(forceData);
                            return forceData;
                        }
                        else
                        {
                            await new MessageDialog(result.Status.ToString()).ShowAsync();
                        }
                    }
                }
                else 
                {
                    await new MessageDialog("Access to the device has been denied =(").ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return null;
        }

        void characteristics_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data = new byte[args.CharacteristicValue.Length];
            Windows.Storage.Streams.DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

           //Update properties
            if (sender.Uuid == GattCharacteristicUuids.BatteryLevel)
            {
                BatteryLevel = Convert.ToInt32(data[0]);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly CoreDispatcher _dispatcher;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (_dispatcher == null || _dispatcher.HasThreadAccess)
            {
                var eventHandler = this.PropertyChanged;
                if (eventHandler != null)
                {
                    eventHandler(this,
                        new PropertyChangedEventArgs(propertyName));
                }
            }
            else
            {
                IAsyncAction doNotAwait =
                    _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () => OnPropertyChanged(propertyName));
            }
        }
    }
}
