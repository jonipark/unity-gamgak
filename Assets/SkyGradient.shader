Shader "Custom/SkyGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.5, 0.7, 1, 0)
        _BottomColor ("Bottom Color", Color) = (1, 0.5, 0.2, 0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            Cull Off
            Lighting Off
            Fog { Mode Off }
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _TopColor;
            fixed4 _BottomColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return lerp(_BottomColor, _TopColor, i.uv.y);
            }
            ENDCG
        }
    }
}
