using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TinyDays.Review;

public static class ShadowQualityPresetBuilder
{
    const string BalancedPath="Assets/Settings/TinyDaysURP.asset";
    const string LowPath="Assets/Settings/TinyDaysURP_ShadowLow.asset";
    const string HighPath="Assets/Settings/TinyDaysURP_ShadowHigh.asset";

    public struct Presets
    {
        public UniversalRenderPipelineAsset low,balanced,high;
    }

    [MenuItem("Tiny Days/Settings/Rebuild shadow quality presets")]
    public static void Rebuild(){EnsurePresets();Debug.Log("TINYDAYS_SHADOW_PRESETS_OK");}

    [MenuItem("Tiny Days/Settings/Install shadow quality in farm review")]
    public static void InstallFarmReview()
    {
        var scene=EditorSceneManager.OpenScene(FarmStudyBuilder.ScenePath);var root=GameObject.Find("GeneratedFarmStudy");
        if(!root)throw new System.InvalidOperationException("GeneratedFarmStudy is missing from FarmStudy.");
        var runtimeSettings=root.GetComponent<ShadowQualitySettings>();if(runtimeSettings)Object.DestroyImmediate(runtimeSettings);
        var review=root.GetComponent<FarmStudyReview>();if(!review)throw new System.InvalidOperationException("FarmStudyReview is missing from GeneratedFarmStudy.");
        var presets=EnsurePresets();review.shadowLow=presets.low;review.shadowBalanced=presets.balanced;review.shadowHigh=presets.high;
        EditorUtility.SetDirty(review);EditorSceneManager.SaveScene(scene);
    }

    public static Presets EnsurePresets()
    {
        var balanced=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(BalancedPath);
        if(!balanced)throw new System.InvalidOperationException("Balanced URP asset is missing.");
        Configure(balanced,2048,2,true,0,60);
        var low=LoadOrClone(LowPath,balanced);Configure(low,1024,1,false,0,60);
        var high=LoadOrClone(HighPath,balanced);Configure(high,4096,4,true,2,60);
        AssetDatabase.SaveAssets();
        return new Presets{low=low,balanced=balanced,high=high};
    }

    static UniversalRenderPipelineAsset LoadOrClone(string path,UniversalRenderPipelineAsset source)
    {
        var asset=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if(asset)return asset;
        asset=Object.Instantiate(source);asset.name=System.IO.Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset,path);return asset;
    }

    static void Configure(UniversalRenderPipelineAsset asset,int resolution,int cascades,bool soft,int softQuality,float distance)
    {
        var data=new SerializedObject(asset);
        data.FindProperty("m_MainLightShadowmapResolution").intValue=resolution;
        data.FindProperty("m_ShadowCascadeCount").intValue=cascades;
        data.FindProperty("m_SoftShadowsSupported").boolValue=soft;
        data.FindProperty("m_SoftShadowQuality").intValue=softQuality;
        data.FindProperty("m_ShadowDistance").floatValue=distance;
        data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);
    }
}
