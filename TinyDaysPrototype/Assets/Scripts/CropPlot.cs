using UnityEngine;

namespace TinyDays
{
    public enum CropStage { Planted, Growing, Ready }

    public sealed class CropPlot : MonoBehaviour
    {
        public string plotId;
        public float growth;
        public float growthSeconds=70f;
        public Transform[] cropVisuals;
        public GameObject harvestCrate;
        public CropStage Stage => growth>=1f ? CropStage.Ready : growth<.28f ? CropStage.Planted : CropStage.Growing;
        public bool HasHarvest => harvestCrate&&harvestCrate.activeSelf;
        public int CarrierId { get; private set; }=-1;
        public bool CanWork => !HasHarvest;
        public void Simulate(float delta) { if(!HasHarvest) growth=Mathf.Clamp01(growth+delta/Mathf.Max(1f,growthSeconds)); RefreshVisuals(); }
        public void Tend(float delta) { if(!HasHarvest) growth=Mathf.Clamp01(growth+delta/Mathf.Max(1f,growthSeconds)*.55f); }
        public bool Harvest()
        {
            if(Stage!=CropStage.Ready||HasHarvest) return false;
            growth=0; CarrierId=-1; if(harvestCrate) harvestCrate.SetActive(true); RefreshVisuals(); return true;
        }
        public bool TryClaimCarry(int residentId)
        {
            if(!HasHarvest||(CarrierId>=0&&CarrierId!=residentId)) return false;
            CarrierId=residentId; return true;
        }
        public void ReleaseCarry(int residentId) { if(CarrierId==residentId) CarrierId=-1; }
        public bool CompleteCarry(int residentId)
        {
            if(!HasHarvest||CarrierId!=residentId) return false;
            harvestCrate.SetActive(false); CarrierId=-1; return true;
        }
        void Start() { RefreshVisuals(); }
        void RefreshVisuals()
        {
            if(cropVisuals==null) return;
            float scale=Stage==CropStage.Planted?.28f:Stage==CropStage.Growing?.66f:1f;
            foreach(var visual in cropVisuals) if(visual) visual.gameObject.SetActive(!HasHarvest&&scale>.3f);
        }
    }
}
