Shader "DoomScrolling/NeonGlow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (0.5,0,1,1)
        _GlowIntensity ("Glow Intensity", Range(0,5)) = 1.5
        _GlowSize ("Glow Size", Range(0,0.05)) = 0.01
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        // Pass 1: Glow
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowSize;
            float _PulseSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Sample neighboring pixels for glow
                float alpha = 0;
                float size = _GlowSize;
                int samples = 8;
                for (int s = 0; s < samples; s++)
                {
                    float angle = (s / (float)samples) * 3.14159 * 2;
                    float2 offset = float2(cos(angle), sin(angle)) * size;
                    alpha += tex2D(_MainTex, i.uv + offset).a;
                }
                alpha /= samples;

                float pulse = (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);
                float intensity = _GlowIntensity * (0.7 + pulse * 0.3);

                fixed4 glow = _GlowColor * alpha * intensity;
                glow.a = alpha * (1 - col.a);

                return col + glow * (1 - col.a);
            }
            ENDCG
        }
    }
}
