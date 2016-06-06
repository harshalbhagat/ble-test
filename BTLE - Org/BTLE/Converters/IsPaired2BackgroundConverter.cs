using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace BTLE.Converters
    {
    public class IsPaired2BackgroundConverter : IValueConverter
        {
        public object Convert( object value, Type targetType, object parameter, string language )
            {
            var isPaired = (bool)value;
            return ( Application.Current.Resources[ isPaired ? "ColorPaired" : "ColorNotPaired" ] as SolidColorBrush );
            }

        public object ConvertBack( object value, Type targetType, object parameter, string language ) => null;
        }
    }