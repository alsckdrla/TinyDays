using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDays
{
    [RequireComponent(typeof(ResidentWanderer))]
    public sealed class ResidentBrain : MonoBehaviour
    {
        public int residentId;
        public float diligence;
        public float calmness;
        public float pathPreference;
        public ResidentMotor Motor { get; private set; }
        readonly Dictionary<string,float> rejected=new Dictionary<string,float>();
        float elapsed;
        public GameObject carryVisualPrefab;
        public ResidentAction CurrentAction { get; private set; }
        public string DecisionReason { get; private set; } = "처음 행동을 고르는 중";
        public float Fatigue { get; private set; }
        public ActivitySlot CurrentSlot { get; private set; }
        public int CompletedActions { get; private set; }
        public int FallbackCount { get; private set; }
        public bool RestedThenWorked { get; private set; }
        public int WorkCount { get; private set; }
        public int CarryCount { get; private set; }
        public int RestCount { get; private set; }
        public int AppreciateCount { get; private set; }

        VillageAutonomySettings settings;
        ActivityCoordinator coordinator;
        VillageResources resources;
        VillagePriority priority;
        DayNightCycle clock;
        ResidentWanderer walker;
        System.Random random;
        float stateSeconds, travelSeconds, cooldown;
        bool traveling, carrying, restedSinceLastWork;
        GameObject carryVisual;
        bool initialized;

        void Awake() { walker=GetComponent<ResidentWanderer>(); }
        void Start() { Initialize(); }
        public void Initialize()
        {
            if(initialized) return;
            settings=GetComponentInParent<VillageAutonomySettings>(); coordinator=GetComponentInParent<ActivityCoordinator>();
            resources=GetComponentInParent<VillageResources>(); priority=GetComponentInParent<VillagePriority>(); clock=GetComponentInParent<DayNightCycle>();
            random=new System.Random(1103+residentId*97); Fatigue=.14f+residentId*.055f;
            if (!settings || !coordinator) { walker.enabled=true; enabled=false; return; }
            walker.enabled=false; Motor=GetComponent<ResidentMotor>(); Motor.Initialize();
            if (carryVisualPrefab) { carryVisual=Instantiate(carryVisualPrefab,transform); carryVisual.name="Carried crate"; carryVisual.transform.localPosition=new Vector3(.27f,.78f,.18f); carryVisual.transform.localScale=Vector3.one*.30f; carryVisual.SetActive(false); }
            initialized=true;
        }
        void OnDisable()
        {
            if(!initialized||!coordinator) return;
            ReleaseResourceClaim(); coordinator.Release(CurrentSlot,residentId); CurrentSlot=null; CurrentAction=ResidentAction.None; Motor.Stop(); if(carryVisual) carryVisual.SetActive(false);
        }
        // ResidentTraffic advances decisions and motion from one common clock.
        public void Simulate(float delta)
        {
            if(!initialized) Initialize();
            if(delta<=0f||!enabled||!initialized) return;
            elapsed+=delta;
            if(cooldown>0f) { cooldown-=delta; walker.Idle(delta); return; }
            if(CurrentAction==ResidentAction.None) { ChooseAction(); return; }
            if(!coordinator.IsStillReserved(CurrentSlot,residentId)) { Fail("예약한 자리를 사용할 수 없어 다른 행동을 찾는 중"); return; }
            if(traveling)
            {
                travelSeconds+=delta;
                if(travelSeconds>settings.destinationTimeout) { Fail("목적지 접근 시간이 지나 다른 행동을 선택"); return; }
                if(Motor.Failed) { Fail(Motor.Reason); return; }
                if(Motor.Arrived)
                {
                    travelSeconds=0f;
                    if(CurrentAction==ResidentAction.Carry&&!carrying) { carrying=true; if(carryVisual) carryVisual.SetActive(true); BeginTravel(true); DecisionReason="수확 상자를 창고로 운반 중"; }
                    else { traveling=false; DecisionReason=ActionLabel(CurrentAction)+" 중"; }
                }
                Fatigue=Mathf.Clamp01(Fatigue+(carrying?settings.carryFatigue:settings.walkingFatigue)*delta*Mathf.Clamp01(Motor.Velocity.magnitude/Mathf.Max(.1f,walker.walkSpeed*settings.moveSpeedMultiplier)));
                return;
            }
            if(CurrentAction==ResidentAction.Work&&CurrentSlot.cropPlot) CurrentSlot.cropPlot.Tend(delta*(resources?resources.ProductivityMultiplier:1f));
            float efficiency=((CurrentAction==ResidentAction.Work||CurrentAction==ResidentAction.Carry)&&resources)?resources.ProductivityMultiplier:1f;
            stateSeconds-=delta*efficiency;
            walker.Idle(delta);
            if(CurrentAction==ResidentAction.Rest) Fatigue=Mathf.Clamp01(Fatigue-settings.restRecovery*(resources?resources.RestRecoveryMultiplier:1f)*delta);
            else if(CurrentAction==ResidentAction.Work) Fatigue=Mathf.Clamp01(Fatigue+settings.workFatigue*delta);
            FaceLookTarget(delta);
            if(stateSeconds<=0f) Complete();
        }
        void ChooseAction()
        {
            var choices=BuildChoices();
            while(choices.Count>0)
            {
                float total=choices.Sum(c=>Mathf.Max(.01f,c.weight)); float roll=(float)random.NextDouble()*total, accumulated=0f;
                var choice=choices[choices.Count-1];
                foreach(var candidate in choices) { accumulated+=Mathf.Max(.01f,candidate.weight); if(roll<=accumulated) { choice=candidate; break; } }
                choices.Remove(choice);
                if(!coordinator.TryReserve(choice.action,residentId,random,out var slot,s=>SlotAllowed(s)&&(!rejected.ContainsKey(s.slotId)||rejected[s.slotId]<=elapsed))) continue;
                if(choice.action==ResidentAction.Carry&&!slot.cropPlot.TryClaimCarry(residentId)) { coordinator.Release(slot,residentId); continue; }
                CurrentAction=choice.action; CurrentSlot=slot; traveling=true; carrying=false; travelSeconds=0f; BeginTravel(false);
                stateSeconds=Mathf.Lerp(settings.actionSeconds.x,settings.actionSeconds.y,(float)random.NextDouble())*(.85f+calmness*.2f);
                DecisionReason=choice.reason; return;
            }
            DecisionReason="사용 가능한 자리가 없어 잠시 기다리는 중"; cooldown=settings.retryDelay; walker.Idle(0f);
        }
        void BeginTravel(bool toCarryDestination) { Motor.SetDestination(toCarryDestination?CurrentSlot.carryDestination:CurrentSlot.transform.position); }
        List<(ResidentAction action,float weight,string reason)> BuildChoices()
        {
            float t=clock?clock.progress:.2f; bool night=t>=.50f&&t<.78f; bool sunset=t>=.28f&&t<.50f;
            if(Fatigue>=settings.restThreshold-calmness*.12f) return new List<(ResidentAction,float,string)> { (ResidentAction.Rest,10f,"피로가 쌓여 휴식이 필요함"),(ResidentAction.Appreciate,.5f,"자리를 쉬며 풍경을 바라봄") };
            float work=settings.workWeight*(priority?priority.WorkMultiplier:1f), carry=settings.carryWeight*(priority?priority.CarryMultiplier:1f), rest=settings.restWeight*(priority?priority.RestMultiplier:1f), appreciate=settings.appreciateWeight*(priority?priority.AppreciateMultiplier:1f);
            bool urgent=resources&&(resources.IsShortage||coordinator.Slots.Any(s=>s.action==ResidentAction.Carry&&s.cropPlot&&s.cropPlot.HasHarvest));
            if(urgent) { work*=2.2f; carry*=3f; rest*=.55f; appreciate*=.25f; }
            if(night) return new List<(ResidentAction,float,string)> { (ResidentAction.Rest,8f,"밤에는 휴식을 우선함"),(ResidentAction.Appreciate,1f,"밤 풍경을 잠시 감상함") };
            if(sunset) return new List<(ResidentAction,float,string)> { (ResidentAction.Appreciate,appreciate*2.2f,"일몰이라 풍경 감상 비중이 높음"),(ResidentAction.Rest,rest,"해 질 무렵 잠시 쉼"),(ResidentAction.Work,work*.45f,"남은 밭일을 정리함"),(ResidentAction.Carry,carry*.45f,"남은 수확물을 옮김") };
            return new List<(ResidentAction,float,string)> { (ResidentAction.Work,work*(.75f+diligence),urgent?"식량 회복을 위해 밭일을 우선함":"낮에는 밭일을 우선함"),(ResidentAction.Carry,carry*(.75f+diligence*.5f),urgent?"수확 상자를 창고로 운반함":"창고 주변 운반이 필요함"),(ResidentAction.Rest,rest*(.8f+calmness),"짧은 휴식으로 피로를 관리함"),(ResidentAction.Appreciate,appreciate*(.75f+calmness),"마을을 둘러보며 쉼") };
        }
        void Complete()
        {
            if(CurrentAction==ResidentAction.Rest) restedSinceLastWork=true;
            if(CurrentAction==ResidentAction.Work&&restedSinceLastWork) { RestedThenWorked=true; restedSinceLastWork=false; }
            if(CurrentAction==ResidentAction.Work) { WorkCount++; if(CurrentSlot.cropPlot) CurrentSlot.cropPlot.Harvest(); }
            else if(CurrentAction==ResidentAction.Carry) { CarryCount++; if(CurrentSlot.cropPlot&&resources&&resources.AddHarvest()) CurrentSlot.cropPlot.CompleteCarry(residentId); else ReleaseResourceClaim(); }
            else if(CurrentAction==ResidentAction.Rest) RestCount++;
            else if(CurrentAction==ResidentAction.Appreciate) AppreciateCount++;
            CompletedActions++; Motor.Stop(); coordinator.Release(CurrentSlot,residentId); CurrentSlot=null; if(carryVisual) carryVisual.SetActive(false); CurrentAction=ResidentAction.None; cooldown=.25f+residentId*.08f;
        }
        void Fail(string reason)
        {
            if(CurrentSlot) rejected[CurrentSlot.slotId]=elapsed+settings.failedSlotCooldown;
            Motor.Stop(); ReleaseResourceClaim(); coordinator.Release(CurrentSlot,residentId); CurrentSlot=null; if(carryVisual) carryVisual.SetActive(false); CurrentAction=ResidentAction.None; carrying=false; FallbackCount++; cooldown=settings.retryDelay; DecisionReason=reason;
        }
        bool SlotAllowed(ActivitySlot slot)
        {
            if(slot.action==ResidentAction.Work) return !slot.cropPlot||slot.cropPlot.CanWork;
            if(slot.action==ResidentAction.Carry) return slot.cropPlot&&slot.cropPlot.HasHarvest&&slot.cropPlot.CarrierId<0&&resources&&resources.CanStore(resources.harvestFood);
            return true;
        }
        void ReleaseResourceClaim() { if(CurrentAction==ResidentAction.Carry&&CurrentSlot&&CurrentSlot.cropPlot) CurrentSlot.cropPlot.ReleaseCarry(residentId); }
        void FaceLookTarget(float delta)
        {
            if(!CurrentSlot||!CurrentSlot.lookTarget) return;
            var direction=CurrentSlot.lookTarget.position-transform.position; direction.y=0;
            if(direction.sqrMagnitude>.01f) transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),settings.turnDegrees*delta);
        }
        public void ForceCurrentSlotUnavailableForVerification() { if(CurrentSlot) CurrentSlot.blocked=true; }
        static string ActionLabel(ResidentAction action) => action==ResidentAction.Work?"밭일":action==ResidentAction.Carry?"운반":action==ResidentAction.Rest?"휴식":"감상";
    }
}
