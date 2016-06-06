using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace BTLE.Converters
    {
    public class IsWalking2VisibilityConverter : IValueConverter
        {
        public object Convert( object value, Type targetType, object parameter, string language )
            {
            var isWalking = (bool)value;
            return isWalking ? Visibility.Visible : Visibility.Collapsed;
            }

        public object ConvertBack( object value, Type targetType, object parameter, string language ) => null;
        }
    }