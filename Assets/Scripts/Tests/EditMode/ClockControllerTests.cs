using NUnit.Framework;
using UnityEngine;
using Zenject;
using ClockApp.Controllers;
using ClockApp.Services;
using ClockApp.Data;
using TMPro;
using System;
using UniRx;

[TestFixture]
public class ClockControllerTests : ZenjectUnitTestFixture
{
    private ClockController _controller;
    private TMP_Text _timeText, _locationText, _timezoneText;
    private TestClockService _fakeService;
    private GameObject _gameObject;

    [SetUp]
    public void SetUp()
    {
        // Create controller and UI components
        _timeText = new GameObject("TimeText").AddComponent<TextMeshPro>();
        _locationText = new GameObject("LocationText").AddComponent<TextMeshPro>();
        _timezoneText = new GameObject("TimezoneText").AddComponent<TextMeshPro>();

        _gameObject = new GameObject("ClockController");
        _controller = _gameObject.AddComponent<ClockController>();

        Inject("_timeText", _timeText);
        Inject("_locationText", _locationText);
        Inject("_timezoneText", _timezoneText);

        _fakeService = new TestClockService();
        Container.Bind<IClockService>().FromInstance(_fakeService);
        Container.InjectGameObject(_gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(_gameObject);
        UnityEngine.Object.DestroyImmediate(_timeText.gameObject);
        UnityEngine.Object.DestroyImmediate(_locationText.gameObject);
        UnityEngine.Object.DestroyImmediate(_timezoneText.gameObject);
    }

    private void Inject(string fieldName, object value)
    {
        typeof(ClockController)
            .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_controller, value);
    }

    [Test]
    public void IgnoresMissingTimeText()
    {
        Inject("_timeText", null);
        _controller.Invoke("Start", 0);
        _fakeService.Emit(ClockData.CreateSystemTimeData());
        Assert.Pass();
    }

    [Test]
    public void IgnoresMissingLocationText()
    {
        Inject("_locationText", null);
        _controller.Invoke("Start", 0);
        _fakeService.Emit(ClockData.CreateSystemTimeData());
        Assert.Pass();
    }

    [Test]
    public void IgnoresMissingTimezoneText()
    {
        Inject("_timezoneText", null);
        _controller.Invoke("Start", 0);
        _fakeService.Emit(ClockData.CreateSystemTimeData());
        Assert.Pass();
    }
}
public class TestClockService : IClockService
{
    private readonly BehaviorSubject<ClockData> _subject = new BehaviorSubject<ClockData>(ClockData.CreateSystemTimeData());
    public IReadOnlyReactiveProperty<ClockData> CurrentClockData => _subject.ToReadOnlyReactiveProperty();
    public void Emit(ClockData data) => _subject.OnNext(data);
}
