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
float4 _MainTex_TexelSize;
float _BlurSize;
float _Sigma;
float2 _Dir;

v2f vert (appdata_t v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.screenPos = ComputeScreenPos(o.vertex);
    return o;
}

float GaussianBlurWeight(int x, int y, float sigma)
{
    int sqrDst = x * x + y * y;
    float c = 2 * sigma * sigma;
    return exp(-sqrDst / c);
}

float4 GaussianBlur(float2 uv, float2 dir, int blurSize, float sigma)
{
    float4 sum = 0;
    float weightSum = 0;
    float2 texelDelta = _MainTex_TexelSize.xy * dir;

    for (int x = -blurSize; x <= blurSize; x++)
    {
        float2 uv2 = uv + texelDelta * x;
        float4 s = tex2D(_MainTex, uv2);

        float weight = GaussianBlurWeight(x, 0, sigma);
        weightSum += weight;
        sum += s * weight;
    }

    return sum / weightSum;
}