using NUnit.Framework;
using UniRx;
using Zenject;
using ClockApp.Data;
using ClockApp.Services;

[TestFixture]
public class ClockServiceTests : ZenjectUnitTestFixture
{
    private ClockService _clockService;

    [SetUp]
    public void SetUp()
    {
        _clockService = new ClockService();
        Container.Bind<IClockService>().FromInstance(_clockService);
    }

    [TearDown]
    public void TearDown()
    {
        _clockService.Dispose();
    }

    [Test]
    public void EmitsInitialSystemTimeImmediately()
    {
        ClockData emittedData = default;
        bool hasReceived = false;

        _clockService.CurrentClockData
            .Take(1)
            .Subscribe(data =>
            {
                emittedData = data;
                hasReceived = true;
            });

        // Assert
        Assert.IsTrue(hasReceived, "Expected to receive clock data immediately.");
        Assert.IsFalse(emittedData.Equals(default), "Expected non-default ClockData.");
        Assert.IsFalse(string.IsNullOrEmpty(emittedData.TimeZoneId), "TimeZoneId should not be null or empty.");
    }

    [Test]
    public void EmitsSystemTimeInitiallyWhenApiFailsOrDelays()
    {
        // Arrange
        var data = _clockService.CurrentClockData.Value;

        // Assert
        Assert.IsFalse(data.IsServerTime, "Expected initial time to be system time (IsServerTime == false).");
    }

    [Test]
    public void SupportsMultipleSubscribers()
    {
        ClockData data1 = default;
        ClockData data2 = default;

        bool received1 = false;
        bool received2 = false;

        _clockService.CurrentClockData
            .Take(1)
            .Subscribe(data =>
            {
                data1 = data;
                received1 = true;
            });

        _clockService.CurrentClockData
            .Take(1)
            .Subscribe(data =>
            {
                data2 = data;
                received2 = true;
            });

        Assert.IsTrue(received1 && received2, "Expected both subscribers to receive data.");
        Assert.AreEqual(data1.CurrentTime, data2.CurrentTime, "Subscribers received different time data.");
    }

    [Test]
    public void DoesNotEmitAfterDispose()
    {
        bool received = false;

        var subscription = _clockService.CurrentClockData
            .Skip(1)
            .Subscribe(_ => received = true);

        _clockService.Dispose();

        System.Threading.Thread.Sleep(1500);

        Assert.IsFalse(received, "Expected no emissions after Dispose().");

        subscription.Dispose();
    }

}
