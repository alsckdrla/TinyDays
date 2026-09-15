using System.Collections.Generic;
using UnityEngine;

namespace TinyDays
{
    public sealed class ResidentTraffic : MonoBehaviour
    {
        ResidentMotor[] motors;
        ResidentBrain[] brains;
        ResidentMotor[] activeMotors;
        Vector3[] positions,velocities,next;
        VillageNavigation navigation;
        VillageResources resources;
        VillageExpansion expansion;
        VillageAutonomySettings settings;
        float clock;
        readonly Dictionary<int,float> entered=new Dictionary<int,float>();
        readonly Dictionary<long,int> priority=new Dictionary<long,int>();
        public bool automatic=true;
        public bool advanceDecisions=true;
        public void RefreshResidents() { motors=null; brains=null; activeMotors=null; }
        void Update() { if(automatic) Simulate(Time.deltaTime); }
        public void Simulate(float delta)
        {
            CacheReferences(); navigation.Refresh();
            while(delta>0)
            {
                float dt=Mathf.Min(.05f,delta); delta-=dt;
                if(resources) resources.Simulate(dt);
                if(expansion) expansion.Simulate(dt);
                int activeCount=0;
                for(int i=0;i<motors.Length;i++)
                {
                    var motor=motors[i]; var brain=brains[i];
                    if(!motor||!brain||!motor.isActiveAndEnabled||!brain.enabled) continue;
                    motor.Initialize(); if(advanceDecisions) brain.Simulate(dt);
                    activeMotors[activeCount]=motor; positions[activeCount]=motor.transform.position; velocities[activeCount]=motor.Velocity; activeCount++;
                }
                clock+=dt;
                for(int i=0;i<activeCount;i++)
                {
                    var motor=activeMotors[i];
                    motor.PriorityWaitFor=-1;
                    var heading=motor.HasGoal?(motor.Goal-motor.transform.position).normalized:motor.transform.forward;
                    var right=Vector3.Cross(Vector3.up,heading);
                    bool narrow=!navigation.ClearSegment(motor.transform.position,motor.transform.position+right*1.1f)&&!navigation.ClearSegment(motor.transform.position,motor.transform.position-right*1.1f);
                    int id=motor.Brain.residentId;
                    if(narrow&&!entered.ContainsKey(id)) entered[id]=clock;
                    if(!narrow) entered.Remove(id);
                }
                for(int i=0;i<activeCount;i++) for(int j=i+1;j<activeCount;j++)
                {
                    int a=activeMotors[i].Brain.residentId,b=activeMotors[j].Brain.residentId; long key=((long)a<<32)|(uint)b;
                    if(Vector3.Distance(positions[i],positions[j])>3.5f || !activeMotors[i].HasGoal || !activeMotors[j].HasGoal) { priority.Remove(key); continue; }
                    if(!entered.ContainsKey(a)&&!entered.ContainsKey(b)&&!priority.ContainsKey(key)) continue;
                    if(!priority.TryGetValue(key,out int winner))
                    {
                        float ta=entered.ContainsKey(a)?entered[a]:float.MaxValue, tb=entered.ContainsKey(b)?entered[b]:float.MaxValue;
                        winner=ta<tb?a:tb<ta?b:activeMotors[i].Brain.calmness<activeMotors[j].Brain.calmness?a:activeMotors[i].Brain.calmness>activeMotors[j].Brain.calmness?b:Mathf.Min(a,b);
                        priority[key]=winner;
                    }
                    (winner==a?activeMotors[j]:activeMotors[i]).PriorityWaitFor=winner;
                }
                for(int i=0;i<activeCount;i++) next[i]=activeMotors[i].Plan(dt,activeMotors,activeCount,positions,velocities,i);
                // Resolve swept pair conflicts against a common snapshot. A stopped agent remains an obstacle.
                float diameter=settings.residentRadius*2;
                for(int pass=0;pass<activeCount;pass++)
                for(int i=0;i<activeCount;i++) for(int j=i+1;j<activeCount;j++)
                {
                    var a=positions[i]-positions[j]; var b=a+(next[i]-next[j])*dt;
                    if(VillageNavigation.SegmentDistance(Vector3.zero,a,b)<diameter+.005f) { next[i]=Vector3.zero; next[j]=Vector3.zero; }
                }
                for(int i=0;i<activeCount;i++) activeMotors[i].Apply(next[i],dt);
            }
        }
        void CacheReferences()
        {
            if(motors==null)
            {
                motors=GetComponentsInChildren<ResidentMotor>();
                System.Array.Sort(motors,(a,b)=>a.GetComponent<ResidentBrain>().residentId.CompareTo(b.GetComponent<ResidentBrain>().residentId));
                brains=new ResidentBrain[motors.Length]; activeMotors=new ResidentMotor[motors.Length]; positions=new Vector3[motors.Length]; velocities=new Vector3[motors.Length]; next=new Vector3[motors.Length];
                for(int i=0;i<motors.Length;i++) brains[i]=motors[i].GetComponent<ResidentBrain>();
            }
            if(!navigation) navigation=GetComponent<VillageNavigation>();
            if(!resources) resources=GetComponent<VillageResources>();
            if(!expansion) expansion=GetComponent<VillageExpansion>();
            if(!settings) settings=GetComponent<VillageAutonomySettings>();
        }
    }
}
