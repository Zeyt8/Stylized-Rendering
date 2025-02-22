Shader "Custom/GaussianBlurHorizontal"
{
    Properties
    {
        [HideInInspector] _MainTex ("Base (RGB)", 2D) = "white" {}
        [HideInInspector] _BlurSize ("Blur Size", Float) = 1.0
    }
    SubShader
    {
        Pass
        {
            Name "GaussianBlurHorizontal"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 screenPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _BlurSize;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                fixed4 color = fixed4(0, 0, 0, 0);
                float2 offset = float2(_BlurSize / _ScreenParams.x, 0);

                float kernel[15] = {
                    0.5, 2.4, 9.2, 27.8, 65.6, 121.0, 174.7, 197.4, 174.7, 121.0, 65.6, 27.8, 9.2, 2.4, 0.5
                };

                for (int x = -7; x <= 7; x++)
                {
                    float2 sampleOffset = float2(x * offset.x, 0);
                    color += tex2D(_MainTex, uv + sampleOffset) * kernel[x + 7];
                }

                color /= 1000;

                return color;
            }
            ENDCG
        }
    }
}