Shader "Hospital/Surface Finish Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Wood Base Colour", 2D) = "white" {}
        [MainColor] _BaseColor("Wood Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Wood Normal", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0,1)) = 0.5
        _Smoothness("Wood Smoothness", Range(0,1)) = 0.15
        _HardwareMap("Original Atlas Hardware", 2D) = "white" {}
        _PreserveHardware("Keep Original Hardware Islands", Float) = 0
        _MetallicGlossMap("Metallic (R) Smoothness (A)", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _UseMetallicMap("Use Packed Metallic Map", Float) = 0
        [NoScaleOffset] _RitualMap("Painted Ritual", 2D) = "black" {}
        _RitualTint("Ritual Tint", Color) = (0.8,0.75,0.7,1)
        _RitualOrigin("Mark Centre (world)", Vector) = (0,1,0,0)
        _RitualAxis("Mark Horizontal Axis (world)", Vector) = (1,0,0,0)
        _RitualSize("Mark Width and Height (metres)", Float) = 1.3
        _RitualStrength("Paint Opacity", Range(0,1)) = 0
        _StencilBandCenters("Stencil Band Centres (world offsets, UV)", Vector) = (0.25,-0.23,0.66,0.295)
        _StencilBandScale("Stencil Letter Height Adjustment", Float) = 0
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
        TEXTURE2D(_RitualMap); SAMPLER(sampler_RitualMap);
        TEXTURE2D(_HardwareMap); SAMPLER(sampler_HardwareMap);
        TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor, _RitualTint;
            float4 _RitualOrigin, _RitualAxis;
            float _RitualSize;
            half _BumpScale, _Smoothness, _RitualStrength;
            half _PreserveHardware, _Metallic, _UseMetallicMap;
            float4 _StencilBandCenters;
            float _StencilBandScale;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float2 lightmapUV : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half4 tangentWS : TEXCOORD2;
            float2 uv : TEXCOORD3;
            half4 fogAndLight : TEXCOORD4;
            DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes v)
        {
            Varyings o=(Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v,o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
            VertexNormalInputs n=GetVertexNormalInputs(v.normalOS,v.tangentOS);
            o.positionCS=p.positionCS; o.positionWS=p.positionWS;
            o.normalWS=n.normalWS;
            o.tangentWS=half4(n.tangentWS,v.tangentOS.w*GetOddNegativeScale());
            o.uv=TRANSFORM_TEX(v.uv,_BaseMap);
            o.fogAndLight=half4(ComputeFogFactor(p.positionCS.z),VertexLighting(p.positionWS,n.normalWS));
            OUTPUT_LIGHTMAP_UV(v.lightmapUV,unity_LightmapST,o.lightmapUV);
            OUTPUT_SH(n.normalWS,o.vertexSH);
            return o;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
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
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=normalize(i.normalWS);
                half3 tangent=normalize(i.tangentWS.xyz);
                half3 bitangent=cross(n,tangent)*i.tangentWS.w;
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                InputData d=(InputData)0;
                d.positionWS=i.positionWS; d.positionCS=i.positionCS;
                d.normalWS=normalize(tangent*normalTS.x+bitangent*normalTS.y+n*normalTS.z);
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
                float3 relative=i.positionWS-_RitualOrigin.xyz;
                float2 markUV=float2(dot(relative,_RitualAxis.xyz),relative.y)/max(_RitualSize,0.01)+0.5;
                if(_StencilBandScale>0.5)
                {
                    bool upper=relative.y>0;
                    float centreY=upper?_StencilBandCenters.x:_StencilBandCenters.y;
                    float centreUV=upper?_StencilBandCenters.z:_StencilBandCenters.w;
                    markUV.y=(relative.y-centreY)/max(_RitualSize,0.01)*_StencilBandScale+centreUV;
                }
                half inside=step(0,markUV.x)*step(markUV.x,1)*step(0,markUV.y)*step(markUV.y,1);
                // Sampling on the plank surfaces keeps the gaps empty and marks vanish with broken planks.
                half4 mark=0;
                if(_RitualStrength>0.001)
                    mark=SAMPLE_TEXTURE2D(_RitualMap,sampler_RitualMap,markUV)*_RitualTint;
                half paint=saturate(mark.a*inside*_RitualStrength);
                SurfaceData s=(SurfaceData)0;
                half3 colour=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                if(_PreserveHardware>0.5)
                {
                    // These two original UV islands are the front and back door skins.
                    // The separate wired-glass, handles, frame and hinge islands keep their source atlas.
                    half face=(step(0.011,i.uv.x)*step(i.uv.x,0.277)
                        +step(0.298,i.uv.x)*step(i.uv.x,0.565))
                        *step(0.509,i.uv.y)*step(i.uv.y,0.989);
                    colour=lerp(SAMPLE_TEXTURE2D(_HardwareMap,sampler_HardwareMap,i.uv).rgb,colour,saturate(face));
                }
                s.albedo=lerp(colour*_BaseColor.rgb,mark.rgb,paint);
                s.alpha=1; s.occlusion=1; s.smoothness=lerp(_Smoothness,0.12,paint);
                if(_UseMetallicMap>0.5)
                {
                    half4 packed=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,i.uv);
                    s.metallic=packed.r*_Metallic;
                    s.smoothness=packed.a*_Smoothness;
                }
                s.normalTS=normalTS;
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
            float3 _LightDirection, _LightPosition;
            float4 ShadowVert(Attributes v):SV_POSITION
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
            half4 ShadowFrag():SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            half DepthFrag(Varyings i):SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
