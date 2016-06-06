using System;

// ReSharper disable UnusedMember.Global

namespace BTLE.Types
    {
    [Flags]
    public enum WeekDayMask
        {
        None = 0x00,
        Sunday = 0x01,
        Monday = 0x02,
        Tuesday = 0x04,
        Wednesday = 0x08,
        Thursday = 0x10,
        Friday = 0x20,
        Saturday = 0x40,
        Daily = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
        }

    public enum AlarmType
        {
        SmartAlarm,
        Reminder
        }

    public enum ActivityType
        {
        Activity,
        Sleep,
        Gap
        }

    public enum MovementType
        {
        Walk,
        Run,
        WalkRun,
        Sedentary
        }

    public enum SleepType
        {
        Unknown = 0,
        Light = 2,
        Deep = 3
        }

    public static class LemondTypes
        {
        public const string SensorConfigSensorsKey = @"Sensors";
        }
    }
