using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using ClockApp.Controllers;
using ClockApp.Services;
using TMPro;
using UnityEngine.UI;
using UniRx;
using System;
using System.Collections.Generic;
using System.Reflection;

public class MockStopwatchService : IStopwatchService
{
    public ReactiveProperty<TimeSpan> ElapsedTime { get; } = new ReactiveProperty<TimeSpan>(TimeSpan.Zero);
    public ReactiveProperty<IReadOnlyList<TimeSpan>> Laps { get; } = new ReactiveProperty<IReadOnlyList<TimeSpan>>(new List<TimeSpan>().AsReadOnly());
    public ReactiveProperty<bool> IsRunning { get; } = new ReactiveProperty<bool>(false);

    IReadOnlyReactiveProperty<TimeSpan> IStopwatchService.ElapsedTime => ElapsedTime;
    IReadOnlyReactiveProperty<IReadOnlyList<TimeSpan>> IStopwatchService.Laps => Laps;
    IReadOnlyReactiveProperty<bool> IStopwatchService.IsRunning => IsRunning;

    public bool StartCalled { get; private set; }
    public bool StopCalled { get; private set; }
    public bool ResetCalled { get; private set; }
    public bool LapCalled { get; private set; }

    public void StartStopwatch()
    {
        StartCalled = true;
        IsRunning.Value = true;
        StopCalled = false;
        ResetCalled = false;
        LapCalled = false;
    }

    public void StopStopwatch()
    {
        StopCalled = true;
        IsRunning.Value = false;
    }

    public void ResetStopwatch()
    {
        ResetCalled = true;
        IsRunning.Value = false;
        ElapsedTime.Value = TimeSpan.Zero;
        Laps.Value = new List<TimeSpan>().AsReadOnly();
        StartCalled = false;
        StopCalled = false;
        LapCalled = false;
    }

    public void RecordLap()
    {
        LapCalled = true;
        if (IsRunning.Value)
        {
            var currentLaps = new List<TimeSpan>(Laps.Value);
            currentLaps.Add(ElapsedTime.Value);
            Laps.Value = currentLaps.AsReadOnly();
        }
    }


    public void SimulateTimePassing(TimeSpan timePassed)
    {
        if (IsRunning.Value)
        {
            ElapsedTime.Value += timePassed;
        }
    }
}


public class StopwatchControllerPlayModeTest : ZenjectIntegrationTestFixture
{
    private MockStopwatchService _mockStopwatchService;
    private StopwatchController _stopwatchController;


    private TMP_Text _displayComponent;
    private Button _startButtonComponent;
    private Button _stopButtonComponent;
    private Button _resetButtonComponent;
    private Button _lapButtonComponent;
    private Transform _lapsContainerTransform;
    private GameObject _lapEntryPrefab;

