/*
 * Full-screen effect for low health and low sanity, drawn over the rendered game (the HUD canvas stays sharp on top).
 * _HealthLow (0 to 1): a faint red tint over the whole screen, a blood-red stain that darkens the picture towards the edges
 * (following the screen's shape and fading in gradually, so no ring shows) and a slight blur at the edges.
 * Colours arrive already in the linear space the game renders in.
 * _SanityLow (0 to 1): a black vignette and a blur that is strongest at the edges and eases off towards the centre, where a
 * slight blur remains. Both can be active at once; at 0 the picture is passed through unchanged.
 */
Shader "Hidden/Hud/LowStatsScreen"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "LowStats"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _HealthLow;
            float _SanityLow;
            float4 _HealthTint;
            float4 _HealthVignette;
            float _Aspect;

            // Twelve points spread over a disc, used to average the picture around each pixel for the blur.
            static const float2 Taps[12] =
            {
                float2(-0.326, -0.406), float2(-0.840, -0.074), float2(-0.696, 0.457), float2(-0.203, 0.621),
                float2(0.962, -0.195), float2(0.473, -0.480), float2(0.519, 0.767), float2(0.185, -0.893),
                float2(0.507, 0.064), float2(0.896, 0.412), float2(-0.322, -0.933), float2(-0.792, -0.598)
            };

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // 0 in the centre of the screen, about 1 in the corners.
                float2 fromCentre = (uv - 0.5) * float2(_Aspect, 1.0);
                float edge = saturate(length(fromCentre) / length(float2(_Aspect, 1.0) * 0.5));

                // Blur radius in screen units: health adds a little at the edges; sanity a little everywhere, much more at the edges.
                float radius = _HealthLow * 0.004 * smoothstep(0.45, 1.0, edge)
                             + _SanityLow * (0.0025 + 0.014 * smoothstep(0.15, 1.0, edge));

                half3 colour = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                if (radius > 0.0001)
                {
                    half3 sum = colour;
                    [unroll] for (int i = 0; i < 12; i++)
                        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + Taps[i] * radius * float2(1.0 / _Aspect, 1.0)).rgb;
                    colour = sum / 13.0;
                }

                // Low health: a faint red wash over everything, then the picture darkens and stains blood red towards the edges.
                // The falloff follows the screen's own shape (0 in the centre, 1 in the corners) and eases in slowly, so it has
                // no visible boundary.
                colour = lerp(colour, colour * _HealthTint.rgb, _HealthLow * _HealthTint.a);
                float screenEdge = saturate(length((uv - 0.5) * 2.0) / 1.41421);
                float redEdge = pow(smoothstep(0.2, 1.0, screenEdge), 1.8);
                half3 stained = colour * 0.3 + _HealthVignette.rgb;
                colour = lerp(colour, stained, redEdge * _HealthVignette.a * _HealthLow);

                // Low sanity: darkness closing in from the edges.
                float darkEdge = smoothstep(0.18, 0.95, edge);
                colour *= 1.0 - darkEdge * 0.92 * _SanityLow;

                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }
}
