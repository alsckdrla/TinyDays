using UnityEngine;

namespace TinyDays
{
    [RequireComponent(typeof(LightingPresets))]
    public sealed class DayNightCycle : MonoBehaviour
    {
        public float cycleSeconds = 180f;
        [Range(0f, 1f)] public float progress = .32f;
        LightingPresets lighting;
        void Awake() { lighting = GetComponent<LightingPresets>(); }
        void Update() { SetProgress(progress + Time.deltaTime / cycleSeconds); }
        public void SetProgress(float value)
        {
            progress = Mathf.Repeat(value, 1f);
            lighting.ApplyCycle(progress);
        }
    }
}
