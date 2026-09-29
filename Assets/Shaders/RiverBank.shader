Shader "MMUnity/RiverBank"
{
    Properties
    {
        _Color ("Color", Color) = (0.3,0.27,0.16,1)
        _MainTex ("Albedo", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = 0;
            o.Smoothness = 0.03;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
