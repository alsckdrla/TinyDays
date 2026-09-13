using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace TinyDays
{
    public sealed class ResidentTraffic : MonoBehaviour
    {
        ResidentMotor[] motors;
        float clock;
        readonly Dictionary<int,float> entered=new Dictionary<int,float>();
        readonly Dictionary<long,int> priority=new Dictionary<long,int>();
        public bool automatic=true;
        public bool advanceDecisions=true;
        public void RefreshResidents() { motors=null; }
        void Update() { if(automatic) Simulate(Time.deltaTime); }
        public void Simulate(float delta)
        {
            if(motors==null) motors=GetComponentsInChildren<ResidentMotor>().OrderBy(m=>m.GetComponent<ResidentBrain>().residentId).ToArray();
            var nav=GetComponent<VillageNavigation>(); nav.Refresh();
            while(delta>0)
            {
                float dt=Mathf.Min(.05f,delta); delta-=dt;
                var resources=GetComponent<VillageResources>(); if(resources) resources.Simulate(dt);
                var active=motors.Where(m=>m&&m.isActiveAndEnabled&&m.GetComponent<ResidentBrain>().enabled).ToArray();
                foreach(var m in active) { m.Initialize(); if(advanceDecisions) m.Brain.Simulate(dt); }
                var positions=active.Select(m=>m.transform.position).ToArray();
                var velocities=active.Select(m=>m.Velocity).ToArray(); var next=new Vector3[active.Length];
                clock+=dt;
                foreach(var motor in active)
                {
                    motor.PriorityWaitFor=-1;
                    var heading=motor.HasGoal?(motor.Goal-motor.transform.position).normalized:motor.transform.forward;
                    var right=Vector3.Cross(Vector3.up,heading);
                    bool narrow=!nav.ClearSegment(motor.transform.position,motor.transform.position+right*1.1f)&&!nav.ClearSegment(motor.transform.position,motor.transform.position-right*1.1f);
                    int id=motor.Brain.residentId;
                    if(narrow&&!entered.ContainsKey(id)) entered[id]=clock;
                    if(!narrow) entered.Remove(id);
                }
                for(int i=0;i<active.Length;i++) for(int j=i+1;j<active.Length;j++)
                {
                    int a=active[i].Brain.residentId,b=active[j].Brain.residentId; long key=((long)a<<32)|(uint)b;
                    if(Vector3.Distance(positions[i],positions[j])>3.5f || !active[i].HasGoal || !active[j].HasGoal) { priority.Remove(key); continue; }
                    if(!entered.ContainsKey(a)&&!entered.ContainsKey(b)&&!priority.ContainsKey(key)) continue;
                    if(!priority.TryGetValue(key,out int winner))
                    {
                        float ta=entered.ContainsKey(a)?entered[a]:float.MaxValue, tb=entered.ContainsKey(b)?entered[b]:float.MaxValue;
                        winner=ta<tb?a:tb<ta?b:active[i].Brain.calmness<active[j].Brain.calmness?a:active[i].Brain.calmness>active[j].Brain.calmness?b:Mathf.Min(a,b);
                        priority[key]=winner;
                    }
                    (winner==a?active[j]:active[i]).PriorityWaitFor=winner;
                }
                for(int i=0;i<active.Length;i++) next[i]=active[i].Plan(dt,active,positions,velocities,i);
                // Resolve swept pair conflicts against a common snapshot. A stopped agent remains an obstacle.
                float diameter=GetComponent<VillageAutonomySettings>().residentRadius*2;
                for(int pass=0;pass<active.Length;pass++)
                for(int i=0;i<active.Length;i++) for(int j=i+1;j<active.Length;j++)
                {
                    var a=positions[i]-positions[j]; var b=a+(next[i]-next[j])*dt;
                    if(VillageNavigation.SegmentDistance(Vector3.zero,a,b)<diameter+.005f) { next[i]=Vector3.zero; next[j]=Vector3.zero; }
                }
                for(int i=0;i<active.Length;i++) active[i].Apply(next[i],dt);
            }
        }
    }
}
