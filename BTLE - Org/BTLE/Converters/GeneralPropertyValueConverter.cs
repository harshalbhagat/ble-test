using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace BTLE.Converters
    {
    public class GeneralPropertyValueConverter : IValueConverter
        {
        public object Convert( object value, Type targetType, object parameter, string language )
            {
            object property = null;

            var pairs = value as IReadOnlyDictionary<string, object>;
            if ( pairs != null && !string.IsNullOrEmpty( parameter as string ) )
                {
                IReadOnlyDictionary<string, object> properties = pairs;
                string propertyName = (string)parameter;

                property = properties[ propertyName ];
                }

            return property;
            }

        public object ConvertBack( object value, Type targetType, object parameter, string language )
            {
            throw new NotImplementedException();
            }
        }
    }