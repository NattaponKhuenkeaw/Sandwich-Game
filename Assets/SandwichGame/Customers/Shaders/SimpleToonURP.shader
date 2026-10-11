Shader "SandwichGame/Simple Toon URP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 0
        [HideInInspector] _ZWrite ("Depth Write", Float) = 1
        [HideInInspector] _Cull ("Cull", Float) = 2
        [Toggle] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = .5
        _EmissionMap ("Emission", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        [Header(Outline)]
        [HideInInspector] _OutlineEnabled ("Outline Enabled", Float) = 1
        [HideInInspector] _SmoothOutlineNormals ("Welded Outline Normals", Float) = 0
        _OutlineColor ("Outline Color", Color) = (0.02,0.02,0.02,1)
        _OutlineWidth ("Outline Width (pixels)", Range(0,6)) = 2

        [Header(Colorize)][Space(5)]  //colorize
		_Color ("Color", COLOR) = (1,1,1,1)

		[HideInInspector] _ColIntense ("Intensity", Range(0,3)) = 1
        [HideInInspector] _ColBright ("Brightness", Range(-1,1)) = 0
		_AmbientCol ("Ambient", Range(0,1)) = 0

        [Header(Detail)][Space(5)]  //detail
        [Toggle] _Segmented ("Segmented", Float) = 1
        _Steps ("Steps", Range(1,25)) = 3
        _StpSmooth ("Smoothness", Range(0,1)) = 0
        _Offset ("Lit Offset", Range(-1,1.1)) = 0

        [Header(Light)][Space(5)]  //light
        [Toggle] _Clipped ("Clipped", Float) = 0
        _MinLight ("Min Light", Range(0,1)) = 0
        _MaxLight ("Max Light", Range(0,1)) = 1
        _Lumin ("Luminocity", Range(0,2)) = 0

        [Header(Shine)][Space(5)]  //shine
		[HDR] _ShnColor ("Color", COLOR) = (1,1,0,1)
        [Toggle] _ShnOverlap ("Overlap", Float) = 0

        _ShnIntense ("Intensity", Range(0,1)) = 0
        _ShnRange ("Range", Range(0,1)) = 0.15
        _ShnSmooth ("Smoothness", Range(0,1)) = 0
    }


    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #define fixed half
            #define fixed4 half4
            #include "Assets/Simple Toon/Shaders/STCore.cginc"
            sampler2D _EmissionMap;
            float4 _EmissionColor;
            float _AlphaClip, _Cutoff;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.uv=input.uv*_MainTex_ST.xy+_MainTex_ST.zw;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 normal=normalize(input.normalWS);
                Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                _MaxAtten=1;
                float toon=Toon(dot(normal,light.direction),light.shadowAttenuation);
                float3 view=normalize(GetWorldSpaceViewDir(input.positionWS));
                half4 texcol=tex2D(_MainTex,input.uv)*_Color*_ColIntense+_ColBright;
                if (_AlphaClip > .5) clip(texcol.a - _Cutoff);
                half3 lighting=lerp(light.color,half3(1,1,1),_AmbientCol);
                half4 color=half4(texcol.rgb*lighting*toon + tex2D(_EmissionMap,input.uv).rgb*_EmissionColor.rgb,texcol.a);
                PostShine(color,dot(normal,light.direction),light.shadowAttenuation);
                return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineEnabled;
            float _SmoothOutlineNormals;
            struct OutlineAttributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; };
            struct OutlineVaryings { float4 positionCS:SV_POSITION; };
            OutlineVaryings OutlineVert(OutlineAttributes input)
            {
                OutlineVaryings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                // Tangents contain welded outline normals, skinned together with the character.
                float3 outlineNormal=_SmoothOutlineNormals>.5 && dot(input.tangentOS.xyz,input.tangentOS.xyz)>.01 ? input.tangentOS.xyz : input.normalOS;
                float3 normalWS=TransformObjectToWorldNormal(outlineNormal);
                float3 normalVS=normalize(TransformWorldToViewDir(normalWS));
                float2 direction=normalVS.xy;
                output.positionCS.xy+=direction*(_OutlineWidth*2.0/_ScaledScreenParams.xy)*output.positionCS.w;
                // Keep the expanded back faces behind the visible surface to avoid dark speckles.
                #if UNITY_REVERSED_Z
                output.positionCS.z-=0.0001*output.positionCS.w;
                #else
                output.positionCS.z+=0.0001*output.positionCS.w;
                #endif
                return output;
            }
            half4 OutlineFrag(OutlineVaryings input):SV_Target { clip(_OutlineEnabled - .5); return _OutlineColor; }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
