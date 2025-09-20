Shader "Custom/SpritePixelate2D"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector]_RendererColor ("RendererColor", Color) = (1,1,1,1)
        [ToggleOff] PixelSnap ("Pixel snap", Float) = 0

        // CONTROLLO (compatibile con BossVFX.SetBlur)
        _BlurAmount ("Pixelate Amount", Range(0,1)) = 0
        _PixelMin ("Min Pixel Size (px)", Range(1,8)) = 1
        _PixelMax ("Max Pixel Size (px)", Range(1,64)) = 12

        // OUTLINE (per lo stun)
        _OutlineEnabled ("Outline Enabled", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,0.6,0,1)
        _OutlineThickness ("Outline Thickness (px)", Range(0,4)) = 1.5
        _AlphaEdge ("Alpha Edge Threshold", Range(0,1)) = 0.1
    }
    SubShader
    {
        Tags{ "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex:POSITION; float4 color:COLOR; float2 texcoord:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 texcoord:TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // x=1/width, y=1/height
            fixed4 _Color, _RendererColor;

            // pixelate
            float _BlurAmount;
            float _PixelMin;
            float _PixelMax;

            // outline
            float _OutlineEnabled;
            fixed4 _OutlineColor;
            float _OutlineThickness;
            float _AlphaEdge;

            v2f vert (appdata_t v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            fixed4 Sample(float2 uv){ return tex2D(_MainTex, uv); }

            fixed4 frag (v2f i) : SV_Target
            {
                // --- PIXELATE ---
                float px = lerp(_PixelMin, _PixelMax, saturate(_BlurAmount));
                float2 stepUV = float2(_MainTex_TexelSize.x * px, _MainTex_TexelSize.y * px);
                // quantizza al centro del blocco
                float2 uvq = floor(i.texcoord / stepUV) * stepUV + stepUV * 0.5;
                fixed4 baseCol = Sample(uvq) * i.color;

                // --- OUTLINE (opzionale) ---
                if (_OutlineEnabled > 0.5)
                {
                    float2 t = _MainTex_TexelSize.xy * _OutlineThickness;

                    float a0 = Sample(i.texcoord).a;
                    float aN = Sample(i.texcoord + float2(0,  t.y)).a;
                    float aS = Sample(i.texcoord + float2(0, -t.y)).a;
                    float aE = Sample(i.texcoord + float2( t.x, 0)).a;
                    float aW = Sample(i.texcoord + float2(-t.x, 0)).a;
                    float aNE= Sample(i.texcoord + float2( t.x,  t.y)).a;
                    float aNW= Sample(i.texcoord + float2(-t.x,  t.y)).a;
                    float aSE= Sample(i.texcoord + float2( t.x, -t.y)).a;
                    float aSW= Sample(i.texcoord + float2(-t.x, -t.y)).a;

                    float neighborMax = max(max(max(aN,aS), max(aE,aW)), max(max(aNE,aNW), max(aSE,aSW)));
                    float isEdge = step(_AlphaEdge, neighborMax) * (1.0 - step(_AlphaEdge, a0));

                    fixed4 ocol = _OutlineColor;
                    ocol.a *= isEdge;

                    // outline "sotto" il colore base
                    baseCol = ocol * ocol.a * (1 - baseCol.a) + baseCol;
                }

                return baseCol;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
