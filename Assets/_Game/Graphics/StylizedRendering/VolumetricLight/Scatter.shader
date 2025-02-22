Shader "Hidden/RadialBlur"
{
    Properties
    {
        [HideInInspector] _OcclusionTex ("Texture", 2D) = "white" {}
        [HideInInspector] _BlurWidth("Blur Width", Range(0,1)) = 0.85
        [HideInInspector] _Intensity("Intensity", Range(0,1)) = 1
        [HideInInspector] _Center("Center", Vector) = (0.5,0.5,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 screenPos : TEXCOORD0;
            };

            TEXTURE2D(_OcclusionTex);
            SAMPLER(sampler_OcclusionTex);
            float _BlurWidth;
            float _Intensity;
            float4 _Center;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float4 posScreenSpace = TransformObjectToHClip(v.positionOS);
                o.screenPos = posScreenSpace.xy / posScreenSpace.w;
                o.screenPos = o.screenPos * 0.5 + 0.5;
                o.screenPos.y = 1.0 - o.screenPos.y;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float4 color = float4(0.0f, 0.0f, 0.0f, 1.0f);
                float2 ray = i.screenPos - _Center.xy;

                for (int i = 0; i < 100; i++)
                {
                    float scale = 1.0f - _BlurWidth * (float(i) / float(99));
                    color.xyz += SAMPLE_TEXTURE2D(_OcclusionTex, sampler_OcclusionTex, (ray * scale) + _Center.xy).xyz / float(100);
                }

                return color * _Intensity;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
