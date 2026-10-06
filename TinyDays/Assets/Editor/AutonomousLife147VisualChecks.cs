using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Life;
using TinyDays.Review;
public static class AutonomousLife147VisualChecks
{
    static void Need(bool b,string m){if(!b)throw new Exception(m);}
    static void Render(Camera c,string label){var rt=new RenderTexture(1200,800,24);var previous=c.targetTexture;var active=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1200,800,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1200,800),0,0);t.Apply();File.WriteAllBytes("Docs/References/Poultry147-"+label+".png",t.EncodeToPNG());c.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);}
    static bool build=true;
    public static void RenderOnly(){build=false;Execute();}
    public static void Execute()
    {
        try{
            EditorSceneManager.OpenScene(AutonomousLifeBuilder.ScenePath);var w=UnityEngine.Object.FindObjectOfType<AutonomousLifeWorld>();w.Initialize();var ui=w.GetComponent<FarmStudyReview>();ui.ResetCameraToPreset();var lighting=w.GetComponent<FarmLightingStudy>();lighting.Apply(1);
            var visual=w.gameObject.AddComponent<AutonomousPoultryVisual>();visual.Configure(w);foreach(Transform child in w.transform)if(child.name=="Grass tuft"||child.name=="Spring flower"||child.name=="Meadow stone")Need(!(child.position.x>=8.25f&&child.position.x<=12.55f&&child.position.z>=-8.05f&&child.position.z<=-2.45f),"Ground decoration overlaps coop/yard");var season=w.gameObject.AddComponent<AutonomousSeasonVisual>();season.Configure();season.Apply(new Vector4(1,0,0,0));
            Render(lighting.reviewCamera,"whole");var c=lighting.reviewCamera;c.transform.position=new Vector3(5,5,-11);c.transform.LookAt(new Vector3(10,.6f,-5));Render(c,"close");
            season.Apply(new Vector4(0,0,0,1));Render(c,"winter");
            var roof=w.GetComponentsInChildren<MeshRenderer>().First(r=>r.name=="Coop pitched roof");Need(roof.sharedMaterial.HasProperty("_SnowAmount")&&roof.sharedMaterial.GetFloat("_SnowAmount")==1,"Coop winter roof missing");
            var occ=w.GetComponent<FarmCameraOcclusion>();var original=roof.sharedMaterial;var inside=new Vector3(10.4f,.7f,-3.8f);occ.Fade(inside,Vector3.zero,1,false,false);Need(roof.sharedMaterial.color.a==.25f,"Coop interior fade missing");season.Apply(new Vector4(1,0,0,0));occ.Fade(new Vector3(0,25,-30),Vector3.zero,1,false,false);Need(roof.sharedMaterial==original&&roof.sharedMaterial.color.a==1&&roof.sharedMaterial.GetFloat("_SnowAmount")==0,"Coop fade/season restoration");
            File.WriteAllText("Docs/AutonomousLife147VisualVerification.txt","Whole/close/winter renders generated\nCoop roof snow / interior alpha25 / spring and material restore: PASS\n");Debug.Log("LIFE147_VISUAL_OK");if(build){AutonomousLifeBuilder.BuildPlayer();Debug.Log("LIFE147_FINAL_BUILD_OK");}
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);throw;}
    }
}
