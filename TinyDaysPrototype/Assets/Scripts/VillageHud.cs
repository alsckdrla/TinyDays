using System.Linq;
using UnityEngine;

namespace TinyDays
{
    public sealed class VillageHud : MonoBehaviour
    {
        public bool visible=true;
        VillageResources resources;
        VillagePriority priority;
        VillageTimeControls timeControls;
        void Start() { resources=GetComponent<VillageResources>(); priority=GetComponent<VillagePriority>(); timeControls=GetComponent<VillageTimeControls>(); }
        void Update() { if(Input.GetKeyDown(KeyCode.Tab)) visible=!visible; }
        void OnGUI()
        {
            if(!visible||!resources||!priority||!timeControls) return;
            var style=new GUIStyle(GUI.skin.box) { fontSize=15, alignment=TextAnchor.MiddleCenter };
            GUI.Box(new Rect(22,Screen.height-94,760,72),"",style);
            var plots=GetComponentsInChildren<CropPlot>().OrderBy(p=>p.plotId).ToArray();
            string crops=string.Join("  ",plots.Select(p=>p.plotId+": "+(p.HasHarvest?"수확 상자":""+p.Stage)));
            GUI.Label(new Rect(38,Screen.height-84,250,25),"식량  "+resources.food+" / "+resources.foodCapacity+(resources.IsShortage?"  (부족)":""));
            GUI.Label(new Rect(290,Screen.height-84,370,25),crops);
            float x=38;
            foreach(VillagePriorityMode mode in System.Enum.GetValues(typeof(VillagePriorityMode))) { if(GUI.Button(new Rect(x,Screen.height-53,90,26),mode==VillagePriorityMode.Production?"생산":mode==VillagePriorityMode.Balanced?"균형":"여유")) priority.Set(mode); x+=96; }
            if(GUI.Button(new Rect(x,Screen.height-53,74,26),timeControls.paused?"재개":"일시정지")) timeControls.SetPaused(!timeControls.paused); x+=80;
            for(int i=0;i<3;i++) { if(GUI.Button(new Rect(x,Screen.height-53,48,26),(i==0?"1×":i==1?"2×":"4×"))) timeControls.SetSpeed(i); x+=54; }
        }
    }
}
