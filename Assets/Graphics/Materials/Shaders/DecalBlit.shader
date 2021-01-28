Shader "Custom/DecalBlitTest"
{
    Properties
    {
        _MainTex("Blit Texture", 2D) = "white" {}
        _OriginalMap("Original Map", 2D) = "white" {}
        _DecalColour("Decal Colour", Color) = (1,1,1,1)
        Scale("Scale", Float) = 1
        UVperUnit("UVPerUnit", Float) = 1
        UVHit("UV Hit", Vector) = (0,0,0,0)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _OriginalMap;
            float4 _OriginalMap_TexelSize;
            fixed4 _DecalColour;
            float Scale;
            float UVperUnit;
            float4 UVHit;

            fixed4 frag(v2f i) : SV_Target
            {
                float minMultiplier = min(_OriginalMap_TexelSize.z / _MainTex_TexelSize.z, _OriginalMap_TexelSize.w / _MainTex_TexelSize.z);
                float modifiedScale = Scale * minMultiplier * UVperUnit;

                float2 newUV = float2(0,0);
                newUV.x = (UVHit.x - i.uv.x) / modifiedScale + 0.5;
                newUV.y = (UVHit.y - i.uv.y) / modifiedScale + 0.5;
                float opacity;
                if (newUV.x > 0 && newUV.x < 1 && newUV.y > 0 && newUV.y < 1) {
                    opacity = 1;
                }
                else {
                    opacity = 0;
                }

                fixed4 decalCol = tex2D(_MainTex, newUV) * _DecalColour;
                opacity *= decalCol.a;
                fixed4 originalCol = tex2D(_OriginalMap, i.uv);

                //return fixed4(newUV.x, newUV.y, 0, 1);
                fixed4 col = originalCol * (1 - opacity) + decalCol * opacity;
                col.a = max(col.a, originalCol.a);

                return col;
            }
            ENDCG
        }
    }
}
