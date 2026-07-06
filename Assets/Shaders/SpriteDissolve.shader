Shader "Custom/SpriteDissolve"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _EdgeColor ("Edge Color", Color) = (1,1,1,1)
        _EdgeWidth ("Edge Width", Range(0,0.3)) = 0.08
        _NoiseScale ("Pixel Grid Resolution", Float) = 16
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            float  _DissolveAmount;
            float4 _EdgeColor;
            float  _EdgeWidth;
            float  _NoiseScale;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            // Chunky pixel-block dissolve: each cell of a coarse grid (matching
            // the sprite's own low-res pixel-art scale) pops fully in or out —
            // no smooth/organic blending — so the sprite breaks apart into
            // discrete pixel blocks instead of dissolving like a soft cloud.
            float4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 col = tex * _Color * IN.color;

                float2 cell = floor(IN.uv * _NoiseScale);
                float n = hash21(cell);
                clip(n - _DissolveAmount);

                // Blocks about to vanish flash the edge color for one step.
                float aboutToVanish = (n - _DissolveAmount < _EdgeWidth) ? 1.0 : 0.0;
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, aboutToVanish);

                return col;
            }
            ENDHLSL
        }
    }
}
