Shader "MMUnity/NorthAlpineSurface20261007"
{
    Properties
    {
        _Rock ("Rock", 2D) = "white" {}
        _Snow ("Snow", 2D) = "white" {}
        _RockNormal ("Rock relief", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "Queue"="Geometry+10" "RenderType"="Transparent" }
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0
        sampler2D _Rock, _Snow, _RockNormal;
        struct Input { float3 worldPos; float3 worldNormal; INTERNAL_DATA };
        float hash(float3 p) { return frac(sin(dot(p,float3(12.9898,78.233,37.719)))*43758.5453); }
        float noise(float3 p)
        {
            float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);
        }

        fixed3 tri(sampler2D tex,float3 p,float3 n,float scale)
        {
            float3 w=pow(abs(n),4);w/=max(dot(w,1),.0001);
            return tex2D(tex,p.zy*scale).rgb*w.x+tex2D(tex,p.xz*scale).rgb*w.y+tex2D(tex,p.xy*scale).rgb*w.z;
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 n=normalize(WorldNormalVector(IN,float3(0,0,1))),p=IN.worldPos;
            fixed3 rock=tri(_Rock,p,n,.055);
            float detail=dot(tri(_Rock,p,n,.63),float3(.3,.59,.11));
            float gray=dot(rock,float3(.3,.59,.11));rock=gray*float3(.62,.68,.74)*(.65+.8*detail)*(.65+.7*noise(p*.07));
            fixed3 snow=tri(_Snow,p,n,.24)*float3(.84,.89,.93);
            float pocket=dot(tri(_Rock,p,n,.052),float3(.3,.59,.11));
            float snowCover=smoothstep(.68,.93,n.y+(pocket-.5)*.36+(noise(p*.12)-.5)*.24);
            snowCover*=smoothstep(2,16,p.y);
            o.Albedo=lerp(rock,snow,snowCover);
            o.Metallic=0;o.Smoothness=lerp(.19,.09,snowCover);
            float cliff=1-smoothstep(.64,.88,n.y);
            float alpine=smoothstep(55,85,p.y);
            float shore=(1-smoothstep(7,17,p.y))*smoothstep(815,855,p.z);
            o.Alpha=max(max(cliff,alpine),shore)*smoothstep(780,825,p.z);
            float3 weights=pow(abs(n),4);weights/=max(dot(weights,1),.0001);
            float3 nx=UnpackNormal(tex2D(_RockNormal,p.zy*.055));
            float3 ny=UnpackNormal(tex2D(_RockNormal,p.xz*.055));
            float3 nz=UnpackNormal(tex2D(_RockNormal,p.xy*.055));
            float3 perturb=float3(nx.z*sign(n.x),nx.y,nx.x)*weights.x+float3(ny.x,ny.z*sign(n.y),ny.y)*weights.y+float3(nz.x,nz.y,nz.z*sign(n.z))*weights.z;
            float3 world=normalize(n+perturb*.45*(1-snowCover));
            float3 tx=WorldNormalVector(IN,float3(1,0,0)),ty=WorldNormalVector(IN,float3(0,1,0));
            o.Normal=normalize(float3(dot(world,tx),dot(world,ty),dot(world,n)));
        }
        ENDCG
    }
    FallBack "Diffuse"
}



