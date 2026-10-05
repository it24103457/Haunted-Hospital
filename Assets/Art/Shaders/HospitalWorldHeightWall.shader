Shader "Hospital/World Height Wall"
{
    Properties
    {
        [NoScaleOffset] _BaseMap("Wall Base Colour", 2D) = "white" {}
        [Normal][NoScaleOffset] _BumpMap("Wall Normal Map", 2D) = "bump" {}
        _BaseColor("Colour Tint", Color) = (1,1,1,1)
        _FloorY("Floor Height (world Y)", Float) = 0
        _WallHeight("Full Texture Height (metres)", Float) = 3
        _RepeatWidth("Horizontal Repeat Width (metres)", Float) = 3
        _BumpScale("Normal Strength", Range(0,1)) = 0.25
        _Smoothness("Smoothness", Range(0,1)) = 0.22
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "UniversalMaterialType"="Lit" }
        Cull Back
        ZWrite On
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _FloorY, _WallHeight, _RepeatWidth;
            half _BumpScale, _Smoothness;
        CBUFFER_END
        struct WallAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv1 : TEXCOORD1;
            float2 uv2 : TEXCOORD2;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct WallVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half4 fogAndLight : TEXCOORD2;
            DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        WallVaryings WallVert(WallAttributes v)
        {
            WallVaryings o = (WallVaryings)0;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v,o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs pos=GetVertexPositionInputs(v.positionOS.xyz);
            o.positionCS=pos.positionCS;
            o.positionWS=pos.positionWS;
            o.normalWS=TransformObjectToWorldNormal(v.normalOS);
            o.fogAndLight=half4(ComputeFogFactor(pos.positionCS.z),VertexLighting(pos.positionWS,o.normalWS));
            OUTPUT_LIGHTMAP_UV(v.uv1,unity_LightmapST,o.lightmapUV);
            OUTPUT_SH(o.normalWS,o.vertexSH);
            return o;
        }
        float2 WallUV(float3 positionWS)
        {
            // Every wall fragment samples the same floor-to-ceiling vertical range.
            // x+z keeps the horizontal sample continuous around orthogonal corners.
            float3 p=GetAbsolutePositionWS(positionWS);
            return float2((p.x+p.z)/max(_RepeatWidth,0.01),
                clamp((p.y-_FloorY)/max(_WallHeight,0.01),0.001,0.999));
        }
        half3 WallNormal(float3 p,half3 geometricNormal)
        {
            half3 n=normalize(geometricNormal);
            // Horizontal caps/reveals keep their geometric normal.
            if(abs(n.y)>0.5) return n;
            half3 along=half3(1,0,1)-n*dot(n,half3(1,0,1));
            if(dot(along,along)<0.001) along=cross(half3(0,1,0),n);
            along=normalize(along);
            half3 up=normalize(half3(0,1,0)-n*n.y);
            half3 map=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,WallUV(p)),_BumpScale);
            return normalize(along*map.x+up*map.y+n*map.z);
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex WallVert
            #pragma fragment WallFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            half4 WallFrag(WallVaryings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                InputData d=(InputData)0;
                d.positionWS=i.positionWS;
                d.positionCS=i.positionCS;
                d.normalWS=WallNormal(i.positionWS,i.normalWS);
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    d.shadowCoord=ComputeScreenPos(TransformWorldToHClip(i.positionWS));
                #else
                    d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                #endif
                d.bakedGI=SAMPLE_GI(i.lightmapUV,i.vertexSH,d.normalWS);
                d.shadowMask=SAMPLE_SHADOWMASK(i.lightmapUV);
                d.vertexLighting=i.fogAndLight.yzw;
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData s=(SurfaceData)0;
                s.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,WallUV(i.positionWS)).rgb*_BaseColor.rgb;
                s.alpha=1; s.occlusion=1; s.smoothness=_Smoothness;
                s.normalTS=half3(0,0,1);
                half4 c=UniversalFragmentPBR(d,s);
                c.rgb=MixFog(c.rgb,i.fogAndLight.x);
                return half4(c.rgb,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection;
            float3 _LightPosition;
            float4 ShadowVert(WallAttributes v):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 p=TransformObjectToWorld(v.positionOS.xyz);
                float3 n=TransformObjectToWorldNormal(v.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 l=normalize(_LightPosition-p);
                #else
                    float3 l=_LightDirection;
                #endif
                float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,l));
                #if UNITY_REVERSED_Z
                    clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE);
                #else
                    clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return clip;
            }
            half4 ShadowFrag():SV_Target {return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex WallVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            half DepthFrag(WallVaryings i):SV_Target {return i.positionCS.z;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex WallVert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 NormalFrag(WallVaryings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 n=WallNormal(i.positionWS,i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct=PackNormalOctQuadEncode(n);
                    return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                    return half4(n,0);
                #endif
            }
            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaVert
            #pragma fragment WallMetaFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            WallVaryings MetaVert(WallAttributes v)
            {
                WallVaryings o=(WallVaryings)0;
                o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=MetaVertexPosition(v.positionOS,v.uv1,v.uv2,unity_LightmapST,unity_DynamicLightmapST);
                return o;
            }
            half4 WallMetaFrag(WallVaryings i):SV_Target
            {
                MetaInput m=(MetaInput)0;
                m.Albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,WallUV(i.positionWS)).rgb*_BaseColor.rgb;
                return MetaFragment(m);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
