Shader "Reverbat/Bat Highlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Stroke Color", Color) = (.76,.66,1,1)
        _OutlinePixels ("Stroke Width (source pixels)", Range(0,20)) = 9
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
            float4 _MainTex_TexelSize;
            fixed4 _Color, _OutlineColor;
            float _OutlinePixels;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float2 world : TEXCOORD1; fixed4 color : COLOR; };
            v2f vert(appdata v)
            {
                float2 expansion = 1 + 2 * _OutlinePixels * _MainTex_TexelSize.xy;
                v.vertex.xy *= expansion;
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.uv = (v.uv - .5) * expansion + .5;
                o.color = v.color * _Color;
                return o;
            }
            float AlphaAt(float2 uv)
            {
                float inside = step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
                return tex2D(_MainTex,uv).a * inside;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 stepUV = _MainTex_TexelSize.xy * _OutlinePixels;
                fixed4 source = tex2D(_MainTex, i.uv);
                float a = AlphaAt(i.uv);
                float around = 0;
                around=max(around,AlphaAt(i.uv+float2(stepUV.x,0)));
                around=max(around,AlphaAt(i.uv-float2(stepUV.x,0)));
                around=max(around,AlphaAt(i.uv+float2(0,stepUV.y)));
                around=max(around,AlphaAt(i.uv-float2(0,stepUV.y)));
                around=max(around,AlphaAt(i.uv+stepUV*.7071));
                around=max(around,AlphaAt(i.uv-stepUV*.7071));
                around=max(around,AlphaAt(i.uv+float2(stepUV.x,-stepUV.y)*.7071));
                around=max(around,AlphaAt(i.uv+float2(-stepUV.x,stepUV.y)*.7071));
                float border = saturate(around-a) * _OutlineColor.a;
                float combined = a + border * (1-a);
                float3 rgb = (source.rgb*a + _OutlineColor.rgb*border*(1-a)) / max(combined,.0001);
                return fixed4(rgb*i.color.rgb, combined*i.color.a*ReverbatVisibility(i.world));
            }
            ENDCG
        }
    }
}
