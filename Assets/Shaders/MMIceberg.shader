Shader "MMUnity/Iceberg"
{
 Properties
 {
  _Color ("Color", Color) = (1,1,1,1)
  _MainTex ("Albedo", 2D) = "white" {}
  _BumpMap ("Normal", 2D) = "bump" {}
  _BumpScale ("Normal Strength", Range(0,2)) = 0.55
  _Glossiness ("Smoothness", Range(0,1)) = 0.18
 }
 SubShader
 {
  Tags { "RenderType"="Opaque" "Queue"="Geometry" }
  LOD 250
  Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex;
  sampler2D _BumpMap;
  fixed4 _Color;
  half _BumpScale;
  half _Glossiness;
  struct Input { float2 uv_MainTex; float2 uv_BumpMap; };
  void surf(Input IN, inout SurfaceOutputStandard o)
  {
   fixed4 c=tex2D(_MainTex,IN.uv_MainTex)*_Color;
   o.Albedo=c.rgb;
   o.Metallic=0;
   o.Smoothness=_Glossiness;
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
   o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Diffuse"
}