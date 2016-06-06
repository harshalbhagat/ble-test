using System;
using System.Text;

namespace bledemo1.Util
{
    public static class Extensions
    {
        //public static bool IsImplementationOf(this Type baseType, Type interfaceType)
        //{
        //    return baseType.GetGenericTypeDefinition() == interfaceType;
        //}

        public static string HexBinToString(this byte[] byteArray, int size)
        {
            StringBuilder hex = new StringBuilder(size * 2);
            for (int i = 0; i < size; i++)
            {
                char c = (char)byteArray[i];
                hex.Append(Convert.ToUInt16(c) + "-");
            }

            return hex.ToString();
        }

        public static string BytesToString(this byte[] byteArray, int size)
        {
            StringBuilder str = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                char c = (char)byteArray[i];
                str.Append(c);
            }

            return str.ToString();
        }

        //
        //
        // http://stackoverflow.com/questions/2883576/how-do-you-convert-epoch-time-in-c
        //
        //
        // Unix time( also known as POSIX time or Epoch time) is a system for describing instants in time, 
        // defined as the number of seconds that have elapsed since 00:00:00 Coordinated Universal Time(UTC), 
        // Thursday, 1 January 1970,[1]
        // [note 1] - not counting leap seconds. 
        public static DateTime FromUnixTime(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        // ToSecondsSince1970 and ToUnixTime are equivalent
        public static uint ToSecondsSince1970(this DateTime date)
        {
            var utc1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (uint)(date.ToUniversalTime().Subtract(utc1970).TotalSeconds);
        }

        public static long ToUnixTime(this DateTime date)
        {
            var utc1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date.ToUniversalTime() - utc1970).TotalSeconds);
        }

        // ToUniversalTime will convert a Local( or Unspecified ) DateTime to UTC.
        // if you dont want to create the epoch DateTime instance when moving from DateTime to epoch you can also do: 
        public static long ToUnixTime1(this DateTime date)
        {
            return (date.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
    }
}
