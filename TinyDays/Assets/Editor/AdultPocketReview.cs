using System.IO;
using UnityEngine;
using UnityEditor.SceneManagement;
using TinyDays.Review;
public static class AdultPocketReview {
    public static void Finish(){
        try{
            AdultRabbitBuilder.Execute();AdultRabbitBuilder.BuildPlayer();
            AdultRabbitMotionBuilder.BuildOnly();After();
            AdultSideRiseChecks.Execute();AdultRabbitMotionBuilder.BuildPlayer();
            Debug.Log("POCKET_REVIEW_OK");
        }catch(System.Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);}
    }
    public static void Before(){Capture("Before");}
    public static void After(){Capture("After");}
    static void Capture(string stage){
        EditorSceneManager.OpenScene("Assets/Scenes/AdultRabbitMotionStudy.unity");
        var r=Object.FindObjectOfType<AdultRabbitMotionReview>();
        string dir="Docs/Captures/PocketRemoval";Directory.CreateDirectory(dir);
        foreach(int clip in new[]{22,28}){
            r.Select(clip);r.Sample(0);
            var focus=new Vector3(0,.5f,-.1f);
            r.reviewCamera.transform.position=focus+new Vector3(2.8f,3.2f,3.6f);r.reviewCamera.transform.LookAt(focus);
            AdultSleepChecks.Capture(r,$"{dir}/{stage}_{clip}.png");
        }
        Debug.Log("POCKET_CAPTURE_OK "+stage);
    }
}
