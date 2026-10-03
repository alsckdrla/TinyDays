using System;
using System.IO;
using System.Linq;
using TinyDays.Review;
using UnityEditor;
using UnityEngine;

// Geometry-only source migration: reuse the established clip/scene builders.
public static class CanonicalRabbitUpdate {
    public static void InspectAssets(){
        AssetDatabase.Refresh();
        foreach(string path in new[]{"Assets/Art/Generated/RabbitWater/AdultRabbitWater.fbx","Assets/Art/Generated/RabbitBench/AdultRabbitBench.fbx"})
            Debug.Log("CLIP_INVENTORY "+path+" "+string.Join(",",AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Select(c=>c.name)));
    }
    public static void Execute(){
        try {
            AssetDatabase.Refresh();
            AdultRabbitBuilder.Execute();
            AdultRabbitBuilder.BuildPlayer();
            AdultRabbitMotionBuilder.Execute();
            AdultSleepChecks.Face();
            CapturePoses();
            AdultRabbitMotionBuilder.BuildPlayer();
            Life();
            Debug.Log("CANONICAL_RABBIT_UNITY_OK");
        } catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    public static void ResumeLife(){
        try{AssetDatabase.Refresh();Life();Debug.Log("CANONICAL_RABBIT_LIFE_OK");}
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
    static void Life(){
        RabbitHomeLifeBuilder.Execute();
        VerifyImportedHeads();
        RabbitBenchChecks.Draft();RabbitBenchChecks.Verify();RabbitBenchChecks.Render();
        RabbitWaterChecks.Draft();RabbitWaterChecks.Verify();
        RabbitHomeGroundChecks.FinalizeReview();
    }
    static void CapturePoses(){
        var r=UnityEngine.Object.FindObjectOfType<AdultRabbitMotionReview>();
        Directory.CreateDirectory("Docs/Captures/CanonicalRabbit");
        foreach(int clip in new[]{0,1,4,7,22,24,28,30}){
            r.Select(clip);r.Sample(r.clips[clip].length*.35f);
            r.reviewCamera.transform.position=new Vector3(3.3f,2.3f,4.5f);
            r.reviewCamera.transform.LookAt(new Vector3(0,clip>=22?.6f:1f,0));
            AdultSleepChecks.Capture(r,$"Docs/Captures/CanonicalRabbit/Clip{clip}.png");
        }
        r.Select(0);
    }
    static void VerifyImportedHeads(){
        string root="Assets/Art/Generated/";
        string[] paths={"AdultRabbit/AdultRabbit.fbx","AdultRabbit/AdultRabbitMotion.fbx","RabbitHome/AdultRabbitHome.fbx","RabbitWater/AdultRabbitWater.fbx","RabbitBench/AdultRabbitBench.fbx"};
        var meshes=paths.Select(p=>AssetDatabase.LoadAllAssetsAtPath(root+p).OfType<Mesh>().Single(m=>m.name=="Head")).ToArray();
        var reference=meshes[0];
        foreach(var mesh in meshes){
            if(mesh.vertexCount!=reference.vertexCount||mesh.blendShapeCount!=reference.blendShapeCount)throw new Exception("Head topology/shape import mismatch");
            float difference=mesh.vertices.Zip(reference.vertices,(a,b)=>Vector3.Distance(a,b)).Max();
            if(difference>.0001f)throw new Exception("Canonical Head export mismatch: "+difference);
            if(mesh.GetBlendShapeIndex("SleepEyesClosed")<0)throw new Exception("Missing closed eyes");
        }
        File.WriteAllText("Docs/CanonicalRabbitUnityVerification.txt","Five imported Head meshes agree within 0.1mm; SleepEyesClosed preserved. Existing asset paths retained. Not OS input or user quality approval.\n");
        Debug.Log("CANONICAL_HEAD_IMPORT_OK");
    }
}
