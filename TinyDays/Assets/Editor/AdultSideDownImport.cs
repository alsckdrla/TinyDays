using UnityEditor;
using UnityEngine;

// Only the two new dense sampled takes: FBX auto tangents can overshoot a
// planted hand between samples. Do not change any legacy animation curves.
public sealed class AdultSideDownImport : AssetPostprocessor {
    void OnPostprocessAnimation(GameObject root,AnimationClip clip){
        if(!assetPath.EndsWith("/AdultRabbitMotion.fbx"))return;
        if(!clip.name.EndsWith("Adult_Stand_To_Left")&&!clip.name.EndsWith("Adult_Sit_To_Left"))return;
        foreach(var binding in AnimationUtility.GetCurveBindings(clip)){
            var curve=AnimationUtility.GetEditorCurve(clip,binding);
            for(int i=0;i<curve.length;i++){
                AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip,binding,curve);
        }
    }
}
