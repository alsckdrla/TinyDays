Shader "TinyDays/LightingBackdrop"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color)=(1,1,1,1)
        [HideInInspector] _SrcBlend("Source blend", Float)=1
        [HideInInspector] _DstBlend("Destination blend", Float)=0
        [HideInInspector] _ZWrite("Depth write", Float)=1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _BaseColor;
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; };
            Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);return o;}
            half4 frag(Varyings i):SV_Target{return _BaseColor;}
            ENDHLSL
        }
    }
}
