Shader "MMUnity/WaterDepth"
{
    Properties
    {
        _WaterMask ("Water Mask", 2D) = "white" {}
        _ShallowColor ("Shallow", Color) = (0.08,0.42,0.48,0.58)
        _DeepColor ("Deep", Color) = (0.015,0.09,0.16,0.92)
        _FoamColor ("Foam", Color) = (0.82,0.92,0.9,1)
        _DepthMax ("Depth Max", Float) = 6
        _FoamDepth ("Foam Depth", Float) = 0.75
        _WaveAmp ("Wave Amplitude", Range(0,0.3)) = 0.07
        _WaveScale ("Wave Scale", Float) = 0.22
        _WaveSpeed ("Wave Speed", Float) = 0.8
        _Distortion ("Refraction", Range(0,0.05)) = 0.012
        _WorldSize ("World Size", Float) = 512
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        GrabPass { "_MMWaterGrab" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MMWaterGrab;
            sampler2D _WaterMask;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            fixed4 _ShallowColor, _DeepColor, _FoamColor;
            float _DepthMax, _FoamDepth, _WaveAmp, _WaveScale, _WaveSpeed, _Distortion, _WorldSize;

            struct appdata { float4 vertex:POSITION; };
            struct v2f
            {
                float4 pos:SV_POSITION;
                float4 grab:TEXCOORD0;
                float4 screen:TEXCOORD1;
                float3 worldPos:TEXCOORD2;
                float3 worldNormal:TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float t = _Time.y * _WaveSpeed;
                float a = sin(wp.x*_WaveScale + t);
                float b = sin(wp.z*(_WaveScale*1.31) - t*0.77);
                v.vertex.y += (a+b) * _WaveAmp * 0.5;
                wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float dx = cos(wp.x*_WaveScale + t) * _WaveScale * _WaveAmp * 0.5;
                float dz = cos(wp.z*(_WaveScale*1.31) - t*0.77) * (_WaveScale*1.31) * _WaveAmp * 0.5;
                o.worldNormal = normalize(float3(-dx, 1, -dz));
                o.worldPos = wp;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grab = ComputeGrabScreenPos(o.pos);
                o.screen = ComputeScreenPos(o.pos);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 maskUV = (i.worldPos.xz + _WorldSize*0.5) / _WorldSize;
                fixed mask = tex2D(_WaterMask, maskUV).r;
                clip(mask - 0.42);
                float raw = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screen));
                float sceneDepth = LinearEyeDepth(raw);
                float surfaceDepth = i.screen.w;
                float depth = max(0, sceneDepth - surfaceDepth);
                float depth01 = saturate(depth / max(_DepthMax, 0.001));
