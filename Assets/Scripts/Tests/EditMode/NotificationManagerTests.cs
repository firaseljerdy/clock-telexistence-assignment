using NUnit.Framework;
using UnityEngine;
using Zenject;
using ClockApp.Services;
using System;
using UniRx;

[TestFixture]
public class NotificationManagerTests : ZenjectUnitTestFixture
{
    private NotificationManager _manager;
    private GameObject _gameObject;
    private TestTimerService _fakeTimerService;
    private TestAudioService _fakeAudioService;
    private AudioClip _timerCompletionSound;

    [SetUp]
    public void SetUp()
    {
        // Create test dependencies
        _fakeTimerService = new TestTimerService();
        _fakeAudioService = new TestAudioService();
        _timerCompletionSound = AudioClip.Create("TestSound", 44100, 1, 44100, false);

        // Bind
        Container.Bind<ITimerService>().FromInstance(_fakeTimerService);
        Container.Bind<IAudioService>().FromInstance(_fakeAudioService);
        Container.Bind<AudioClip>().WithId("TimerCompletionSound").FromInstance(_timerCompletionSound);

        // Create manager
        _gameObject = new GameObject("NotificationManager");
        _manager = _gameObject.AddComponent<NotificationManager>();
        Container.InjectGameObject(_gameObject);

        // Initialize
        _manager.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up
        UnityEngine.Object.DestroyImmediate(_gameObject);
        UnityEngine.Object.DestroyImmediate(_timerCompletionSound);
    }

    [Test]
    public void PlaysTimerCompletionSoundWhenTimerCompletes()
    {
        _fakeTimerService.EmitCompleted();

        Assert.AreEqual(_timerCompletionSound, _fakeAudioService.LastPlayedClip);
        Assert.AreEqual(1, _fakeAudioService.PlayCount);
    }

    [Test]
    public void DoesNotPlaySoundWhenTimerNotCompleted()
    {
        Assert.AreEqual(0, _fakeAudioService.PlayCount);
        Assert.IsNull(_fakeAudioService.LastPlayedClip);
    }
}

// Test double for TimerService
public class TestTimerService : ITimerService
{
    private readonly BehaviorSubject<TimeSpan> _remainingTime = new BehaviorSubject<TimeSpan>(TimeSpan.Zero);
    private readonly BehaviorSubject<bool> _isRunning = new BehaviorSubject<bool>(false);
    private readonly Subject<Unit> _completed = new Subject<Unit>();

    public IReadOnlyReactiveProperty<TimeSpan> RemainingTime => _remainingTime.ToReadOnlyReactiveProperty();
    public IObservable<Unit> TimerCompleted => _completed;
    public IReadOnlyReactiveProperty<bool> IsRunning => _isRunning.ToReadOnlyReactiveProperty();

    public void EmitRemainingTime(TimeSpan time) => _remainingTime.OnNext(time);
    public void EmitIsRunning(bool isRunning) => _isRunning.OnNext(isRunning);
    public void EmitCompleted() => _completed.OnNext(Unit.Default);

    public void PauseTimer() { }
    public void ResetTimer() { }
    public void StartTimer(TimeSpan duration) { }
}

// Test double for AudioService
public class TestAudioService : IAudioService
{
    public AudioClip LastPlayedClip { get; private set; }
    public int PlayCount { get; private set; }

    public void PlaySound(AudioClip clip)
    {
        LastPlayedClip = clip;
        PlayCount++;
    }
} 