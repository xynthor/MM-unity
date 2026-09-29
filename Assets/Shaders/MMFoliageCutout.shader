Shader "MMUnity/FoliageCutout"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _AlphaTex ("Alpha", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.35
        _WindStrength ("Wind Strength", Range(0,0.3)) = 0.045
        _WindScale ("Wind Scale", Float) = 0.7
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        LOD 300
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff addshadow vertex:vert
        #pragma target 3.0
        sampler2D _MainTex;
        sampler2D _AlphaTex;
        fixed4 _Color;
        float _WindStrength, _WindScale;
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_AlphaTex;
        };

        void vert(inout appdata_full v)
        {
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            float h = saturate(v.vertex.y * 0.35 + 0.2);
            float sway = sin(_Time.y * 1.35 + (wp.x + wp.z) * _WindScale) * _WindStrength * h;
            v.vertex.x += sway;
            v.vertex.z += sway * 0.45;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed a = tex2D(_AlphaTex, IN.uv_AlphaTex).r * c.a;
            clip(a - _Cutoff);
            o.Albedo = c.rgb;
            o.Metallic = 0;
            o.Smoothness = 0.12;
            o.Alpha = a;
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/VertexLit"
}
