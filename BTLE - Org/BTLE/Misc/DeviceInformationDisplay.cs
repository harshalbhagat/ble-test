using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media.Imaging;
using BTLE.Utils;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace BTLE.Misc
    {
    public class DeviceInformationDisplay : INotifyPropertyChanged
        {
        public DeviceInformation DeviceInfo;

        public DeviceInformationDisplay( DeviceInformation deviceInfoIn )
            {
            DeviceInfo = deviceInfoIn;
            UpdateGlyphBitmapImage();

            BtleAddress = 0x00;

            // Decode the BTLE Address
            string deviceId = deviceInfoIn.Id;
            if ( deviceId.IndexOf( "BluetoothLE#BluetoothLE", StringComparison.Ordinal ) == -1 )
                {
                return;
                }

            var index = deviceId.IndexOf( "-", StringComparison.Ordinal );
            if ( index == -1 )
                {
                return;
                }

            var lAddress = deviceId.Substring( index + 1 );
            var sAddress = lAddress.Substring( 0, 2 ) + lAddress.Substring( 3, 2 ) + lAddress.Substring( 6, 2 ) +
                           lAddress.Substring( 9, 2 ) + lAddress.Substring( 12, 2 ) + lAddress.Substring( 15, 2 );
            BtleAddress = Convert.ToUInt64( sAddress, 16 );
            }

        public DeviceInformationKind Kind => DeviceInfo.Kind;

        public string Id => DeviceInfo.Id;

        public string Name => DeviceInfo.Name;

        public string Address => BtleAddress != 0x00 ? Utility.BtleAddress2String( BtleAddress ) : "xx-xx-xx-xx-xx-xx";

        public BitmapImage GlyphBitmapImage
            {
            get;
            set;
            }

        private ulong _btleAddress;
        public ulong BtleAddress
            {
            get
                {
                return _btleAddress;
                }
            set
                {
                _btleAddress = value;
                OnPropertyChanged( "BtleAddress" );
                OnPropertyChanged( "Address" );
                }
            }

        public bool CanPair => DeviceInfo.Pairing.CanPair;

        public bool IsPaired => DeviceInfo.Pairing.IsPaired;

        public IReadOnlyDictionary<string, object> Properties => DeviceInfo.Properties;

        public DeviceInformation DeviceInformation
            {
            get
                {
                return DeviceInfo;
                }

            set
                {
                DeviceInfo = value;
                }
            }

        public void Update( DeviceInformationUpdate deviceInfoUpdate, string watcher )
            {
            DeviceInfo.Update( deviceInfoUpdate );

            OnPropertyChanged( "Kind" );
            OnPropertyChanged( "Id" );
            OnPropertyChanged( "Name" );
            OnPropertyChanged( "DeviceInformation" );
            OnPropertyChanged( "CanPair" );
            OnPropertyChanged( "IsPaired" );

#if DEBUG
            var msg = new StringBuilder();

            msg.AppendFormat( "\nDeviceInformationUpdate: Properties\n" );
            msg.AppendFormat( "===================================\n" );
            msg.AppendFormat( "    Name:    " + Name + "\n" );
            msg.AppendFormat( "    watcher: " + watcher + "\n" );

            IReadOnlyDictionary<string, object> properties = deviceInfoUpdate.Properties;
            foreach ( var property in properties )
                {
                msg.AppendFormat( "    " + property + "\n" );
                msg.AppendFormat( "        " + property.Key + "\n" );
                msg.AppendFormat( "        " + property.Value + "\n" );
                msg.AppendFormat( "\n" );
                }

            Debug.WriteLine( msg );
#endif

            UpdateGlyphBitmapImage();
            }

        private async void UpdateGlyphBitmapImage()
            {
            DeviceThumbnail deviceThumbnail = await DeviceInfo.GetGlyphThumbnailAsync();
            var glyphBitmapImage = new BitmapImage();
            await glyphBitmapImage.SetSourceAsync( deviceThumbnail );
            GlyphBitmapImage = glyphBitmapImage;
            OnPropertyChanged( "GlyphBitmapImage" );
            }

        public static ulong DecodeBtleAddress( DeviceInformation device )
            {
            string id = device.Id;

            char c = device.Pairing.IsPaired ? '_' : '-';
            int length = device.Pairing.IsPaired ? 12 : 17;

            var start = id.IndexOf( c );
            var lAddress = id.Substring( start + 1, length ).ToUpper();

            string sAddress;
            if ( device.Pairing.IsPaired )
                {
                sAddress = lAddress.Substring( 0, 2 ) + lAddress.Substring( 2, 2 ) + lAddress.Substring( 4, 2 ) +
                           lAddress.Substring( 6, 2 ) + lAddress.Substring( 8, 2 ) + lAddress.Substring( 10, 2 );
                }
            else
                {
                sAddress = lAddress.Substring( 0, 2 ) + lAddress.Substring( 3, 2 ) + lAddress.Substring( 6, 2 ) +
                           lAddress.Substring( 9, 2 ) + lAddress.Substring( 12, 2 ) + lAddress.Substring( 15, 2 );
                }
            var address = Convert.ToUInt64( sAddress, 16 );
            return address;
            }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged( string propertyName )
            {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }
    }