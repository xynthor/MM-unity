Shader "MMUnity/Linked Two Sided Leaves" {
 Properties {
  _MainTex("Albedo and coverage",2D)="white" {}
  _Color("Tint",Color)=(1,1,1,1)
  _Cutoff("Alpha cutoff",Range(0,1))=.34
  _BumpMap("Normal",2D)="bump" {}
  _BumpScale("Normal strength",Float)=1
  _Glossiness("Smoothness",Range(0,1))=.18
  _Metallic("Metallic",Range(0,1))=0
 }
 SubShader {
  Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"}
  Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff addshadow
  #pragma target 3.0
  #pragma multi_compile_instancing
  sampler2D _MainTex,_BumpMap;
  fixed4 _Color;
  half _BumpScale,_Glossiness,_Metallic;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float facing:VFACE;};
  void surf(Input i,inout SurfaceOutputStandard o) {
   fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;
   o.Albedo=c.rgb;o.Alpha=c.a;
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_BumpMap),_BumpScale);
   o.Normal.z*=i.facing>=0?1:-1;
   o.Metallic=_Metallic;o.Smoothness=_Glossiness;
  }
  ENDCG
 }
 FallBack "Transparent/Cutout/VertexLit"
}
