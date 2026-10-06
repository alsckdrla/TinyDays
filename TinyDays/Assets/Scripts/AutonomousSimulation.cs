using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDays.Life
{
    [Serializable] public sealed class LifeTuning
    {
        public int initialFood=9,capacity=36,targetFood=18,harvestYield=3,berryYield=2;
        public float cropSeconds=80,berrySeconds=100,workSeconds=5,mealSeconds=4,restSeconds=12,leisureSeconds=10;
        public float walkSpeed=.9f,hungerPerSecond=.0018f,workFatigue=.0025f,walkFatigue=.001f;
        public float hungerThreshold=.6f,fatigueThreshold=.7f,mealRelief=.62f,restRecovery=.025f;
        public bool temperamentsEnabled=true;
        public float relaxedRestOffset=.1f,relaxedLeisureMultiplier=1.5f;
    }
    public enum LifeAction { None, Plant, Harvest, Gather, Deliver, Eat, Rest, Leisure, Home, Collect, Wait, Shelter, FeedCollect, FeedDeliver, EggCollect, WoodGather, WoodDeliver, WoodRecover, Build, ClearField, Garden }
    public enum SiteKind { Crop, Berry, Store, Rest, Home, Collect, Leisure, Wait, Shelter, Feeder, Nest, WoodSource, WoodStore, Build, ClearField, Garden }
    public enum LifeTemperament { Balanced, Farmer, Forager, Relaxed }
    [Serializable] public sealed class LifeSite
    {
        public string id;public SiteKind kind;public Vector3 position;public int homeResident=-1;
        [NonSerialized] public int owner=-1;
        [NonSerialized] public float growth;
        [NonSerialized] public bool planted;
    }
    [Serializable] public sealed class LifeResident
    {
        [NonSerialized] public FoodBundle bundle;
        public int id,site=-1,cargo,waypoint;public Vector3 position,forward=Vector3.forward;
        public LifeAction action;public List<Vector3> path;public string reason="주변을 살펴보는 중";
        public float hunger,fatigue,work,wait,blockedSeconds,walkDistance;public bool sleeping;
        public LifeTemperament temperament;
        public int productionReservation,mealReservation,collectionReservation,completed,lastLeisureSite=-1;
        public int woodCargo,woodReservation,woodDrop;
        public int feedReservation,eggReservation;public bool carryingFeed;
        public int socialPartner; // Resident ID + 1; zero also migrates old saves safely.
        public float socialTime,socialReadyAt;
        public float waitingSince,nextRepath=.6f;
        public bool Moving=>path!=null&&waypoint<path.Count;
        public readonly Dictionary<int,float> rejected=new Dictionary<int,float>();
    }
    [Serializable] public sealed class FoodBundle { public int id;public Vector3 position;public int amount,owner=-1;public float retryAfter; }
    public sealed partial class AutonomousSimulation
    {
        public readonly LifeTuning tuning;public readonly LifeSite[] sites;public readonly LifeResident[] residents;
        public readonly List<FoodBundle> bundles=new List<FoodBundle>();
        public readonly AutonomousNavigation navigation;
        public float elapsed;public int food,produced,consumed,planted,harvested,gathered,meals,rests,homeArrivals,recovered;
        int nextBundleId;
        public float Hour {get;private set;}=12;
        public bool Raining {get;set;}
        public bool Winter {get;set;}
        public AutonomousSimulation(LifeTuning tuning,LifeSite[] definitions,Vector3[] starts,AutonomousNavigation navigation)
        {
            this.tuning=tuning;this.navigation=navigation;
            sites=definitions.Select(s=>new LifeSite{id=s.id,kind=s.kind,position=s.position,homeResident=s.homeResident,growth=s.kind==SiteKind.Berry?1:0}).ToArray();
            Poultry=sites.Any(s=>s.kind==SiteKind.Feeder)?PoultryState.Create():null;
            food=tuning.initialFood;residents=starts.Select((p,i)=>new LifeResident{id=i,position=p,hunger=.18f+(i%3)*.08f,fatigue=.12f+(i%3)*.09f,wait=i*.7f,temperament=tuning.temperamentsEnabled?(LifeTemperament)(1+i%3):LifeTemperament.Balanced}).ToArray();
        }
        public int Committed=>residents.Sum(r=>r.cargo);
        public int ReservedProduction=>residents.Sum(r=>r.productionReservation);
        public int ReservedCollections=>residents.Sum(r=>r.collectionReservation);
        public int AvailableFood=>food-residents.Sum(r=>r.mealReservation+r.feedReservation);
        public int PlannedStock=>food+Committed+ReservedProduction+ReservedCollections;
        public float RestThreshold(LifeResident r)=>tuning.fatigueThreshold-(r.temperament==LifeTemperament.Relaxed?tuning.relaxedRestOffset:0)-(Winter?.1f:0);
        public float LeisureDuration(LifeResident r)=>tuning.leisureSeconds*(r.temperament==LifeTemperament.Relaxed?tuning.relaxedLeisureMultiplier:1)*(Winter?1.5f:1);
        public bool Night=>Hour>=20||Hour<6;
        public int TotalFood=>food+Committed+bundles.Sum(b=>b.amount);
        public void Tick(float dt,float hour)
        {
            if(dt<=0)return;elapsed+=dt;Hour=hour;
            foreach(var s in sites)if(s.kind==SiteKind.Berry||s.kind==SiteKind.Crop&&s.planted)s.growth=Mathf.Min(1,s.growth+dt*(Winter?.5f:1)/(s.kind==SiteKind.Crop?tuning.cropSeconds:tuning.berrySeconds));
            AdvanceGrowth(dt);AdvanceField(dt);AdvanceGarden(dt);
            AdvancePoultry(dt);
            AdvanceSocial(dt);
            // Equal needs are admitted oldest-first, independently of resident number/frame rate.
            foreach(var r in residents.OrderBy(NeedPriority).ThenBy(r=>r.waitingSince).ThenBy(r=>r.id).ToArray())Advance(r,dt);
            FindSocialPairs();
        }
        int NeedPriority(LifeResident r)=>(r.cargo>0||r.woodCargo>0)?0:r.hunger>=tuning.hungerThreshold?1:Night?2:r.fatigue>=RestThreshold(r)?3:4;
        void Advance(LifeResident r,float dt)
        {
            r.hunger=Mathf.Min(1,r.hunger+dt*tuning.hungerPerSecond*(r.sleeping?.35f:1));
            if(r.sleeping){r.fatigue=Mathf.Max(0,r.fatigue-dt*tuning.restRecovery);if(Night)return;r.sleeping=false;Release(r);r.wait=r.id*.6f;}
            if(r.socialPartner>0){r.fatigue=Mathf.Max(0,r.fatigue-dt*tuning.restRecovery);return;}
            InterruptPoultryCare(r);InterruptGrowth(r);InterruptField(r);InterruptGarden(r);
            if(r.wait>0){r.wait-=dt;return;}
            if(r.action==LifeAction.Shelter&&(!Raining||Night||r.hunger>=tuning.hungerThreshold&&AvailableFood>0||AvailableFood<4&&r.fatigue<RestThreshold(r)))Release(r);
            if(r.action==LifeAction.None){Choose(r);return;}
            if(r.Moving)
            {
                Vector3 goal=r.path[r.waypoint],next=Vector3.MoveTowards(r.position,goal,tuning.walkSpeed*dt);
                bool traffic=residents.Any(o=>o!=r&&!o.sleeping&&!TrafficClear(r.position,next,o.position));
                if(!navigation.Segment(r.position,next)||traffic)
                {
                    r.blockedSeconds+=dt;
                    if(traffic&&r.blockedSeconds>.2f)Yield(r,dt);
                    if(r.blockedSeconds>=r.nextRepath){r.nextRepath+=.9f;var route=Route(r,r.path[r.path.Count-1]);if(route!=null){r.path=route;r.waypoint=0;}}
                    if(r.blockedSeconds>3.5f){Fail(r);recovered++;}return;
                }
                r.blockedSeconds=0;r.nextRepath=.6f;Vector3 delta=next-r.position;if(delta.sqrMagnitude>1e-8f)r.forward=delta.normalized;
                r.walkDistance+=delta.magnitude;r.position=next;r.fatigue=Mathf.Min(1,r.fatigue+dt*tuning.walkFatigue);
                if((r.position-goal).sqrMagnitude<.00001f)r.waypoint++;
                return;
            }
            if(r.action==LifeAction.Collect){if(r.bundle!=null&&r.bundle.owner==r.id){int amount=r.collectionReservation;r.cargo+=amount;r.bundle.amount-=amount;if(r.bundle.amount==0)bundles.Remove(r.bundle);}Release(r);return;}
            if(r.action==LifeAction.WoodRecover){CompleteGrowth(r,dt);return;}
            if(r.site<0||sites[r.site].owner!=r.id){Fail(r);return;}
            var site=sites[r.site];r.work+=dt;
            if(CompletePoultryCare(r)||CompleteGrowth(r,dt)||CompleteField(r,dt)||CompleteGarden(r,dt))return;
            if(r.action==LifeAction.Shelter){r.fatigue=Mathf.Max(0,r.fatigue-dt*tuning.restRecovery);r.reason=Winter?"처마 아래에서 눈을 피하며 휴식":"처마 아래에서 비를 피하며 휴식";return;}
            if(r.action==LifeAction.Wait){if(r.work>=1)Release(r,false);return;}
            if(r.action==LifeAction.Home){r.sleeping=Night;homeArrivals++;if(!Night)Release(r);return;}
            if(r.action==LifeAction.Rest){r.fatigue=Mathf.Max(0,r.fatigue-dt*tuning.restRecovery);if(r.work>=tuning.restSeconds){rests++;Release(r);}return;}
            if(r.action==LifeAction.Leisure){r.fatigue=Mathf.Max(0,r.fatigue-dt*tuning.restRecovery*.4f);if(Night||r.hunger>=tuning.hungerThreshold||r.work>=LeisureDuration(r))Release(r);return;}
            if(r.action==LifeAction.Eat)
            {
                if(r.work< tuning.mealSeconds)return;
                if(r.mealReservation==1){food--;consumed++;meals++;r.hunger=Mathf.Max(0,r.hunger-tuning.mealRelief);}Release(r);return;
            }
            if(r.action==LifeAction.Deliver)
            {
                int count=Mathf.Min(r.cargo,Capacity-food);food+=count;r.cargo-=count;
                if(r.cargo>0)Drop(r);Release(r);return;
            }
            r.fatigue=Mathf.Min(1,r.fatigue+dt*tuning.workFatigue);
            if(r.work<tuning.workSeconds)return;
            if(r.action==LifeAction.Plant){site.planted=true;site.growth=0;planted++;}
            else if(r.action==LifeAction.Harvest&&site.planted&&site.growth>=1){r.cargo=tuning.harvestYield;produced+=r.cargo;site.planted=false;site.growth=0;harvested++;}
            else if(r.action==LifeAction.Gather&&site.growth>=1){r.cargo=tuning.berryYield;produced+=r.cargo;site.growth=0;gathered++;}
            Release(r);
        }
        void Yield(LifeResident r,float dt)
        {
            var other=residents.Where(o=>o!=r&&!o.sleeping).OrderBy(o=>(o.position-r.position).sqrMagnitude).FirstOrDefault();
            if(other==null)return;
            // Carrying residents have right of way; equal loads use oldest waiting age.
            if(other.Moving&&CompareTraffic(r,other)<0)return;
            Vector3 away=(r.position-other.position).normalized;
            for(int i=0;i<8;i++){
                float angle=(i%2==0?1:-1)*(i/2)*30;
                Vector3 direction=Quaternion.Euler(0,angle,0)*away;
                Vector3 next=r.position+direction*tuning.walkSpeed*dt;
                if(!navigation.Segment(r.position,next)||residents.Any(o=>o!=r&&!o.sleeping&&!TrafficClear(r.position,next,o.position)))continue;
                r.walkDistance+=Vector3.Distance(r.position,next);r.position=next;r.forward=direction;return;
            }
        }
        static int CompareTraffic(LifeResident a,LifeResident b)
        {int load=(b.cargo>0||b.woodCargo>0?1:0).CompareTo(a.cargo>0||a.woodCargo>0?1:0);if(load!=0)return load;int age=a.waitingSince.CompareTo(b.waitingSince);return age!=0?age:a.id.CompareTo(b.id);}
        static bool TrafficClear(Vector3 from,Vector3 to,Vector3 other)
        {
            Vector3 delta=to-from;float t=delta.sqrMagnitude<1e-9f?0:Mathf.Clamp01(Vector3.Dot(other-from,delta)/delta.sqrMagnitude);
            float before=(from-other).sqrMagnitude,after=(to-other).sqrMagnitude;
            return (from+delta*t-other).sqrMagnitude>=.599f*.599f||(before<.36f&&after>=before);
        }
        void Release(LifeResident r,bool completed=true)
        {
            EndSocial(r);ReleaseGrowth(r);
            if(completed){r.completed++;r.waitingSince=elapsed;}
            if(r.action==LifeAction.Leisure)r.lastLeisureSite=r.site;
            if(r.bundle!=null){r.bundle.owner=-1;r.bundle=null;}
            if(r.site>=0&&sites[r.site].owner==r.id)sites[r.site].owner=-1;
            r.productionReservation=0;r.mealReservation=0;r.collectionReservation=0;r.feedReservation=0;r.eggReservation=0;
            r.site=-1;r.action=LifeAction.None;r.path=null;r.waypoint=0;r.work=0;r.blockedSeconds=0;r.nextRepath=.6f;
        }
        void Drop(LifeResident r)
        {
            DropWood(r);
            if(r.cargo<=0)return;
            // Current resident position was reached by a valid route; do not teleport food through obstacles.
            bundles.Add(new FoodBundle{id=nextBundleId++,position=r.position,amount=r.cargo,retryAfter=elapsed+8});r.cargo=0;r.carryingFeed=false;
        }
        public void CancelWork(int residentId)
        {Fail(residents[residentId]);}
        void Fail(LifeResident r)
        {
            if(r.site>=0)r.rejected[r.site]=elapsed+8;
            Release(r,false);r.reason="길이 막혀 다른 일을 찾는 중";r.wait=.6f+r.id*.3f;Drop(r);
        }
        List<Vector3> Route(LifeResident r,Vector3 end)
        {
            // A resident occupying the destination may leave. Plan the approach, then let
            // per-step traffic handling yield/wait; never treat that resident as a static wall.
            navigation.Avoidance=residents.Where(o=>o!=r&&!o.sleeping&&(o.position-end).sqrMagnitude>=.36f).Select(o=>o.position).ToArray();
            try{return navigation.Find(r.position,end);}finally{navigation.Avoidance=Array.Empty<Vector3>();}
        }
        bool Assign(LifeResident r,SiteKind kind,LifeAction action,string reason,Func<LifeSite,bool> allowed=null)
        {
            int yield=action==LifeAction.Harvest?tuning.harvestYield:action==LifeAction.Gather?tuning.berryYield:0;
            if(yield>0&&PlannedStock+yield>Capacity||action==LifeAction.Eat&&AvailableFood<=0)return false;
            var candidates=Enumerable.Range(0,sites.Length).Where(i=>sites[i].kind==kind&&FieldSiteEnabled(sites[i])&&GardenSiteEnabled(sites[i])&&sites[i].owner<0&&(allowed==null||allowed(sites[i]))&&(!r.rejected.TryGetValue(i,out float until)||elapsed>=until))
                .OrderBy(i=>action==LifeAction.Leisure&&i==r.lastLeisureSite?1:0).ThenBy(i=>(sites[i].position-r.position).sqrMagnitude);
            foreach(int i in candidates)
            {
                var path=Route(r,sites[i].position);
                if(path==null){r.rejected[i]=elapsed+8;continue;}
                sites[i].owner=r.id;r.site=i;r.action=action;r.reason=reason;r.path=path;r.waypoint=0;r.work=0;
                r.productionReservation=yield;r.mealReservation=action==LifeAction.Eat?1:0;return true;
            }
            return false;
        }
        void Choose(LifeResident r)
        {
            if(ChooseWoodCargo(r))return;
            if(r.cargo>0)
            {
                if(r.carryingFeed&&!Night&&!Raining&&r.hunger<tuning.hungerThreshold&&r.fatigue<RestThreshold(r)&&AvailableFood>=4&&Assign(r,SiteKind.Feeder,LifeAction.FeedDeliver,"닭에게 먹이를 가져가는 중"))return;
                r.carryingFeed=false;
                if(Assign(r,SiteKind.Store,LifeAction.Deliver,"수확한 먹거리를 창고로 운반"))return;
                Drop(r);
            }
            if(r.hunger>=tuning.hungerThreshold&&Assign(r,SiteKind.Store,LifeAction.Eat,"배가 고파 공동 식량으로 식사"))return;
            if(Night)
            {
                if(Assign(r,SiteKind.Home,LifeAction.Home,"밤이 되어 집으로 돌아가 휴식",s=>s.homeResident==r.id))return;
                r.fatigue=Mathf.Max(0,r.fatigue-.03f);r.reason="귀갓길이 막혀 안전한 곳에서 쉬며 재시도";r.wait=2;return;
            }
            if(r.hunger>=tuning.hungerThreshold&&food>0){WaitAside(r);return;}
            if(r.fatigue>=RestThreshold(r)){if(Raining){SeekShelter(r);return;}if(!Assign(r,SiteKind.Rest,LifeAction.Rest,Winter?"겨울이라 조금 일찍 쉬기":"피로가 쌓여 잠시 휴식"))WaitAside(r);return;}
            if(PlannedStock<Capacity)
            {
                foreach(var bundle in bundles.Where(b=>b.owner<0&&elapsed>=b.retryAfter).OrderBy(b=>(b.position-r.position).sqrMagnitude))
                {
                    var path=Route(r,bundle.position);if(path==null)continue;
                    bundle.owner=r.id;r.bundle=bundle;r.collectionReservation=Mathf.Min(bundle.amount,Capacity-PlannedStock);r.action=LifeAction.Collect;r.reason="내려둔 먹거리를 다시 운반";r.path=path;r.waypoint=0;return;
                }
            }
            if(Raining&&AvailableFood>=4){SeekShelter(r);return;}
            if(ChoosePoultryCare(r))return;
            if(ChooseField(r)||ChooseGrowth(r)||ChooseGarden(r))return;
            bool need=PlannedStock<TargetFood;
            if(need)
            {
                bool urgent=AvailableFood<4;
                if(urgent){if(Harvest(r,"식량이 부족해 다 자란 당근 확보")||Gather(r,"식량이 부족해 바로 먹을 열매 확보"))return;}
                else if(r.temperament==LifeTemperament.Forager&&Gather(r,"채집을 좋아해 열매를 보충"))return;
                if(Harvest(r,"다 자란 당근을 수확"))return;
                if(PlannedStock<Capacity&&Assign(r,SiteKind.Crop,LifeAction.Plant,r.temperament==LifeTemperament.Farmer?"농사를 좋아해 당근 심기":"다음 먹거리를 위해 당근 심기",s=>!s.planted))return;
                if(Gather(r,"공동 식량을 보충할 열매 채집"))return;
            }
            if(r.fatigue>.25f&&Assign(r,SiteKind.Rest,LifeAction.Rest,"급한 일을 마치고 쉬기"))return;
            string leisure=Winter&&!need?"겨울에는 여가를 더 길게 즐기기":need?"작물이 자라는 동안 풍경 감상":"식량이 넉넉해 여유롭게 보내기";
            if(r.hunger>=tuning.hungerThreshold||r.fatigue>=RestThreshold(r)){WaitAside(r);return;}
            if(JoinRest(r))return;
            if(Assign(r,SiteKind.Leisure,LifeAction.Leisure,leisure)||Assign(r,SiteKind.Rest,LifeAction.Leisure,leisure))return;
            WaitAside(r);
        }
        bool Harvest(LifeResident r,string reason)=>Assign(r,SiteKind.Crop,LifeAction.Harvest,reason,s=>s.planted&&s.growth>=1);
        bool Gather(LifeResident r,string reason)=>Assign(r,SiteKind.Berry,LifeAction.Gather,reason,s=>s.growth>=1);
        void SeekShelter(LifeResident r)
        {
            if(Assign(r,SiteKind.Shelter,LifeAction.Shelter,Winter?"일을 정리하고 눈을 피해 처마로 이동":"일을 정리하고 처마 아래로 이동"))return;
            WaitAside(r);r.reason=Winter?"눈 피할 자리를 기다리며 안전한 경로를 다시 찾는 중":"비 피할 자리를 기다리며 안전한 경로를 다시 찾는 중";
            // Rest while waiting if all sheltered routes are blocked, without moving through obstacles.
            r.fatigue=Mathf.Max(0,r.fatigue-.025f);
        }
        void WaitAside(LifeResident r)
        {r.reason="다른 주민이 자리를 쓰는 동안 안전한 곳에서 기다림";if(!Assign(r,SiteKind.Wait,LifeAction.Wait,r.reason))r.wait=1;}
        public void Validate()
        {
            ValidateSocial();ValidatePoultry();ValidateGrowth();ValidateField();ValidateGarden();
            if(food<0||food>Capacity||TotalFood+consumed+(Poultry?.feedSpent??0)!=tuning.initialFood+produced)throw new InvalidOperationException("Food conservation failure");
            if(AvailableFood<0||PlannedStock>Capacity)throw new InvalidOperationException("Overbooked food/storage");
            foreach(var r in residents){
                int expected=r.action==LifeAction.Harvest?tuning.harvestYield:r.action==LifeAction.Gather?tuning.berryYield:r.action==LifeAction.EggCollect?r.eggReservation:0;
                if(r.productionReservation!=expected||r.mealReservation!=(r.action==LifeAction.Eat?1:0))throw new InvalidOperationException("Work reservation mismatch "+r.id);
                if(r.action==LifeAction.Collect&&(r.bundle==null||r.bundle.owner!=r.id||r.collectionReservation<=0||r.collectionReservation>r.bundle.amount))throw new InvalidOperationException("Bundle reservation mismatch");
                if(r.site>=0&&sites[r.site].owner!=r.id)throw new InvalidOperationException("Resident reservation mismatch");
            }
            foreach(var b in bundles)if(b.amount<=0||b.owner>=0&&(b.owner>=residents.Length||residents[b.owner].bundle!=b))throw new InvalidOperationException("Orphan bundle reservation");
            foreach(var s in sites)if(s.owner>=0&&(s.owner>=residents.Length||residents[s.owner].site!=Array.IndexOf(sites,s)))throw new InvalidOperationException("Reservation leak "+s.id);
            foreach(var r in residents)foreach(var o in residents)if(r.id<o.id&&!r.sleeping&&!o.sleeping&&(r.position-o.position).sqrMagnitude<.595f*.595f)throw new InvalidOperationException("Resident collision");
            foreach(var r in residents)if(!navigation.Clear(r.position)||r.cargo<0)throw new InvalidOperationException("Invalid resident "+r.id+" "+r.position);
        }
        public static string Label(LifeAction a)=>new[]{"살펴보기","당근 심기","당근 수확","열매 채집","운반","식사","휴식","풍경 감상","귀가·집안 휴식","먹거리 회수","자리 기다리기","비 피하기","닭 먹이 준비","닭 먹이 주기","달걀 수거","목재 수집","목재 운반","목재 회수","창고 건설","밭 개간","정원·쉼터 조성"}[(int)a];
        public static string TraitLabel(LifeTemperament trait)=>new[]{"기본 성향","농사 선호","채집 선호","느긋함"}[(int)trait];
    }
}
