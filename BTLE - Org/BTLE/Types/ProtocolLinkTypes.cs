// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace BTLE.Types
    {
    public enum ComponentId
        {
        Protocol = 0x00,                                                    // FromDeviceMessageIds
        RecordManager = 0x30,                                               // TickMessageType
        AlarmsAlerts = 0x40,                                                // AlertRequestIds
        SensorLog = 0x50,                                                   // SensorLogRequestIds
        Motion = 0x70,                                                      // MotionRequestIds
        Maintenance = 0x80,                                                 // DeviceMaintenanceIds
        Application = 0xc0                                                  // SettingsIds
        }

    public enum FromDeviceMessageIds
        {
        UnpairRequest = ComponentId.Protocol                                // 0x00
        }

    public enum TickMessageType
        {
        Invalid = ComponentId.RecordManager,                                // 0x30
        TickRecord = ComponentId.RecordManager + 1,                         // 0x31
        Epoch = ComponentId.RecordManager + 2,                              // 0x32
        SetRange = ComponentId.RecordManager + 3,                           // 0x33
        Replay = ComponentId.RecordManager + 4,                             // 0x34
        QueryEpochs = ComponentId.RecordManager + 5,                        // 0x35
        DailySummary = ComponentId.RecordManager + 6,                       // 0x36
        RealTimeSummaryState = ComponentId.RecordManager + 7,               // 0x37
        SetTickRecordRate = ComponentId.RecordManager + 8                   // 0x38
        }

    public enum AlertRequestIds
        {
        AlertNow = ComponentId.AlarmsAlerts + 0,                            // 0x40
        SmartAlarmSet = ComponentId.AlarmsAlerts + 1,                       // 0x41
        SmartAlarmDeleteAll = ComponentId.AlarmsAlerts + 2,                 // 0x42
        SmartAlarmResponseFired = ComponentId.AlarmsAlerts + 3,             // 0x43
        IdleAlertSet = ComponentId.AlarmsAlerts + 4,                        // 0x44
        IdleAlertDeleteAll = ComponentId.AlarmsAlerts + 5,                  // 0x45
        IdleAlertResponseFired = ComponentId.AlarmsAlerts + 6,              // 0x46
        ReminderSet = ComponentId.AlarmsAlerts + 7,                         // 0x47
        ReminderResponseFired = ComponentId.AlarmsAlerts + 8,               // 0x48
        TriggerAlarmAlertRequest = ComponentId.AlarmsAlerts + 9             // 0x49
        }

    public enum SensorLogRequestIds
        {
        SensorLogRequestEnableDisable = ComponentId.SensorLog + 0,          // 0x50
        SensorLogResponseBioimpedanceStream = ComponentId.SensorLog + 1,    // 0x51
        SensorLogResponseAccelStream = ComponentId.SensorLog + 2,           // 0x52
        SensorLogRequestRecordStatus = ComponentId.SensorLog + 3,           // 0x53
        SensorLogRequestSensorLogUpload = ComponentId.SensorLog + 4,        // 0x54         // deprecated
        SensorLogRequestRecordStart = ComponentId.SensorLog + 5,            // 0x55
        SensorLogRequestRecordStop = ComponentId.SensorLog + 6,             // 0x56
        SensorLogRequestRecordReset = ComponentId.SensorLog + 7,            // 0x57
        SensorLogRequestRawAccel = ComponentId.SensorLog + 8,               // 0x58
        SensorLogRequestHbStream = ComponentId.SensorLog + 9                // 0x59
        }


    public enum DeviceMaintenanceIds
        {
        UpdateClassifier = ComponentId.Maintenance + 0,                     // 0x80
        GetClassifierVersion = ComponentId.Maintenance + 1,                 // 0x81
        SetErrorRecordCursor = ComponentId.Maintenance + 2,                 // 0x82
        NewErrorRecord = ComponentId.Maintenance + 3,                       // 0x83
        NewBatteryReading = ComponentId.Maintenance + 4,                    // 0x84
        SetStageFirmwareInformation = ComponentId.Maintenance + 5,          // 0x85
        GetStageFirmwareInformation = ComponentId.Maintenance + 6,          // 0x86
        GetOtaProgress = ComponentId.Maintenance + 7,                       // 0x87
        EraseStageFirmware = ComponentId.Maintenance + 8,                   // 0x88
        PerformFirmwareUpdate = ComponentId.Maintenance + 9,                // 0x89
        ResendPacket = ComponentId.Maintenance + 10,                        // 0x8A
        CompletedTransfer = ComponentId.Maintenance + 11,                   // 0x8B
        ResetDevice = ComponentId.Maintenance + 12,                         // 0x8C
        PauseTransfer = ComponentId.Maintenance + 13,                       // 0x8D
        ResumeTransfer = ComponentId.Maintenance + 14                       // 0x8E
        }

    public enum SettingsIds
        {
        SetPersonData = ComponentId.Application + 0,                        // 0xC0
        SetGoals = ComponentId.Application + 1,                             // 0xC1
        GetSyncVersions = ComponentId.Application + 2,                      // 0xC2
        VibrationPlayPattern = ComponentId.Application + 3,                 // 0xC3
        HeartRateRequestQuery = ComponentId.Application + 4,                // 0xC4
        NfcIdQuery = ComponentId.Application + 5,                           // 0xC5
        TimerStart = ComponentId.Application + 6,                           // 0xC6
        TimerStop = ComponentId.Application + 7                             // 0xC7
        }

    public enum MotionRequestIds
        {
        // Notification from the device that walking started
        BeginWalking = ComponentId.Motion + 1,                              // 0x71

        // Notification from the device that walking stopped
        EndWalking = ComponentId.Motion + 2,                                // 0x72

        // Request to set step threshold for triggering begin 
        // and end walking signals WalkingThresholdRequest_t
        SetStepThreshold = ComponentId.Motion + 3                           // 0x73
        }

    public enum ConnectModes
        {
        // Forget pairing information and disconnect
        Bonding = 1,                                                        // 0x01

        // Initiate a disconnect from the device
        Connect = 2                                                         // 0x02
        }

    public static class LemondLinkTypes
        {
        public const ushort LEMOND_PROTOCOL_VENDOR = 0x1F6F;                // 0x1F6F
        public const ushort LEMOND_PROTOCOL_NUMBER = 0;                     // 0x00
        public const ushort LEMOND_PROTOCOL_VERSION_MAJOR = 42;             // 0x2A
        public const ushort LEMOND_PROTOCOL_VERSION_MINOR = 0;              // 0x00

        // deprecated
        public const ushort LEMOND_PROTOCOL_VERSION = LEMOND_PROTOCOL_VERSION_MAJOR;
        }
    }
