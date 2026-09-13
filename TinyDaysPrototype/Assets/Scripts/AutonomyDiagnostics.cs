using UnityEngine;

namespace TinyDays
{
    public sealed class AutonomyDiagnostics : MonoBehaviour
    {
        public bool visible;
        ResidentBrain[] residents;
        void Start() { residents=GetComponentsInChildren<ResidentBrain>(); }
        void Update() { if(Input.GetKeyDown(KeyCode.F)) visible=!visible; }
        public void SetVisible(bool value) { visible=value; }
        void OnGUI()
        {
            if(!visible||residents==null) return;
            GUILayout.BeginArea(new Rect(14,14,650,40+residents.Length*80),GUI.skin.box);
            GUILayout.Label("Tiny Days · 주민 판단 진단 (F 키로 숨김)");
            foreach(var resident in residents) GUILayout.Label(resident.name+" | "+resident.CurrentAction+" | 피로 "+resident.Fatigue.ToString("0.00")+" | "+(resident.CurrentSlot?resident.CurrentSlot.slotId:"예약 없음")+"\n"+resident.DecisionReason+"\n"+"부지런 "+resident.diligence.ToString("0.00")+" / 느긋 "+resident.calmness.ToString("0.00")+" / 길 선호 "+resident.pathPreference.ToString("0.00")+" | "+(resident.Motor?resident.Motor.Reason+" / 재탐색 "+resident.Motor.Replans:"초기화"));
            GUILayout.EndArea();
        }
    }
}
