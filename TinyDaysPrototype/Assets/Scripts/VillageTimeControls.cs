using UnityEngine;

namespace TinyDays
{
    public sealed class VillageTimeControls : MonoBehaviour
    {
        static readonly float[] Speeds={1f,2f,4f,8f,16f};
        public bool paused;
        public int speedIndex;
        float defaultMaximumDelta;
        public float Speed => paused?0f:Speeds[speedIndex];
        void Awake() { defaultMaximumDelta=Time.maximumDeltaTime; ApplyTime(); }
        void Update()
        {
            if(GetComponent<VillageSaveSystem>()?.MenuOpen==true) { SetPaused(true); return; }
            if(Input.GetKeyDown(KeyCode.Space)) paused=!paused;
            if(Input.GetKeyDown(KeyCode.LeftBracket)) DecreaseSpeed();
            if(Input.GetKeyDown(KeyCode.RightBracket)) IncreaseSpeed();
            ApplyTime();
        }
        public void SetPaused(bool value) { paused=value; ApplyTime(); }
        public void SetSpeed(int index) { speedIndex=Mathf.Clamp(index,0,Speeds.Length-1); paused=false; ApplyTime(); }
        public void IncreaseSpeed() { SetSpeed(speedIndex+1); }
        public void DecreaseSpeed() { SetSpeed(speedIndex-1); }
        public string SpeedText => Speeds[speedIndex].ToString("0")+"×";
        void ApplyTime()
        {
            Time.timeScale=Speed;
            Time.maximumDeltaTime=paused?defaultMaximumDelta:Mathf.Max(defaultMaximumDelta,Speed/45f);
        }
        void OnDisable() { Time.timeScale=1f; Time.maximumDeltaTime=defaultMaximumDelta; }
    }
}
