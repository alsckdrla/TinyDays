using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyDays;
using UnityEngine;

public static class VerifyNavigation
{
    static void Check(bool value,string message) { if(!value) throw new Exception("Navigation scenario: "+message); }
    public static void Run(GameObject village)
    {
        var navigation=village.GetComponent<VillageNavigation>();
        var traffic=village.GetComponent<ResidentTraffic>();
        var residents=village.GetComponentsInChildren<ResidentBrain>().OrderBy(b=>b.residentId).ToArray();
        var obstacles=UnityEngine.Object.FindObjectsOfType<NavigationObstacle>();
        var positions=residents.Select(b=>b.transform.position).ToArray();
        var rotations=residents.Select(b=>b.transform.rotation).ToArray();
        var diligence=residents.Select(b=>b.diligence).ToArray(); var calmness=residents.Select(b=>b.calmness).ToArray();
        float cautiousWait=0,eagerWait=0;
        var trace=new List<string>{"scenario,time,resident,x,z,speed,reason"};
        var report=new List<string>(); var fixtures=new List<GameObject>();
        try
        {
            traffic.advanceDecisions=false;
            foreach(var b in residents) { b.Motor.Stop(); b.enabled=false; }
            foreach(var obstacle in obstacles) obstacle.enabled=false;
            foreach(string scenario in new[]{"head_on","crossing","overtake","stationary","narrow","close_start","cautious","eager"})
            {
                foreach(var obj in fixtures) UnityEngine.Object.DestroyImmediate(obj); fixtures.Clear();
                // Personality has meaning when both residents cannot freely sidestep in open grass.
                // Use the same constrained corridor for the cautious/eager comparison.
                if(scenario=="narrow"||scenario=="cautious"||scenario=="eager")
                {
                    foreach(float z in new[]{-1.3f,1.3f})
                    {
                        var obj=new GameObject("Test corridor wall"); var obstacle=obj.AddComponent<NavigationObstacle>();
                        obj.transform.position=new Vector3(0,0,z); obstacle.size=new Vector2(3f,1f); fixtures.Add(obj);
                    }
                }
                navigation.Refresh();
                var a=residents[0]; var b=residents[1]; a.enabled=b.enabled=true;
                a.Motor.Stop(); b.Motor.Stop();
                a.Motor.ResetDiagnostics(); b.Motor.ResetDiagnostics();
                a.diligence=diligence[0]; a.calmness=calmness[0]; b.diligence=diligence[1]; b.calmness=calmness[1];
                if(scenario=="cautious"||scenario=="eager") { a.diligence=scenario=="cautious"?.2f:.95f; a.calmness=scenario=="cautious"?.95f:.2f; b.diligence=b.calmness=.5f; }
                float waitBefore=a.Motor.WaitSeconds;
                a.transform.position=new Vector3(-4,.065f,0); b.transform.position=new Vector3(4,.065f,0);
                var aGoal=new Vector3(4,.065f,0); var bGoal=new Vector3(-4,.065f,0);
                if(scenario=="close_start") { a.transform.position=new Vector3(-.46f,.065f,0); b.transform.position=new Vector3(.46f,.065f,0); }
                if(scenario=="crossing") { b.transform.position=new Vector3(0,.065f,-4); bGoal=new Vector3(0,.065f,4); }
                if(scenario=="overtake") { a.transform.position=new Vector3(-4,.065f,0); b.transform.position=new Vector3(-2,.065f,0); bGoal=new Vector3(5,.065f,0); }
                if(scenario=="stationary") b.transform.position=new Vector3(0,.065f,0);
                a.transform.rotation=Quaternion.LookRotation(aGoal-a.transform.position); b.transform.rotation=Quaternion.LookRotation(bGoal-b.transform.position);
                Check(a.Motor.SetDestination(aGoal),scenario+" A path");
                if(scenario!="stationary") Check(b.Motor.SetDestination(bGoal),scenario+" B path");
                float minimum=float.MaxValue, maxTurn=0, maxAcceleration=0;
                for(int tick=0;tick<1000;tick++)
                {
                    var pa=a.transform.position; var pb=b.transform.position; var ra=a.transform.rotation; var rb=b.transform.rotation;
                    float sa=a.Motor.Velocity.magnitude,sb=b.Motor.Velocity.magnitude;
                    traffic.Simulate(.05f);
                    float d=VillageNavigation.SegmentDistance(Vector3.zero,pa-pb,a.transform.position-b.transform.position); minimum=Mathf.Min(minimum,d);
                    Check(d>=.899f,scenario+" collision "+d);
                    Check(navigation.ClearSegment(pa,a.transform.position)&&navigation.ClearSegment(pb,b.transform.position),scenario+" obstacle sweep");
                    maxTurn=Mathf.Max(maxTurn,Quaternion.Angle(ra,a.transform.rotation),Quaternion.Angle(rb,b.transform.rotation));
                    Check(maxTurn<=7.51f,scenario+" instantaneous turn "+maxTurn);
                    maxAcceleration=Mathf.Max(maxAcceleration,Mathf.Abs(sa-a.Motor.Velocity.magnitude)/.05f,Mathf.Abs(sb-b.Motor.Velocity.magnitude)/.05f);
                    if(tick%5==0) foreach(var person in new[]{a,b}) trace.Add(scenario+","+(tick*.05f).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+","+person.residentId+","+person.transform.position.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+person.transform.position.z.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+person.Motor.Velocity.magnitude.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+person.Motor.Reason);
                    if(a.Motor.Arrived&&(scenario=="stationary"||b.Motor.Arrived)) break;
                    Check(!a.Motor.Failed&&!b.Motor.Failed,scenario+" stuck: "+a.Motor.Reason+" / "+b.Motor.Reason);
                }
                Check(a.Motor.Arrived&&(scenario=="stationary"||b.Motor.Arrived),scenario+" arrival");
                Check(maxAcceleration<=2.51f,scenario+" abrupt speed change "+maxAcceleration);
                if(scenario=="head_on") Check(a.Motor.FirstAvoidanceDistance>2f&&a.Motor.FirstAvoidanceDistance<3.6f,"anticipation must start before close contact");
                report.Add("PASS "+scenario+": minimum spacing "+minimum+", max turn per 0.05s "+maxTurn+", max acceleration "+maxAcceleration);
                if(scenario=="cautious") cautiousWait=a.Motor.WaitSeconds-waitBefore;
                if(scenario=="eager") eagerWait=a.Motor.WaitSeconds-waitBefore;
                a.enabled=b.enabled=false;
            }
            Check(cautiousWait>eagerWait+.1f,"personality must change waiting behavior "+cautiousWait+" / "+eagerWait);
            report.Add("PASS personality waiting: cautious "+cautiousWait+"s, eager "+eagerWait+"s");
            // A box and a circle must route around their footprints, including diagonal segments.
            foreach(var obj in fixtures) UnityEngine.Object.DestroyImmediate(obj); fixtures.Clear();
            foreach(bool circle in new[]{false,true})
            {
                var obj=new GameObject(circle?"Test barrel":"Test building"); fixtures.Add(obj);
                var obstacle=obj.AddComponent<NavigationObstacle>(); obstacle.circle=circle; obstacle.radius=.6f; obstacle.size=new Vector2(3,3);
                navigation.Refresh(); var path=new List<Vector3>(); var start=new Vector3(-4,.065f,0); var end=new Vector3(4,.065f,0);
                Check(navigation.FindPath(start,end,.5f,path),"obstacle route exists");
                var previous=start;
                foreach(var point in path)
                {
                    for(int i=0;i<=100;i++) Check(!obstacle.Contains(Vector3.Lerp(previous,point,i/100f),.55f),"independent obstacle footprint");
                    previous=point;
                }
                obstacle.enabled=false;
                report.Add("PASS "+(circle?"circular trunk/barrel":"rectangular building/sign")+" detour");
            }
            foreach(var obj in fixtures) UnityEngine.Object.DestroyImmediate(obj); fixtures.Clear();
            foreach(var layout in new[]{new Vector4(-4,.9f,1,2.8f),new Vector4(0,1.8f,9,1),new Vector4(4,.9f,1,2.8f)})
            {
                var obj=new GameObject("Test preferred road"); fixtures.Add(obj); obj.transform.position=new Vector3(layout.x,0,layout.y);
                var road=obj.AddComponent<NavigationObstacle>(); road.road=true; road.size=new Vector2(layout.z,layout.w);
            }
            navigation.Refresh(); var roadFractions=new List<float>();
            foreach(float preference in new[]{0f,1f})
            {
                var path=new List<Vector3>(); var start=new Vector3(-4,.065f,0);
                Check(navigation.FindPath(start,new Vector3(4,.065f,0),preference,path),"road preference route");
                float roadLength=0,total=0; var p=start;
                foreach(var end in path)
                {
                    int samples=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(p,end)/.05f)); float step=Vector3.Distance(p,end)/samples;
                    for(int i=0;i<samples;i++) { total+=step; if(navigation.IsRoad(Vector3.Lerp(p,end,(i+.5f)/samples))) roadLength+=step; }
                    p=end;
                }
                roadFractions.Add(roadLength/total);
            }
            Check(roadFractions[1]>roadFractions[0]+.15f,"path preference must change road use: "+string.Join(" / ",roadFractions));
            report.Add("PASS road preference: road fractions "+string.Join(" / ",roadFractions));
            var dynamicObject=new GameObject("Test movable obstacle"); fixtures.Add(dynamicObject);
            var dynamicObstacle=dynamicObject.AddComponent<NavigationObstacle>(); dynamicObstacle.size=new Vector2(2,1);
            navigation.Refresh(); Check(!navigation.ClearPoint(new Vector3(0,.065f,0)),"new obstacle blocks ground");
            dynamicObject.transform.position=new Vector3(8,0,8); dynamicObject.transform.rotation=Quaternion.Euler(0,40,0);
            navigation.Refresh(); Check(navigation.ClearPoint(new Vector3(0,.065f,0))&&!navigation.ClearPoint(new Vector3(8,.065f,8)),"moved obstacle refreshes graph");
            dynamicObstacle.enabled=false; navigation.Refresh(); Check(navigation.ClearPoint(new Vector3(8,.065f,8)),"disabled obstacle releases ground");
            report.Add("PASS obstacle registration, movement, rotation and disable refresh");
        }
        finally
        {
            File.WriteAllLines("Docs/NavigationScenarios.csv",trace);
            File.WriteAllLines("Docs/NavigationScenarios.txt",report);
            foreach(var obj in fixtures) UnityEngine.Object.DestroyImmediate(obj);
            foreach(var o in obstacles) if(o) o.enabled=true;
            navigation.Refresh();
            for(int i=0;i<residents.Length;i++) { residents[i].Motor.Stop(); residents[i].transform.position=positions[i]; residents[i].transform.rotation=rotations[i]; residents[i].diligence=diligence[i]; residents[i].calmness=calmness[i]; residents[i].enabled=true; }
            traffic.advanceDecisions=true;
        }
    }
}
