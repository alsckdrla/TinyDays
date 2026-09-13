using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyDays
{
    [RequireComponent(typeof(ResidentWanderer))]
    public sealed class ResidentMotor : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        public bool Arrived { get; private set; }
        public bool Failed { get; private set; }
        public string Reason { get; private set; }="대기";
        public int Replans { get; private set; }
        public int AvoidanceCount { get; private set; }
        public float FirstAvoidanceDistance { get; private set; }=float.MaxValue;
        public float WaitSeconds { get; private set; }
        public bool HasGoal { get; private set; }
        public Vector3 Goal { get; private set; }
        public ResidentBrain Brain { get; private set; }
        public int PriorityWaitFor { get; set; }=-1;
        public void ResetDiagnostics() { FirstAvoidanceDistance=float.MaxValue; WaitSeconds=0; AvoidanceCount=0; Replans=0; }
        VillageNavigation nav;
        VillageAutonomySettings settings;
        ResidentWanderer visual;
        readonly List<Vector3> path=new List<Vector3>();
        int index, version;
        float stagnant, progressClock, lastRemaining, yieldTime, sideLock, waitRemaining;
        int side=1, yieldingTo=-1;
        Vector3 desiredHeading;
        Vector3 escapeHeading;
        float escapeTime, elapsed, nextWait;
        int lastWaitPartner=-1;
        public void Initialize()
        {
            if(nav) return;
            nav=GetComponentInParent<VillageNavigation>(); settings=GetComponentInParent<VillageAutonomySettings>();
            Brain=GetComponent<ResidentBrain>(); visual=GetComponent<ResidentWanderer>();
        }
        public bool SetDestination(Vector3 point)
        {
            Initialize(); Goal=point; HasGoal=true; Arrived=false; Failed=false; index=0;
            stagnant=progressClock=yieldTime=waitRemaining=escapeTime=0; yieldingTo=-1;
            bool found=nav.FindPath(transform.position,point,Brain.pathPreference,path);
            version=nav.Version; lastRemaining=Remaining();
            if(!found) { Failed=true; HasGoal=false; Reason="도달 불가능"; }
            return found;
        }
        public void Stop() { HasGoal=false; Velocity=Vector3.zero; path.Clear(); Reason="대기"; }
        float Remaining()
        {
            if(index>=path.Count) return 0;
            float d=Vector3.Distance(transform.position,path[index]);
            for(int i=index+1;i<path.Count;i++) d+=Vector3.Distance(path[i-1],path[i]); return d;
        }
        public Vector3 Plan(float dt,ResidentMotor[] motors,Vector3[] positions,Vector3[] velocities,int self)
        {
            Initialize(); var position=positions[self]; sideLock=Mathf.Max(0,sideLock-dt);
            elapsed+=dt; escapeTime=Mathf.Max(0,escapeTime-dt);
            waitRemaining=Mathf.Max(0,waitRemaining-dt);
            if(!HasGoal||Failed) return Vector3.zero;
            if(Vector3.Distance(position,Goal)<.08f && Velocity.magnitude<=settings.acceleration*dt) { Arrived=true; HasGoal=false; Reason="도착"; return Vector3.zero; }
            if(version!=nav.Version) Replan(motors);
            if(path.Count==0) return Vector3.zero;
            while(index<path.Count-1&&Vector3.Distance(position,path[index])<.3f&&nav.ClearSegment(position,path[index+1])) index++;
            if(!nav.ClearSegment(position,path[index])) Replan(motors);
            if(path.Count==0) return Vector3.zero;
            Vector3 target=path[index];
            if(index<path.Count-1 && Vector3.Distance(position,target)<settings.lookAhead)
            {
                var ahead=Vector3.MoveTowards(target,path[index+1],settings.lookAhead-Vector3.Distance(position,target));
                if(nav.ClearSegment(position,ahead)) target=ahead;
            }
            var direction=(target-position).normalized;
            desiredHeading=direction;
            var resources=GetComponentInParent<VillageResources>();
            float speed=visual.walkSpeed*settings.moveSpeedMultiplier*(resources?resources.ProductivityMultiplier:1f);
            if(index<path.Count-1) speed=Mathf.Min(speed,Mathf.Sqrt(2*settings.acceleration*Mathf.Max(.01f,Vector3.Distance(position,target)-.07f)));
            speed=Mathf.Min(speed,Mathf.Sqrt(2*settings.acceleration*Mathf.Max(0,Vector3.Distance(position,Goal)-.04f)));
            int threat=-1; float nearest=settings.detectionDistance;
            for(int j=0;j<motors.Length;j++) if(j!=self)
            {
                var relative=positions[j]-position; float distance=relative.magnitude;
                if(distance>settings.detectionDistance) continue;
                var rv=velocities[j]-direction*speed;
                float horizon=Mathf.Min(settings.predictionSeconds,Vector3.Distance(position,Goal)/Mathf.Max(.1f,speed));
                float t=Mathf.Clamp(-Vector3.Dot(relative,rv)/Mathf.Max(.0001f,rv.sqrMagnitude),0,horizon);
                float comfort=Vector3.Distance(position,Goal)<1.3f?.02f:.3f;
                if((relative+rv*t).magnitude<settings.residentRadius*2+comfort && distance<nearest) { threat=j; nearest=distance; }
            }
            bool wait=false;
            if(threat>=0)
            {
                AvoidanceCount++; if(FirstAvoidanceDistance==float.MaxValue) FirstAvoidanceDistance=nearest;
                bool newEncounter=yieldingTo!=motors[threat].Brain.residentId;
                if(newEncounter) { yieldingTo=motors[threat].Brain.residentId; yieldTime=0; }
                yieldTime+=dt;
                bool lowerPriority=Brain.calmness>motors[threat].Brain.calmness || (Mathf.Approximately(Brain.calmness,motors[threat].Brain.calmness)&&Brain.residentId>yieldingTo);
                if(newEncounter&&lowerPriority&&Brain.calmness>Brain.diligence&&(lastWaitPartner!=yieldingTo||elapsed>=nextWait))
                { waitRemaining=.6f+1.4f*Brain.calmness; lastWaitPartner=yieldingTo; nextWait=elapsed+10f; }
                wait=(PriorityWaitFor>=0&&yieldTime<3f)||waitRemaining>0;
                if(sideLock<=0) { side=1; sideLock=1f; }
                Reason=wait?"느긋함: 양보 "+motors[threat].name:"우회: "+motors[threat].name;
            }
            else { yieldingTo=-1; yieldTime=0; Reason="경로 이동"; }
            if(waitRemaining>0) { wait=true; Reason="느긋함: 잠시 양보"; }
            if((threat>=0||sideLock>0)&&!wait)
            {
                direction=Quaternion.Euler(0,side*(threat>=0&&nearest<1.5f?75f:40f),0)*direction;
                desiredHeading=direction;
            }
            if(Velocity.sqrMagnitude<.001f&&!wait&&escapeTime<=0&&progressClock>.3f&&Vector3.Distance(position,Goal)>.2f)
            {
                // Turn in place toward a genuinely clear exit when the preferred side is blocked.
                var forward=(target-position).normalized; float escapeScore=float.NegativeInfinity;
                foreach(float angle in new[]{0f,30f,-30f,60f,-60f,90f,-90f,120f,-120f,180f})
                {
                    var heading=Quaternion.Euler(0,angle,0)*forward; var end=position+heading*.7f;
                    if(!nav.ClearSegment(position,end)) continue;
                    bool safe=true;
                    for(int j=0;j<motors.Length;j++) if(j!=self&&VillageNavigation.SegmentDistance(positions[j],position,end)<settings.residentRadius*2+.005f) { safe=false; break; }
                    if(!safe) continue;
                    float score=Vector3.Dot(heading,forward)-Mathf.Abs(angle)*.001f;
                    if(score>escapeScore) { escapeScore=score; escapeHeading=heading; }
                }
                if(escapeScore>float.NegativeInfinity) escapeTime=1f;
            }
            if(escapeTime>0&&!wait) { direction=escapeHeading; desiredHeading=direction; }
            float best=float.NegativeInfinity; Vector3 chosen=Vector3.zero;
            foreach(float angle in new[]{0f,20f,-20f,40f,-40f,65f,-65f,90f,-90f})
            foreach(float factor in new[]{1f,.5f,0f})
            {
                Vector3 wanted=Quaternion.Euler(0,angle,0)*direction*(wait?0:speed*factor);
                var candidate=Vector3.MoveTowards(Velocity,wanted,settings.acceleration*dt);
                if(candidate.magnitude>.02f)
                {
                    var heading=Vector3.RotateTowards(transform.forward,candidate.normalized,settings.turnDegrees*Mathf.Deg2Rad*dt,0);
                    candidate=heading*candidate.magnitude;
                }
                if(!nav.ClearSegment(position,position+candidate*dt)) continue;
                // Reserve enough clear space to brake before a wall, rather than stopping on contact.
                var brakingEnd=position+candidate*dt+candidate.normalized*(candidate.sqrMagnitude/(2*settings.acceleration));
                if(!nav.ClearSegment(position,brakingEnd)) continue;
                float collision=0;
                bool canBrake=true;
                for(int j=0;j<motors.Length;j++) if(j!=self && Vector3.Distance(position,positions[j])<settings.detectionDistance)
                {
                    var rel=positions[j]-position; var rv=velocities[j]-candidate;
                    float toward=Mathf.Max(0,Vector3.Dot(candidate,rel.normalized));
                    float otherToward=Mathf.Max(0,-Vector3.Dot(Vector3.MoveTowards(velocities[j],Vector3.zero,settings.acceleration*dt),rel.normalized));
                    float brakingGap=(toward*toward+otherToward*otherToward)/(2*settings.acceleration)+(toward+otherToward)*dt;
                    if(rel.magnitude<settings.residentRadius*2+.02f+brakingGap&&Vector3.Dot(rel,rv)<-.00001f) canBrake=false;
                    float horizon=Mathf.Min(settings.predictionSeconds,Vector3.Distance(position,Goal)/Mathf.Max(.1f,candidate.magnitude));
                    float t=Mathf.Clamp(-Vector3.Dot(rel,rv)/Mathf.Max(.0001f,rv.sqrMagnitude),0,horizon);
                    float separation=(rel+rv*t).magnitude;
                    if(separation<settings.residentRadius*2+(Vector3.Distance(position,Goal)<1.3f?.02f:.12f)) collision+=8*(settings.predictionSeconds-t+.2f);
                    if(VillageNavigation.SegmentDistance(Vector3.zero,rel,rel+rv*dt)<settings.residentRadius*2+.004f) collision+=1000;
                }
                if(!canBrake) continue;
                float score=Vector3.Dot(candidate,direction)*(1+Brain.diligence)-collision-Mathf.Abs(angle)*.003f;
                score-=(candidate-Velocity).magnitude*.15f;
                if(!nav.IsRoad(position+candidate*.7f)) score-=Brain.pathPreference*.2f;
                if(threat>=0&&angle*side>0) score+=.12f;
                if(factor==0) score+=wait?.6f:Brain.calmness*.06f;
                if(score>best) { best=score; chosen=candidate; }
            }
            if(best<-100) chosen=Vector3.zero;
            if(wait) WaitSeconds+=dt;
            progressClock+=dt;
            if(progressClock>=settings.stuckSeconds)
            {
                float remaining=Remaining();
                if(lastRemaining-remaining<.2f) { stagnant+=progressClock; Replan(motors); }
                else stagnant=0;
                lastRemaining=Remaining(); progressClock=0;
                if(stagnant>=settings.failureSeconds) { Failed=true; HasGoal=false; Reason="정체 복구 실패"; return Vector3.zero; }
            }
            return chosen;
        }
        void Replan(ResidentMotor[] motors)
        {
            Replans++; index=0;
            var occupied=motors.Where(m=>m!=this&&Vector3.Distance(m.transform.position,transform.position)<settings.detectionDistance).Select(m=>m.transform.position).ToList();
            if(!nav.FindPath(transform.position,Goal,Brain.pathPreference,path,occupied)) nav.FindPath(transform.position,Goal,Brain.pathPreference,path);
            version=nav.Version; Reason="혼잡 재탐색";
        }
        public void Apply(Vector3 velocity,float dt)
        {
            Velocity=velocity; transform.position+=velocity*dt;
            if(velocity.sqrMagnitude>.0001f) transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(velocity),settings.turnDegrees*dt);
            else if(HasGoal&&desiredHeading.sqrMagnitude>.001f) transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(desiredHeading),settings.turnDegrees*dt);
            visual.AnimateMotion(velocity.magnitude,dt);
        }
        void OnDisable() { if(visual) Stop(); }
        void OnDrawGizmosSelected()
        {
            Gizmos.color=Color.cyan; Vector3 p=transform.position;
            for(int i=index;i<path.Count;i++) { Gizmos.DrawLine(p,path[i]); p=path[i]; }
        }
    }
}
