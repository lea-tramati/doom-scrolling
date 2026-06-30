Shader "DoomScrolling/ScanlineOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScanlineColor ("Scanline Color", Color) = (0,0,0,0.15)
        _ScanlineFreq ("Scanline Frequency", Float) = 200
        _ScanlineSpeed ("Scroll Speed", Float) = 0.5
        _VignetteStrength ("Vignette", Range(0,1)) = 0.4
        _GlitchAmount ("Glitch", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _ScanlineColor;
            float _ScanlineFreq;
            float _ScanlineSpeed;
            float _VignetteStrength;
            float _GlitchAmount;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // Glitch horizontal offset
                if (_GlitchAmount > 0)
                {
                    float glitchLine = step(0.97, sin(uv.y * 57.3 + _Time.y * 13.7));
                    uv.x += glitchLine * _GlitchAmount * (sin(_Time.y * 97.3) * 0.02);
                }

                fixed4 col = tex2D(_MainTex, uv);

                // Scanlines
                float scan = sin(uv.y * _ScanlineFreq + _Time.y * _ScanlineSpeed);
                scan = scan * 0.5 + 0.5;
                scan = pow(scan, 4);
                col.rgb = lerp(col.rgb, _ScanlineColor.rgb, scan * _ScanlineColor.a);

                // Vignette
                float2 vig = uv - 0.5;
                float vigVal = 1.0 - dot(vig, vig) * _VignetteStrength * 4;
                col.rgb *= saturate(vigVal);

                return col;
            }
            ENDCG
        }
    }
}
