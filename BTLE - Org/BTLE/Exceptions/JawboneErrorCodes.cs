// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
namespace BTLE.Exceptions
    {
    public enum JawboneErrorCodes
        {
        BLUETOOTH_NOT_ENABLED,
        SCAN_ALREADY_IN_PROGRESS,
        NO_DEVICES_FOUND_IN_SCAN,
        MULTIPLE_DEVICES_FOUND_IN_SCAN,
        SCANNED_DEVICE_NOT_IN_PAIRABLE_STATE,
        FAILED_TO_CONNECT_TO_DEVICE,
        DEVICE_PAIRING_FAILED,
        FAILED_TO_RESOLVE_BTLE_DEVICE_FROM_ADDRESS,
        DEVICE_DISCONNECTED,
        DEVICE_IS_NOT_REACHABLE,
        PROTOCOL_VERSION_INCOMPLETE_RESPONSE,
        PROTOCOL_VERSION_MISMATCH,
        PROTOCOL_VERSION_OTA_ONLY,
        AUTHENTICATION_FAILED,
        DEVICE_INFO_INCOMPLETE_RESPONSE,
        SETTINGS_SYNC_VERSIONS_INCOMPLETE_RESPONSE,
        NOT_SMART_ALARM_TYPE,
        GREATER_THAN_IDLE_ALERT_MAX_NUMBER,
        INVALID_EPOCH_REPONSE,
        INVALID_STEP_MINUTES,
        INVALID_STEP_THRESHOLD,
        SPEED_CHANGE_ALREADY_IN_PROGRESS,
        INVALID_SET_SPEED_CONNECTION_RESPONSE,
        INVALID_TICK_RECORD_RATE,
        MISSING_CONFIG_SENSORS_KEY,
        INVALIUD_IDENTIFIER_LENGTH,
        MISSING_HEART_RATE_VALUE,
        INVALID_PHONE_CHALLENGE_RESPONSE,
        DECRYPTION_FAILED,
        INVALID_RESPOND_TO_CHALLENGE_RESPONSE,
        INVALID_ESTABLISH_SECURE_CHANNEL_RESPONSE,
        INVALID_MAINTENANCE_RESET_TYPE,
        INVALID_SENSOR_LOG_HB_STREAM_PACKET_SIZE,
        TRANSACTION_TIMED_OUT,
        GATT_COMMUNICATION_FAILED,
        NO_NETWORK_CONNECTION,
        INVALID_USERNAME_PASSWORD,
        SERVER_CALL_FAILED,
        INVALID_PERSON_DATA_REPONSE,
        }
    }