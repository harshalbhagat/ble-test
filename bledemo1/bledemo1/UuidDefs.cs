namespace bledemo1
{
    public static class UuidDefs
    {
        // Device Information Service
        public const string DEVICE_INFORMATION_SERVICE_UUID = @"0000180A-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_MODEL_NUMBER_CHARACTERISTIC_UUID = @"00002A24-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_SERIAL_NUMBER_CHARACTERISTIC_UUID = @"00002A25-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_FIRMWARE_REVISION_CHARACTERISTIC_UUID = @"00002A26-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_SOFTWARE_REVISION_CHARACTERISTIC_UUID = @"00002A28-0000-1000-8000-00805F9B34FB";      // Used
        public const string DEVICE_INFORMATION_MANUFACTURER_NAME_CHARACTERISTIC_UUID = @"00002A29-0000-1000-8000-00805F9B34FB";      // Used

        // Jawbone Control Service                                                      
        public const string JAWBONE_CONTROL_SERVICE_UUID = @"151c0000-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string JAWBONE_CONTROL_DATETIME_CHARACTERISTIC_UUID = @"151c0001-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string JAWBONE_CONTROL_CONNECT_MODE_CHARACTERISTIC_UUID = @"151c0002-4580-4111-9ca1-5056f3454fbc";      // Used

        // GATT Stream Service                                                          
        public const string GATT_STREAM_SERVICE_UUID = @"151c1000-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string GATT_STREAM_RX_CHARACTERISTIC_UUID = @"151c1001-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string GATT_STREAM_TX_CHARACTERISTIC_UUID = @"151c1002-4580-4111-9ca1-5056f3454fbc";      // Used
        public const string GATT_STREAM_HBTX_CHARACTERISTIC_UUID = @"151c1003-4580-4111-9ca1-5056f3454fbc";      // Used ??

        // Jawbone Log Stream Service                                                   
        public const string JAWBONE_LOG_STREAM_SERVICE_UUID = @"151c2000-4580-4111-9ca1-5056f3454fbc";
        public const string JAWBONE_LOG_STREAM_RX_UUID = @"151c2001-4580-4111-9ca1-5056f3454fbc";
        public const string JAWBONE_LOG_STREAM_TX_UUID = @"151c2002-4580-4111-9ca1-5056f3454fbc";

        // Jawbone OTA Service                                                          
        public const string JAWBONE_OTA_SERVICE_UUID = @"151c3000-4580-4111-9ca1-5056f3454fbc";
        public const string JAWBONE_OTA_IN_CHARACTERISTIC_UUID = @"151c3001-4580-4111-9ca1-5056f3454fbc";
        public const string JAWBONE_OTA_OUT_CHARACTERISTIC_UUID = @"151c3002-4580-4111-9ca1-5056f3454fbc";      // Used
    }
}
