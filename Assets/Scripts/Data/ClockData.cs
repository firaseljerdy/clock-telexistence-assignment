using System;

namespace ClockApp.Data
{
    /// <summary>
    /// Holds the combined data for the clock display.
    /// </summary>
    public struct ClockData
    {
        public DateTime CurrentTime { get; }
        public string TimeZoneId { get; }
        public string UtcOffset { get; }
        public bool IsServerTime { get; }

        public ClockData(DateTime time, string timeZoneId, string utcOffset, bool isServerTime)
        {
            CurrentTime = time;
            TimeZoneId = timeZoneId ?? "Local"; 
            UtcOffset = utcOffset ?? "";
            IsServerTime = isServerTime;
        }

        // Factory method for creating system time fallback data
        public static ClockData CreateSystemTimeData()
        {
            var now = DateTime.Now;
            var localTimeZone = TimeZoneInfo.Local;
            string offset = localTimeZone.GetUtcOffset(now).ToString("c").Substring(0, 6); // Format as +/-HH:mm
            return new ClockData(now, localTimeZone.StandardName, offset, false);
        }
    }
} 