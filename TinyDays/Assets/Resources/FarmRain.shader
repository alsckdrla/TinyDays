Shader "TinyDays/FarmRain"
{
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; half4 color:COLOR; float height:TEXCOORD0; };
            V Vert(A a) { V o; float3 p=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(p);o.height=p.y;o.color=a.color;return o; }
            half4 Frag(V i):SV_Target { clip(i.height-.08);return i.color; }
            ENDHLSL
        }
    }
}
