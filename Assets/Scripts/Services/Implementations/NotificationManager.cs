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
            // Subscribe to timer completion events
            _timerService.TimerCompleted
                .Subscribe(_ => PlayTimerCompletionSound())
                .AddTo(_disposables);
                
            DontDestroyOnLoad(this.gameObject);
                
            //Debug.Log("NotificationManager initialized and subscribed to timer events");
        }
        
        private void PlayTimerCompletionSound()
        {
            if (_timerCompletionSound != null)
            {
                _audioService.PlaySound(_timerCompletionSound);
                //Debug.Log("Timer completion sound played by NotificationManager");
            }
            else
            {
                Debug.LogWarning("Timer completion sound not assigned in NotificationManager");
            }
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
} 