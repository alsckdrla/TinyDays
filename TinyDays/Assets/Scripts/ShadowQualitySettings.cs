using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TinyDays.Review
{
    // Runtime selection only. The authored URP assets remain the source of each preset's values.
    public sealed class ShadowQualitySettings : MonoBehaviour
    {
        const string PreferenceKey="TinyDays.ShadowQuality";
        UniversalRenderPipelineAsset low;
        UniversalRenderPipelineAsset balanced;
        UniversalRenderPipelineAsset high;
        int selected=1;

        public int Selected=>selected;

        void Awake(){}
        void OnDisable(){if(Application.isPlaying&&balanced)QualitySettings.renderPipeline=balanced;}

        public void Configure(UniversalRenderPipelineAsset lowPreset,UniversalRenderPipelineAsset balancedPreset,UniversalRenderPipelineAsset highPreset)
        {
            low=lowPreset;balanced=balancedPreset;high=highPreset;Apply(PlayerPrefs.GetInt(PreferenceKey,1),false);
        }

        public void Apply(int index,bool save=true)
        {
            selected=Mathf.Clamp(index,0,2);
            UniversalRenderPipelineAsset preset=selected==0?low:selected==2?high:balanced;
            if(!preset)return;
            QualitySettings.renderPipeline=preset;
            if(save){PlayerPrefs.SetInt(PreferenceKey,selected);PlayerPrefs.Save();}
        }
    }
}
