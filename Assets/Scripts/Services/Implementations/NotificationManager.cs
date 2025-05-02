using UnityEngine;
using Zenject;
using UniRx;
using System;

namespace ClockApp.Services
{
    public class NotificationManager : MonoBehaviour, IInitializable, IDisposable
    {
        [Inject] private ITimerService _timerService;
        [Inject] private IAudioService _audioService;
        private AudioClip _timerCompletionSound;
        private CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct([Inject(Id = "TimerCompletionSound")] AudioClip timerCompletionSound)
        {
            _timerCompletionSound = timerCompletionSound;
        }

        public void Initialize()
        {
            _timerService.TimerCompleted
                .Subscribe(_ => PlayTimerCompletionSound())
                .AddTo(_disposables);

            // Only call DontDestroyOnLoad at runtime (play mode)
            if (Application.isPlaying)
                DontDestroyOnLoad(this.gameObject);
        }

        private void PlayTimerCompletionSound()
        {
            if (_timerCompletionSound != null)
                _audioService.PlaySound(_timerCompletionSound);
            else
                Debug.LogWarning("Timer completion sound not assigned in NotificationManager");
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
