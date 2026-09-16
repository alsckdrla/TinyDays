using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;

public static class FarmLightingChecks
{
    public static void Execute()
    {
        try
        {
            var prefs=new System.Collections.Generic.Dictionary<string,string>();
            var settings=new LightingColors(k=>prefs.TryGetValue(k,out var v)?v:"",(k,v)=>prefs[k]=v);
            for(int t=0;t<4;t++)for(int k=0;k<3;k++)
            {
                var originalColor=settings.Get(t,k);settings.Set(t,k,Color.magenta);
                if(prefs.Count!=t*3+k)throw new Exception("Preview unexpectedly saved");
                settings.Set(t,k,originalColor);settings.Set(t,k,new Color(.2f,.4f,.6f));settings.Save(t,k);
            }
            var loaded=new LightingColors(k=>prefs[k]);
            for(int t=0;t<4;t++)for(int k=0;k<3;k++)if(loaded.Get(t,k)!=new Color(.2f,.4f,.6f))throw new Exception("Palette persistence failed");
            if(new LightingColors(k=>"invalid").Get(0,0)!=LightingColors.Default(0,0))throw new Exception("Invalid setting fallback failed");
            FarmStudyBuilder.Build();FarmStudyBuilder.Verify(false);
            var lighting=UnityEngine.Object.FindObjectOfType<FarmLightingStudy>();
            var review=lighting.GetComponent<FarmStudyReview>();var camera=lighting.reviewCamera;
            string folder="Docs/Captures/Stage27Lighting";Directory.CreateDirectory(folder);
            var original=lighting.GetComponentsInChildren<MeshRenderer>().Select(r=>r.sharedMaterial).Distinct().ToArray();
            var colors=original.Select(m=>m.color).ToArray();
            var paletteBefore=WindowPalette(lighting);
            for(int i=0;i<4;i++)
            {
                lighting.Apply(i);
                var panes=lighting.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.name.StartsWith("Glass Lighting study")).ToArray();
                if(panes.Length<15||panes.Select(r=>FarmLightingStudy.WindowTint(r.transform)).Distinct().Count()<4)throw new Exception("Window separation or palette variety missing");
                foreach(var house in panes.Where(r=>r.transform.parent.name.StartsWith("Home ")).GroupBy(r=>r.transform.parent))
                    if(house.Select(r=>FarmLightingStudy.WindowTint(r.transform)).Distinct().Count()<2)throw new Exception("House windows lack variation");
                foreach(var pane in panes)
                {
                    Color expected=i==1?Color.black:FarmLightingStudy.WindowTint(pane.transform)*(i==3?1.05f:.4f);
                    if(pane.sharedMaterial.GetColor("_EmissionColor")!=expected)throw new Exception("Window emission differs from stable palette");
                }
                if(lighting.selected!=i||lighting.sun.intensity<=0)throw new Exception("Static light selection failed");
                review.ShowOverview();Capture(camera,folder+"/"+i+"-Overview.png",1280,800);
                if(i==0||i==2)
                {
                    var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(folder+"/"+i+"-Overview.png"));
                    var actual=(Color32)tex.GetPixel(10,tex.height-11);var expected=(Color32)LightingColors.Default(i,0);
                    UnityEngine.Object.DestroyImmediate(tex);
                    if(Math.Abs(actual.r-expected.r)>1||Math.Abs(actual.g-expected.g)>1||Math.Abs(actual.b-expected.b)>1)throw new Exception("Rendered background differs: "+actual+" expected "+expected);
                }
                camera.transform.position=new Vector3(8,5,-6);camera.transform.LookAt(new Vector3(6,1.7f,6));Capture(camera,folder+"/"+i+"-Detail.png",1280,800);
                var fade=lighting.GetComponent<FarmCameraOcclusion>();
                fade.Fade(new Vector3(0,1,7.1f),new Vector3(0,1,7.2f),.2f,false);fade.Restore();
                foreach(var pane in panes)if(i!=1&&pane.sharedMaterial.color!=FarmLightingStudy.WindowTint(pane.transform))throw new Exception("Window tint lost after fade restore");
                if(original.Where((m,j)=>m.color!=colors[j]).Any())throw new Exception("Shared source material changed");
            }
            lighting.Apply(1);review.ShowOverview();
            FarmStudyBuilder.Build();
            var regenerated=UnityEngine.Object.FindObjectOfType<FarmLightingStudy>();
            if(!paletteBefore.SequenceEqual(WindowPalette(regenerated)))throw new Exception("Window palette changed after scene regeneration");
            File.WriteAllText("Docs/Stage27LightingVerification.txt","v0.44 / 2-7-1: rendered dawn #8A4636 and dusk #A66A59 within one RGB byte; 12 isolated color settings preview/save/reload and invalid-value fallback PASS (in-memory store, no user preferences). Static looks, separate windows, per-house palette variety, deterministic palette after scene regeneration, daytime emission off, tint after fade restore, source material isolation PASS. Four same-camera overview/detail renders generated. Farm regeneration/manual preservation and existing camera/occlusion checks PASS. Actual player input is recorded separately in Stage27LightingObservation.md. User approval pending. Automatic cycle and five-minute observation deferred.\n");
            Debug.Log("STAGE27_LIGHTING_OK");
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static string[] WindowPalette(FarmLightingStudy lighting)
    {
        return lighting.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.name=="Glass"||r.sharedMaterial.name=="HouseGlass")
            .Select(r=>r.transform.parent.name+"/"+r.name+"/"+ColorUtility.ToHtmlStringRGB(FarmLightingStudy.WindowTint(r.transform))).OrderBy(s=>s).ToArray();
    }
    static void Capture(Camera camera,string path,int width,int height)
    {
        var rt=new RenderTexture(width,height,24);var old=RenderTexture.active;var target=camera.targetTexture;float aspect=camera.aspect;
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.aspect=width/(float)height;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally{RenderTexture.active=old;camera.targetTexture=target;camera.aspect=aspect;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
