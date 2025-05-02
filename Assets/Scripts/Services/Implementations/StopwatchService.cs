using System;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using UnityEngine;

namespace ClockApp.Services
{
    /// <summary>
    /// Implementation of IStopwatchService/>.
    /// </summary>
    public class StopwatchService : IStopwatchService, IDisposable
    {
        // Properties for state
        private readonly ReactiveProperty<TimeSpan> _elapsedTime = new ReactiveProperty<TimeSpan>(TimeSpan.Zero);
        private readonly ReactiveProperty<IReadOnlyList<TimeSpan>> _laps = new ReactiveProperty<IReadOnlyList<TimeSpan>>(new List<TimeSpan>().AsReadOnly());
        private readonly ReactiveProperty<bool> _isRunning = new ReactiveProperty<bool>(false);

        private IDisposable _timerSubscription;
        private float _lastTickTime;
        private static int _activeInstanceCount = 0;

        public IReadOnlyReactiveProperty<TimeSpan> ElapsedTime => _elapsedTime;
        public IReadOnlyReactiveProperty<IReadOnlyList<TimeSpan>> Laps => _laps;
        public IReadOnlyReactiveProperty<bool> IsRunning => _isRunning;
        public static int ActiveInstanceCount => _activeInstanceCount;

        public void StartStopwatch()
        {
            if (_isRunning.Value) return;
            if (Interlocked.CompareExchange(ref _activeInstanceCount, 0, 0) > 0) return; // Prevent multiple instances from running
            _isRunning.Value = true;
            Interlocked.Increment(ref _activeInstanceCount);
            StartInterval();
        }

        private void StartInterval()
        {
            _timerSubscription?.Dispose(); // Ensure only one interval runs
            _lastTickTime = Time.realtimeSinceStartup;

            _timerSubscription = Observable.EveryUpdate()
                .Where(_ => _isRunning.Value)
                .Subscribe(_ => UpdateStopwatch());
        }

        private void UpdateStopwatch()
        {
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - _lastTickTime;
            _lastTickTime = currentTime;

            if (!_isRunning.Value) return;

            _elapsedTime.Value += TimeSpan.FromSeconds(deltaTime);
        }

        public void StopStopwatch()
        {
            if (!_isRunning.Value) return;
            _isRunning.Value = false;
            Interlocked.Decrement(ref _activeInstanceCount);

        }

        public void ResetStopwatch()
        {
            _isRunning.Value = false;
            _timerSubscription?.Dispose();
            _timerSubscription = null;
            _elapsedTime.Value = TimeSpan.Zero;
            _laps.Value = new List<TimeSpan>().AsReadOnly(); // empty list
        }

        public void RecordLap()
        {
            // if paused can still record 
            var currentLaps = new List<TimeSpan>(_laps.Value);
            currentLaps.Add(_elapsedTime.Value);
            _laps.Value = currentLaps.AsReadOnly();
        }

        public void Dispose()
        {
            StopStopwatch();
            _timerSubscription?.Dispose();
            _elapsedTime.Dispose();
            _laps.Dispose();
            _isRunning.Dispose();
        }
    }
}
