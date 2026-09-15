using System.Linq;
using UnityEngine;

namespace TinyDays
{
    public sealed class VillageResources : MonoBehaviour
    {
        public int foodCapacity=24;
        public int food=12;
        public int harvestFood=3;
        public int TotalHarvested { get; private set; }
        public int TotalConsumed { get; private set; }
        float previousProgress;
        DayNightCycle cycle;
        VillageAutonomySettings settings;
        public bool IsShortage => food<=0;
        public bool IsStorageBlocked => food+harvestFood>foodCapacity&&GetComponentsInChildren<CropPlot>().Any(plot=>plot.HasHarvest);
        public float ProductivityMultiplier => IsShortage?.75f:1f;
        public float RestRecoveryMultiplier => IsShortage?.60f:1f;
        public bool CanStore(int amount) => food+amount<=foodCapacity;
        void Start() { cycle=GetComponent<DayNightCycle>(); settings=GetComponent<VillageAutonomySettings>(); previousProgress=cycle?cycle.progress:0; }
        public void Simulate(float delta)
        {
            foreach(var plot in GetComponentsInChildren<CropPlot>()) plot.Simulate(delta);
            if(!cycle) cycle=GetComponent<DayNightCycle>();
            float current=cycle?cycle.progress:previousProgress;
            if(current<previousProgress-.5f) ConsumeDay();
            previousProgress=current;
        }
        public bool AddHarvest()
        {
            if(!CanStore(harvestFood)) return false;
            food+=harvestFood; TotalHarvested+=harvestFood; return true;
        }
        public void ConsumeDay()
        {
            if(!settings) settings=GetComponent<VillageAutonomySettings>();
            int residents=settings?settings.residentCount:6;
            int consumed=Mathf.Min(food,residents); food-=consumed; TotalConsumed+=consumed;
        }
        public void RestoreState(int savedFood,int savedCapacity,int harvested,int consumed)
        {
            foodCapacity=Mathf.Max(1,savedCapacity); food=Mathf.Clamp(savedFood,0,foodCapacity); TotalHarvested=Mathf.Max(0,harvested); TotalConsumed=Mathf.Max(0,consumed);
        }
        public void SyncDayProgress(float progress) { previousProgress=Mathf.Repeat(progress,1f); }
    }
}
