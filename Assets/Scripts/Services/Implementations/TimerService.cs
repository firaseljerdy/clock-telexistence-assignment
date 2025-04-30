using System;
using UniRx;
using UnityEngine; 

namespace ClockApp.Services
{
    /// <summary>
    /// Implementation of <see cref="ITimerService"/>.
    /// </summary>
    public class TimerService : ITimerService, IDisposable
    {
        // state management - emits current value on subscribe.
        private readonly ReactiveProperty<TimeSpan> _remainingTime = new ReactiveProperty<TimeSpan>(TimeSpan.Zero);
        private readonly ReactiveProperty<bool> _isRunning = new ReactiveProperty<bool>(false);
        private readonly Subject<Unit> _completed = new Subject<Unit>();

        // Manage the timer subscription explicitly.
        private IDisposable _timerSubscription;
        private float _lastTickTime; //  actual time for accuracy
        private TimeSpan _targetDuration; //  initial duration for resets if needed

        public IReadOnlyReactiveProperty<TimeSpan> RemainingTime => _remainingTime;
        public IObservable<Unit> TimerCompleted => _completed;
        public IReadOnlyReactiveProperty<bool> IsRunning => _isRunning;

        public void StartTimer(TimeSpan duration)
        {
            if (_isRunning.Value) return;

            // Resume paused timer
            if (!_isRunning.Value && _remainingTime.Value > TimeSpan.Zero)
            {
                _isRunning.Value = true;
                StartInterval();
                return;
            }

            // Start a new timer
            ResetTimerInternal(disposeSubscription: false);
            _targetDuration = duration;
            _remainingTime.Value = duration;
            _isRunning.Value = true;
            StartInterval();
        }

        private void StartInterval()
        {
            // Dispose previous subscription if it exists (e.g., after reset)
            _timerSubscription?.Dispose();

            _lastTickTime = Time.realtimeSinceStartup;


            _timerSubscription = Observable.EveryUpdate()
                .Where(_ => _isRunning.Value) 
                .Subscribe(_ => UpdateTimer());
        }

        private void UpdateTimer()
        {
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - _lastTickTime;
            _lastTickTime = currentTime;


            if (!_isRunning.Value) return; 

            var newRemaining = _remainingTime.Value - TimeSpan.FromSeconds(deltaTime);

            if (newRemaining <= TimeSpan.Zero)
            {
                _remainingTime.Value = TimeSpan.Zero;
                _isRunning.Value = false;
                _completed.OnNext(Unit.Default);
                _timerSubscription?.Dispose();
                _timerSubscription = null;
            }
            else
            {
                _remainingTime.Value = newRemaining;
            }
        }

        public void PauseTimer()
        {
            if (!_isRunning.Value) return;
            _isRunning.Value = false;
            // _timerSubscription?.Dispose();
            // _timerSubscription = null;
        }

        public void ResetTimer()
        {
            ResetTimerInternal(disposeSubscription: true);
        }

        private void ResetTimerInternal(bool disposeSubscription)
        {
            _isRunning.Value = false;
            if (disposeSubscription)
            {
                _timerSubscription?.Dispose();
                _timerSubscription = null;
            }
            _targetDuration = TimeSpan.Zero;
            _remainingTime.Value = TimeSpan.Zero;
        }

        public void Dispose()
        {
            _timerSubscription?.Dispose();
            _remainingTime.Dispose();
            _isRunning.Dispose();
            _completed.Dispose();
        }
    }
}
