Shader "MMUnity/Linked Natural Water"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color)=(0.105,0.29,0.28,0.60)
        _MidColor("Mid Color", Color)=(0.028,0.13,0.17,0.82)
        _DeepColor("Deep Color", Color)=(0.006,0.028,0.06,0.98)
        _FoamColor("Foam Color", Color)=(0.73,0.82,0.76,0.90)
        _ReflectionColor("Reflection Tint", Color)=(0.22,0.31,0.35,1)
        _DepthRange("Depth Range", Range(2,40))=18
        _FoamDepth("Foam Depth", Range(0.05,3))=0.8
        _WaveAmp("Wave Amplitude", Range(0,0.35))=0.055
        _WaveAmp2("Wave Amplitude 2", Range(0,0.2))=0.025
        _WaveScale("Wave Scale", Range(0.01,0.3))=0.065
        _WaveScale2("Wave Scale 2", Range(0.01,0.4))=0.115
        _WaveSpeed("Wave Speed", Vector)=(0.42,0.27,0,0)
        _NormalTex("Water Normal", 2D)="bump" {}
        _NormalStrength("Normal Strength", Range(0,1.5))=0.55
        _Distortion("Refraction", Range(0,0.04))=0.010
        _FresnelPower("Fresnel Power", Range(1,8))=4.2
        _ReflectionStrength("Reflection Strength", Range(0,1))=0.52
        _SpecularStrength("Specular Strength", Range(0,1))=0.42
    }    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        GrabPass { "_EnrothWaterGrab" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _EnrothWaterGrab;
            sampler2D _NormalTex;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _CameraDepthTexture_TexelSize;
            fixed4 _ShallowColor,_MidColor,_DeepColor,_FoamColor,_ReflectionColor;
            float _DepthRange,_FoamDepth,_WaveAmp,_WaveAmp2,_WaveScale,_WaveScale2;
            float4 _WaveSpeed;
            float _NormalStrength,_Distortion,_FresnelPower,_ReflectionStrength,_SpecularStrength;

            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f
            {
                float4 pos:SV_POSITION;
                float4 grab:TEXCOORD0;
                float4 screen:TEXCOORD1;
                float3 worldPos:TEXCOORD2;
                float3 worldNormal:TEXCOORD3;
                UNITY_FOG_COORDS(4)
                float eyeDepth:TEXCOORD5;
            };            v2f vert(appdata v)
            {
                v2f o;
                float3 wp=mul(unity_ObjectToWorld,v.vertex).xyz;
                float t=_Time.y;
                float p1=(wp.x*.78+wp.z*.63)*_WaveScale+t*_WaveSpeed.x;
                float p2=(-wp.x*.46+wp.z*.89)*_WaveScale2-t*_WaveSpeed.y;
                // Connected water uses a fixed level. Ripples are shaded per
                // pixel so adjoining rectangles never separate or tilt banks.
                wp=mul(unity_ObjectToWorld,v.vertex).xyz;

                float dx=cos(p1)*_WaveAmp*_WaveScale*.78
                        +cos(p2)*_WaveAmp2*_WaveScale2*(-.46);
                float dz=cos(p1)*_WaveAmp*_WaveScale*.63
                        +cos(p2)*_WaveAmp2*_WaveScale2*.89;
                o.worldNormal=normalize(float3(-dx,1,-dz));
                o.worldPos=wp;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.grab=ComputeGrabScreenPos(o.pos);
                o.screen=ComputeScreenPos(o.pos);
                o.eyeDepth=-UnityObjectToViewPos(v.vertex).z;
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }

            float SceneEyeDepthFromRaw(float raw)
            {
                float orthoRaw=raw;
                #if defined(UNITY_REVERSED_Z)
                orthoRaw=1-orthoRaw;
                #endif
                return lerp(LinearEyeDepth(raw),lerp(_ProjectionParams.y,_ProjectionParams.z,orthoRaw),unity_OrthoParams.w);
            }

            float WaterDepthAt(float2 uv,float surfaceDepth)
            {
                float raw=SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,uv);
                return max(0,SceneEyeDepthFromRaw(raw)-surfaceDepth);
            }

            fixed4 frag(v2f i):SV_Target
            {
                float surfaceDepth=i.eyeDepth;
                float2 depthUv=i.screen.xy/max(i.screen.w,.0001);
                float centerDepth=WaterDepthAt(depthUv,surfaceDepth);

                // Far-water depth discontinuities can occur where a finite
                // source terrain ends below a continuous ocean. Softly borrow
                // nearby shallower depth only at those abrupt distant edges.
                float2 tap=_CameraDepthTexture_TexelSize.xy*6.0;
                float cap=max(_DepthRange,.001);
                float d0=min(centerDepth,cap);
                float dl=min(WaterDepthAt(depthUv-float2(tap.x,0),surfaceDepth),cap);
                float dr=min(WaterDepthAt(depthUv+float2(tap.x,0),surfaceDepth),cap);
                float dd=min(WaterDepthAt(depthUv-float2(0,tap.y),surfaceDepth),cap);
                float du=min(WaterDepthAt(depthUv+float2(0,tap.y),surfaceDepth),cap);
                float nearDepth=min(min(dl,dr),min(dd,du));
                float edgeGap=max(0,d0-nearDepth);
                float edgeMask=smoothstep(_DepthRange*.12,_DepthRange*.55,edgeGap);
                float viewDistance=distance(_WorldSpaceCameraPos,i.worldPos);
                float farMask=lerp(smoothstep(110,260,viewDistance),.65,unity_OrthoParams.w);
                float softened=lerp(nearDepth,d0,.45);
                float depth=lerp(d0,softened,edgeMask*farMask*.88);
                float depth01=saturate(depth/max(_DepthRange,.001));                float t=_Time.y;
                float2 uv1=i.worldPos.xz*.110+float2(t*.008,t*.004);
                float2 uv2=float2(i.worldPos.x*.3907-i.worldPos.z*.9205,
                                  i.worldPos.x*.9205+i.worldPos.z*.3907)*.074+float2(-t*.005,t*.007);
                float3 n1=UnpackNormal(tex2Dbias(_NormalTex,float4(uv1,0,1.25)));
                float3 n2=UnpackNormal(tex2Dbias(_NormalTex,float4(uv2,0,1.25)));
                // Subpixel ripples should become a quiet distant surface,
                // rather than a repeating high-contrast grid at the horizon.
                float normalVisibility=lerp(1,.15,smoothstep(60,450,distance(_WorldSpaceCameraPos,i.worldPos)));
                float2 ripple=(n1.xy+n2.xy*.72)*_NormalStrength*normalVisibility;
                float p1=(i.worldPos.x*.78+i.worldPos.z*.63)*_WaveScale+t*_WaveSpeed.x;
                float p2=(-i.worldPos.x*.46+i.worldPos.z*.89)*_WaveScale2-t*_WaveSpeed.y;
                float dx=cos(p1)*_WaveAmp*_WaveScale*.78+cos(p2)*_WaveAmp2*_WaveScale2*(-.46);
                float dz=cos(p1)*_WaveAmp*_WaveScale*.63+cos(p2)*_WaveAmp2*_WaveScale2*.89;
                float3 n=normalize(float3(-dx+ripple.x,1,-dz+ripple.y));

                float3 perspectiveView=normalize(_WorldSpaceCameraPos-i.worldPos);
                float3 viewDir=normalize(lerp(perspectiveView,UNITY_MATRIX_V[2].xyz,unity_OrthoParams.w));
                float fresnel=pow(1-saturate(dot(n,viewDir)),_FresnelPower);

                float2 suv=i.grab.xy/i.grab.w;
                float refrScale=_Distortion*saturate(depth*.5)*(1-depth01*.72);
                fixed3 refr=tex2D(_EnrothWaterGrab,suv+n.xz*refrScale).rgb;

                float mid01=saturate(depth/max(_DepthRange*.34,.001));
                fixed4 water=lerp(_ShallowColor,_MidColor,mid01);
                water=lerp(water,_DeepColor,smoothstep(.22,1,depth01));
                float absorption=clamp(1-exp(-depth/max(_DepthRange*.18,.001)),.15,.98);
                water.rgb=lerp(refr,water.rgb,absorption);

                float3 reflDir=reflect(-viewDir,n);
                fixed4 envRaw=UNITY_SAMPLE_TEXCUBE(unity_SpecCube0,reflDir);
                fixed3 env=DecodeHDR(envRaw,unity_SpecCube0_HDR);
                env=lerp(_ReflectionColor.rgb,env,saturate(dot(env,env)*.8));
                water.rgb=lerp(water.rgb,env,fresnel*_ReflectionStrength);                float3 l=normalize(_WorldSpaceLightPos0.xyz);
                float3 h=normalize(l+viewDir);
                float spec=pow(saturate(dot(n,h)),64)*saturate(dot(n,l));
                water.rgb+=_LightColor0.rgb*spec*_SpecularStrength;

                float verticalDepth=depth*max(.02,abs(viewDir.y));
                float shore=(1-smoothstep(.025,_FoamDepth,verticalDepth))*smoothstep(0,.04,verticalDepth);
                float noise=saturate(n1.x*.5+n2.y*.5+.5);
                float crest=saturate((1-n.y)*7.5-.03);
                float foam=saturate(shore*noise*.48);
                water.rgb=lerp(water.rgb,_FoamColor.rgb,foam*.50);
                water.a=saturate(lerp(_ShallowColor.a,_DeepColor.a,depth01)+fresnel*.10+foam*.06);

                // World-map / top-down views must read as one connected ocean.
                // Keep a narrow shallow-water cue near coasts, but suppress the
                // large depth/bathymetry colour blocks that expose terrain-tile
                // boundaries when viewed orthographically from above.
                // Treat steep/high perspective cameras like map views too.
                // This prevents seabed/tile depth differences from reading as
                // rectangular water colour blocks in aerial Scene/Game views.
                float cameraAboveWater=max(0,_WorldSpaceCameraPos.y-i.worldPos.y);
                float topDown=smoothstep(.52,.88,abs(viewDir.y));
                float aerial=smoothstep(90,260,cameraAboveWater);
                float overview=max(unity_OrthoParams.w,aerial);

                // In overview mode only the shallow coastal shelf affects colour.
                // Beyond ~2 m all water converges to the same ocean family.
                float overviewDepth=saturate(depth/2.0);
                fixed3 overviewDeep=lerp(_MidColor.rgb,_DeepColor.rgb,.58);
                fixed3 overviewColor=lerp(_ShallowColor.rgb,overviewDeep,smoothstep(.08,1.0,overviewDepth));
                water.rgb=lerp(water.rgb,overviewColor,overview);
                water.a=lerp(water.a,.985,overview*.98);

                // Northern reference climate: keep deep ocean dark while
                // making shallow northern lakes, rivers and shelves colder.
                float northMask=smoothstep(-1300,-1240,i.worldPos.x)*(1-smoothstep(760,815,i.worldPos.x))*smoothstep(775,835,i.worldPos.z);
                float shallowIce=1-smoothstep(1.5,7.0,depth);
                fixed3 iceShallow=fixed3(.30,.56,.63);
                fixed3 iceMid=fixed3(.075,.22,.29);
                fixed3 iceTarget=lerp(iceMid,iceShallow,shallowIce);
                float iceStrength=northMask*lerp(.16,.58,shallowIce);
                water.rgb=lerp(water.rgb,iceTarget,iceStrength);

                UNITY_APPLY_FOG(i.fogCoord,water);
                return water;
            }
            ENDCG
        }
    }
    Fallback "Transparent/Diffuse"
}



