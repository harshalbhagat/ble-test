namespace bledemo1.Type
{
    public static class BtleLinkTypes
    {
        public const int LEMOND_AUTH_VERSION = 1;

        // Specifies the size of an encrypted block
        public const int ENCR_CHALLENGE_SIZE = 8;

        public const int ENCRYPTED_BLOCK_SIZE = 16;

        // Specifies the data size of an OTA packet 
        public const int OTA_DATA_SIZE = 18;

        // Specifies the largest characteristic values that can be written or read.
        // This can technically be larger for reads, but effectively this is capped
        // by the size of the underlying service characteristic for a stream.        
        public const int BTLE_PACKET_SIZE = 20;

        public const int FIRMWARE_VERSION_MAX_LENGTH = 64;
        public const int LEMOND_SERIAL_NUMBER_LENGTH = 4;

        public const int PROTOCOL_VERSION_V1_SIZE = 6;
        public const int PROTOCOL_VERSION_V41_SIZE = 9;

        public const int COMPONENTID_BTLESTREAM = 0;

        public const int TRANSACTION_HEADER_SIZE = 4;                                                           // 4
        public const int RESPONSE_HEADER_SIZE = 4;                                                              // 4
        public const int SIMPLE_TRANSACTION_MAX_PAYLOAD_SIZE = BTLE_PACKET_SIZE - TRANSACTION_HEADER_SIZE - 1;  // 15
        public const int MULTI_TRANSACTION_MAX_PAYLOAD_SIZE = BTLE_PACKET_SIZE - 1;                             // 19
    }
}
