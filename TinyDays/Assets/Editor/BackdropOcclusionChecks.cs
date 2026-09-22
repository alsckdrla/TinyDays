using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class BackdropOcclusionChecks
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Execute()
    {
        try
        {
            ResidentOcclusionChecks.Execute();
            EditorSceneManager.OpenScene(FarmStudyBuilder.ScenePath);
            var lighting=UnityEngine.Object.FindObjectOfType<FarmLightingStudy>();
            lighting.Apply(1);
            var review=lighting.GetComponent<FarmStudyReview>();
            var fade=lighting.GetComponent<FarmCameraOcclusion>();
            var backdrop=GameObject.Find("Backdrop").GetComponent<Renderer>();
            var original=backdrop.sharedMaterial;
            var camera=lighting.reviewCamera;
            string folder="Docs/Captures/BackdropOcclusion";Directory.CreateDirectory(folder);
            // A contrasting opaque marker behind the actual backdrop proves GPU blending,
            // rather than merely asserting the alpha value on a material.
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material=new Material(original);material.SetColor("_BaseColor",new Color(.05f,.8f,.15f));
            marker.GetComponent<Renderer>().sharedMaterial=material;
            marker.transform.position=new Vector3(50,1,50);marker.transform.localScale=new Vector3(8,.2f,8);
            try
            {
                camera.transform.position=new Vector3(50,-5,50);
                camera.transform.LookAt(marker.transform.position,Vector3.forward);
                var opaque=Capture(camera,folder+"/Opaque.png");
                for(int cycle=0;cycle<3;cycle++)
                {
                    fade.Fade(camera.transform.position,Vector3.zero,.1f,false,false);
                    Require(backdrop.sharedMaterial.color.a>.25f&&backdrop.sharedMaterial.color.a<1,"Transition not gradual");
                    fade.Fade(camera.transform.position,Vector3.zero,.1f,false,false);
                    Require(Mathf.Abs(backdrop.sharedMaterial.color.a-.25f)<.001f,"Outside meadow below backdrop did not fade");
                    var transparent=Capture(camera,folder+"/Transparent.png");
                    backdrop.enabled=false;
                    var unobstructed=Capture(camera,folder+"/Unobstructed.png");backdrop.enabled=true;
                    Require(Vector3.Distance(Rgb(opaque),Rgb(unobstructed))>.15f,"Render fixture has insufficient contrast");
                    Require(Vector3.Distance(Rgb(transparent),Rgb(unobstructed))<Vector3.Distance(Rgb(opaque),Rgb(unobstructed))*.7f,"Backdrop does not visibly reveal marker");
                    fade.Fade(new Vector3(50,3,50),Vector3.zero,.2f,false,false);
                    Require(backdrop.sharedMaterial==original,"Original backdrop material not restored");
                }
                for(int time=0;time<4;time++)
                {
                    lighting.Apply(time);
                    fade.Fade(new Vector3(0,-5,0),Vector3.zero,.2f,false,false);
                    Require(Mathf.Abs(backdrop.sharedMaterial.color.a-.25f)<.001f,"Meadow underground alpha");
                    lighting.Colors.Set(time,0,new Color(.25f,.35f,.45f));lighting.Apply(time);
                    Require(Vector3.Distance(Rgb(backdrop.sharedMaterial.color),new Vector3(.25f,.35f,.45f))<.001f,"Color edit lost during fade");
                    Require(Mathf.Abs(backdrop.sharedMaterial.color.a-.25f)<.001f,"Lighting erased fade alpha");
                    review.ShowOverview();review.UpdateResidentOcclusion(.2f);
                    Require(backdrop.sharedMaterial==original,"Home did not restore backdrop");
                }
                fade.Fade(new Vector3(200,-5,200),Vector3.zero,.2f,false,false);
                Require(Mathf.Abs(backdrop.sharedMaterial.color.a-.25f)<.001f,"Below farm level outside backdrop footprint did not fade");
                marker.SetActive(false);
                lighting.Apply(1);
                var ground=new[]{GameObject.Find("Spring meadow").GetComponent<Renderer>(),GameObject.Find("Meadow foundation").GetComponent<Renderer>(),backdrop};
                foreach(var position in new[]{new Vector3(0,-.1f,0),new Vector3(0,-.5f,0),new Vector3(0,-5,0),new Vector3(0,-20,0),new Vector3(25,-8,-25)})
                {
                    foreach(bool focus in new[]{false,true})
                    {
                        fade.Fade(position,Vector3.up,.2f,false,focus);
                        foreach(var layer in ground)Require(Mathf.Abs(layer.sharedMaterial.color.a-.25f)<.001f,"Ground not faded at "+position+" focus="+focus);
                    }
                }
                camera.transform.position=new Vector3(0,-5,-10);camera.transform.LookAt(new Vector3(0,1,3));
                fade.Fade(camera.transform.position,Vector3.zero,.2f,false,false);
                Capture(camera,folder+"/FarmBelowCenter.png");
                camera.transform.position=new Vector3(25,-8,-25);camera.transform.LookAt(new Vector3(0,1,3));
                fade.Fade(camera.transform.position,Vector3.zero,.2f,false,false);
                Capture(camera,folder+"/FarmBelowOutside.png");
                review.ShowOverview();review.UpdateResidentOcclusion(.2f);
                foreach(var layer in ground)Require(Mathf.Abs(layer.sharedMaterial.color.a-1)<.001f,"Ground Home restore failed");
                File.WriteAllText("Docs/BackdropOcclusionVerification.txt","PASS v0.56: actual farm backdrop GPU opaque/faded/unobstructed comparison; three 0.2s fade/restore cycles; all three ground layers from inside to deep underground and outside meadow, both focused and overview; four lighting presets and in-memory color edits while faded; Home restoration; below farm height outside backdrop footprint; resident selection/focus regression. FarmBelowCenter and FarmBelowOutside renders captured. Editor direct calls and GPU captures, not live player input.\n");
                Debug.Log("BACKDROP_OCCLUSION_OK");
            }
            finally{backdrop.enabled=true;fade.Restore();UnityEngine.Object.DestroyImmediate(marker);UnityEngine.Object.DestroyImmediate(material);}
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static Vector3 Rgb(Color c)=>new Vector3(c.r,c.g,c.b);
    static Color Capture(Camera camera,string path)
    {
        var rt=new RenderTexture(512,512,24);var active=RenderTexture.active;var target=camera.targetTexture;
        var texture=new Texture2D(512,512,TextureFormat.RGB24,false);float aspect=camera.aspect;
        try
        {
            camera.targetTexture=rt;camera.aspect=1;camera.Render();RenderTexture.active=rt;
            texture.ReadPixels(new Rect(0,0,512,512),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
            return texture.GetPixel(256,256);
        }
        finally{camera.targetTexture=target;camera.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
