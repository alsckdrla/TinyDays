using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TinyDays.Review;

public static class ShadowQualityChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Tiny Days/Settings/Verify shadow quality presets")]
    public static void Execute()
    {
        const string key="TinyDays.ShadowQuality";
        bool hadPreference=PlayerPrefs.HasKey(key);int oldPreference=PlayerPrefs.GetInt(key);
        var oldPipeline=QualitySettings.renderPipeline;
        try
        {
            ShadowQualityPresetBuilder.InstallFarmReview();EditorSceneManager.OpenScene(FarmStudyBuilder.ScenePath);
            var review=UnityEngine.Object.FindObjectOfType<FarmStudyReview>();Check(review,"Farm review controller missing");
            var panel=(Rect)typeof(FarmStudyReview).GetMethod("SettingsPanel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(review,null);
            Check(panel.height==216&&184+24<=panel.height,"Shadow settings help text does not fit inside its panel");
            var settings=review.gameObject.AddComponent<ShadowQualitySettings>();settings.Configure(review.shadowLow,review.shadowBalanced,review.shadowHigh);
            CheckAsset(review.shadowLow,1024,1,false,0,60,"Low");
            CheckAsset(review.shadowBalanced,2048,2,true,0,60,"Balanced");
            CheckAsset(review.shadowHigh,4096,4,true,2,60,"High");
            PlayerPrefs.DeleteKey(key);settings.Configure(review.shadowLow,review.shadowBalanced,review.shadowHigh);
            Check(settings.Selected==1&&QualitySettings.renderPipeline==review.shadowBalanced,"Missing preference did not choose balanced shadow quality");
            settings.Apply(0);Check(settings.Selected==0&&QualitySettings.renderPipeline==review.shadowLow&&PlayerPrefs.GetInt(key)==0,"Low shadow selection failed");
            settings.Apply(2);Check(settings.Selected==2&&QualitySettings.renderPipeline==review.shadowHigh&&PlayerPrefs.GetInt(key)==2,"High shadow selection or persistence failed");
            PlayerPrefs.SetInt(key,0);settings.Configure(review.shadowLow,review.shadowBalanced,review.shadowHigh);
            Check(settings.Selected==0&&QualitySettings.renderPipeline==review.shadowLow,"Saved shadow selection did not restore");
            System.IO.File.WriteAllText("Docs/ShadowQualityVerification.txt","PASS v0.30: low 1024/1 cascade/hard/60m, balanced 2048/2 cascades/low soft/60m, high 4096/4 cascades/high soft/60m; missing preference selects balanced; immediate selection and saved preference restoration. Direct editor checks, not live Game input.\n");
            Debug.Log("TINYDAYS_SHADOW_QUALITY_OK");
        }
        catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
        finally
        {
            var testSettings=UnityEngine.Object.FindObjectOfType<ShadowQualitySettings>();if(testSettings)UnityEngine.Object.DestroyImmediate(testSettings);
            if(hadPreference)PlayerPrefs.SetInt(key,oldPreference);else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();QualitySettings.renderPipeline=oldPipeline;
        }
    }

    static void CheckAsset(UniversalRenderPipelineAsset asset,int resolution,int cascades,bool soft,int softQuality,float distance,string label)
    {
        Check(asset,label+" shadow asset missing");var data=new SerializedObject(asset);
        Check(data.FindProperty("m_MainLightShadowmapResolution").intValue==resolution,label+" shadow resolution mismatch");
        Check(data.FindProperty("m_ShadowCascadeCount").intValue==cascades,label+" cascade count mismatch");
        Check(data.FindProperty("m_SoftShadowsSupported").boolValue==soft,label+" soft shadow setting mismatch");
        Check(data.FindProperty("m_SoftShadowQuality").intValue==softQuality,label+" soft shadow quality mismatch");
        Check(Mathf.Abs(data.FindProperty("m_ShadowDistance").floatValue-distance)<.001f,label+" shadow distance mismatch");
    }
}
