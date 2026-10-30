Shader "MMUnity/WorldMountainRock20261007"
{
 Properties { _Rock("Fractured rock",2D)="white"{} _Normal("Rock relief",2D)="bump"{} _Snow("Snow",2D)="white"{} }
 SubShader {
 Tags {"Queue"="Geometry+12" "RenderType"="Transparent"}
 ZWrite Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows alpha:fade
 #pragma target 3.0
 sampler2D _Rock,_Normal,_Snow;
 struct Input {float3 worldPos;float3 worldNormal;float4 color:COLOR;INTERNAL_DATA};
 float3 tri(sampler2D tex,float3 p,float3 n,float scale){float3 w=pow(abs(n),4);w/=max(dot(w,1),.001);return tex2D(tex,p.zy*scale).rgb*w.x+tex2D(tex,p.xz*scale).rgb*w.y+tex2D(tex,p.xy*scale).rgb*w.z;}
 void surf(Input IN,inout SurfaceOutputStandard o){float3 p=IN.worldPos,n=normalize(WorldNormalVector(IN,float3(0,0,1)));float3 rock=tri(_Rock,p,n,.075);float detail=dot(tri(_Rock,p,n,.45),float3(.3,.59,.11));rock*=.66+.5*detail;float snow=IN.color.r*smoothstep(.52,.86,n.y);o.Albedo=lerp(rock*float3(.74,.77,.79),tri(_Snow,p,n,.23)*float3(.87,.91,.94),snow);o.Smoothness=lerp(.17,.08,snow);o.Metallic=0;o.Alpha=(1-smoothstep(.72,.94,n.y))*IN.color.a;float3 w=pow(abs(n),4);w/=max(dot(w,1),.001);float3 nx=UnpackNormal(tex2D(_Normal,p.zy*.075)),ny=UnpackNormal(tex2D(_Normal,p.xz*.075)),nz=UnpackNormal(tex2D(_Normal,p.xy*.075));float3 perturb=float3(nx.z*sign(n.x),nx.y,nx.x)*w.x+float3(ny.x,ny.z*sign(n.y),ny.y)*w.y+float3(nz.x,nz.y,nz.z*sign(n.z))*w.z;float3 world=normalize(n+perturb*.35*(1-snow));float3 tx=WorldNormalVector(IN,float3(1,0,0)),ty=WorldNormalVector(IN,float3(0,1,0));o.Normal=normalize(float3(dot(world,tx),dot(world,ty),dot(world,n)));}
 ENDCG
 }
 FallBack "Diffuse"
}