    [SetUp]
    public void CommonInstall()
    {
        _mockStopwatchService = new MockStopwatchService();

        PreInstall();

        Container.BindInterfacesAndSelfTo<MockStopwatchService>().FromInstance(_mockStopwatchService).AsSingle();

        var canvasGo = new GameObject("TestCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        var displayGo = new GameObject("Display");
        displayGo.transform.SetParent(canvasGo.transform);
        _displayComponent = displayGo.AddComponent<TextMeshProUGUI>();

        var startGo = new GameObject("StartButton");
        startGo.transform.SetParent(canvasGo.transform);
        _startButtonComponent = startGo.AddComponent<Button>();

        var stopGo = new GameObject("StopButton");
        stopGo.transform.SetParent(canvasGo.transform);
        _stopButtonComponent = stopGo.AddComponent<Button>();

        var resetGo = new GameObject("ResetButton");
        resetGo.transform.SetParent(canvasGo.transform);
        _resetButtonComponent = resetGo.AddComponent<Button>();

        var lapGo = new GameObject("LapButton");
        lapGo.transform.SetParent(canvasGo.transform);
        _lapButtonComponent = lapGo.AddComponent<Button>();

        var lapsContainerGo = new GameObject("LapsContainer");
        lapsContainerGo.transform.SetParent(canvasGo.transform);
        _lapsContainerTransform = lapsContainerGo.transform;

        _lapEntryPrefab = new GameObject("LapEntryPrefab");
        var lapTextGo = new GameObject("LapText");
        lapTextGo.transform.SetParent(_lapEntryPrefab.transform);
        lapTextGo.AddComponent<TextMeshProUGUI>();
        _lapEntryPrefab.SetActive(false);

        var controllerGo = new GameObject("StopwatchController");
        _stopwatchController = controllerGo.AddComponent<StopwatchController>();

        SetPrivateField(_stopwatchController, "_display", _displayComponent);
        SetPrivateField(_stopwatchController, "_startBtn", _startButtonComponent);
        SetPrivateField(_stopwatchController, "_stopBtn", _stopButtonComponent);
        SetPrivateField(_stopwatchController, "_resetBtn", _resetButtonComponent);
        SetPrivateField(_stopwatchController, "_lapBtn", _lapButtonComponent);
        SetPrivateField(_stopwatchController, "_lapsContainer", _lapsContainerTransform);
        SetPrivateField(_stopwatchController, "_lapEntryPrefab", _lapEntryPrefab);

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
        if (_lapEntryPrefab != null)
        {
             UnityEngine.Object.Destroy(_lapEntryPrefab);
        }
    }


    [UnityTest]
    public IEnumerator StopwatchController_StartStopwatch_UpdatesStateAndCallsService()
    {
        yield return null; 

        _startButtonComponent.onClick.Invoke();
        yield return null; 

        // Assert
        Assert.IsTrue(_mockStopwatchService.StartCalled, "MockStopwatchService.StartStopwatch was not called");
        Assert.IsFalse(_startButtonComponent.interactable, "Start button should be disabled after starting");
        Assert.IsTrue(_stopButtonComponent.interactable, "Stop button should be enabled after starting");
        Assert.IsTrue(_lapButtonComponent.interactable, "Lap button should be enabled after starting");
        Assert.IsFalse(_resetButtonComponent.interactable, "Reset button should be disabled after starting (ElapsedTime is zero)");
    }

    [UnityTest]
    public IEnumerator StopwatchController_StopStopwatch_UpdatesStateAndCallsService()
    {
        yield return null; 
        _startButtonComponent.onClick.Invoke(); 
        yield return null;
        _mockStopwatchService.SimulateTimePassing(TimeSpan.FromSeconds(1.5));
        yield return null;

        _stopButtonComponent.onClick.Invoke();
        yield return null; 

        Assert.IsTrue(_mockStopwatchService.StopCalled, "MockStopwatchService.StopStopwatch was not called");
        Assert.IsTrue(_startButtonComponent.interactable, "Start button should be enabled after stopping");
        Assert.IsFalse(_stopButtonComponent.interactable, "Stop button should be disabled after stopping");
        Assert.IsFalse(_lapButtonComponent.interactable, "Lap button should be disabled after stopping");
        Assert.IsTrue(_resetButtonComponent.interactable, "Reset button should be enabled after stopping (ElapsedTime > 0)");
    }

    [UnityTest]
    public IEnumerator StopwatchController_RecordLap_UpdatesStateAndCallsService()
    {

        yield return null; 
        _startButtonComponent.onClick.Invoke();
        yield return null;
        _mockStopwatchService.SimulateTimePassing(TimeSpan.FromSeconds(1.23));
        yield return null;

        _lapButtonComponent.onClick.Invoke();
        yield return null; 

        Assert.IsTrue(_mockStopwatchService.LapCalled, "MockStopwatchService.RecordLap was not called");
        Assert.AreEqual(1, _mockStopwatchService.Laps.Value.Count, "Lap count in service incorrect");
        Assert.AreEqual(1, _lapsContainerTransform.childCount, "Lap UI count incorrect");

        var lapText = _lapsContainerTransform.GetChild(0).GetComponentInChildren<TMP_Text>();
        Assert.IsNotNull(lapText, "Lap UI prefab missing TMP_Text");
        // Expected format: "LAP {i + 1}: {lapTime.Minutes:00}:{lapTime.Seconds:00}.{(lapTime.Milliseconds / 100):0}"
        Assert.AreEqual("LAP 1: 00:01.2", lapText.text, "Lap UI text incorrect");
    }

    [UnityTest]
    public IEnumerator StopwatchController_ElapsedTimeUpdate_UpdatesDisplay()
    {

        yield return null; 
        _startButtonComponent.onClick.Invoke(); 
        yield return null;

        TimeSpan testTime = TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(34) + TimeSpan.FromMilliseconds(567);

        _mockStopwatchService.ElapsedTime.Value = testTime; 
        yield return null; 

        Assert.AreEqual("01:34.5", _displayComponent.text, "Display text did not update correctly with elapsed time");
    }

    [UnityTest]
    public IEnumerator StopwatchController_MultipleLaps_UpdatesUICorrectly()
    {
        yield return null; 
        _startButtonComponent.onClick.Invoke(); 
        yield return null;

        var laps = new List<TimeSpan>
        {
            TimeSpan.FromSeconds(1.23),
            TimeSpan.FromSeconds(5.67),
            TimeSpan.FromSeconds(10.99)
        };
        _mockStopwatchService.Laps.Value = laps.AsReadOnly();
        yield return null; 

        // Assert
        Assert.AreEqual(3, _lapsContainerTransform.childCount, "Incorrect number of lap UI elements");
        Assert.AreEqual("LAP 1: 00:01.2", _lapsContainerTransform.GetChild(0).GetComponentInChildren<TMP_Text>().text);
        Assert.AreEqual("LAP 2: 00:05.6", _lapsContainerTransform.GetChild(1).GetComponentInChildren<TMP_Text>().text);
        Assert.AreEqual("LAP 3: 00:10.9", _lapsContainerTransform.GetChild(2).GetComponentInChildren<TMP_Text>().text);
    }
}
