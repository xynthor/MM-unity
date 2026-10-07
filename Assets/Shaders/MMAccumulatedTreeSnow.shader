Shader "MMUnity/Accumulated Tree Snow"
{
 Properties {
  _MainTex("Original albedo and coverage",2D)="white"{}
  _Color("Original tint",Color)=(1,1,1,1)
  _BumpMap("Original normal",2D)="bump"{}
  _BumpScale("Normal strength",Float)=1
  _Cutoff("Coverage cutoff",Range(0,1))=.37
  _SnowAmount("Snow accumulation",Range(0,1))=.8
  _Foliage("Branch cards",Range(0,1))=1
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
  half _BumpScale,_SnowAmount,_Foliage;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float3 worldPos;float3 worldNormal;float facing:VFACE;INTERNAL_DATA};
  void surf(Input i,inout SurfaceOutputStandard o) {
   fixed4 c=tex2D(_MainTex,i.uv_MainTex)*_Color;
   float3 n=normalize(WorldNormalVector(i,float3(0,0,1)));
   float mottling=.5+.25*sin(i.worldPos.x*4.7+i.worldPos.y*7.3)+.25*sin(i.worldPos.z*8.1-i.worldPos.y*5.9);
   float exposure=lerp(saturate(n.y),.48+.52*saturate(abs(n.y)),_Foliage);
   float snow=smoothstep(.18,.68,exposure*_SnowAmount+(mottling-.5)*.26);
   o.Albedo=lerp(c.rgb,float3(.82,.88,.92)*(.9+.1*mottling),snow);
   o.Alpha=c.a;
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,i.uv_BumpMap),_BumpScale*(1-.6*snow));
   o.Normal.z*=i.facing>=0?1:-1;
   o.Smoothness=lerp(.12,.07,snow);o.Metallic=0;
  }
  ENDCG
 }
 FallBack "Transparent/Cutout/VertexLit"
}
