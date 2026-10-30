Shader "MMUnity/RiverWater"
{
    Properties
    {
        _Color("Color",Color)=(1,1,1,1)
        _ShallowColor("Shallow",Color)=(0.07,0.34,0.33,0.80)
        _DeepColor("Deep",Color)=(0.012,0.075,0.09,0.95)
        _FoamColor("Foam",Color)=(0.88,0.96,0.94,0.92)
        _ReflectionColor("Reflection",Color)=(0.28,0.48,0.62,1)
        _DepthMax("Depth Max",Float)=3.5
        _FoamDepth("Foam Depth",Float)=0.3
        _WaveAmp("Wave Amp",Range(0,0.15))=0.015
        _WaveScale("Wave Scale",Float)=0.19
        _WaveSpeed("Wave Speed",Float)=1.0
        _WaveAmp2("Wave Amp 2",Float)=0
        _WaveScale2("Wave Scale 2",Float)=0
        _WaveSpeed2("Wave Speed 2",Float)=0
        _WorldSize("World Size",Float)=1536
        _Distortion("Refraction",Range(0,0.03))=0.004
        _FresnelPower("Fresnel",Range(1,8))=3.2
        _ReflectionStrength("Reflection Strength",Range(0,1))=0.24
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        GrabPass { "_MMRiverGrab" }
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
            sampler2D _MMRiverGrab;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            fixed4 _ShallowColor,_DeepColor,_FoamColor,_ReflectionColor;
            float _DepthMax,_FoamDepth,_WaveAmp,_WaveScale,_WaveSpeed;
            float _Distortion,_FresnelPower,_ReflectionStrength;

            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f
            {
                float4 pos:SV_POSITION;
                float4 grab:TEXCOORD0;
                float4 screen:TEXCOORD1;
                float3 worldPos:TEXCOORD2;
                float3 worldNormal:TEXCOORD3;
                float2 uv:TEXCOORD4;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 wp=mul(unity_ObjectToWorld,v.vertex).xyz;
                float phase=wp.x*.41+wp.z*.83+_Time.y*_WaveSpeed;
                float wave=sin(phase*_WaveScale)*_WaveAmp;
                v.vertex.y+=wave;
                wp=mul(unity_ObjectToWorld,v.vertex).xyz;
                float slope=cos(phase*_WaveScale)*_WaveAmp*_WaveScale;
                float3 baseN=normalize(UnityObjectToWorldNormal(v.normal));
                o.worldNormal=normalize(baseN+float3(-slope*.41,0,-slope*.83));
                o.worldPos=wp;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.grab=ComputeGrabScreenPos(o.pos);
                o.screen=ComputeScreenPos(o.pos);
                o.uv=v.uv;
                return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float raw=SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen));
                float sceneDepth=LinearEyeDepth(raw);
                float surfaceDepth=i.screen.w;
                float depth=max(0,sceneDepth-surfaceDepth);
                float depth01=saturate(depth/max(_DepthMax,.001));
                float3 n=normalize(i.worldNormal);
                float3 v=normalize(_WorldSpaceCameraPos-i.worldPos);
                float fresnel=pow(1-saturate(dot(n,v)),_FresnelPower);
                float2 suv=i.grab.xy/i.grab.w;
                fixed3 refr=tex2D(_MMRiverGrab,suv+n.xz*_Distortion).rgb;
                fixed4 water=lerp(_ShallowColor,_DeepColor,depth01);
                water.rgb=lerp(refr,water.rgb,.58+depth01*.32);
                water.rgb=lerp(water.rgb,_ReflectionColor.rgb,fresnel*_ReflectionStrength);
                float shore=1-smoothstep(0,_FoamDepth,depth);
                float edge=smoothstep(.465,.499,abs(i.uv.x-.5));
                float ripple=.5+.5*sin(i.worldPos.x*.31+i.worldPos.z*.17+_Time.y*2.2);
                float slopeFoam=smoothstep(.08,.48,1-abs(n.y));
                float foam=saturate(slopeFoam*(.18+.34*ripple));
                water.rgb=lerp(water.rgb,_FoamColor.rgb,foam);
                water.a=saturate(lerp(_ShallowColor.a,_DeepColor.a,depth01)+fresnel*.06);
                return water;
            }
            ENDCG
        }
    }
    Fallback "Transparent/Diffuse"
}
