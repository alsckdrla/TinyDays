using UnityEngine;

namespace TinyDays
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AmbientAudioSettings : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0f;
        AudioSource source;
        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true; source.playOnAwake = false; source.volume = volume;
        }
        public void SetVolume(float value) { volume = Mathf.Clamp01(value); if (source) source.volume = volume; }
    }
}
