Shader "MMUnity/Linked Far Tree Impostor" {
 Properties { _MainTex("Eight azimuth views",2D)="white" {} _Center("Local center",Vector)=(0,0,0,0) _Size("Local image size",Float)=1 _Cutoff("Coverage",Range(0,1))=.3 }
 SubShader {
  Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
  Cull Off ZWrite On
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "UnityCG.cginc"
   sampler2D _MainTex;float4 _Center;float _Size,_Cutoff;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1)};
   v2f vert(appdata v) {
    UNITY_SETUP_INSTANCE_ID(v);v2f o;
    float3 center=mul(unity_ObjectToWorld,float4(_Center.xyz,1)).xyz;
    float3 fullView=normalize(_WorldSpaceCameraPos-center);
    float top=step(.75,fullView.y);
    float3 view=normalize(float3(fullView.x,0,fullView.z)+float3(0,0,.00001));
    float3 right=normalize(cross(float3(0,1,0),view));
    float sx=max(length(unity_ObjectToWorld._m00_m10_m20),length(unity_ObjectToWorld._m02_m12_m22));
    float sy=length(unity_ObjectToWorld._m01_m11_m21);
    float3 world=center+right*(v.uv.x-.5)*_Size*sx+float3(0,1,0)*(v.uv.y-.5)*_Size*sy;
    float3 topWorld=mul(unity_ObjectToWorld,float4(_Center.xyz+float3(-v.uv.x+.5,0,v.uv.y-.5)*_Size,1)).xyz;
    world=lerp(world,topWorld,top);
    o.pos=mul(UNITY_MATRIX_VP,float4(world,1));
    float3 localView=mul((float3x3)unity_WorldToObject,view);
    float frame=fmod(floor(atan2(localView.x,localView.z)*1.273239545+8.5),8);
    frame=lerp(frame,8,top);
    o.uv=float2((v.uv.x+frame)/9,v.uv.y);UNITY_TRANSFER_FOG(o,o.pos);return o;
   }
   fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);clip(c.a-_Cutoff);UNITY_APPLY_FOG(i.fogCoord,c);return c;}
   ENDCG
  }
 }
}
