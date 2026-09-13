using UnityEngine;

namespace TinyDays
{
    public sealed class GentleSway : MonoBehaviour
    {
        public float amplitude = 4f, speed = .75f, scaleAmount = .025f, phaseOffset;
        Quaternion restRotation;
        Vector3 restScale;
        void Awake() { restRotation = transform.localRotation; restScale = transform.localScale; }
        void LateUpdate() { Simulate(Time.time); }
        public void Simulate(float time)
        {
            float a = Mathf.Sin(time * speed + phaseOffset);
            transform.localRotation = restRotation * Quaternion.Euler(a * amplitude, 0f, a * amplitude * .55f);
            transform.localScale = restScale * (1f + a * scaleAmount);
        }
    }
}
