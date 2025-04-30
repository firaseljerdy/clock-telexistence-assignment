using System;
using UniRx;
using ClockApp.Data;

namespace ClockApp.Services
{
    /// <summary>
    /// Provides clock data including time, timezone, and source.
    /// Attempts to fetch from an external source, falling back to system time.
    /// </summary>
    public interface IClockService
    {

        // Gets a reactive property representing the current clock data.
        // Contains time, timezone identifier, UTC offset, and whether it's server time.
        // Updates periodically
        IReadOnlyReactiveProperty<ClockData> CurrentClockData { get; }
    }
}
