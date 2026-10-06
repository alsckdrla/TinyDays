using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife145VisualChecks
{
    static void Need(bool v,string m){if(!v)throw new Exception(m);}
    static void Render(Camera camera,string name){var rt=new RenderTexture(960,640,24);var previous=camera.targetTexture;var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(960,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,640),0,0);image.Apply();File.WriteAllBytes("Docs/References/Season145-"+name+".png",image.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);var w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();w.Initialize();var ui=w.GetComponent<FarmStudyReview>();ui.ResetCameraToPreset();var light=w.GetComponent<FarmLightingStudy>();light.Apply(1);var v=w.gameObject.AddComponent<AutonomousSeasonVisual>();v.Configure();
            for(int i=0;i<4;i++){var weights=Vector4.zero;weights[i]=1;v.Apply(weights);Render(light.reviewCamera,new[]{"spring","summer","autumn","winter"}[i]);}
            var meadow=w.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Spring meadow");var normal=meadow.sharedMaterial;var occ=w.GetComponent<FarmCameraOcclusion>();occ.Fade(new Vector3(0,-3,0),Vector3.zero,1,false,false);
            Need(Mathf.Abs(meadow.sharedMaterial.color.a-.25f)<.001f&&meadow.sharedMaterial.GetFloat("_SnowAmount")==1,"Winter ground fade");
            var camera=light.reviewCamera;camera.transform.position=new Vector3(0,-2,-8);camera.transform.LookAt(new Vector3(0,1,2));Render(camera,"underground");
            v.Apply(new Vector4(1,0,0,0));Need(meadow.sharedMaterial.GetFloat("_SnowAmount")==0,"Season fade material stale");occ.Fade(new Vector3(0,25,-30),Vector3.zero,1,false,false);Need(meadow.sharedMaterial==normal&&meadow.sharedMaterial.color.a==1,"Season material restore");
            UnityEngine.Object.DestroyImmediate(v);File.WriteAllText("Docs/AutonomousLife145VisualVerification.txt","Four season same-noon views: rendered\nWinter below-ground alpha/snow: PASS\nSeason change while faded/material restoration: PASS\n");Debug.Log("LIFE145_VISUAL_OK");AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE145_FINAL_BUILD_OK");
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
