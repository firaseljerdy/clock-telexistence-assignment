using UnityEngine;

namespace ClockApp.Services
{
    /// <summary>
    /// Service responsible for playing audio effects.
    /// </summary>
    public interface IAudioService
    {
        void PlaySound(AudioClip clip);
    }
} 