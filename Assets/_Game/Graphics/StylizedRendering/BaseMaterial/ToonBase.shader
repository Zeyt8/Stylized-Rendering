Shader "Custom/ToonBase"
{
    Properties
    {
        //[Header("Albedo")]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        //[Header("Lighting")]
        _DiffuseStepThreshold ("Diffuse Step Threshold", Range(0.0, 1.0)) = 0.5
        _Shininess ("Shininess", Range(0.0, 128.0)) = 32.0
        _SpecularStepThreshold ("Specular Step Threshold", Range(0.0, 1.0)) = 0.5
        //[Header("Shadows")]
        [NoScaleOffset] _StipplingTex ("Stippling Texture", 2D) = "white" {}
        _StipplingScale ("Stippling Scale", Range(0.0, 1.0)) = 0.5
        //[Header("Transparency")]
        [NoScaleOffset] _TransparencyTex ("Transparency Texture", 2D) = "white" {}
        _TransparencyDotsScale ("Transparency Dots Scale", Range(0.0, 20)) = 0.5
        //[Header("Emission and Bloom")]
        [HDR] _EmissiveColor ("Emissive Color", Color) = (0, 0, 0, 0)
        _BloomThreshold ("Bloom Threshold", Range(0.0, 10)) = 1
        _BloomCrosshatchThickness ("Bloom Crosshatch Thickness", Range(0.0, 0.5)) = 0.05
        _BloomCrosshatchSpacing ("Bloom Crosshatch Spacing", Range(0.0, 0.1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        UsePass "Universal Render Pipeline/Lit/DepthOnly"

        UsePass "Universal Render Pipeline/Lit/DepthNormals"

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Color;
            float _DiffuseStepThreshold;
            float _Shininess;
            float _SpecularStepThreshold;
            TEXTURE2D(_StipplingTex);
            SAMPLER(sampler_StipplingTex);
            float _StipplingScale;
            TEXTURE2D(_TransparencyTex);
            SAMPLER(sampler_TransparencyTex);
            float _TransparencyDotsScale;
            float _BloomThreshold;
            float _BloomCrosshatchThickness;
            float _BloomCrosshatchSpacing;
            float4 _EmissiveColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shadowCoords : TEXCOORD3;
                float2 screenPos : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.shadowCoords = GetShadowCoord(positions);
                float4 posScreenSpace = TransformObjectToHClip(IN.positionOS);
                OUT.screenPos = posScreenSpace.xy / posScreenSpace.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                OUT.screenPos.x *= aspect;

                return OUT;
            }

            half CalculateShadowAmount(float4 shadowCoords, float3 positionWS)
            {
                float totalShadow = 0;

                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; i++)
                {
                    Light additionalLight = GetAdditionalLight(i, positionWS);
                    half additionalLightShadow = AdditionalLightRealtimeShadow(i, positionWS);
                    totalShadow += additionalLightShadow * additionalLight.distanceAttenuation;
                }

                Light mainLight = GetMainLight(shadowCoords);
                half mainLightShadow = MainLightRealtimeShadow(shadowCoords);
                totalShadow += mainLightShadow * mainLight.shadowAttenuation;

                return totalShadow;
            }

            float3 DiffuseLighting(float3 normalWS, float3 positionWS, float stepThreshold)
            {
                float3 diffuseLight = float3(0, 0, 0);

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));

                NdotL = round(NdotL / stepThreshold) * stepThreshold;
                NdotL = max(NdotL, 0.1);
                diffuseLight += NdotL * mainLight.color;

                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; i++)
                {
                    Light additionalLight = GetAdditionalLight(i, positionWS);
                    NdotL = saturate(dot(normalWS, additionalLight.direction));

                    NdotL = round(NdotL / stepThreshold) * stepThreshold;
                    NdotL = max(NdotL, 0.1);
                    diffuseLight += NdotL * additionalLight.color;
                }

                return diffuseLight;
            }

            float3 SpecularLighting(float3 normalWS, float3 viewDir, float shininess, float specularStepThreshold)
            {
                float3 specularLight = float3(0, 0, 0);

                Light mainLight = GetMainLight();
                float3 reflectedLightDir = reflect(-mainLight.direction, normalWS);
                float NdotR = saturate(dot(reflectedLightDir, viewDir));
                NdotR = round(NdotR / specularStepThreshold) * specularStepThreshold;

                specularLight += pow(NdotR, shininess) * mainLight.color;

                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; i++)
                {
                    Light additionalLight = GetAdditionalLight(i, viewDir);
                    reflectedLightDir = reflect(-additionalLight.direction, normalWS);
                    NdotR = saturate(dot(reflectedLightDir, viewDir));
                    NdotR = round(NdotR / specularStepThreshold) * specularStepThreshold;

                    specularLight += pow(NdotR, shininess) * additionalLight.color;
                }

                return specularLight;
            }

            float CalculateLuminance(float3 color)
            {
                return dot(color, float3(0.2126729, 0.7151522, 0.0721750));
            }

            float4 TriplanarSample(TEXTURE2D_PARAM(tex, texSample), float3 worldPosition, float3 normalWS, float scale)
            {
                float3 absNormal = abs(normalize(normalWS));
                float3 blendWeights = absNormal / (absNormal.x + absNormal.y + absNormal.z);
            
                float4 xSample = SAMPLE_TEXTURE2D(tex, texSample, worldPosition.yz * scale);
                float4 ySample = SAMPLE_TEXTURE2D(tex, texSample, worldPosition.xz * scale);
                float4 zSample = SAMPLE_TEXTURE2D(tex, texSample, worldPosition.xy * scale);
            
                return (xSample * blendWeights.x) + (ySample * blendWeights.y) + (zSample * blendWeights.z);
            }

            float ComputeCrosshatch(float2 screenPos, float lineWidth, float lineSpacing)
            {
                float horizontal = abs(frac(screenPos.y / lineSpacing) - 0.5);
                float vertical = abs(frac(screenPos.x / lineSpacing) - 0.5);

                float hLine = step(horizontal, lineWidth);
                float vLine = step(vertical, lineWidth);

                return max(hLine, vLine);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Object Color
                float4 sampledTexture = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 baseColor = sampledTexture.rgba * _Color.rgba;

                // Ambient Light
                float3 ambientLight = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

                // Diffuse Light
                float3 diffuseLit = DiffuseLighting(normalize(IN.normalWS), IN.positionWS, _DiffuseStepThreshold);

                // Specular Light
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 specularLit = SpecularLighting(normalize(IN.normalWS), viewDir, _Shininess, _SpecularStepThreshold);

                // Shadows
                half shadowAmount = CalculateShadowAmount(IN.shadowCoords, IN.positionWS);
                float luminance = CalculateLuminance(diffuseLit * shadowAmount + ambientLight);
                float stipplingValue = TriplanarSample(TEXTURE2D_ARGS(_StipplingTex, sampler_StipplingTex), IN.positionWS, IN.normalWS, _StipplingScale).r;
                float ditheringFactor = step(stipplingValue, luminance);

                // Transparency
                half halftonePattern = SAMPLE_TEXTURE2D(_TransparencyTex, sampler_TransparencyTex, IN.screenPos * _TransparencyDotsScale).r;
                if (halftonePattern < 1 - baseColor.a)
                {
                    discard;
                }

                // Bloom
                float4 finalColor = float4((baseColor.rgb * diffuseLit + ambientLight + specularLit) * ditheringFactor, 1.0);
                float totalLuminance = CalculateLuminance(finalColor + _EmissiveColor);
                float crosshatch = ComputeCrosshatch(IN.screenPos, _BloomCrosshatchThickness, _BloomCrosshatchSpacing);
                crosshatch *= step(_BloomThreshold, totalLuminance);

                return half4(crosshatch * _EmissiveColor.rgb, 1.0) + (1 - crosshatch) * finalColor.rgba;
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
