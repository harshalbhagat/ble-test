using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace BTLE.Converters
    {
    public class IsWalking2BackgroundConverter : IValueConverter
        {
        public object Convert( object value, Type targetType, object parameter, string language )
            {
            var isWalking = (bool)value;
            return ( Application.Current.Resources[ isWalking ? "ColorWalking" : "ColorNotWalking" ] as SolidColorBrush );
            }

        public object ConvertBack( object value, Type targetType, object parameter, string language ) => null;
        }
    }