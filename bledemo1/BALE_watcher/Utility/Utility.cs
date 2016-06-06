using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Storage.Streams;
using Buffer = System.Buffer;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace BTLE.Utils
{
    public static class Utility
    {
        public static ushort ConvertUuidToShortId(Guid uuid)
        {
            // Get the short Uuid
            byte[] bytes = uuid.ToByteArray();
            ushort shortUuid = (ushort)(bytes[0] | (bytes[1] << 8));
            return shortUuid;
        }

        public static byte[] ReadBuffer(IBuffer buffer)
        {
            byte[] bytes = new byte[buffer.Length];
            DataReader.FromBuffer(buffer).ReadBytes(bytes);
            return bytes;
        }

        public static string ReadIBuffer2Str(IBuffer buffer, bool isLen = false)
        {
            byte[] bytes = ReadBuffer(buffer);
            string strData = ByteArray2String(bytes, isLen);
            return strData;
        }

        public static T BytesToStruct<T>(byte[] bytes) where T : struct
        {
            T result;
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                result = Marshal.PtrToStructure<T>(rawDataPtr);
            }
            finally
            {
                handle.Free();
            }

            return result;
        }

        public static byte[] StructToBytes<T>(T data) where T : struct
        {
            byte[] bytes = new byte[Marshal.SizeOf(data)];
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                Marshal.StructureToPtr(data, rawDataPtr, false);
            }
            finally
            {
                handle.Free();
            }

            return bytes;
        }

        public static byte[] ArrayConcat(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, c, 0, a.Length);
            Buffer.BlockCopy(b, 0, c, a.Length, b.Length);

            return c;
        }

        public static byte[] Xor(byte[] data1, byte[] data2, int len)
        {
            Debug.Assert(data1 != null && data1.Length >= len);
            Debug.Assert(data2 != null && data2.Length >= len);

            byte[] response = new byte[len];

            for (int i = 0; i < len; i++)
            {
                response[i] = (byte)(data1[i] ^ data2[i]);
            }

            return response;
        }

        public static string BtleAddress2String(ulong ulongData)
        {
            return Ulong2String(ulongData, 6);
        }

        public static string Ulong2String(ulong ulongData, byte len = 8)
        {
            var byteArray = BitConverter.GetBytes(ulongData);

            // A Bluetooth–enabled device address is a unique, 48–bit (6 bytes) address
            Array.Resize(ref byteArray, len);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }

            return ByteArray2String(byteArray);
        }

        public static string ByteArray2String(byte[] byteArray, bool isLen = false)
        {
            var message = String.Empty;
            if (isLen)
            {
                message = "[" + byteArray.Length + "] => ";
            }

            for (var i = 0; i < byteArray.Length; i++)
            {
                message += byteArray[i].ToString("X2");
                if (i != byteArray.Length - 1)
                {
                    message += "-";
                }
            }

            return message;
        }

        // ulong
        public static ulong CreateULongBitfieldMask(int offset, int len)
        {
            return (ulong)(((1 << len) - 1) << offset);
        }

        public static ulong GetULongBitfieldData(ulong ulongData, int offset, int len)
        {
            Debug.Assert(offset < 64);
            Debug.Assert(offset + len < 64);

            return (ulongData & CreateULongBitfieldMask(offset, len)) >> offset;
        }


        // uint
        public static uint CreateUIntBitfieldMask(int offset, int len)
        {
            return (uint)(((1 << len) - 1) << offset);
        }

        public static uint GetUintBitfieldData(uint uintData, int offset, int len)
        {
            Debug.Assert(offset < 32);
            Debug.Assert(offset + len < 32);

            return (uintData & CreateUIntBitfieldMask(offset, len)) >> offset;
        }


        // ushort
        public static ushort CreateUShortBitfieldMask(int offset, int len)
        {
            return (ushort)(((1 << len) - 1) << offset);
        }

        public static ushort GetUShortBitfieldData(ushort ushortData, int offset, int len)
        {
            Debug.Assert(offset < 16);
            Debug.Assert(offset + len < 16);

            return (ushort)((ushortData & CreateUShortBitfieldMask(offset, len)) >> offset);
        }


        // byte
        public static ushort CreateByteBitfieldMask(int offset, int len)
        {
            return (byte)(((1 << len) - 1) << offset);
        }

        public static ushort GetByteBitfieldData(byte byteData, int offset, int len)
        {
            Debug.Assert(offset < 8);
            Debug.Assert(offset + len < 8);

            return (byte)((byteData & CreateByteBitfieldMask(offset, len)) >> offset);
        }


        //
        // METS (Calories/hour/kg) is a function of speed. The equation here
        // assumes that speed is in meters/second.
        //
        // These values are computed from the published METS data for
        // walking and running. This data includes many METS values for
        // different speeds of walking and running, all of which are linear
        // with speed.
        //
        //   mph   METS
        // --------------
        //   0      0
        //   2.5    3
        //   3      3.3
        //   3.5    3.8
        //   4      5
        //   4.5    6.3
        //   5      8
        //   5.2    9
        //   6      10
        //   6.7    11
        //   7      11.5
        //   7.5    12.5
        //   8      13.5
        //   8.6    14
        //   9      15
        //   10     16
        //   10.9   18
        //
        // What follows is a polynomial regression of these points, with the units converted
        // from mph to meters/second.
        //
        // y = 1.1051*x^2 + 0.9665*x
        //
        // N.B. 2.5mph = 1.11736 meters/second
        //

        public static double CalculateCaloriesPerKgForSpeedMeters(double metersPerSecond, ushort duration)
        {
            double caloriesPerKgPerHour = 1.1051 * metersPerSecond * metersPerSecond + 0.9665 * metersPerSecond;
            return caloriesPerKgPerHour * duration / (60 * 60);
        }

        public static double CalculateCaloriesPerKgForSpeedMiles(double milesPerHour, ushort duration)
        {
            double metersPerSecond = (milesPerHour * 1600) / 3600;
            return CalculateCaloriesPerKgForSpeedMeters(metersPerSecond, duration);
        }

        public static string Seconds2String(int activeTimeInSeconds)
        {
            return string.Format("{0}", new TimeSpan(0, 0, activeTimeInSeconds));
        }

        public static void VibratePhone(int seconds = 1)
        {
            // IsTypePresent( "Windows.Phone.UI.Input.HardwareButtons" )
            // IsTypePresent( "Windows.Phone.Devices.Notification.VibrationDevice" )
            // IsTypePresent( "Windows.UI.ViewManagement.StatusBar" )
            // IsTypePresent( "Windows.UI.Core.AnimationMetrics.AnimationDescription" )
            // IsTypePresent( "Windows.Devices.SmartCards.SmartCardConnection" )
            // ....

            //if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.Devices.Notification.VibrationDevice"))
            //{
            //    VibrationDevice vibrationDevice = VibrationDevice.GetDefault();
            //    vibrationDevice.Vibrate(TimeSpan.FromSeconds(seconds));
            //}
        }
    }
}
