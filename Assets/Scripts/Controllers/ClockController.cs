using UnityEngine;
using UniRx;
using Zenject;
using ClockApp.Services;
using ClockApp.Data;
using TMPro;

namespace ClockApp.Controllers
{
    /// <summary>
    /// Displays the current time, timezone, and location provided by "IClockService"/>.
    /// Uses TextMeshPro components assigned in the Inspector.
    /// </summary>
    public class ClockController : MonoBehaviour
    {
        [Inject] private IClockService _clockService; // Field Injection

        [Header("UI References")]
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _locationText; 
        [SerializeField] private TMP_Text _timezoneText; 

        void Start()
        {
            // Make sure that these variables are initialized
            if (_timeText == null) Debug.LogError("ClockController: _timeText is not assigned.", this);
            if (_locationText == null) Debug.LogError("ClockController: _locationText is not assigned.", this);
            if (_timezoneText == null) Debug.LogError("ClockController: _timezoneText is not assigned.", this);

            // Subscribe to the ClockData stream from the service and ensure that update happens on the main thread
            _clockService.CurrentClockData
                .ObserveOnMainThread() 
                .Subscribe(UpdateClockDisplay)
                .AddTo(this);
        }

        private void UpdateClockDisplay(ClockData data)
        {
            if (_timeText != null)
            {
                _timeText.text = data.CurrentTime.ToString("HH:mm:ss");
            }
            if (_locationText != null)
            {
                
                // Could fetch city name from another API if needed, but TimeZoneId is provided by worldtimeapi
                _locationText.text = data.TimeZoneId;
            }
            if (_timezoneText != null)
            {
                // Displaying UTC offset
                _timezoneText.text = $"UTC{data.UtcOffset}";
            }
        }
    }
}
