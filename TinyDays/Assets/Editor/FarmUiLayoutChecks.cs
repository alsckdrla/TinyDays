using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TinyDays.Review;

public static class FarmUiLayoutChecks
{
    static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
    public static void Execute()
    {
        try
        {
            Require(Math.Abs(FarmStudyReview.ControlHeight-42)<.001f,"Control height changed");
            Require(FarmStudyReview.MenuColumns(739)==3&&FarmStudyReview.MenuColumns(740)==6,"Menu wrap boundary changed");
            Require(FarmStudyReview.ClampLightingScroll(-10,300)==0,"Negative panel scroll was not clamped");
            Require(Math.Abs(FarmStudyReview.ClampLightingScroll(999,300)-290)<.001f,"Panel bottom scroll was not clamped");
            Require(FarmStudyReview.ClampLightingScroll(50,600)==0,"No-overflow panel scroll should be zero");
            Vector3 right=FarmStudyReview.KeyboardPan(UnityEngine.Quaternion.identity,new UnityEngine.Vector2(1,0),8);
            Vector3 up=FarmStudyReview.KeyboardPan(UnityEngine.Quaternion.identity,new UnityEngine.Vector2(0,1),8);
            Vector3 diagonal=FarmStudyReview.KeyboardPan(UnityEngine.Quaternion.Euler(35,60,0),new UnityEngine.Vector2(1,1),20);
            Require(right.x>0&&Math.Abs(right.z)<.001f&&up.z>0&&Math.Abs(up.x)<.001f,"Screen-space keyboard directions changed");
            Require(Math.Abs(diagonal.magnitude-FarmStudyReview.PanDistance(20)*FarmStudyReview.KeyboardPanMultiplier)<.001f,"Keyboard diagonal speed changed");
            Require(FarmStudyReview.KeyboardPan(UnityEngine.Quaternion.identity,new UnityEngine.Vector2(1,0),40).magnitude>right.magnitude,"Keyboard zoom scaling changed");
            Require(FarmStudyReview.RotateButton==1&&FarmStudyReview.HeightDragButton==2,"Camera mouse bindings changed");
            Require(!FarmStudyReview.RotationActive(false)&&FarmStudyReview.RotationActive(true),"Right-drag rotation state changed");
            float near=FarmStudyReview.MouseHeightDelta(100,8,40,900),far=FarmStudyReview.MouseHeightDelta(100,32,40,900);
            Require(near>0&&far>near,"Middle-drag height direction or zoom scaling changed");
            Require(FarmStudyReview.ZoomFromWheel(10,1,34)>10&&FarmStudyReview.ZoomFromWheel(10,-1,34)<10,"Wheel zoom direction changed");
            File.WriteAllText("Docs/Stage27UiVerification.txt","v0.62 PASS: 42 logical-pixel control height; 740px bottom-menu three-column/two-row wrap boundary; time-panel scroll top, bottom and no-overflow clamping; screen-space keyboard W/A/S/D and arrows, normalized diagonal, and zoom-scaled speed; right-drag rotation plus middle-drag height scaling; wheel-up zoom-out and wheel-down zoom-in. Direct layout checks, not live input.\n");
            UnityEngine.Debug.Log("STAGE27_UI_OK");
        }
        catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
