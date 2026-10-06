Shader "TinyDays/FarmSeason"
{
    Properties {
        [MainColor] _BaseColor("Color", Color)=(1,1,1,1)
        _BaseMap("Base",2D)="white" {}
        _SnowAmount("Snow",Range(0,1))=0
        _Surface("Surface",Float)=0
        _SrcBlend("Source",Float)=1
        _DstBlend("Destination",Float)=0
        _ZWrite("Depth",Float)=1
        _Cull("Cull",Float)=2
        _Cutoff("Cutoff",Float)=0.5
    }
    SubShader {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;float _SnowAmount;
            CBUFFER_END
            struct A {float4 vertex:POSITION;float3 normal:NORMAL;};
            struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half fog:TEXCOORD2;};
            V Vert(A a){V o;o.world=TransformObjectToWorld(a.vertex.xyz);o.position=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normal);o.fog=ComputeFogFactor(o.position.z);return o;}
            half4 Frag(V i):SV_Target {
                half3 n=normalize(i.normal);half snow=smoothstep(.25,.8,n.y)*_SnowAmount;
                half3 albedo=lerp(_BaseColor.rgb,half3(.9,.94,.98),snow);
                Light light=GetMainLight(TransformWorldToShadowCoord(i.world));
                half3 illumination=SampleSH(n)+light.color*saturate(dot(n,light.direction))*light.shadowAttenuation;
                return half4(MixFog(albedo*illumination,i.fog),_BaseColor.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
