using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;
using System;
using ClockApp.Services;
using TMPro;

namespace ClockApp.Controllers
{
    /// <summary>
    /// Controls the UI for the Timer functionality, interacting with <see cref="ITimerService"/>.
    /// </summary>
    public class TimerController : MonoBehaviour
    {
        [Inject] private ITimerService _timerService;

        [Header("Display")][SerializeField] private TMP_Text _display;
        [SerializeField] private AudioSource _ring;

        [Header("Buttons")][SerializeField] private Button _startBtn;
        [SerializeField] private Button _pauseBtn;
        [SerializeField] private Button _resetBtn;

        [Header("Input")][SerializeField] private TMP_InputField _minutesInput;
        [SerializeField] private TMP_InputField _secondsInput;

        // Store the last valid duration set by the user or 1 by default
        private TimeSpan _currentSetDuration = TimeSpan.FromMinutes(1);

        void Start()
        {
             Debug.Log($"TimerController Start() called on {gameObject.name}");

             // Dependndecy Checks
             if (_timerService == null) Debug.LogError(" TimerService NOT Injected!", this);
             else Debug.Log(" TimerService Injected successfully.", this);

             if (_display == null) Debug.LogError(" _display NOT assigned in prefab!", this);
             if (_startBtn == null) Debug.LogError(" _startBtn NOT assigned in prefab!", this);
             if (_pauseBtn == null) Debug.LogError(" _pauseBtn NOT assigned in prefab!", this);
             if (_resetBtn == null) Debug.LogError(" _resetBtn NOT assigned in prefab!", this);
             if (_minutesInput == null) Debug.LogError(" _minutesInput NOT assigned in prefab!", this);
             if (_secondsInput == null) Debug.LogError(" _secondsInput NOT assigned in prefab!", this);
             if (_ring == null) Debug.LogWarning(" _ring (AudioSource) not assigned in prefab.", this);

            //Input Field Setup & Validation
            InitializeInputFields(_currentSetDuration);
            _minutesInput?.onValueChanged.AddListener(ValidateInput);
            _secondsInput?.onValueChanged.AddListener(ValidateInput);

            //  Button Click Handling
            _startBtn?.OnClickAsObservable()
                .Select(_ => GetDurationFromInput())
                .Where(duration => duration.HasValue)
                .Subscribe(duration => {
                    _currentSetDuration = duration.Value;
                     _timerService.StartTimer(_currentSetDuration);
                 })
                .AddTo(this);

            _pauseBtn?.OnClickAsObservable()
                .Subscribe(_ => _timerService.PauseTimer())
                .AddTo(this);

            _resetBtn?.OnClickAsObservable()
                .Subscribe(_ => {
                    _timerService.ResetTimer();
                    InitializeInputFields(_currentSetDuration);
                    HandleRunningState(false);
                })
                .AddTo(this);

            // --- Service Subscriptions ---
             Debug.Log("Setting up service subscriptions...");
            _timerService.RemainingTime
                .ObserveOnMainThread()
                .Subscribe(UpdateDisplay)
                .AddTo(this);

            _timerService.TimerCompleted
                .ObserveOnMainThread()
                .Subscribe(_ => PlayRingSound())
                .AddTo(this);

            _timerService.IsRunning
                .ObserveOnMainThread()
                .Subscribe(HandleRunningState)
                .AddTo(this);

            // Initial state setup
             Debug.Log("Applying initial state...");
            UpdateDisplay(_timerService.RemainingTime.Value);
            HandleRunningState(_timerService.IsRunning.Value);
        }

        void OnDestroy()
        {
             if (_minutesInput != null) _minutesInput.onValueChanged.RemoveListener(ValidateInput);
             if (_secondsInput != null) _secondsInput.onValueChanged.RemoveListener(ValidateInput);
             Debug.Log($"TimerController OnDestroy() called on {gameObject.name}");
        }

        private void InitializeInputFields(TimeSpan duration)
        {
             if (_minutesInput != null) _minutesInput.text = duration.Minutes.ToString("D2");
             if (_secondsInput != null) _secondsInput.text = duration.Seconds.ToString("D2");
        }

        private TimeSpan? GetDurationFromInput()
        {
            if (_minutesInput == null || _secondsInput == null) {
                 Debug.LogError("Timer input fields not assigned.", this);
                 return null;
            }

            if (int.TryParse(_minutesInput.text, out int minutes) &&
                int.TryParse(_secondsInput.text, out int seconds))
            {
                minutes = Mathf.Max(0, minutes);
                seconds = Mathf.Clamp(seconds, 0, 59);
                 _minutesInput.text = minutes.ToString("D2");
                 _secondsInput.text = seconds.ToString("D2");
                TimeSpan duration = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                if (duration <= TimeSpan.Zero) {
                     Debug.LogWarning("Timer duration must be greater than zero.");
                     return null;
                 }
                return duration;
            }
            else
            {
                Debug.LogError("Invalid timer input. Please enter numbers.");
                 InitializeInputFields(_currentSetDuration);
                return null;
            }
        }


        private void ValidateInput(string input)
        {
            // potential real-time validation logic
        }

        private void UpdateDisplay(TimeSpan ts)
        {
             // Add check here before accessing text
             if (_display != null)
            {
                _display.text = $"{Math.Floor(ts.TotalMinutes):00}:{ts.Seconds:00}";
            }
             else { Debug.LogError("UpdateDisplay called but _display is null!", this); }
        }

        private void PlayRingSound()
        {
            if (_ring != null && !_ring.isPlaying)
            {
                _ring.Play();
            }
            else if (_ring == null)
            {
                 // Already logged in Start, but can add warning here too if needed
                 // Debug.LogWarning("TimerController: PlayRingSound called but _ring AudioSource not assigned.", this);
            }
        }

        private void HandleRunningState(bool isRunning)
        {
             // Add checks before accessing interactable
             if (_startBtn != null) _startBtn.interactable = !isRunning;
             else { Debug.LogError("HandleRunningState: _startBtn is null!", this); }

             if (_pauseBtn != null) _pauseBtn.interactable = isRunning;
             else { Debug.LogError("HandleRunningState: _pauseBtn is null!", this); }

            bool allowInput = !isRunning && _timerService.RemainingTime.Value <= TimeSpan.Zero;
             if (_minutesInput != null) _minutesInput.interactable = allowInput;
             else { Debug.LogError("HandleRunningState: _minutesInput is null!", this); }

             if (_secondsInput != null) _secondsInput.interactable = allowInput;
             else { Debug.LogError("HandleRunningState: _secondsInput is null!", this); }

             if (_resetBtn != null) _resetBtn.interactable = !isRunning || _timerService.RemainingTime.Value > TimeSpan.Zero;
             else { Debug.LogError("HandleRunningState: _resetBtn is null!", this); }
        }
    }
}
