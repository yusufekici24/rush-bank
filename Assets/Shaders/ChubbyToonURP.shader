Shader "RushBank/Chubby Toon URP"
{
    Properties
    {
        [MainTexture] _BaseMap ("Main Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 0.86, 0.62, 1)
        _VertexColorStrength ("Vertex Color Strength", Range(0, 1)) = 0

        _WarmLightColor ("Warm Light Color", Color) = (1, 0.88, 0.62, 1)
        _ShadowColor ("Soft Shadow Color", Color) = (0.58, 0.68, 0.78, 1)
        _ToonSmoothness ("Toon Smoothness", Range(0.01, 1)) = 0.45
        _ShadowLift ("Shadow Lift", Range(0, 1)) = 0.32

        _RimColor ("Rim Color", Color) = (0.9, 1, 0.92, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.45
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _VertexColorStrength;
                half4 _WarmLightColor;
                half4 _ShadowColor;
                half _ToonSmoothness;
                half _ShadowLift;
                half4 _RimColor;
                half _RimPower;
                half _RimIntensity;
                half _RimThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : COLOR;
                float4 shadowCoord : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.shadowCoord = GetShadowCoord(positionInputs);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);

                Light mainLight = GetMainLight(input.shadowCoord);
                half ndotl = saturate(dot(normalWS, mainLight.direction));

                // Wide smoothstep keeps the look soft and plush instead of harsh cel shading.
                half softBand = smoothstep(0.0h, max(0.01h, _ToonSmoothness), ndotl);
                half shadowedBand = lerp(_ShadowLift, 1.0h, softBand) * mainLight.shadowAttenuation;

                half4 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 vertexTint = lerp(half3(1.0h, 1.0h, 1.0h), input.color.rgb, _VertexColorStrength);
                half3 albedo = textureColor.rgb * _BaseColor.rgb * vertexTint;

                half3 warmDiffuse = lerp(_ShadowColor.rgb, _WarmLightColor.rgb, shadowedBand);
                half3 litColor = albedo * warmDiffuse * mainLight.color;

                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), _RimPower);
                half rimMask = smoothstep(_RimThreshold, 1.0h, fresnel);
                half3 rim = _RimColor.rgb * rimMask * _RimIntensity;

                return half4(litColor + rim, textureColor.a * _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(
                    positionInputs.positionWS,
                    normalInputs.normalWS,
                    _MainLightPosition.xyz));
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
