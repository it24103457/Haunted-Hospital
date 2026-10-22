Shader "Hospital/World Text Depth Tested"
{
 Properties { _MainTex ("Font Atlas", 2D) = "white" {} }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   ZTest LEqual
   ZWrite Off
   Cull Back
   Blend SrcAlpha OneMinusSrcAlpha
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; o.color=v.color; return o; }
   half4 frag(Varyings i):SV_Target { half a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a; return half4(i.color.rgb,i.color.a*a); }
   ENDHLSL
  }
 }
}
