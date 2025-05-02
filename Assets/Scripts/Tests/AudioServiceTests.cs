using NUnit.Framework;
using UnityEngine;
using Zenject;
using ClockApp.Services;
using System.Reflection;

[TestFixture]
public class AudioServiceTests : ZenjectUnitTestFixture
{
    private AudioService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new AudioService();
        _service.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public void InitializeCreatesAudioSource()
    {
        var audioSourceField = typeof(AudioService).GetField("_audioSource", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var audioSource = audioSourceField.GetValue(_service) as AudioSource;
        
        Assert.IsNotNull(audioSource, "AudioSource should be created during initialization");
    }

    [Test]
    public void PlaySoundHandlesNullClip()
    {
        Assert.DoesNotThrow(() => _service.PlaySound(null));
    }

    [Test]
    public void DisposeDestroysGameObject()
    {
        var audioPlayerField = typeof(AudioService).GetField("_audioPlayerObject", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var audioPlayerBefore = audioPlayerField.GetValue(_service) as GameObject;
        Assert.IsNotNull(audioPlayerBefore, "AudioPlayer GameObject should exist before disposal");
        
        _service.Dispose();
        
        var audioPlayerAfter = audioPlayerField.GetValue(_service);
        Assert.IsNull(audioPlayerAfter, "AudioPlayer GameObject should be null after disposal");
    }
} 