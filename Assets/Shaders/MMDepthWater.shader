Shader "MMUnity/DepthWater"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.16,0.55,0.58,0.58)
        _DeepColor("Deep Color", Color) = (0.02,0.12,0.26,0.78)
        _FoamColor("Foam Color", Color) = (0.78,0.93,0.88,0.75)
        _DepthRange("Depth Range", Range(0.5,20)) = 8
        _FoamDepth("Foam Depth", Range(0.05,3)) = 0.7
        _WaveScale("Wave Scale", Range(0.01,1)) = 0.12
        _WaveSpeed("Wave Speed", Vector) = (0.035,0.02,0,0)
        _Glossiness("Smoothness", Range(0,1)) = 0.88
        _Metallic("Metallic", Range(0,1)) = 0.05
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
            #include "UnityCG.cginc"
            sampler2D _CameraDepthTexture;
            sampler2D _MMWaterGrab;
            float4 _ShallowColor, _DeepColor, _FoamColor;
            float _DepthRange, _FoamDepth, _WaveScale;
            float4 _WaveSpeed;

            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f
            {
                float4 pos:SV_POSITION;
                float4 screenPos:TEXCOORD0;
                float3 worldPos:TEXCOORD1;
                float3 worldNormal:TEXCOORD2;
                float2 uv:TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
                float sceneEye = LinearEyeDepth(rawDepth);
                float waterEye = i.screenPos.w;
                float depth = max(0, sceneEye - waterEye);
                float depth01 = saturate(depth / _DepthRange);

                float wave = sin((i.worldPos.x + _Time.y * _WaveSpeed.x * 100) * _WaveScale)
                           + sin((i.worldPos.z + _Time.y * _WaveSpeed.y * 100) * (_WaveScale * 1.37));
                float3 n = normalize(i.worldNormal + float3(wave * 0.035, 0, wave * 0.035));
                float fresnel = pow(1 - saturate(dot(n, normalize(_WorldSpaceCameraPos - i.worldPos))), 3);

                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 distortion = n.xz * 0.012;
                fixed3 refracted = tex2D(_MMWaterGrab, screenUV + distortion).rgb;
                fixed4 water = lerp(_ShallowColor, _DeepColor, depth01);
                water.rgb = lerp(refracted, water.rgb, 0.5 + depth01 * 0.35);
                water.rgb += fresnel * 0.18;
                float foam = 1 - smoothstep(0, _FoamDepth, depth);
                water.rgb = lerp(water.rgb, _FoamColor.rgb, foam * 0.55);
                water.a = saturate(water.a + fresnel * 0.12 + depth01 * 0.08);
                return water;
            }
            ENDCG
        }
    }
    Fallback "Transparent/Diffuse"
}
