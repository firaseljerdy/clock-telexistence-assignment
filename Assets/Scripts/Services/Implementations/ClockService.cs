using System;
using UniRx;
using UnityEngine;
using UnityEngine.Networking; // Required for UnityWebRequest
using ClockApp.Data; // Import ClockData
using System.Collections; // Required for IEnumerator coroutine

// Helper class for JSON deserialization
[System.Serializable]
public class WorldTimeApiResponse
{
    public string datetime;
    public string timezone;
    public string utc_offset;
}

namespace ClockApp.Services
{
    /// <summary>
    /// Implementation of <see cref="IClockService"/> that fetches time data from worldtimeapi.org
    /// and falls back to system time on failure. Ensures continuous time update.
    /// </summary>
    public class ClockService : IClockService, IDisposable
    {
        private readonly TimeSpan _apiCheckInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _displayUpdateInterval = TimeSpan.FromSeconds(1);
        private const string TimeApiUrl = "http://worldtimeapi.org/api/ip";

        private readonly BehaviorSubject<ClockData> _currentClockDataSubject;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private ClockData _lastGoodData; // Cache the last known state (server or system)

        public IReadOnlyReactiveProperty<ClockData> CurrentClockData { get; }

        public ClockService()
        {
            // Initialize with system time immediately
            _lastGoodData = ClockData.CreateSystemTimeData();
            _currentClockDataSubject = new BehaviorSubject<ClockData>(_lastGoodData);
            CurrentClockData = _currentClockDataSubject.ToReadOnlyReactiveProperty();

            Debug.Log("ClockService: Initializing with system time.");

            // --- API Check Timer ---
            Observable.Timer(TimeSpan.Zero, _apiCheckInterval) // Start immediately, then repeat
                .Do(_ => Debug.Log("ClockService: Attempting API fetch..."))
                .SelectMany(_ => FetchTimeFromApiObservable()
                    .Catch((Exception ex) =>
                    {
                        // Log clearly on API error and provide fallback data
                        Debug.LogWarning($"ClockService: API Error: {ex.Message}. Falling back to system time.");
                        return Observable.Return(ClockData.CreateSystemTimeData());
                    }))
                .Subscribe(UpdateClockDataSource) // Update the underlying data source
                .AddTo(_disposables);

            // --- Continuous Display Update Timer ---
            // This timer runs independently every second to increment the display time
            Observable.Interval(_displayUpdateInterval)
                .Select(_ => IncrementAndUpdateCachedTime()) // Increment AND update the cache
                .Do(data => Debug.Log($"ClockService: Tick - Pushing Time: {data.CurrentTime:HH:mm:ss}, IsServer: {data.IsServerTime}"))
                .Subscribe(_currentClockDataSubject.OnNext) // Push update to subscribers
                .AddTo(_disposables);
        }

        // Called when new data (from API or fallback) is available
        private void UpdateClockDataSource(ClockData newData)
        {
            Debug.Log($"ClockService: Updating clock data source. IsServerTime: {newData.IsServerTime}");
            _lastGoodData = newData; // Update the cached data
            // Immediately push the fresh data to the subject so the UI updates instantly,
            // rather than waiting for the next 1-second interval tick.
            _currentClockDataSubject.OnNext(_lastGoodData);
        }

        // Increments the time in the last known data source by one second AND updates the cache
        private ClockData IncrementAndUpdateCachedTime()
        {
            // Create the new data with incremented time
            var incrementedData = new ClockData(
                _lastGoodData.CurrentTime.AddSeconds(1),
                _lastGoodData.TimeZoneId,
                _lastGoodData.UtcOffset,
                _lastGoodData.IsServerTime
            );
            // IMPORTANT: Update the cached field itself
            _lastGoodData = incrementedData;
            // Return the updated data
            return _lastGoodData;
        }

        // Creates an Observable wrapper around the web request coroutine
        private IObservable<ClockData> FetchTimeFromApiObservable()
        {
            return Observable.FromCoroutine<ClockData>(observer => FetchTimeCoroutine(observer));
        }

        // Coroutine to handle the web request
        private IEnumerator FetchTimeCoroutine(IObserver<ClockData> observer)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(TimeApiUrl))
            {
                request.timeout = 10; // seconds
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonResponse = request.downloadHandler.text;
                        WorldTimeApiResponse apiResponse = JsonUtility.FromJson<WorldTimeApiResponse>(jsonResponse);

                        if (DateTime.TryParse(apiResponse.datetime, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime serverTime))
                        {
                            Debug.Log("ClockService: API fetch successful.");
                            observer.OnNext(new ClockData(serverTime, apiResponse.timezone, apiResponse.utc_offset, true));
                            observer.OnCompleted();
                        }
                        else
                        {
                             Debug.LogError("ClockService: Failed to parse datetime from API response.");
                            observer.OnError(new FormatException("Failed to parse datetime from API response."));
                        }
                    }
                    catch (Exception ex)
                    {
                         Debug.LogError($"ClockService: Error processing API response: {ex.Message}");
                        observer.OnError(ex);
                    }
                }
                else
                {
                    // Log the specific network error
                    Debug.LogWarning($"ClockService: API request failed. Error: {request.error} (Curl Error Code might be relevant)");
                    observer.OnError(new Exception($"API request failed: {request.error}"));
                }
            }
        }

        public void Dispose()
        {
            _disposables.Dispose(); // Disposes API check and display update timers
            _currentClockDataSubject.Dispose();
        }
    }
}
