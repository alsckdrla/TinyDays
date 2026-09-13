using UnityEngine;
using UnityEngine.Rendering;

namespace TinyDays
{
    public enum TimeLook { Day, Sunset, Night }
    [System.Serializable]
    public class LightingLook
    {
        public Color sunlight, ambient, background;
        public float intensity;
        public Vector3 sunRotation;
    }
    public sealed class LightingPresets : MonoBehaviour
    {
        public Light sun;
        public Camera view;
        public Light[] windowLights;
        public LightingLook[] looks;
        public TimeLook current = TimeLook.Sunset;
        DayNightCycle cycle;
        void Awake() { cycle = GetComponent<DayNightCycle>(); }
        void Start() { Apply(current); }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetTime(.08f);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetTime(.34f);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetTime(.60f);
        }
        public void Apply(TimeLook look)
        {
            current = look;
            var p = looks[(int)look];
            sun.color = p.sunlight; sun.intensity = p.intensity;
            sun.transform.rotation = Quaternion.Euler(p.sunRotation);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = p.ambient;
            RenderSettings.fog = false;
            view.backgroundColor = p.background;
            foreach (var light in windowLights) light.enabled = look == TimeLook.Night;
        }
        public void ApplyCycle(float progress)
        {
            // Key moments: early day, sunset, deep night, then a gentle dawn return.
            LightingLook a, b; float t;
            if (progress < .34f) { a = looks[(int)TimeLook.Day]; b = looks[(int)TimeLook.Sunset]; t = progress / .34f; }
            else if (progress < .60f) { a = looks[(int)TimeLook.Sunset]; b = looks[(int)TimeLook.Night]; t = (progress-.34f)/.26f; }
            else if (progress < .82f) { a = looks[(int)TimeLook.Night]; b = looks[(int)TimeLook.Day]; t = (progress-.60f)/.22f; }
            else { a = looks[(int)TimeLook.Day]; b = looks[(int)TimeLook.Day]; t = 0f; }
            t = t * t * (3f - 2f * t);
            sun.color = Color.Lerp(a.sunlight,b.sunlight,t); sun.intensity = Mathf.Lerp(a.intensity,b.intensity,t);
            sun.transform.rotation = Quaternion.Slerp(Quaternion.Euler(a.sunRotation),Quaternion.Euler(b.sunRotation),t);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.Lerp(a.ambient,b.ambient,t);
            view.backgroundColor = Color.Lerp(a.background,b.background,t);
            float night = Mathf.Clamp01((progress-.39f)/.16f) * (1f-Mathf.Clamp01((progress-.60f)/.19f));
            foreach (var light in windowLights) { light.enabled = night > .01f; light.intensity = 2.6f * night; }
            current = progress < .20f || progress >= .82f ? TimeLook.Day : progress < .47f ? TimeLook.Sunset : TimeLook.Night;
        }
        void SetTime(float progress)
        {
            if (cycle) cycle.SetProgress(progress); else ApplyCycle(progress);
        }
    }
}
