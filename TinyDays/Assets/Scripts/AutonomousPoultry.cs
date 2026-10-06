using System;
using System.Linq;
using UnityEngine;

namespace TinyDays.Life
{
    // Review values, independent of the original scene identity and balance settings.
    public static class PoultryLayout
    {
        public static readonly Bounds Area=new Bounds(new Vector3(10.4f,.6f,-5.25f),new Vector3(4.2f,1.2f,5.5f));
        public static LifeSite[] Sites()=>new[]{
            new LifeSite{id="poultry-feeder",kind=SiteKind.Feeder,position=new Vector3(7.55f,0,-7.15f)},
            new LifeSite{id="poultry-nest",kind=SiteKind.Nest,position=new Vector3(7.55f,0,-4.15f)}};
        public static Vector3 Home(int id)=>new Vector3(9.2f+id*.95f,0,-4.15f);
        public static Vector3 Yard(int id,int step)=>new Vector3(9.2f+id*.95f,0,-7.2f+(step%3)*.53f);
    }
    [Serializable] public sealed class HenState
    {
        public int id,step;public Vector3 position,target,forward=Vector3.back;
        public float progress,idle;public bool fed,inside;
    }
    [Serializable] public sealed class PoultryState
    {
        public int feed,eggs,feedSpent,feedsEaten,laid,collected;
        public HenState[] hens;
        public static PoultryState Create()=>new PoultryState{hens=Enumerable.Range(0,3).Select(i=>new HenState{id=i,position=PoultryLayout.Yard(i,0),target=PoultryLayout.Yard(i,0),idle=i*.5f}).ToArray()};
    }
    public sealed partial class AutonomousSimulation
    {
        public PoultryState Poultry {get;private set;}
        public float DayMinutes {get;set;}=10;
        void AdvancePoultry(float dt)
        {
            if(Poultry==null)return;
            foreach(var h in Poultry.hens){
                bool shelter=Night||Raining;
                if(shelter)h.target=PoultryLayout.Home(h.id);
                else if(h.inside||h.target.z> -5.2f)h.target=PoultryLayout.Yard(h.id,h.step);
                if(!shelter&&!h.fed&&Poultry.feed>0&&Poultry.eggs<6)h.target=PoultryLayout.Yard(h.id,0);
                Vector3 delta=h.target-h.position;
                if(delta.sqrMagnitude>.0001f){h.forward=delta.normalized;h.position=Vector3.MoveTowards(h.position,h.target,.48f*dt);h.idle=0;}
                else if(!shelter){h.idle+=dt;if(h.idle>3+h.id){h.step=(h.step+1)%300;h.target=PoultryLayout.Yard(h.id,h.step);h.idle=0;}}
                h.inside=shelter&&(h.position-PoultryLayout.Home(h.id)).sqrMagnitude<.0001f;
                if(!h.fed&&!shelter&&Poultry.feed>0&&Poultry.eggs<6&&(h.position-PoultryLayout.Yard(h.id,0)).sqrMagnitude<.0001f){Poultry.feed--;Poultry.feedsEaten++;h.fed=true;h.progress=0;}
                if(!h.fed)continue;
                h.progress=Mathf.Min(1,h.progress+dt/(Mathf.Max(1,DayMinutes)*30));
                if(h.progress>=1&&Poultry.eggs<6){Poultry.eggs++;Poultry.laid++;h.fed=false;h.progress=0;}
            }
        }
        void InterruptPoultryCare(LifeResident r)
        {
            if(r.action!=LifeAction.FeedCollect&&r.action!=LifeAction.FeedDeliver&&r.action!=LifeAction.EggCollect)return;
            bool emergency=Night||r.fatigue>=RestThreshold(r)||r.hunger>=tuning.hungerThreshold&&AvailableFood>0;
            bool feeding=r.action!=LifeAction.EggCollect;
            if(emergency||feeding&&(Raining||AvailableFood<4)){
                r.carryingFeed=false;Release(r,false);
            }
        }
        bool ChoosePoultryCare(LifeResident r)
        {
            if(Poultry==null)return false;
            int eggs=Mathf.Min(3,Mathf.Min(Poultry.eggs-residents.Sum(x=>x.eggReservation),Capacity-PlannedStock));
            if(eggs>0&&Assign(r,SiteKind.Nest,LifeAction.EggCollect,AvailableFood<4?"식량이 부족해 준비된 달걀부터 수거":"둥지의 달걀을 공동 창고로 가져오기")){
                r.eggReservation=r.productionReservation=eggs;return true;
            }
            if(!Raining&&AvailableFood>=5&&Poultry.feed==0&&Poultry.eggs<6&&Poultry.hens.Any(h=>!h.fed)&&!residents.Any(x=>x.carryingFeed||x.feedReservation>0)&&
                Assign(r,SiteKind.Store,LifeAction.FeedCollect,"주민 식량을 남기고 닭 먹이 한 몫 준비")){
                r.feedReservation=1;return true;
            }
            return false;
        }
        bool CompletePoultryCare(LifeResident r)
        {
            if(r.action!=LifeAction.FeedCollect&&r.action!=LifeAction.FeedDeliver&&r.action!=LifeAction.EggCollect)return false;
            if(r.work<tuning.workSeconds)return true;
            if(r.action==LifeAction.FeedCollect){food--;r.cargo=1;r.carryingFeed=true;}
            else if(r.action==LifeAction.FeedDeliver){r.cargo--;r.carryingFeed=false;Poultry.feed+=3;Poultry.feedSpent++;}
            else {int n=r.eggReservation;Poultry.eggs-=n;Poultry.collected+=n;produced+=n;r.cargo+=n;}
            Release(r);return true;
        }
        void ValidatePoultry()
        {
            Action<bool,string> need=AutonomousSaveCodec.Require;
            if(Poultry==null){need(!sites.Any(s=>s.kind==SiteKind.Feeder)&&residents.All(r=>r.feedReservation==0&&r.eggReservation==0&&!r.carryingFeed&&(int)r.action<=11),"Missing poultry state");return;}
            var p=Poultry;
            need(p.hens!=null&&p.hens.Length==3&&p.feed>=0&&p.feed<=3&&p.eggs>=0&&p.eggs<=6&&p.feedSpent>=0&&p.feedsEaten>=0&&p.laid>=0&&p.collected>=0,"Invalid poultry counters");
            need((long)p.feed+p.feedsEaten==(long)p.feedSpent*3&&p.hens.Count(h=>h!=null&&h.fed)+(long)p.laid==p.feedsEaten&&(long)p.eggs+p.collected==p.laid,"Poultry conservation");
            need(residents.Sum(r=>r.eggReservation)<=p.eggs&&residents.Count(r=>r.carryingFeed||r.feedReservation>0)<=1,"Poultry double reservation");
            foreach(var r in residents){
                need(r.feedReservation==(r.action==LifeAction.FeedCollect?1:0),"Feed reservation");
                need(r.eggReservation>=0&&r.eggReservation<=3&&(r.eggReservation>0)==(r.action==LifeAction.EggCollect),"Egg reservation");
                need(!r.carryingFeed||r.cargo==1&&(r.action==LifeAction.None||r.action==LifeAction.FeedDeliver),"Feed cargo");
                need(r.action!=LifeAction.FeedDeliver||r.carryingFeed,"Feed delivery without food");
                if(r.action==LifeAction.FeedCollect)need(sites[r.site].kind==SiteKind.Store,"Feed source");
                if(r.action==LifeAction.FeedDeliver)need(sites[r.site].kind==SiteKind.Feeder,"Feed destination");
                if(r.action==LifeAction.EggCollect)need(sites[r.site].kind==SiteKind.Nest,"Egg destination");
            }
            for(int i=0;i<3;i++){
                var h=p.hens[i];need(h!=null&&h.id==i&&h.step>=0&&h.step<300&&AutonomousSaveCodec.Finite(h.position)&&AutonomousSaveCodec.Finite(h.target)&&AutonomousSaveCodec.Finite(h.forward)&&AutonomousSaveCodec.Finite(h.progress)&&AutonomousSaveCodec.Finite(h.idle),"Invalid hen");
                need(h.progress>=0&&h.progress<=1&&(h.fed||h.progress==0)&&h.idle>=0&&h.idle<=6.1f&&Mathf.Abs(h.position.x-PoultryLayout.Home(i).x)<.001f&&h.position.z>=-7.21f&&h.position.z<=-4.14f&&Mathf.Abs(h.position.y)<.001f,"Hen progress/yard");
                need(Mathf.Abs(h.target.x-PoultryLayout.Home(i).x)<.001f&&h.target.z>=-7.21f&&h.target.z<=-4.14f&&Mathf.Abs(h.target.y)<.001f,"Hen target");
                need(!h.inside||(h.position-PoultryLayout.Home(i)).sqrMagnitude<.0001f,"Hen shelter");
            }
        }
    }
}
