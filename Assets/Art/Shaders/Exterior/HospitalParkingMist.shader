Shader "HauntedHospital/Exterior/ParkingMist"
{
    Properties { _BaseColor("Mist Colour", Color) = (0.18,0.23,0.30,0.12) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float heightFade = smoothstep(0.0, 0.18, input.uv.y)
                    * (1.0 - smoothstep(0.28, 1.0, input.uv.y));
                float ends = smoothstep(0.0, 0.08, input.uv.x)
                    * (1.0 - smoothstep(0.92, 1.0, input.uv.x));
                float variation = 0.65 + 0.18 * sin(input.uv.x * 37.0 + input.uv.y * 8.0)
                    + 0.12 * sin(input.uv.x * 73.0 - input.uv.y * 12.0);
                return half4(_BaseColor.rgb, _BaseColor.a * heightFade * ends * variation);
            }
            ENDHLSL
        }
    }
}
