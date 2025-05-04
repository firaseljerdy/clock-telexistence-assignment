using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using ClockApp.Controllers;
using ClockApp.Services;
using TMPro;
using UnityEngine.UI; // Required for Button
using UniRx;
using System;
using System.Reflection;

public class MockTimerService : ITimerService
{
    public ReactiveProperty<TimeSpan> RemainingTime { get; } = new ReactiveProperty<TimeSpan>(TimeSpan.Zero);
    public ReactiveProperty<bool> IsRunning { get; } = new ReactiveProperty<bool>(false);
    public Subject<Unit> Completed { get; } = new Subject<Unit>();

    IReadOnlyReactiveProperty<TimeSpan> ITimerService.RemainingTime => RemainingTime;
    IObservable<Unit> ITimerService.TimerCompleted => Completed;
    IReadOnlyReactiveProperty<bool> ITimerService.IsRunning => IsRunning;

    public TimeSpan LastStartedDuration { get; private set; }
    public bool StartCalled { get; private set; }
    public bool PauseCalled { get; private set; }
    public bool ResetCalled { get; private set; }

    public void StartTimer(TimeSpan duration)
    {
        StartCalled = true;
        LastStartedDuration = duration;
        if (RemainingTime.Value <= TimeSpan.Zero)
        {
             RemainingTime.Value = duration;
        }
        IsRunning.Value = true;
        PauseCalled = false;
        ResetCalled = false;
    }

    public void PauseTimer()
    {
        PauseCalled = true;
        IsRunning.Value = false;
    }

    public void ResetTimer()
    {
        ResetCalled = true;
        IsRunning.Value = false;
        RemainingTime.Value = TimeSpan.Zero;
        StartCalled = false;
        PauseCalled = false;
    }

    public void SimulateTimePassing(TimeSpan timePassed)
    {
        if (IsRunning.Value)
        {
            var newTime = RemainingTime.Value - timePassed;
            if (newTime <= TimeSpan.Zero)
            {
                RemainingTime.Value = TimeSpan.Zero;
                IsRunning.Value = false;
                Completed.OnNext(Unit.Default);
            }
            else
            {
                RemainingTime.Value = newTime;
            }
        }
    }

    public void SetRemainingTime(TimeSpan time)
    {
         RemainingTime.Value = time;
    }
}

public class MockAudioService : IAudioService
{
    public bool PlaySoundCalled { get; private set; }
    public AudioClip LastClipPlayed { get; private set; }

    public void PlaySound(AudioClip clip)
    {
        PlaySoundCalled = true;
        LastClipPlayed = clip;
    }
}

public class TimerControllerPlayModeTest : ZenjectIntegrationTestFixture
{
    private MockTimerService _mockTimerService;
    private MockAudioService _mockAudioService;
    private TimerController _timerController;

    // UI Components
    private TMP_Text _displayComponent;
    private Button _startButtonComponent;
    private Button _pauseButtonComponent;
    private Button _resetButtonComponent;
    private TMP_InputField _minutesInputComponent;
    private TMP_InputField _secondsInputComponent;
    private AudioClip _testAudioClip;

    [SetUp]
    public void CommonInstall()
    {
        _mockTimerService = new MockTimerService();
        _mockAudioService = new MockAudioService();
        _testAudioClip = AudioClip.Create("TestClip", 1, 1, 1000, false); // Dummy clip

        PreInstall();

        // Bind Mocks
        Container.BindInterfacesAndSelfTo<MockTimerService>().FromInstance(_mockTimerService).AsSingle();
        Container.BindInterfacesAndSelfTo<MockAudioService>().FromInstance(_mockAudioService).AsSingle();
        Container.Bind<AudioClip>().WithId("TimerCompletionSound").FromInstance(_testAudioClip).AsSingle();

        var canvasGo = new GameObject("TestCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();

        var displayGo = new GameObject("Display");
        displayGo.transform.SetParent(canvasGo.transform);
        _displayComponent = displayGo.AddComponent<TextMeshProUGUI>();

        var startGo = new GameObject("StartButton");
        startGo.transform.SetParent(canvasGo.transform);
        _startButtonComponent = startGo.AddComponent<Button>();

        var pauseGo = new GameObject("PauseButton");
        pauseGo.transform.SetParent(canvasGo.transform);
        _pauseButtonComponent = pauseGo.AddComponent<Button>();

        var resetGo = new GameObject("ResetButton");
        resetGo.transform.SetParent(canvasGo.transform);
        _resetButtonComponent = resetGo.AddComponent<Button>();

        var minutesGo = new GameObject("MinutesInput");
        minutesGo.transform.SetParent(canvasGo.transform);
        _minutesInputComponent = minutesGo.AddComponent<TMP_InputField>();
        var minutesTextGo = new GameObject("Text");
        minutesTextGo.transform.SetParent(minutesGo.transform);
        minutesTextGo.AddComponent<TextMeshProUGUI>();
         _minutesInputComponent.textComponent = minutesTextGo.GetComponent<TextMeshProUGUI>();

        var secondsGo = new GameObject("SecondsInput");
        secondsGo.transform.SetParent(canvasGo.transform);
        _secondsInputComponent = secondsGo.AddComponent<TMP_InputField>();
        var secondsTextGo = new GameObject("Text");
        secondsTextGo.transform.SetParent(secondsGo.transform);
        secondsTextGo.AddComponent<TextMeshProUGUI>();
         _secondsInputComponent.textComponent = secondsTextGo.GetComponent<TextMeshProUGUI>();

        var controllerGo = new GameObject("TimerController");
        _timerController = controllerGo.AddComponent<TimerController>();

        SetPrivateField(_timerController, "_display", _displayComponent);
        SetPrivateField(_timerController, "_startBtn", _startButtonComponent);
        SetPrivateField(_timerController, "_pauseBtn", _pauseButtonComponent);
        SetPrivateField(_timerController, "_resetBtn", _resetButtonComponent);
        SetPrivateField(_timerController, "_minutesInput", _minutesInputComponent);
        SetPrivateField(_timerController, "_secondsInput", _secondsInputComponent);
        SetPrivateField(_timerController, "_timerRingClip", _testAudioClip);

        Container.InjectGameObject(controllerGo);

        PostInstall();
    }

    private void SetPrivateField<TController, TField>(TController controller, string fieldName, TField value)
    {
        var field = typeof(TController).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(controller, value);
        }
        else
        {
            Debug.LogError($"Could not find field '{fieldName}' in {typeof(TController).Name} via reflection.");
        }
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.Destroy(_testAudioClip);
    }


