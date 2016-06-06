using System;
using System.Runtime.InteropServices;
using BTLE.Types;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace BTLE.Cmds
    {
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct PersonCommand
        {
        public PersonCommand( ushort weight, ushort height, Gender gender, DateTimeOffset birthday ) : this()
            {
            Weight = weight;
            Height = height;
            Gender = (byte)gender;
            Birthday.DayOfMonth = (byte)birthday.Day;
            Birthday.Month = (byte)birthday.Month;
            Birthday.Year = (ushort)birthday.Year;
            }

        // Total of 9 bytes
        public byte Gender;                 // 0
        public ushort Height;               // 1-2
        public ushort Weight;               // 3-4
        public Date Birthday;               // 5-8
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct DailyGoals
        {
        public DailyGoals( uint sleepCount, uint stepCount )
            {
            SleepCount = sleepCount;
            StepCount = stepCount;
            }

        public uint SleepCount;
        public uint StepCount;
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct SettingsSyncVersionResponse
        {
        public uint SyncVersion;            // 4 bytes
        }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct AllSettingsSyncVersionsResponse
        {
        // Specifies the sync version of the alarm settings 
        public uint SmartAlarmSyncVersion;

        // Specifies the sync version of the idle alert settings 
        public uint IdleAlertSyncVersion;

        // Specifies the sync version of the demographics information 
        public uint DemographicsSyncVersion;

        // Specifies the sync version of the daily goals settings 
        public uint GoalsSyncVersion;
        }

    public static class SettingTypes
        {
        // Specifies the default or starting version for user settings.
        // Each time user settings are updated, the version is incremented. A sync
        // between the phone and device is only necessary if the version stamp does not match.
        public const int DEFAULT_SYNC_VERSION = 0;
        }
    }
