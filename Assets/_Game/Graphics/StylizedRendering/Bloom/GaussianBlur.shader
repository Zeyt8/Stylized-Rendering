Shader "Custom/GaussianBlur"
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
            Name "GaussianBlur"
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
                float2 offset = float2(_BlurSize / _ScreenParams.x, _BlurSize / _ScreenParams.y);

                float kernel[7][7] = {
                    { 1.0,  6.0,  15.0,  20.0,  15.0,  6.0,  1.0 },
                    { 6.0, 36.0,  90.0, 120.0,  90.0, 36.0,  6.0 },
                    { 15.0, 90.0, 225.0, 300.0, 225.0, 90.0, 15.0 },
                    { 20.0,120.0, 300.0, 400.0, 300.0,120.0, 20.0 },
                    { 15.0, 90.0, 225.0, 300.0, 225.0, 90.0, 15.0 },
                    { 6.0, 36.0,  90.0, 120.0,  90.0, 36.0,  6.0 },
                    { 1.0,  6.0,  15.0,  20.0,  15.0,  6.0,  1.0 }
                };

                float kernelSum = 4096.0;

                for (int x = -3; x <= 3; x++)
                {
                    for (int y = -3; y <= 3; y++)
                    {
                        float2 sampleOffset = float2(x * offset.x, y * offset.y);
                        color += tex2D(_MainTex, uv + sampleOffset) * kernel[x + 3][y + 3];
                    }
                }

                color /= kernelSum;

                return color;
            }
            ENDCG
        }
    }
}
