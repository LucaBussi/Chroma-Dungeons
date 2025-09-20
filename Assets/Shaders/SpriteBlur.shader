Shader "Custom/SpriteBlur2D"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector]_RendererColor ("RendererColor", Color) = (1,1,1,1)
        [ToggleOff] PixelSnap ("Pixel snap", Float) = 0

        // BLUR
        _BlurAmount ("Blur Amount", Range(0,1)) = 0
        _BlurRadius ("Blur Radius (px)", Range(0,8)) = 4

        // OUTLINE
        _OutlineEnabled ("Outline Enabled", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,0.6,0,1)
        _OutlineThickness ("Outline Thickness (px)", Range(0,4)) = 1.5
        _AlphaEdge ("Alpha Edge Threshold", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            // ---- costante di compilazione per il bound del loop ----
            #define MAX_RADIUS 8   // deve combaciare con il Range del _BlurRadius

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 pos    : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            fixed4 _Color, _RendererColor;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // x=1/w, y=1/h

            float _BlurAmount;
            float _BlurRadius;

            float _OutlineEnabled;
            fixed4 _OutlineColor;
            float _OutlineThickness;
            float _AlphaEdge;

            v2f vert (appdata_t v)
            {
                v2f o;
                #ifdef PIXELSNAP_ON
                o.pos = UnityPixelSnap(UnityObjectToClipPos(v.vertex));
                #else
                o.pos = UnityObjectToClipPos(v.vertex);
                #endif
                o.uv    = v.uv;
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            inline fixed4 Sample(float2 uv) { return tex2D(_MainTex, uv); }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 src = Sample(i.uv);
                fixed4 baseCol = src;

                // ----------------- BLUR (alpha-aware) -----------------
                if (_BlurAmount > 0.001 && _BlurRadius > 0.001)
                {
                    // passi effettivi (interi) limitati dal bound costante
                    int steps = (int)ceil(_BlurRadius * _BlurAmount);
                    steps = clamp(steps, 1, (int)MAX_RADIUS);

                    float2 du = float2(_MainTex_TexelSize.x, 0);
                    float2 dv = float2(0, _MainTex_TexelSize.y);

                    // orizzontale
                    float3 accRGBh = float3(0,0,0);
                    float  accWh   = 0.0;
                    float  maxAh   = 0.0;

                    // verticale
                    float3 accRGBv = float3(0,0,0);
                    float  accWv   = 0.0;
                    float  maxAv   = 0.0;

                    // loop a bound costante → il compilatore può unrollare in modo sicuro
                    [unroll]
                    for (int k = -MAX_RADIUS; k <= MAX_RADIUS; k++)
                    {
                        // mask 1 se |k| <= steps, altrimenti 0 (niente branch)
                        float m = step(abs(k), steps);

                        fixed4 sh = Sample(i.uv + du * k);
                        float  wh = sh.a * m;
                        accRGBh += sh.rgb * wh;
                        accWh   += wh;
                        maxAh    = max(maxAh, sh.a * m);

                        fixed4 sv = Sample(i.uv + dv * k);
                        float  wv = sv.a * m;
                        accRGBv += sv.rgb * wv;
                        accWv   += wv;
                        maxAv    = max(maxAv, sv.a * m);
                    }

                    float3 rgbh = (accWh > 1e-5) ? (accRGBh / accWh) : float3(0,0,0);
                    float3 rgbv = (accWv > 1e-5) ? (accRGBv / accWv) : float3(0,0,0);

                    fixed4 blurAvg = fixed4( (rgbh + rgbv) * 0.5, max(maxAh, maxAv) );

                    baseCol = lerp(src, blurAvg, _BlurAmount);
                }

                // tint finale
                baseCol *= i.color;

                // ----------------- OUTLINE (sotto) -----------------
                if (_OutlineEnabled > 0.5)
                {
                    float2 t = _MainTex_TexelSize.xy * _OutlineThickness;

                    float a0 = Sample(i.uv).a;
                    float aN = Sample(i.uv + float2( 0,  t.y)).a;
                    float aS = Sample(i.uv + float2( 0, -t.y)).a;
                    float aE = Sample(i.uv + float2( t.x, 0)).a;
                    float aW = Sample(i.uv + float2(-t.x, 0)).a;
                    float aNE= Sample(i.uv + float2( t.x,  t.y)).a;
                    float aNW= Sample(i.uv + float2(-t.x,  t.y)).a;
                    float aSE= Sample(i.uv + float2( t.x, -t.y)).a;
                    float aSW= Sample(i.uv + float2(-t.x, -t.y)).a;

                    float neighborMax = max(max(max(aN,aS), max(aE,aW)), max(max(aNE,aNW), max(aSE,aSW)));

                    // bordo esterno dove i vicini hanno alpha ma il centro no
                    float edgeMask = step(_AlphaEdge, neighborMax) * (1.0 - step(_AlphaEdge, a0));

                    fixed4 outlineCol = _OutlineColor * i.color;
                    outlineCol.a *= edgeMask;

                    // outline SOTTO al pixel base
                    fixed4 outCol;
                    outCol.rgb = baseCol.rgb + outlineCol.rgb * outlineCol.a * (1.0 - baseCol.a);
                    outCol.a   = baseCol.a   + outlineCol.a   * (1.0 - baseCol.a);
                    return outCol;
                }

                return baseCol;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
