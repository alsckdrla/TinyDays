using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;
public static class RabbitRoutePerformanceChecks {
    static void Check(bool ok,string text){if(!ok)throw new Exception("Route138: "+text);}
    static RabbitHomeLifeReview Open(){EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");return UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();}
    static List<string> Measure(){
        var h=Open();var rows=new List<string>();
        h.SelectLifeFlow();h.lifeFlow.StartFlow(true);
        for(int i=0;i<60*100&&!h.lifeFlow.BenchOwned;i++)h.Advance(1f/60);
        Check(h.lifeFlow.BenchOwned&&!h.benchRest.PathBlocked,"Default departure blocked");
        for(int index=0;index<33;index++){
            if(index>0){
                h.SelectLifeFlow();var can=h.watering.can;int side=(index-1)/4;float yaw=(index-1)%4*90;
                can.SetPositionAndRotation(h.lifeFlow.storage.position,Quaternion.Euler(0,yaw,0));
                var origin=can.position+Quaternion.Euler(0,side*45,0)*Vector3.forward*.58f;origin.y=h.groundHeight;
                h.resident.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,side*45,0));h.idleClip.SampleAnimation(h.resident,0);
                new RabbitHomeFootwork(h.resident,h.groundHeight){FitStandingReach=true}.Hold();
            }
            var center=h.benchRest.bench.position; // Use the same geometric approach as the controller.
            var bounds=h.benchRest.BenchBounds;center=bounds.center;center.y=h.groundHeight;
            var target=center+h.benchRest.bench.forward*(bounds.extents.z+.44f);
            var delta=target-h.resident.transform.position;var tangent=h.benchRest.bench.right*(Vector3.Dot(delta,h.benchRest.bench.right)>=0?1:-1);
            float arrival=Mathf.Atan2(tangent.x,tangent.z)*Mathf.Rad2Deg;
            var timer=Stopwatch.StartNew();var planner=new RabbitCanAvoidance(h,h.watering.can);double setup=timer.Elapsed.TotalMilliseconds;
            RabbitWalkingRoute route;Pose preparation;bool found=planner.Find(target,arrival,out route,out preparation);timer.Stop();
            var samples=found?Enumerable.Range(0,9).Select(i=>route.At(route.Length*i/8f)).ToArray():new Pose[0];
            string signature=found?FormattableString.Invariant($"{route.Length:F6}|{preparation.position.x:F6},{preparation.position.z:F6},{preparation.rotation.eulerAngles.y:F6}|")+string.Join(";",samples.Select(p=>FormattableString.Invariant($"{p.position.x:F6},{p.position.z:F6},{p.rotation.eulerAngles.y:F6}"))):"blocked";
            rows.Add(FormattableString.Invariant($"{index}|{setup:F3}|{timer.Elapsed.TotalMilliseconds:F3}|{signature}"));
            UnityEngine.Debug.Log("ROUTE138_SAMPLE "+rows.Last());
        }
        return rows;
    }
    public static void Before(){try{Directory.CreateDirectory("Logs/Route138");File.WriteAllLines("Logs/Route138/Before.txt",Measure());UnityEngine.Debug.Log("ROUTE138_BEFORE_OK");}catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);}}
    public static void Finish(){try{
        var hashes=RabbitWaterBenchLifeChecks.ArtHashes();var scene=File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity");
        var before=File.ReadAllLines(File.Exists("Docs/RabbitRoute138Before.txt")?"Docs/RabbitRoute138Before.txt":"Logs/Route138/Before.txt");var after=Measure();
        Check(before.Length==after.Count,"Case count");
        for(int i=0;i<before.Length;i++)Check(before[i].Split('|').Skip(3).SequenceEqual(after[i].Split('|').Skip(3)),"Route decision changed case"+i);
        File.WriteAllLines("Docs/RabbitRoute138Comparison.txt",new[]{"33 identical route signatures (found/blocked, preparation, length and nine path samples); timings in milliseconds; direct editor callbacks, not OS input."}.Concat(before.Select((b,i)=>"before "+b+"\nafter "+after[i])));
        RabbitCanAvoidanceChecks.Verify();RabbitBenchChecks.Verify();RabbitWaterChecks.Verify();RabbitWaterBenchLifeChecks.Verify();RabbitWaterBenchLifeChecks.RepeatPositions();RabbitHomeGroundChecks.FinalizeReview();
        RabbitHomeLifeBuilder.BuildPlayer();var latest=RabbitWaterBenchLifeChecks.ArtHashes();Check(hashes.All(k=>latest[k.Key]==k.Value)&&scene.SequenceEqual(File.ReadAllBytes("Assets/Scenes/RabbitHomeLifeStudy.unity")),"Art/scene preservation");
        File.AppendAllText("Docs/RabbitRoute138Comparison.txt","\nContact/obstacle/32 arrangement/block/unblock/interrupt regression; water/bench/home/full flow/20 repeats; Windows build and art/scene preservation PASS. OS input and user motion quality not approved.\n");
        UnityEngine.Debug.Log("ROUTE138_COMPLETE_OK");
    }catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);}}
}
