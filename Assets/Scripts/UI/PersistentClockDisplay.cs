using UnityEngine;
using TMPro;
using UniRx;
using ClockApp.Services;
using ClockApp.Data;
using Zenject;

namespace ClockApp.UI
{
    /// <summary>
    /// Can be added to any scene to display the persistent clock.
    /// </summary>
    public class PersistentClockDisplay : MonoBehaviour
    {
        // --- Injected Dependencies ---
        [Inject] private IClockService _clockService;

        [Header("UI References")]
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private bool _showTimeZone = false;
        [SerializeField] private TMP_Text _timezoneText;

        [Header("Display Options")]
        [SerializeField] private string _timeFormat = "HH:mm:ss";
        // Removed _updateEveryFrame as the service pushes updates reactively

        // --- Runtime State ---
        private CompositeDisposable _disposables = new CompositeDisposable();

        private void Start()
        {
            // Basic UI validation
            if (_timeText == null)
            {
                Debug.LogError("PersistentClockDisplay: _timeText is not assigne", this);
                enabled = false;
                return;
            }
            if (_showTimeZone && _timezoneText == null)
            {
                Debug.LogWarning("PersistentClockDisplay: _timezoneText is not assigned but _showTimeZone is true. Disabling timezone display", this);
                _showTimeZone = false;
            }

            // Hide timezone text initially if not shown or missing
            if (_timezoneText != null)
            {
                _timezoneText.gameObject.SetActive(_showTimeZone);
            }

            // Check if injection worked
            if (_clockService == null)
            {
                 Debug.LogError("PersistentClockDisplay: IClockService was NOT injected:", this);
                 //UpdateDisplayWithSystemTime(); 
                 enabled = false; 
                 return;
            }

            // Subscribe to clock data updates from the injected service
            _clockService.CurrentClockData
                .ObserveOnMainThread()
                .Subscribe(UpdateDisplay)
                .AddTo(_disposables);
        }

        private void UpdateDisplay(ClockData data)
        {
            if (_timeText != null)
            {
                 _timeText.text = data.CurrentTime.ToString(_timeFormat);
            }

            if (_timezoneText != null)
            {
                if (_showTimeZone)
                {
                    _timezoneText.gameObject.SetActive(true);
                    _timezoneText.text = $"{data.TimeZoneId} ({data.UtcOffset})";
                }
                else
                {
                    _timezoneText.gameObject.SetActive(false);
                }
            }
        }

        // Fallback method in case service injection fails
        private void UpdateDisplayWithSystemTime()
        {
             Debug.LogWarning("PersistentClockDisplay: Using system time as fallback.", this);
            if (_timeText != null)
            {
                 _timeText.text = System.DateTime.Now.ToString(_timeFormat);
            }
            if (_timezoneText != null)
            {
                 if (_showTimeZone)
                 {
                     var localTimeZone = System.TimeZoneInfo.Local;
                     var offset = localTimeZone.GetUtcOffset(System.DateTime.Now).ToString("c").Substring(0, 6);
                     _timezoneText.gameObject.SetActive(true);
                     _timezoneText.text = $"{localTimeZone.StandardName} ({offset})";
                 }
                 else
                 {
                     _timezoneText.gameObject.SetActive(false);
                 }
            }
        }

        private void OnDestroy()
        {
            // Dispose UniRx subscriptions to prevent memory leaks
            _disposables.Dispose();
        }
    }
} 