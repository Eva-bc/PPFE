Shader "Custom/GhostFresnel_URP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.5,0,1,1)
        _EmissionColor ("Emission Color", Color) = (1,0,1,1)
        _FresnelPower ("Fresnel Power", Float) = 3
        _FresnelIntensity ("Fresnel Intensity", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _EmissionColor;
            float _FresnelPower;
            float _FresnelIntensity;

            Varyings vert (Attributes v)
            {
                Varyings o;

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionHCS = TransformWorldToHClip(worldPos);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float fresnel = pow(1.0 - saturate(dot(i.normalWS, i.viewDirWS)), _FresnelPower);

                float3 emission = _EmissionColor.rgb * fresnel * _FresnelIntensity;
                float3 color = _BaseColor.rgb;

                return float4(color + emission, fresnel);
            }

            ENDHLSL
        }
    }
}