using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;
using System;
using System.Collections.Generic;
using ClockApp.Services;
using TMPro;

namespace ClockApp.Controllers
{
    /// <summary>
    /// Controls the UI for the Stopwatch functionality, interacting with IStopwatchService/>.
    /// </summary>
    public class StopwatchController : MonoBehaviour
    {
        [Inject] private IStopwatchService _swService;
        [SerializeField] private TMP_Text _display;
        [SerializeField] private Button _startBtn;
        [SerializeField] private Button _stopBtn;
        [SerializeField] private Button _resetBtn;
        [SerializeField] private Button _lapBtn;
        [SerializeField] private Transform _lapsContainer;
        [SerializeField] private GameObject _lapEntryPrefab;

        void Start()
        {
            // Input Handling
            _startBtn?.OnClickAsObservable()
                .Subscribe(_ => _swService.StartStopwatch())
                .AddTo(this);

            _stopBtn?.OnClickAsObservable()
                .Subscribe(_ => _swService.StopStopwatch())
                .AddTo(this);

            _resetBtn?.OnClickAsObservable()
                .Subscribe(_ => _swService.ResetStopwatch())
                .AddTo(this);

            _lapBtn?.OnClickAsObservable()
                .Subscribe(_ => _swService.RecordLap())
                .AddTo(this);

            // Service Subscriptions
            _swService.ElapsedTime
                .ObserveOnMainThread()
                .Subscribe(UpdateDisplay)
                .AddTo(this);

            _swService.Laps
                .ObserveOnMainThread()
                .Subscribe(UpdateLapsUI)
                .AddTo(this);

            // Update button based on state
            _swService.IsRunning
                .ObserveOnMainThread()
                .Subscribe(HandleRunningState)
                .AddTo(this);

            // Initial state setup
            UpdateDisplay(_swService.ElapsedTime.Value);
            UpdateLapsUI(_swService.Laps.Value);
            HandleRunningState(_swService.IsRunning.Value);
        }

        private void UpdateDisplay(TimeSpan ts)
        {
            // minutes, seconds, and 1/10 of a second
            if (_display != null) {
                 _display.text = $"{ts.Minutes:00}:{ts.Seconds:00}.{(ts.Milliseconds / 100):0}";
            }
        }

        private void UpdateLapsUI(IReadOnlyList<TimeSpan> laps)
        {
            if (_lapsContainer == null || _lapEntryPrefab == null) return;

            foreach (Transform child in _lapsContainer)
            {
                Destroy(child.gameObject);
            }

            if (laps == null) return;

            for (int i = 0; i < laps.Count; i++)
            {
                var go = Instantiate(_lapEntryPrefab, _lapsContainer);

                var lapTextComponent = go.GetComponentInChildren<TMP_Text>();
                if (lapTextComponent != null)
                {                  var lapTime = laps[i];
                    lapTextComponent.text = $"LAP {i + 1}: {lapTime.Minutes:00}:{lapTime.Seconds:00}.{(lapTime.Milliseconds / 100):0}";
                }
                else
                {
                    // handle invalid entry
                    Debug.LogError("StopwatchController: Lap Entry Prefab or its children is missing TMP_Text component", go);
                    Destroy(go); 
                }
            }
        }

         private void HandleRunningState(bool isRunning)
        {

            if (_startBtn != null) _startBtn.interactable = !isRunning;
            if (_stopBtn != null) _stopBtn.interactable = isRunning;
            if (_lapBtn != null) _lapBtn.interactable = isRunning;
            if (_resetBtn != null) _resetBtn.interactable = !isRunning || _swService.ElapsedTime.Value > TimeSpan.Zero;
        }
    }
}
