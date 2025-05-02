using NUnit.Framework;
using ClockApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

[TestFixture]
public class StopwatchServiceTests
{
    private StopwatchService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new StopwatchService();
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public void StopwatchStopsCorrectly()
    {
        _service.StartStopwatch();
        Thread.Sleep(500);
        _service.StopStopwatch();
        var timeAtStop = _service.ElapsedTime.Value;

        Thread.Sleep(500);
        Assert.AreEqual(timeAtStop, _service.ElapsedTime.Value, "Time should not increment after stopping.");
    }

    [Test]
    public void StopwatchResetsCorrectly()
    {
        _service.StartStopwatch();
        Thread.Sleep(500);
        _service.ResetStopwatch();

        Assert.AreEqual(TimeSpan.Zero, _service.ElapsedTime.Value);
        Assert.IsEmpty(_service.Laps.Value);
        Assert.IsFalse(_service.IsRunning.Value);
    }

    [Test]
    public void RecordsLapCorrectly()
    {
        _service.StartStopwatch();
        Thread.Sleep(600);
        _service.RecordLap();

        Thread.Sleep(400);
        _service.RecordLap();

        var laps = _service.Laps.Value.ToList();
        Assert.AreEqual(2, laps.Count);
    }

    [Test]
    public void PreventsMultipleStartCalls()
    {
        var s1 = new StopwatchService();
        var s2 = new StopwatchService();

        s1.StartStopwatch();
        s2.StartStopwatch();

        Assert.AreEqual(1, StopwatchService.ActiveInstanceCount);
    }

    [Test]
    public void ResetStopsAndClearsLaps()
    {
        _service.StartStopwatch();
        Thread.Sleep(500);
        _service.RecordLap();

        _service.ResetStopwatch();

        var laps = _service.Laps.Value.ToList();

        Assert.AreEqual(0, laps.Count);
    }
} 