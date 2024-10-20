Shader "Custom/FullscreenCrosshatchBloom"
{
    Properties
    {
        _BloomThreshold ("Bloom Threshold", Range(0.0, 10.0)) = 1.0
        _BloomCrosshatchThickness ("Crosshatch Line Thickness", Range(0.01, 0.2)) = 0.05
        _BloomCrosshatchSpacing ("Crosshatch Line Spacing", Range(0, 0.1)) = 0.1
        _BloomScale ("Bloom Scale", Range(0, 100)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Overlay" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "FullscreenCrosshatchBloom"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float _BloomThreshold;
            float _BloomCrosshatchThickness;
            float _BloomCrosshatchSpacing;
            float _BloomScale;

            float CalculateLuminance(float3 color)
            {
                return dot(color, float3(0.2126, 0.7152, 0.0722));
            }

            float ComputeCrosshatch(float2 uv, float thickness, float spacing)
            {
                float horizontal = abs(frac(uv.y / spacing) - 0.5);
                float vertical = abs(frac(uv.x / spacing) - 0.5);

                float hLine = step(horizontal, thickness);
                float vLine = step(vertical, thickness);

                return max(hLine, vLine);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 baseColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, IN.texcoord);

                // Calculate luminance of the base color
                float luminance = CalculateLuminance(baseColor.rgb);

                float2 offsets[8] = {
                    float2(-1, 0), float2(1, 0),
                    float2(0, -1), float2(0, 1),
                    float2(-1, -1), float2(1, 1),
                    float2(-1, 1), float2(1, -1)
                };

                float isBright = step(_BloomThreshold, luminance);
                float3 bloomColor = baseColor.rgb * isBright;

                for (int i = 0; i < 8; i++)
                {
                    float2 neighborUV = IN.texcoord + offsets[i] * _BloomScale / _ScreenParams.xy;
                    float4 neighborColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, neighborUV);
                    float neighborLuminance = CalculateLuminance(neighborColor.rgb);

                    if (neighborLuminance >= _BloomThreshold)
                    {
                        bloomColor += neighborColor.rgb;
                        isBright = 1.0;
                    }
                }

                bloomColor /= max(1.0, 1.0 + step(0.0, isBright)); 

                float2 crosshatchUV = IN.texcoord;
                crosshatchUV.x *= _ScreenParams.x / _ScreenParams.y;
                float crosshatch = ComputeCrosshatch(crosshatchUV, _BloomCrosshatchThickness, _BloomCrosshatchSpacing);

                float3 emissiveResult = crosshatch * bloomColor;
                return half4(emissiveResult, 1.0) + (1.0 - isBright * crosshatch) * half4(baseColor.rgb, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