     [UnityTest]
     public IEnumerator TimerController_StartTimer_UpdatesStateAndCallsService()
     {
         yield return null; 
         _minutesInputComponent.text = "02";
         _secondsInputComponent.text = "30";

         _startButtonComponent.onClick.Invoke();
         yield return null;

         Assert.IsTrue(_mockTimerService.StartCalled, "MockTimerService.StartTimer was not called");
         Assert.AreEqual(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(30), _mockTimerService.LastStartedDuration, "Timer started with incorrect duration");

         Assert.IsFalse(_startButtonComponent.interactable, "Start button should be disabled after starting");
         Assert.IsTrue(_pauseButtonComponent.interactable, "Pause button should be enabled after starting");
         Assert.IsTrue(_resetButtonComponent.interactable, "Reset button should be enabled after starting");
         Assert.IsFalse(_minutesInputComponent.interactable, "Minutes input should be disabled after starting");
         Assert.IsFalse(_secondsInputComponent.interactable, "Seconds input should be disabled after starting");
     }

      [UnityTest]
      public IEnumerator TimerController_UpdateDisplay_ReflectsRemainingTime()
      {
          yield return null; 
          _minutesInputComponent.text = "01";
          _secondsInputComponent.text = "00";
          _startButtonComponent.onClick.Invoke();
          yield return null;

          TimeSpan testTime = TimeSpan.FromSeconds(45.5);

          _mockTimerService.SetRemainingTime(testTime);
          yield return null; 

          Assert.AreEqual($"{Math.Floor(testTime.TotalMinutes):00}:{testTime.Seconds:00}", _displayComponent.text, "Display text did not update correctly");
      }

       [UnityTest]
       public IEnumerator TimerController_PauseTimer_UpdatesStateAndCallsService()
       {

           yield return null;
           _minutesInputComponent.text = "01";
           _secondsInputComponent.text = "00";
           _startButtonComponent.onClick.Invoke(); 
           yield return null;

           _pauseButtonComponent.onClick.Invoke();
           yield return null; 

           Assert.IsTrue(_mockTimerService.PauseCalled, "MockTimerService.PauseTimer was not called");
           Assert.IsFalse(_mockTimerService.IsRunning.Value, "Timer should not be running after pause");
           Assert.IsTrue(_startButtonComponent.interactable, "Start button should be enabled after pausing");
           Assert.IsFalse(_pauseButtonComponent.interactable, "Pause button should be disabled after pausing");
           Assert.IsTrue(_resetButtonComponent.interactable, "Reset button should still be enabled after pausing");
           Assert.IsFalse(_minutesInputComponent.interactable, "Minutes input should remain disabled when paused");
           Assert.IsFalse(_secondsInputComponent.interactable, "Seconds input should remain disabled when paused");
       }

        [UnityTest]
        public IEnumerator TimerController_ResumeTimer_UpdatesState()
        {

            yield return null; 
            _minutesInputComponent.text = "00";
            _secondsInputComponent.text = "30";
            _startButtonComponent.onClick.Invoke(); 
            yield return null;
            _pauseButtonComponent.onClick.Invoke(); 
            yield return null;
            Assert.IsFalse(_mockTimerService.IsRunning.Value, "Timer should be paused before resuming");

            _startButtonComponent.onClick.Invoke();
            yield return null;


            Assert.IsTrue(_mockTimerService.IsRunning.Value, "Timer should be running after resume");
            Assert.IsFalse(_startButtonComponent.interactable, "Start button should be disabled after resuming");
            Assert.IsTrue(_pauseButtonComponent.interactable, "Pause button should be enabled after resuming");
        }


         [UnityTest]
         public IEnumerator TimerController_ZeroDurationInput_DoesNotStartTimer()
         {
                // Arrange
                yield return null; // Wait for Start()
                _minutesInputComponent.text = "00";
                _secondsInputComponent.text = "00";

                // Act
                _startButtonComponent.onClick.Invoke();
                yield return null;

                // Assert
                Assert.IsFalse(_mockTimerService.StartCalled, "MockTimerService.StartTimer should not be called with zero duration");
                Assert.IsTrue(_startButtonComponent.interactable, "Start button should remain interactable after failed start (zero duration)");
         }
}
