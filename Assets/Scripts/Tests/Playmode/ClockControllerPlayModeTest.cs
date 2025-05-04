using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using ClockApp.Controllers;
using ClockApp.Services;
using ClockApp.Data;
using TMPro;
using UniRx;
using System;
using System.Reflection;

public class MockClockService : IClockService
{
    private readonly BehaviorSubject<ClockData> _currentClockDataSubject;
    public IReadOnlyReactiveProperty<ClockData> CurrentClockData { get; }

    public MockClockService(ClockData initialData)
    {
        _currentClockDataSubject = new BehaviorSubject<ClockData>(initialData);
        CurrentData = initialData; 
        CurrentClockData = _currentClockDataSubject.ToReadOnlyReactiveProperty();
    }

    public ClockData CurrentData { get; private set; }

    public void UpdateData(ClockData newData)
    {
        CurrentData = newData;
        _currentClockDataSubject.OnNext(newData);
    }
}

public class ClockControllerPlayModeTest : ZenjectIntegrationTestFixture
{
    private MockClockService _mockClockService;
    private TMP_Text _timeTextComponent;
    private TMP_Text _locationTextComponent;
    private TMP_Text _timezoneTextComponent;
    private ClockController _clockController;

    [SetUp]
    public void CommonInstall()
    {

        var initialData = new ClockData(new DateTime(2024, 1, 1, 10, 30, 0), "Test/Zone", "+01:00", true);
        _mockClockService = new MockClockService(initialData);

        PreInstall(); 


        Container.BindInterfacesAndSelfTo<MockClockService>().FromInstance(_mockClockService).AsSingle();

  
        var canvasGo = new GameObject("TestCanvas"); 
        var canvas = canvasGo.AddComponent<Canvas>();

        var timeGo = new GameObject("TimeText");
        timeGo.transform.SetParent(canvasGo.transform);
        _timeTextComponent = timeGo.AddComponent<TextMeshProUGUI>();

        var locationGo = new GameObject("LocationText");
        locationGo.transform.SetParent(canvasGo.transform);
        _locationTextComponent = locationGo.AddComponent<TextMeshProUGUI>();

        var timezoneGo = new GameObject("TimezoneText");
        timezoneGo.transform.SetParent(canvasGo.transform);
        _timezoneTextComponent = timezoneGo.AddComponent<TextMeshProUGUI>();

        var controllerGo = new GameObject("ClockController");
        controllerGo.transform.SetParent(canvasGo.transform); 
        _clockController = controllerGo.AddComponent<ClockController>();

        var clockControllerType = typeof(ClockController);

        var timeTextField = clockControllerType.GetField("_timeText", BindingFlags.NonPublic | BindingFlags.Instance);
        if (timeTextField != null)
        {
            timeTextField.SetValue(_clockController, _timeTextComponent);
        } else { Debug.LogError("Could not find _timeText field via reflection."); }

        var locationTextField = clockControllerType.GetField("_locationText", BindingFlags.NonPublic | BindingFlags.Instance);
         if (locationTextField != null)
         {
             locationTextField.SetValue(_clockController, _locationTextComponent);
         } else { Debug.LogError("Could not find _locationText field via reflection."); }

        var timezoneTextField = clockControllerType.GetField("_timezoneText", BindingFlags.NonPublic | BindingFlags.Instance);
         if (timezoneTextField != null)
         {
            timezoneTextField.SetValue(_clockController, _timezoneTextComponent);
         } else { Debug.LogError("Could not find _timezoneText field via reflection."); }



        Container.InjectGameObject(controllerGo);

        PostInstall(); 
    }


    [UnityTest]
    public IEnumerator ClockController_DisplaysInitialTimeCorrectly()
    {

        yield return null;
        yield return null; 


        var initialData = _mockClockService.CurrentData;
        Assert.AreEqual(initialData.CurrentTime.ToString("HH:mm:ss"), _timeTextComponent.text, "Initial time mismatch");
        Assert.AreEqual(initialData.TimeZoneId, _locationTextComponent.text, "Initial location mismatch");
        Assert.AreEqual($"UTC{initialData.UtcOffset}", _timezoneTextComponent.text, "Initial timezone mismatch");
    }

    [UnityTest]
    public IEnumerator ClockController_UpdatesTimeCorrectly()
    {

         yield return null; 
         yield return null; 

        var newData = new ClockData(new DateTime(2024, 5, 15, 22, 05, 10), "New/Zone", "-05:00", true);

        _mockClockService.UpdateData(newData);
        yield return null; 


        Assert.AreEqual(newData.CurrentTime.ToString("HH:mm:ss"), _timeTextComponent.text, "Updated time mismatch");
        Assert.AreEqual(newData.TimeZoneId, _locationTextComponent.text, "Updated location mismatch");
        Assert.AreEqual($"UTC{newData.UtcOffset}", _timezoneTextComponent.text, "Updated timezone mismatch");
    }

}
