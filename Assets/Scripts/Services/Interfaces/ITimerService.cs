using System;
using UniRx;

namespace ClockApp.Services
{
    /// <summary>
    /// Manages a countdown timer
    /// </summary>
    public interface ITimerService
    {
        /// <summary>Starts the timer with the specified duration. If paused, resumes. If running or completed, does nothing unless reset first.</summary>
        void StartTimer(TimeSpan duration);
        //Pauses the currently running timer
        void PauseTimer();
        //Stops and resets the timer to zero
        void ResetTimer();

        // property representing the time remaining
        IReadOnlyReactiveProperty<TimeSpan> RemainingTime { get; }
        //Gets an observable that fires when the timer completes..
        IObservable<Unit> TimerCompleted { get; }
        // property indicating if the timer is currently running
        IReadOnlyReactiveProperty<bool> IsRunning { get; }
    }
}
