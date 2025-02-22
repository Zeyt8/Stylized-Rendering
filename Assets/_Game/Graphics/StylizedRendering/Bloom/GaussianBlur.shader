Shader "Custom/GaussianBlur"
{
    Properties
    {
        [HideInInspector] _MainTex ("Base (RGB)", 2D) = "white" {}
        [HideInInspector] _BlurSize ("Blur Size", Float) = 1.0
        [HideInInspector] _Sigma ("Sigma", Float) = 1.0
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
            #include "GaussianPass.hlsl"


            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                return GaussianBlur(uv, _Dir, _BlurSize, _Sigma);
            }
            ENDCG
        }
    }
}