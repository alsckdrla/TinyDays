using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class ResidentOcclusionChecks
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Execute()
    {
        GameObject fixture=null;
        try
        {
            EditorSceneManager.OpenScene(FarmStudyBuilder.ScenePath);
            fixture=new GameObject("Selection test");var camera=fixture.AddComponent<Camera>();camera.aspect=1;camera.fieldOfView=40;
            var resident=new GameObject("Resident test");resident.transform.SetParent(fixture.transform);var residents=new[]{resident.transform};
            Action<float,float> place=(x,z)=>resident.transform.position=camera.ViewportToWorldPoint(new Vector3(.5f+(x-.5f)*Mathf.Min(camera.pixelWidth,camera.pixelHeight)/camera.pixelWidth,.5f,z))-Vector3.up*.8f;
            place(.5f,10);Check(FarmStudyReview.SelectOcclusionResident(camera,residents)==resident.transform,"Center resident missed");
            place(.63f,10);Check(FarmStudyReview.SelectOcclusionResident(camera,residents)==null,"Entry radius ignored");
            Check(FarmStudyReview.SelectOcclusionResident(camera,residents,resident.transform)==resident.transform,"Retention radius failed");
            place(.67f,10);Check(FarmStudyReview.SelectOcclusionResident(camera,residents,resident.transform)==null,"Stale resident retained");
            place(.5f,-10);Check(FarmStudyReview.SelectOcclusionResident(camera,residents)==null,"Behind camera selected");
            place(.5f,10);resident.SetActive(false);Check(FarmStudyReview.SelectOcclusionResident(camera,residents)==null,"Inactive resident selected");resident.SetActive(true);
            var review=UnityEngine.Object.FindObjectOfType<FarmStudyReview>();review.ResetCameraToPreset();var o=review.GetComponent<FarmCameraOcclusion>();
            var house=GameObject.Find("Home A").GetComponentsInChildren<MeshRenderer>();var originals=house.Select(r=>r.sharedMaterial).ToArray();
            camera.transform.position=new Vector3(-5,1.5f,2);camera.transform.LookAt(new Vector3(-5,1.5f,8));resident.transform.position=new Vector3(-5,.7f,8);
            Check(FarmStudyReview.SelectOcclusionResident(camera,residents)==resident.transform,"Occluded resident not eligible");
            o.Fade(camera.transform.position,resident.transform.position+Vector3.up*.8f,.2f,false,true);
            Check(house.All(r=>Mathf.Abs(r.sharedMaterial.color.a-.25f)<.001f),"Resident behind house did not fade house");
            o.Fade(camera.transform.position,resident.transform.position+Vector3.up*.8f,.1f,false,false);
            Check(house.All(r=>r.sharedMaterial.color.a>.25f&&r.sharedMaterial.color.a<1),"No-target recovery not gradual");
            o.Fade(camera.transform.position,resident.transform.position+Vector3.up*.8f,.1f,false,false);
            Check(house.Select((r,i)=>r.sharedMaterial==originals[i]).All(v=>v),"No-target original restore failed");
            o.Fade(new Vector3(0,-3,0),new Vector3(0,1,0),.2f,false,false);
            Check(Mathf.Abs(GameObject.Find("Spring meadow").GetComponent<Renderer>().sharedMaterial.color.a-.25f)<.001f,"Underground exception missing");
            // Exercise the actual review dispatch both with and without a centered resident.
            var cam=review.reviewCamera;cam.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);
            review.director.residents[0].root.position=resident.transform.position;
            review.UpdateResidentOcclusion(.2f);
            Check(house.All(r=>Mathf.Abs(r.sharedMaterial.color.a-.25f)<.001f),"Review resident dispatch failed");
            cam.transform.position=new Vector3(0,50,0);cam.transform.rotation=Quaternion.LookRotation(Vector3.up);
            review.UpdateResidentOcclusion(.2f);
            Check(house.Select((r,i)=>r.sharedMaterial==originals[i]).All(v=>v),"Review no-target dispatch failed");
            o.Restore();
            File.WriteAllText("Docs/ResidentOcclusionVerification.txt","PASS: center selection; entry/retention/exit; behind-camera and inactive exclusion; occluded resident selection; house 25%; no-target gradual 0.2s restoration; underground exception; actual review target/no-target dispatch. Direct editor calls, not live Game input.\n");
            Debug.Log("RESIDENT_OCCLUSION_OK");
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        finally{if(fixture)UnityEngine.Object.DestroyImmediate(fixture);}
    }
}
