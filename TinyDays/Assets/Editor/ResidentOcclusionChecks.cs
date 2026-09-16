using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class ResidentOcclusionChecks
{
    // Local input verification of FarmStudy only; not a release-quality build.
    public static void BuildReviewPlayer()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{FarmStudyBuilder.ScenePath},locationPathName="Logs/ResidentSelectionPlayer/TinyDaysReview.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Review player build failed");
        Debug.Log("RESIDENT_REVIEW_PLAYER_OK");
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Execute()
    {
        try
        {
            EditorSceneManager.OpenScene(FarmStudyBuilder.ScenePath);
            var review=UnityEngine.Object.FindObjectOfType<FarmStudyReview>();review.ResetCameraToPreset();
            var o=review.GetComponent<FarmCameraOcclusion>();var cam=review.reviewCamera;
            var houseRoot=GameObject.Find("Home A").transform;var house=houseRoot.GetComponentsInChildren<MeshRenderer>();
            var originals=house.Select(r=>r.sharedMaterial).ToArray();
            var resident=review.director.residents[0].root;
            Action behindHouse=()=>{cam.transform.position=houseRoot.position+Vector3.back*5+Vector3.up*1.5f;cam.transform.LookAt(houseRoot.position+Vector3.up*1.5f);resident.position=houseRoot.position+Vector3.forward*2+Vector3.up*.7f;};
            behindHouse();review.UpdateResidentOcclusion(.2f);
            Check(house.All(r=>r.sharedMaterial.color.a==1),"Overview incorrectly fades centered resident obstacle");
            review.ClickResident(0,1);Check(review.selected==0&&!review.IsFollowing,"Single click changes camera mode");
            review.ClickResident(0,1.2);Check(review.IsFollowing&&review.focus==0,"Double click did not focus");
            behindHouse();review.UpdateResidentOcclusion(.2f);
            Check(house.All(r=>Mathf.Abs(r.sharedMaterial.color.a-.25f)<.001f),"Focused resident obstacle not faded");
            review.ReleaseFocus();review.UpdateResidentOcclusion(.1f);
            Check(house.All(r=>r.sharedMaterial.color.a>.25f&&r.sharedMaterial.color.a<1),"Release recovery not gradual");
            review.UpdateResidentOcclusion(.1f);
            Check(house.Select((r,i)=>r.sharedMaterial==originals[i]).All(v=>v),"Release did not restore original materials");
            review.ClickResident(1,2);review.ClickResident(1,2.4);Check(!review.IsFollowing,"Slow clicks counted as double click");
            review.ClickResident(2,2.5);Check(!review.IsFollowing,"Different resident clicks counted as double click");
            review.residentListOpen=true;review.ClickResident(3,3);
            Check(review.focus==3&&review.IsFollowing&&!review.residentListOpen&&review.selected==3,"List click focus/close failed");
            review.BeginPointer(new Vector2(10,200));review.MovePointer(new Vector2(15,200));
            Check(review.IsFollowing,"Sub-threshold jitter released focus");
            review.MovePointer(new Vector2(16,200));Check(review.IsFollowing&&review.close,"Drag released focus");
            var offsetField=typeof(FarmStudyReview).GetField("followOffset",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Check(offsetField!=null&&((Vector3)offsetField.GetValue(review)).sqrMagnitude>0,"Drag did not retain a follow framing offset");
            review.EndPointer(new Vector2(16,200),4);Check(review.selected==3,"Drag unexpectedly selected resident");
            review.FocusResident(4);review.residentListOpen=true;review.ShowOverview();
            Check(!review.IsFollowing&&!review.close&&review.view==0&&review.selected==-1&&!review.residentListOpen,"Overview did not clear focus, selection and list");
            Check(((Vector3)offsetField.GetValue(review)).sqrMagnitude==0,"Overview did not clear the follow framing offset");
            review.ClickResident(4,5);Check(review.selected==4&&!review.IsFollowing,"Reselection after overview failed");
            review.ShowOverview();
            behindHouse();review.UpdateResidentOcclusion(.2f);
            Check(house.All(r=>r.sharedMaterial.color.a==1),"Overview retained focus fade");
            cam.transform.position=houseRoot.position+Vector3.up*1.5f;review.UpdateResidentOcclusion(.2f);
            Check(house.All(r=>r.sharedMaterial.color.a<.26f),"Overview interior exception lost");
            cam.transform.position=new Vector3(0,-3,0);review.UpdateResidentOcclusion(.2f);
            Check(GameObject.Find("Spring meadow").GetComponent<Renderer>().sharedMaterial.color.a<.26f,"Overview underground exception lost");
            o.Restore();o.Initialize(review.transform);review.director.Sample(0);
            for(int i=0;i<review.director.residents.Length;i++)
            {
                var actor=review.director.residents[i];var skin=actor.root.GetComponentInChildren<SkinnedMeshRenderer>();
                var target=skin.bounds.center;var ray=new Ray(target+Vector3.up*3,Vector3.down);
                var hit=o.PickResident(ray,5);
                Check(hit&&hit.IsChildOf(actor.root),"Animated resident ray selection failed: "+i+" root="+actor.root.position+" bounds="+skin.bounds+" hit="+(hit?hit.name:"none"));
            }
            var fixture=GameObject.CreatePrimitive(PrimitiveType.Cube);fixture.name="Pick occluder";fixture.transform.SetParent(review.transform);
            try
            {
                var actor=review.director.residents[0];var target=actor.root.GetComponentInChildren<SkinnedMeshRenderer>().bounds.center;
                fixture.transform.position=target+Vector3.up*1.5f;fixture.transform.localScale=new Vector3(2,.2f,2);
                o.Initialize(review.transform);var ray=new Ray(target+Vector3.up*3,Vector3.down);
                Check(!o.PickResident(ray,5),"Opaque scenery permits click-through");
                fixture.SetActive(false);var hit=o.PickResident(ray,5);Check(hit&&hit.IsChildOf(actor.root),"Inactive scenery blocks picking");
            }
            finally{UnityEngine.Object.DestroyImmediate(fixture);o.Restore();}
            File.WriteAllText("Docs/ResidentOcclusionVerification.txt","PASS v0.26: overview ignores centered residents; single/double/slow/different-resident clicks; list focus and close; 6px drag threshold and no selection on release; focus release/overview; 25% focus fade and 0.2s original restoration; independent interior/underground exceptions; animated mesh picking for six actors; opaque blocker and inactive exclusion. Direct editor checks, not live Game input.\n");
            Debug.Log("RESIDENT_OCCLUSION_OK");
        }
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
}
