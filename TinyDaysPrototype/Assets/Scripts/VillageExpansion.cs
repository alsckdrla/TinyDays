using UnityEngine;

namespace TinyDays
{
    // One deliberately small, stateful expansion. Persistence is added in stage 6.
    public sealed class VillageExpansion : MonoBehaviour
    {
        public const string ExpansionId = "storage_shed_v1";
        public float shortageSecondsRequired = 30f;
        public float buildSecondsRequired = 60f;
        public int addedCapacity = 12;
        public ActivitySlot buildSlot;
        public GameObject foundationVisual;
        public GameObject frameVisual;
        public GameObject finishedVisual;
        public float shortageSeconds { get; private set; }
        public float buildSeconds { get; private set; }
        public bool Decided { get; private set; }
        public bool IsComplete { get; private set; }
        public bool NeedsBuilders => Decided && !IsComplete;
        public float Progress => Mathf.Clamp01(buildSeconds / Mathf.Max(.01f, buildSecondsRequired));
        VillageResources resources;
        void Awake() { resources=GetComponent<VillageResources>(); RefreshVisuals(); }
        public void Simulate(float delta)
        {
            if(IsComplete||delta<=0f) return;
            if(!resources) resources=GetComponent<VillageResources>();
            shortageSeconds=resources&&resources.IsStorageBlocked?shortageSeconds+delta:0f;
            if(!Decided&&shortageSeconds>=shortageSecondsRequired) { Decided=true; RefreshVisuals(); }
        }
        public void Contribute(float seconds)
        {
            if(!NeedsBuilders||seconds<=0f) return;
            buildSeconds=Mathf.Min(buildSecondsRequired,buildSeconds+seconds);
            if(buildSeconds>=buildSecondsRequired) { IsComplete=true; if(!resources) resources=GetComponent<VillageResources>(); if(resources) resources.foodCapacity+=addedCapacity; }
            RefreshVisuals();
        }
        public void RestoreState(float shortage,float build,bool decided,bool complete)
        {
            shortageSeconds=Mathf.Max(0,shortage); buildSeconds=Mathf.Clamp(build,0,buildSecondsRequired); Decided=decided; IsComplete=complete; RefreshVisuals();
        }
        public string StatusText => IsComplete?"확장 헛간 완공":Decided?"확장 공사 "+Mathf.RoundToInt(Progress*100f)+"%":"저장공간 관찰 "+Mathf.CeilToInt(Mathf.Max(0,shortageSecondsRequired-shortageSeconds))+"초";
        void RefreshVisuals()
        {
            if(buildSlot) buildSlot.gameObject.SetActive(NeedsBuilders);
            // The foundation remains as the navigation footprint beneath the finished shed.
            if(foundationVisual) foundationVisual.SetActive(Decided);
            if(frameVisual) frameVisual.SetActive(Decided&&!IsComplete&&Progress>=.35f);
            if(finishedVisual) finishedVisual.SetActive(IsComplete);
        }
    }
}
