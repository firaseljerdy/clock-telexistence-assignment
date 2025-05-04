using NUnit.Framework;
using ClockApp.Services;
using System;
using System.Threading;
using UniRx;
using Zenject;

[TestFixture]
public class TimerServiceTests : ZenjectUnitTestFixture
{
    private TimerService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new TimerService();
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public void TimerStartsCorrectly()
    {
        var duration = TimeSpan.FromSeconds(10);
        _service.StartTimer(duration);
        Assert.IsTrue(_service.IsRunning.Value);
        Assert.AreEqual(duration, _service.RemainingTime.Value);
    }

    [Test]
    public void TimerPausesCorrectly()
    {
        var duration = TimeSpan.FromSeconds(10);
        _service.StartTimer(duration);
        Thread.Sleep(500);
        _service.PauseTimer();
        
        var timeAtPause = _service.RemainingTime.Value;
        Assert.IsFalse(_service.IsRunning.Value);
        
        Thread.Sleep(500);
        Assert.AreEqual(timeAtPause, _service.RemainingTime.Value, "Time should not decrement after pausing");
    }

    [Test]
    public void TimerResetsCorrectly()
    {
        _service.StartTimer(TimeSpan.FromSeconds(10));
        Thread.Sleep(500);
        _service.ResetTimer();

        Assert.AreEqual(TimeSpan.Zero, _service.RemainingTime.Value);
        Assert.IsFalse(_service.IsRunning.Value);
    }

    [Test]
    public void TimerCanBeResumedAfterPause()
    {
        _service.StartTimer(TimeSpan.FromSeconds(5));
        Thread.Sleep(200);
        _service.PauseTimer();
        
        var pausedValue = _service.RemainingTime.Value;
        Assert.IsFalse(_service.IsRunning.Value);
        
        _service.StartTimer(TimeSpan.Zero);
        Assert.IsTrue(_service.IsRunning.Value);
        Assert.AreEqual(pausedValue, _service.RemainingTime.Value, "Timer should resume with previous value");
    }
} 