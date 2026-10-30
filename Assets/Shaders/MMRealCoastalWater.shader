Shader "MMUnity/RealCoastalWater"
{
    Properties
    {
        _Color("Color",Color)=(1,1,1,1)
        _WaterMask("Water Mask",2D)="white"{}
        _ShallowColor("Shallow",Color)=(0.055,0.34,0.40,0.68)
        _DeepColor("Deep",Color)=(0.004,0.035,0.085,0.96)
        _FoamColor("Foam",Color)=(0.88,0.96,0.94,0.95)
        _ReflectionColor("Reflection",Color)=(0.30,0.52,0.66,1)
        _DepthMax("Depth Max",Float)=14
        _FoamDepth("Foam Depth",Float)=0.85
        _WaveAmp("Wave Amp",Range(0,0.6))=0.18
        _WaveScale("Wave Scale",Float)=0.055
        _WaveSpeed("Wave Speed",Float)=0.7
        _WaveAmp2("Wave Amp 2",Range(0,0.6))=0.08
        _WaveScale2("Wave Scale 2",Float)=0.11
        _WaveSpeed2("Wave Speed 2",Float)=1.1
        _Distortion("Refraction",Range(0,0.05))=0.012
        _FresnelPower("Fresnel",Range(1,8))=4.0
        _ReflectionStrength("Reflection Strength",Range(0,1))=0.42
        _WorldSize("World Size",Float)=1536
    }    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        GrabPass { "_MMRealSeaGrab" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            sampler2D _WaterMask;
            sampler2D _MMRealSeaGrab;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            fixed4 _ShallowColor,_DeepColor,_FoamColor,_ReflectionColor;
            float _DepthMax,_FoamDepth,_WaveAmp,_WaveScale,_WaveSpeed;
            float _WaveAmp2,_WaveScale2,_WaveSpeed2,_Distortion;
            float _FresnelPower,_ReflectionStrength,_WorldSize;
            struct appdata { float4 vertex:POSITION; };
            struct v2f
            {
                float4 pos:SV_POSITION;
                float4 grab:TEXCOORD0;
                float4 screen:TEXCOORD1;
                float3 worldPos:TEXCOORD2;
                float3 worldNormal:TEXCOORD3;
            };
            v2f vert(appdata v)
            {
                v2f o;
                float3 wp=mul(unity_ObjectToWorld,v.vertex).xyz;
                float t=_Time.y;
                float p1=(wp.x*.82+wp.z*.57)*_WaveScale+t*_WaveSpeed;
                float p2=(-wp.x*.41+wp.z*.91)*_WaveScale2-t*_WaveSpeed2;
                float w1=sin(p1)*_WaveAmp;
                float w2=sin(p2)*_WaveAmp2;
                v.vertex.y+=w1+w2;
                wp=mul(unity_ObjectToWorld,v.vertex).xyz;
                float dx=cos(p1)*_WaveAmp*_WaveScale*.82 + cos(p2)*_WaveAmp2*_WaveScale2*(-.41);
                float dz=cos(p1)*_WaveAmp*_WaveScale*.57 + cos(p2)*_WaveAmp2*_WaveScale2*.91;
                o.worldNormal=normalize(float3(-dx,1,-dz));
                o.worldPos=wp;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.grab=ComputeGrabScreenPos(o.pos);
                o.screen=ComputeScreenPos(o.pos);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 maskUV=(i.worldPos.xz+_WorldSize*.5)/_WorldSize;
                fixed mask=tex2D(_WaterMask,maskUV).r;
                clip(mask-.42);
                float raw=SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen));
                float sceneDepth=LinearEyeDepth(raw);
                float surfaceDepth=i.screen.w;
                float depth=max(0,sceneDepth-surfaceDepth);
                float depth01=saturate(depth/max(_DepthMax,.001));
                float3 n=normalize(i.worldNormal);
                float3 v=normalize(_WorldSpaceCameraPos-i.worldPos);
                float fresnel=pow(1-saturate(dot(n,v)),_FresnelPower);
                float2 suv=i.grab.xy/i.grab.w;
                fixed3 refr=tex2D(_MMRealSeaGrab,suv+n.xz*_Distortion).rgb;
                fixed4 water=lerp(_ShallowColor,_DeepColor,depth01);
                water.rgb=lerp(refr,water.rgb,.48+depth01*.40);
                water.rgb=lerp(water.rgb,_ReflectionColor.rgb,fresnel*_ReflectionStrength);
                float3 l=normalize(_WorldSpaceLightPos0.xyz);
                float3 h=normalize(l+v);
                float spec=pow(saturate(dot(n,h)),96)*saturate(dot(n,l));
                water.rgb+=_LightColor0.rgb*spec*.42;
                float shore=1-smoothstep(0,_FoamDepth,depth);
                float crest=saturate((1-n.y)*11.0-.08);
                float ripple=.5+.5*sin(i.worldPos.x*.19+i.worldPos.z*.13+_Time.y*1.8);
                float foam=saturate(shore*.82+crest*ripple*.32);
                water.rgb=lerp(water.rgb,_FoamColor.rgb,foam);
                water.a=saturate(lerp(_ShallowColor.a,_DeepColor.a,depth01)+fresnel*.08);
                return water;
            }
            ENDCG
        }
    }
    Fallback "Transparent/Diffuse"
}
