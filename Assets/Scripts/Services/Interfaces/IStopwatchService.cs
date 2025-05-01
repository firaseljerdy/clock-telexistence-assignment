using System;
using System.Collections.Generic;
using UniRx;

namespace ClockApp.Services
{
    /// <summary>
    /// Manages a stopwatch with lap recording functionality.
    /// </summary>
    public interface IStopwatchService
    {
        void StartStopwatch();
        void StopStopwatch();
        void ResetStopwatch();
        void RecordLap();

        IReadOnlyReactiveProperty<TimeSpan> ElapsedTime { get; }
        IReadOnlyReactiveProperty<IReadOnlyList<TimeSpan>> Laps { get; }
        IReadOnlyReactiveProperty<bool> IsRunning { get; }
    }
}
