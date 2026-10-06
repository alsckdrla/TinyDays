using System;
using System.Linq;
using UnityEngine;

namespace TinyDays.Life
{
    public sealed partial class AutonomousSimulation
    {
        // Review timing, not final personality or relationship balance.
        public const float GreetingSeconds=1.4f, ConversationSeconds=4.8f, TogetherSeconds=6;
        public const float SocialDuration=GreetingSeconds+ConversationSeconds+TogetherSeconds;
        public static string SocialLabel(LifeResident r)=>r.socialPartner==0?"":r.socialTime<GreetingSeconds?"인사":r.socialTime<GreetingSeconds+ConversationSeconds?"짧은 대화":"함께 쉬기";
        public static bool Talking(LifeResident r)=>r.socialPartner>0&&r.socialTime>=GreetingSeconds&&r.socialTime<GreetingSeconds+ConversationSeconds;
        bool SocialNeed(LifeResident r)=>!r.sleeping&&r.cargo==0&&r.bundle==null&&!Night&&!Raining&&AvailableFood>=4&&r.hunger<tuning.hungerThreshold&&r.fatigue<RestThreshold(r);
        bool SocialReady(LifeResident r)=>SocialNeed(r)&&!r.Moving&&r.wait<=0&&(r.action==LifeAction.Rest||r.action==LifeAction.Leisure);
        void EndSocial(LifeResident r)
        {
            if(r.socialPartner==0)return;
            var other=residents[r.socialPartner-1];
            r.socialPartner=other.socialPartner=0;r.socialTime=other.socialTime=0;
            r.socialReadyAt=elapsed+40+r.id*7;other.socialReadyAt=elapsed+40+other.id*7;
        }
        void AdvanceSocial(float dt)
        {
            foreach(var r in residents.Where(r=>r.socialPartner>r.id+1).ToArray())
            {
                var other=residents[r.socialPartner-1];
                bool valid=SocialReady(r)&&SocialReady(other)&&navigation.Segment(r.position,other.position);
                if(!valid||r.socialTime+dt>=SocialDuration){EndSocial(r);Release(r);Release(other);continue;}
                r.socialTime+=dt;other.socialTime=r.socialTime;
                foreach(var a in new[]{r,other}){
                    var b=a==r?other:r;var direction=b.position-a.position;direction.y=0;
                    if(direction.sqrMagnitude>.01f)a.forward=Vector3.RotateTowards(a.forward,direction.normalized,dt*2.5f,0);
                }
            }
        }
        void FindSocialPairs()
        {
            foreach(var r in residents)
            {
                if(r.socialPartner!=0||elapsed<r.socialReadyAt||!SocialReady(r))continue;
                var other=residents.Where(o=>o.id>r.id&&o.socialPartner==0&&elapsed>=o.socialReadyAt&&SocialReady(o))
                    .OrderBy(o=>(o.position-r.position).sqrMagnitude)
                    .FirstOrDefault(o=>(o.position-r.position).sqrMagnitude<=2.4f*2.4f&&navigation.Segment(r.position,o.position));
                if(other==null)continue;
                r.socialPartner=other.id+1;other.socialPartner=r.id+1;r.socialTime=other.socialTime=0;
            }
        }
        bool JoinRest(LifeResident r)
        {
            if(!SocialNeed(r)||elapsed<r.socialReadyAt)return false;
            var company=residents.Where(o=>o!=r&&!o.sleeping&&(o.action==LifeAction.Rest||o.action==LifeAction.Leisure)&&o.site>=0&&sites[o.site].kind==SiteKind.Rest).ToArray();
            if(company.Length>0&&Assign(r,SiteKind.Rest,LifeAction.Leisure,"이웃이 쉬는 쉼터에서 함께 쉬러 가기",s=>company.Any(o=>(sites[o.site].position-s.position).sqrMagnitude<=2.4f*2.4f)))return true;
            return r.completed%3==0&&Assign(r,SiteKind.Rest,LifeAction.Leisure,"쉼터에서 잠시 여유롭게 쉬기");
        }
        void ValidateSocial()
        {
            foreach(var r in residents){
                if(r.socialPartner<0||r.socialPartner>residents.Length||r.socialPartner==r.id+1||!AutonomousSaveCodec.Finite(r.socialTime)||!AutonomousSaveCodec.Finite(r.socialReadyAt)||r.socialTime<0||r.socialReadyAt<0)throw new InvalidOperationException("Invalid social state");
                if(r.socialPartner==0){if(r.socialTime!=0)throw new InvalidOperationException("Orphan social timer");continue;}
                var other=residents[r.socialPartner-1];
                if(other.socialPartner!=r.id+1||other.socialTime!=r.socialTime||r.socialTime>=SocialDuration||r.Moving||r.sleeping||r.cargo!=0||r.site<0||(r.action!=LifeAction.Rest&&r.action!=LifeAction.Leisure)||(r.position-other.position).sqrMagnitude>2.4f*2.4f)throw new InvalidOperationException("Invalid paired social activity");
            }
        }
    }
}
