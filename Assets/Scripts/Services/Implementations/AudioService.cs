using UnityEngine;
using Zenject;
using System;

namespace ClockApp.Services
{
    /// <summary>
    /// Concrete implementation of IAudioService/>.
    /// </summary>
    public class AudioService : IAudioService, IInitializable, IDisposable
    {
        private AudioSource _audioSource;
        private GameObject _audioPlayerObject;

        // Called after dependencies are injected
        public void Initialize()
        {
            // Create a dedicated GameObject to hold the AudioSource
            _audioPlayerObject = new GameObject("AudioPlayer_Service");
            UnityEngine.Object.DontDestroyOnLoad(_audioPlayerObject);
            _audioSource = _audioPlayerObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
            //Debug.Log("AudioSource created:", _audioPlayerObject);
        }

        public void PlaySound(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogWarning("AudioService: Tried to play a null AudioClip");
                return;
            }

            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
                 //Debug.Log($"AudioService: Playing clip '{clip.name}'", _audioPlayerObject);
            }
            else
            {
                Debug.LogError("AudioService: AudioSource is not initialized");
            }
        }

        public void Dispose()
        {
            if (_audioPlayerObject != null)
            {
                UnityEngine.Object.Destroy(_audioPlayerObject);
                //Debug.Log("AudioService Disposed and AudioPlayer GameObject destroyed.");
            }
            _audioSource = null;
        }
    }
} 