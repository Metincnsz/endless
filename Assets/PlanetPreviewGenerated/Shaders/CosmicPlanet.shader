Shader "PlanetPreview/CosmicPlanet"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.001, 0.009, 0.024, 1)
        [HDR] _RimColor ("Rim Color", Color) = (0, 0.65, 1.4, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.15
        _RimStrength ("Rim Strength", Range(0, 5)) = 1.65
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Planet"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 perspectiveView = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 orthoView = normalize(-GetViewForwardDir());
                float3 viewDirection = normalize(lerp(perspectiveView, orthoView, unity_OrthoParams.w));
                float edge = 1.0 - saturate(dot(n, viewDirection));
                float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_V, n));
                float illumination = 0.25 + 0.75 * saturate(dot(normalVS, normalize(float3(0.65, 0.7, 0.1))));
                float rim = pow(edge, _RimPower) * illumination;
                float softBlue = pow(edge, 0.8) * 0.07;
                return half4(_BaseColor.rgb + _RimColor.rgb * (rim * _RimStrength + softBlue), 1);
            }
            ENDHLSL
        }
    }
}
