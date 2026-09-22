using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TinyDays.Review;

public static class FarmCameraTargetingChecks
{
    static void Require(bool valid,string message){if(!valid)throw new Exception(message);}

    public static void Execute()
    {
        try
        {
            EditorSceneManager.OpenScene(FarmStudyBuilder.ScenePath);
            var review=UnityEngine.Object.FindObjectOfType<FarmStudyReview>();
            review.ShowOverview();review.ResetCameraToPreset();
            var occlusion=review.GetComponent<FarmCameraOcclusion>();var camera=review.reviewCamera;
            Require(occlusion.TryPickStaticSurface(camera.ViewportPointToRay(new Vector3(.5f,.5f,0)),camera.farClipPlane,out Vector3 center),"Overview center did not find static scenery");
            var orbit=typeof(FarmStudyReview).GetMethod("BeginOverviewOrbitAtScreenCenter",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var apply=typeof(FarmStudyReview).GetMethod("ApplyCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Require(orbit!=null&&apply!=null,"Overview orbit helpers missing");
            Vector3 beforeOrbit=camera.transform.position;orbit.Invoke(review,null);apply.Invoke(review,null);
            Require((camera.transform.position-beforeOrbit).sqrMagnitude<.0001f,"Overview target changed the current camera position");
            Require(occlusion.TryGetMeadowSurface(new Vector3(0,0,.55f),out float meadowHeight),"Meadow center was not found");
            Require(Mathf.Abs(meadowHeight)<.1f,"Unexpected meadow height");
            var inside=new Vector3(0,2,.55f);var outside=new Vector3(100,2,.55f);
            Vector3 clamped=occlusion.ClampToMeadow(inside,outside);
            Require(occlusion.TryGetMeadowSurface(clamped,out _),"Overview pan target escaped meadow");
            Require(Mathf.Abs(clamped.y-outside.y)<.001f,"Planar meadow clamp changed target height");
            Vector3 drag=FarmStudyReview.GroundScreenPan(Quaternion.Euler(42,35,0),new Vector2(90,-45),8,40,900);
            Require(Mathf.Abs(drag.y)<.001f,"Mouse pan is not level with the ground");
            var pivotField=typeof(FarmStudyReview).GetField("pivot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var elevate=typeof(FarmStudyReview).GetMethod("ElevateCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Require(pivotField!=null&&elevate!=null,"Camera target or elevator helper missing");
            review.ShowOverview();review.ResetCameraToPreset();Vector3 pivotBefore=(Vector3)pivotField.GetValue(review);Vector3 cameraBefore=camera.transform.position;Quaternion rotationBefore=camera.transform.rotation;
            elevate.Invoke(review,new object[]{3f});apply.Invoke(review,null);
            Require((camera.transform.position-cameraBefore-Vector3.up*3f).sqrMagnitude<.0001f,"Overview elevator was not world-vertical");
            Require(Quaternion.Angle(camera.transform.rotation,rotationBefore)<.001f,"Overview elevator tilted the camera");
            Require(((Vector3)pivotField.GetValue(review)-pivotBefore).sqrMagnitude>.0001f,"Overview elevator did not refresh its target");
            review.FocusResident(0);review.ResetCameraToPreset();cameraBefore=camera.transform.position;rotationBefore=camera.transform.rotation;
            elevate.Invoke(review,new object[]{2f});apply.Invoke(review,null);
            Require(review.IsFollowing,"Elevator released resident focus");
            Require((camera.transform.position-cameraBefore-Vector3.up*2f).sqrMagnitude<.0001f,"Focused elevator was not world-vertical");
            Require(Quaternion.Angle(camera.transform.rotation,rotationBefore)<.001f,"Focused elevator tilted the camera");
            var anchor=review.director.residents[0].root.position+Vector3.up*.8f;
            Vector3 focused=occlusion.ClampToMeadow(anchor,anchor+new Vector3(100,3,0));
            Require(occlusion.TryGetMeadowSurface(focused,out _),"Focused pan target escaped meadow");
            Require(Mathf.Abs(focused.y-(anchor.y+3))<.001f,"Focused clamp changed framing height");
            review.ShowOverview();
            File.WriteAllText("Docs/CameraTargetingVerification.txt","v0.63 PASS: generated mesh center target excludes backdrop/residents; Spring meadow mesh defines planar pan boundary; overview and focused targets clamp at the terrain edge; mouse pan remains level; elevator height motion is world-vertical without tilt, refreshes overview target, and preserves resident focus. Direct editor checks, not live Game input.\n");
            Debug.Log("CAMERA_TARGETING_OK");
        }
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
    }
}
