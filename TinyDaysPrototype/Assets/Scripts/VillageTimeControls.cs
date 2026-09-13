using UnityEngine;

namespace TinyDays
{
    public sealed class VillageTimeControls : MonoBehaviour
    {
        static readonly float[] Speeds={1f,2f,4f};
        public bool paused;
        public int speedIndex;
        public float Speed => paused?0f:Speeds[speedIndex];
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space)) paused=!paused;
            if(Input.GetKeyDown(KeyCode.LeftBracket)) speedIndex=(speedIndex+Speeds.Length-1)%Speeds.Length;
            if(Input.GetKeyDown(KeyCode.RightBracket)) speedIndex=(speedIndex+1)%Speeds.Length;
            Time.timeScale=Speed;
        }
        public void SetPaused(bool value) { paused=value; Time.timeScale=Speed; }
        public void SetSpeed(int index) { speedIndex=Mathf.Clamp(index,0,Speeds.Length-1); paused=false; Time.timeScale=Speed; }
        void OnDisable() { Time.timeScale=1f; }
    }
}
