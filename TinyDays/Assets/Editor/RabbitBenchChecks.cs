using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;
public static class RabbitBenchChecks {
    const string Out="Docs/Captures/RabbitBench";
    static void Check(bool ok,string message){if(!ok)throw new Exception("Bench: "+message);}
    static RabbitHomeLifeReview Open(){EditorSceneManager.OpenScene("Assets/Scenes/RabbitHomeLifeStudy.unity");var h=UnityEngine.Object.FindObjectOfType<RabbitHomeLifeReview>();Check(h&&h.benchRest,"missing scene task");h.SelectBenchMode();return h;}
    static void Capture(Camera c,string name){
        string path=(name.StartsWith("Sequence")?"Logs/RabbitBenchV0122":Out)+"/"+name+".png";Directory.CreateDirectory(Path.GetDirectoryName(path));
        var rt=new RenderTexture(960,720,24);var old=c.targetTexture;var active=RenderTexture.active;var tex=new Texture2D(960,720,TextureFormat.RGB24,false);
        // Batch camera renders share one editor frame. Use evaluated world skin
        // vertices so Unity's cached GPU skin pose cannot repeat an older pose.
        var skins=UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>().Where(s=>s.enabled&&s.gameObject.activeInHierarchy).ToArray();var previews=new List<GameObject>();var meshes=new List<Mesh>();
        foreach(var s in skins){var mesh=UnityEngine.Object.Instantiate(s.sharedMesh);mesh.vertices=AdultRabbitSitCoat.World(s).Select(s.transform.InverseTransformPoint).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            var preview=new GameObject("Evaluated render skin");preview.transform.SetParent(s.transform,false);preview.AddComponent<MeshFilter>().sharedMesh=mesh;preview.AddComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;s.enabled=false;previews.Add(preview);meshes.Add(mesh);}
        try{c.targetTexture=rt;c.aspect=4f/3;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();ReviewCaptureFile.Write(path,tex.EncodeToPNG());}
        finally{foreach(var p in previews)UnityEngine.Object.DestroyImmediate(p);foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);foreach(var s in skins)s.enabled=true;c.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
    public static void Draft(){
        RabbitHomeLifeBuilder.Build();var h=Open();var b=h.benchRest;
        var watches=h.resident.GetComponentsInChildren<Transform>().Where(t=>new[]{"Pelvis","Spine","Head","Hand_L","Hand_R","Foot_L","Foot_R"}.Contains(t.name)).ToArray();
        var previous=watches.Select(t=>t.position).ToArray();var rows=new List<string>{"time,state,bone,x,y,z,speed"};var state=b.State;float boundary=0;bool rested=false;
        b.StartFlow();
        for(int n=0;n<240*24;n++){
            h.Advance(1f/240);
            for(int j=0;j<watches.Length;j++){var p=watches[j].position;float d=Vector3.Distance(p,previous[j]);if(state!=b.State)boundary=Mathf.Max(boundary,d);rows.Add(FormattableString.Invariant($"{n/240f:F6},{b.State},{watches[j].name},{p.x:F6},{p.y:F6},{p.z:F6},{d*240:F6}"));previous[j]=p;}state=b.State;
            if(!rested&&b.State==RabbitBenchReview.TaskState.Resting&&b.TimeInState>.1f){rested=true;foreach(float a in new[]{180f,90f,140f}){h.ViewBench(a);Capture(h.reviewCamera,"Rest"+a);}foreach(var skin in h.resident.GetComponentsInChildren<SkinnedMeshRenderer>())Debug.Log("BENCH_SKIN "+skin.name);}
        }
        File.WriteAllLines("Docs/RabbitBenchMotion240.csv",rows);
        var result=$"rested={rested}, state={b.State}, sole={b.MinSole:F6}, gap={b.MaxGap:F6}, drift={b.MaxDrift:F6}, reach={b.MaxReach:F6}, turnSteps={b.TurnSteps}, boundary={boundary:F6}";
        File.WriteAllText("Docs/RabbitBenchVerification.txt",result);Debug.Log("BENCH_DRAFT "+result+" "+b.ReachDetail);h.ResetStudy();
    }
    static void Contacts(RabbitBenchReview b){Check(b.MinSole>=-.0005f&&b.MaxGap<=.005f&&b.MaxDrift<=.0035f&&b.MaxReach<=.001f,$"contacts sole={b.MinSole} gap={b.MaxGap} drift={b.MaxDrift} reach={b.MaxReach} {b.ReachDetail}");}
    static float SeatGap(RabbitHomeLifeReview h){
        var skin=h.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(t=>t.name=="Bottom");
        var pelvis=h.resident.GetComponentsInChildren<Transform>().First(t=>t.name=="Pelvis");var hip=h.resident.transform.InverseTransformPoint(pelvis.position);
        var points=AdultRabbitSitCoat.World(skin).Where(p=>{var local=h.resident.transform.InverseTransformPoint(p);return local.z<hip.z+.01f&&Mathf.Abs(local.x-hip.x)<.18f;}).ToArray();
        return points.Min(p=>p.y)-(h.benchRest.bench.position.y+h.benchRest.seatHeight);
    }
    static float HorizontalClearance(RabbitHomeLifeReview h){
        var bounds=h.benchRest.BenchBounds;
        var points=h.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.name=="Shoes"||s.name=="Bottom").SelectMany(AdultRabbitSitCoat.World);
        float result=float.PositiveInfinity;
        foreach(var p in points){if(p.y>bounds.max.y+.001f)continue;float dx=Mathf.Max(bounds.min.x-p.x,0,p.x-bounds.max.x);float dz=Mathf.Max(bounds.min.z-p.z,0,p.z-bounds.max.z);result=Mathf.Min(result,Mathf.Sqrt(dx*dx+dz*dz));}return result;
    }
    static void SeatFootprint(RabbitHomeLifeReview h){
        var skin=h.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="Bottom");var pelvis=h.resident.GetComponentsInChildren<Transform>().First(t=>t.name=="Pelvis");var hip=h.resident.transform.InverseTransformPoint(pelvis.position);
        var p=AdultRabbitSitCoat.World(skin).Where(v=>{var local=h.resident.transform.InverseTransformPoint(v);return local.z<hip.z+.01f&&Mathf.Abs(local.x-hip.x)<.18f;}).OrderBy(v=>v.y).First();var b=h.benchRest.BenchBounds;
        Check(p.x>b.min.x&&p.x<b.max.x&&p.z>b.min.z&&p.z<b.max.z,$"seat support outside footprint {p} {b}");
    }
    static bool RayTriangle(Vector3 origin,Vector3 direction,Vector3 a,Vector3 b,Vector3 c,out float distance){
        distance=0;var e=b-a;var f=c-a;var p=Vector3.Cross(direction,f);float determinant=Vector3.Dot(e,p);if(Mathf.Abs(determinant)<1e-8f)return false;
        var s=origin-a;float u=Vector3.Dot(s,p)/determinant;if(u<0||u>1)return false;var q=Vector3.Cross(s,e);float v=Vector3.Dot(direction,q)/determinant;if(v<0||u+v>1)return false;
        distance=Vector3.Dot(f,q)/determinant;return distance>1e-6f;
    }
    static void BenchIntersection(RabbitHomeLifeReview h){
        var b=h.benchRest;var mesh=b.bench.GetComponentInChildren<MeshFilter>();var solid=mesh.sharedMesh.vertices.Select(mesh.transform.TransformPoint).ToArray();var triangles=mesh.sharedMesh.triangles;
        var direction=new Vector3(.371f,.529f,.763f).normalized;
        foreach(var skin in h.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.name=="Shoes"||s.name=="Bottom")){
            var vertices=AdultRabbitSitCoat.World(skin);
            foreach(var p in vertices){if(!b.BenchBounds.Contains(p))continue;var distances=new List<float>();for(int j=0;j<triangles.Length;j+=3){float d;if(RayTriangle(p,direction,solid[triangles[j]],solid[triangles[j+1]],solid[triangles[j+2]],out d)&&!distances.Any(x=>Mathf.Abs(x-d)<.00001f))distances.Add(d);}
                Check(distances.Count%2==0,$"{skin.name} intersects actual bench solid at {p}, state {b.State} t={b.TimeInState}");
            }
        }
    }
    public static void CheckConnectedPose(RabbitHomeLifeReview h){
        BenchIntersection(h);
        var b=h.benchRest;
        if(b.State==RabbitBenchReview.TaskState.Walking||b.State==RabbitBenchReview.TaskState.Turning)Check(HorizontalClearance(h)>=.05f,"connected approach clearance below 5cm");
        if(b.State==RabbitBenchReview.TaskState.Resting){
            SeatFootprint(h);float gap=SeatGap(h);Check(gap>=-.0005f&&gap<=.005f,"connected seat contact "+gap);
            for(int i=0;i<2;i++)Check(b.SoleHeight(i)>=.05f&&b.SoleHeight(i)<=.06f,"connected floating foot "+i);
        }
    }
    public static void CheckStoredCanClearance(RabbitHomeLifeReview h){
        var direction=new Vector3(.371f,.529f,.763f).normalized;
        foreach(var filter in h.watering.can.GetComponentsInChildren<MeshFilter>()){
            var solid=filter.sharedMesh.vertices.Select(filter.transform.TransformPoint).ToArray();var triangles=filter.sharedMesh.triangles;
            var box=new Bounds(solid[0],Vector3.zero);foreach(var p in solid)box.Encapsulate(p);
            foreach(var skin in h.resident.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s=>s.name=="Shoes"||s.name=="Bottom"))foreach(var p in AdultRabbitSitCoat.World(skin)){
                if(!box.Contains(p))continue;var distances=new List<float>();for(int j=0;j<triangles.Length;j+=3){float d;if(RayTriangle(p,direction,solid[triangles[j]],solid[triangles[j+1]],solid[triangles[j+2]],out d)&&!distances.Any(x=>Mathf.Abs(x-d)<.00001f))distances.Add(d);}
                Check(distances.Count%2==0,"connected route intersects stored watering can: "+skin.name+" at "+p+" state="+h.benchRest.State+" t="+h.benchRest.TimeInState);
            }
        }
    }
    public static void Verify(){
        var h=Open();var b=h.benchRest;var rows=new List<string>{"v0.122 bench task checks; automatic, not OS input or quality approval."};
        b.StartFlow();float clearance=float.PositiveInfinity;
        float airLow=float.PositiveInfinity,airHigh=0;
        for(int n=0;n<240*25;n++){h.Advance(1f/240);BenchIntersection(h);if(b.State==RabbitBenchReview.TaskState.Walking||b.State==RabbitBenchReview.TaskState.Turning)clearance=Mathf.Min(clearance,HorizontalClearance(h));if(b.State==RabbitBenchReview.TaskState.Resting){SeatFootprint(h);for(int side=0;side<2;side++){float sole=b.SoleHeight(side);airLow=Mathf.Min(airLow,sole);airHigh=Mathf.Max(airHigh,sole);Check(sole>=.05f&&sole<=.06f,$"floating sole {side} {sole}");}}}
        Check(clearance>=.05f,$"walking/turning bench clearance {clearance}");rows.Add($"240Hz walking/turning clearance={clearance:F6}m; both airborne soles={airLow:F6}..{airHigh:F6}m; actual bench solid vertices and seat footprint: PASS");
        foreach(bool slow in new[]{false,true})foreach(int fps in new[]{30,60,120}){
            h.SelectBenchMode();h.slow=slow;b.StartFlow();bool rest=false;int n=0;
            for(;n<fps*50;n++){h.Advance(1f/fps);rest|=b.State==RabbitBenchReview.TaskState.Resting;if(rest&&b.State==RabbitBenchReview.TaskState.Standing)break;}
            Check(rest&&b.State==RabbitBenchReview.TaskState.Standing,"flow timeout");Contacts(b);Check(b.TurnSteps<=3,"too many turn steps");rows.Add($"{fps}fps {(slow?.5f:1)}x complete={n/(float)fps:F4}s sole={b.MinSole:F6} gap={b.MaxGap:F6} drift={b.MaxDrift:F6} reach={b.MaxReach:F6}");
        }
        // Transitions hold their contact targets at 240Hz; resume requests never restart them.
        foreach(int action in new[]{0,2})foreach(int phase in Enumerable.Range(0,8)){
            b.Pose(action,phase);h.paused=false;h.slow=false;b.RequestWalk();b.RequestWalk();h.Advance(6);Check(b.State==RabbitBenchReview.TaskState.Standing,$"phase movement failed {action}/{phase} {b.State}");
            h.SelectBenchMode();b.RequestSit();h.Advance(.2f);b.StopTask();h.Advance(3);Check(b.State==RabbitBenchReview.TaskState.Standing,"approach cancellation failed");
        }
        foreach(int phase in Enumerable.Range(0,8)){
            b.Pose(1,phase);h.paused=false;h.slow=false;b.RequestWalk();h.Advance(5);Check(b.State==RabbitBenchReview.TaskState.Standing,"rest-to-walk failed");Contacts(b);
        }
        foreach(float requestTime in new[]{.2f,1.7f,2.3f,3f,4f,10f,11.7f}){
            h.SelectBenchMode();b.StartFlow();h.Advance(requestTime);b.StopTask();h.Advance(5);
            Check(b.State==RabbitBenchReview.TaskState.Standing||b.State==RabbitBenchReview.TaskState.Resting,"cancel left task in transition");Contacts(b);
        }
        b.Pose(1,0);h.paused=false;var joints=h.resident.GetComponentsInChildren<Transform>();var spine=joints.First(t=>t.name=="Spine");float low=spine.position.y,high=low;
        float seatLow=float.PositiveInfinity,seatHigh=float.NegativeInfinity;
        for(int i=0;i<960;i++){h.Advance(1f/240);low=Mathf.Min(low,spine.position.y);high=Mathf.Max(high,spine.position.y);float gap=SeatGap(h);seatLow=Mathf.Min(seatLow,gap);seatHigh=Mathf.Max(seatHigh,gap);}Check(high-low>=.013f&&high-low<=.015f,$"breath width {high-low}");Check(seatLow>=-.0005f&&seatHigh<=.005f,$"butt/seat gap {seatLow}..{seatHigh}");rows.Add($"seated chest vertical width={high-low:F6}m; physical butt/seat gap={seatLow:F6}..{seatHigh:F6}");Debug.Log(rows.Last());Contacts(b);
        var position=spine.position;float time=b.TimeInState;h.paused=true;h.Advance(2);Check(spine.position==position&&b.TimeInState==time,"pause moved pose");h.paused=false;b.RequestWalk();b.StopTask();h.Advance(3);Check(b.State==RabbitBenchReview.TaskState.Standing&&!b.PendingWalk,"cancelled rise did not finish safely");
        for(int i=0;i<3;i++){h.SelectBenchMode();b.RequestSit();h.Advance(1);h.SelectWaterMode(true);Check(!b.Active&&h.watering.Active,"ownership collision");h.SelectWaterMode(false);Check(!b.Active&&!h.watering.Active&&h.Inside&&h.DoorClosed,"reset failed");}
        foreach(bool slow in new[]{false,true})foreach(int fps in new[]{30,60,120}){
            h.SelectBenchMode();h.slow=slow;float rate=slow?.5f:1;b.RequestSit();h.Advance(10/rate);b.RequestWalk();h.Advance(5/rate);b.RequestSit();
            for(int i=0;i<fps*12/rate;i++){h.Advance(1f/fps);BenchIntersection(h);}Check(b.State==RabbitBenchReview.TaskState.Resting,"re-seating failed");Contacts(b);
        }
        var oldBench=b.bench.position;var oldPreparation=b.SeatPreparation;var shift=new Vector3(.1f,0,.05f);b.bench.position+=shift;b.ResetTask();Check(Vector3.Distance(b.SeatPreparation,oldPreparation+shift)<.00001f,"bench anchors did not move with actual geometry");b.bench.position=oldBench;b.ResetTask();
        rows.Add("re-seating through safe route and geometry-derived anchors after bench translation: PASS");
        rows.Add("phase movement/repeated requests, cancellation, pause, mode restore: PASS");File.WriteAllLines("Docs/RabbitBenchRegression.txt",rows);h.ResetStudy();Debug.Log("BENCH_VERIFY_OK");
    }
    public static void Render(){
        var h=Open();var b=h.benchRest;
        foreach(float a in new[]{180f,90f,140f}){
            h.SelectBenchMode();h.ViewBench(a);b.StartFlow();
            for(int f=0;f<30*18;f++){h.Advance(1f/30);Capture(h.reviewCamera,$"Sequence{a}/frame{f:D4}");}
            for(int action=0;action<3;action++)for(int pose=0;pose<8;pose++){b.Pose(action,pose);h.ViewBench(a);Capture(h.reviewCamera,$"Pose{action}_{pose}_{a}");}
        }h.ResetStudy();Debug.Log("BENCH_RENDER_OK");
    }
    public static void Complete(){Draft();Verify();Render();RabbitWaterChecks.Verify();RabbitHomeGroundChecks.FinalizeReview();Debug.Log("BENCH_COMPLETE_OK");}
    public static void Finish(){Draft();Verify();Render();RabbitHomeLifeBuilder.BuildPlayer();Debug.Log("BENCH_FINISH_OK");}
    public static void RegressionAndBuild(){Verify();RabbitWaterChecks.Verify();RabbitHomeGroundChecks.FinalizeReview();Debug.Log("BENCH_REGRESSION_BUILD_OK");}
}
