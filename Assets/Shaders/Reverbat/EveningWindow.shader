Shader "Reverbat/Evening Window"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" "DisableBatching"="True" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "EchoReveal.cginc"
            sampler2D _MainTex;
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; fixed4 color : COLOR; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                // Keep the drawn curtains, arch and mullions intact. Only the sky and its
                // yellow moon/stars are replaced, inside the bounds of the four panes.
                bool pane = i.uv.x > .27 && i.uv.x < .72 && i.uv.y > .17 && i.uv.y < .86;
                bool sky = c.b > c.r * 1.65 && c.b > c.g * 1.3;
                bool gold = c.r > .5 && c.g > .3 && c.b < c.g * .45;
                // Cover antialiased ink around the old moon and stars, including mixed edge pixels.
                float2 pixel = float2(i.uv.x * 437, (1-i.uv.y) * 421);
                bool oldSymbol = length((pixel-float2(265,132))/float2(31,38)) < 1.06
                    || length(pixel-float2(167,159)) < 23 || length(pixel-float2(271,277)) < 24;
                if (pane && (sky || gold || oldSymbol))
                {
                    float h = saturate((i.uv.y - .17) / .69);
                    float3 sunset = lerp(float3(1,.50,.11),float3(.68,.16,.30),h);
                    float d = length((i.uv - float2(.61,.27)) * float2(1.038,1));
                    c.rgb = lerp(sunset,float3(1,.91,.48),1-smoothstep(.063,.068,d));
                }
                c *= i.color;
                c.a *= ReverbatVisibility(i.world);
                return c;
            }
            ENDCG
        }
    }
}
