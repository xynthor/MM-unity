Shader "MMUnity/TriplanarPBR"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _RoughnessTex ("Roughness", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _WorldScale ("World Scale", Float) = 0.45
        _Metallic ("Metallic", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        sampler2D _RoughnessTex;
        fixed4 _Tint;
        float _WorldScale, _Metallic;
        struct Input { float3 worldPos; float3 worldNormal; };
        fixed4 Tri(sampler2D tex, float3 p, float3 n)
        {
            float3 w = pow(abs(n), 4.0);
            w /= max(w.x + w.y + w.z, 0.0001);
            fixed4 x = tex2D(tex, p.zy * _WorldScale);
            fixed4 y = tex2D(tex, p.xz * _WorldScale);
            fixed4 z = tex2D(tex, p.xy * _WorldScale);
            return x*w.x + y*w.y + z*w.z;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 n = normalize(IN.worldNormal);
            fixed4 c = Tri(_MainTex, IN.worldPos, n) * _Tint;
            fixed rough = Tri(_RoughnessTex, IN.worldPos, n).r;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = saturate(1.0 - rough);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
