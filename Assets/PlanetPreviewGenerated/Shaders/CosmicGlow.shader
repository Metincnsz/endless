Shader "PlanetPreview/CosmicGlow"
{
    Properties
    {
        [HDR] _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Atmosphere ("Atmosphere Mode", Float) = 0
        _FresnelPower ("Fresnel Power", Range(1, 12)) = 5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "Glow"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            Cull Off
            ZWrite Off
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; float4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Atmosphere;
                float _FresnelPower;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                if (_Atmosphere > 0.5)
                {
                    float3 perspectiveView = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    float3 orthoView = normalize(-GetViewForwardDir());
                    float3 viewDirection = normalize(lerp(perspectiveView, orthoView, unity_OrthoParams.w));
                    float edge = 1.0 - saturate(dot(normalize(input.normalWS), viewDirection));
                    return half4(_Tint.rgb, pow(edge, _FresnelPower) * _Tint.a);
                }
                float2 p = input.uv * 2.0 - 1.0;
                float d = dot(p, p);
                float softness = exp(-3.0 * d) * saturate(1.0 - d);
                return half4(input.color.rgb * _Tint.rgb, input.color.a * _Tint.a * softness);
            }
            ENDHLSL
        }
    }
}
