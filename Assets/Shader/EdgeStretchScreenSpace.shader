Shader "Custom/EdgeStretchScreenSpace"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

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
                // o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1, 0, 0, 1);
                float2 uv = i.uv;
                // 中心保持区域的UV范围
                float2 minUV = float2(0.25, 0.25);
                float2 maxUV = float2(0.75, 0.75);

                // 如果在中心保持区域内，直接映射UV到原图
                if (uv.x >= minUV.x && uv.x <= maxUV.x && uv.y >= minUV.y && uv.y <= maxUV.y)
                {
                    float2 remappedUV = (uv - minUV) / (maxUV - minUV);
                    return tex2D(_MainTex, remappedUV);
                }
                else
                {
                    // 计算UV到中心保持区域边缘的方向
                    float2 center = (minUV + maxUV) * 0.5;
                    float2 dir = normalize(uv - center);
                    
                    // 估算边缘UV值
                    float2 edgeUV;
                    if (abs(dir.x) > abs(dir.y))
                    {
                        float edgeX = dir.x > 0 ? maxUV.x : minUV.x;
                        edgeUV = center + (edgeX - center.x) / dir.x * dir;
                    }
                    else
                    {
                        float edgeY = dir.y > 0 ? maxUV.y : minUV.y;
                        edgeUV = center + (edgeY - center.y) / dir.y * dir;
                    }

                    // 将边缘UV映射回原图的[0, 1]范围
                    float2 mappedEdgeUV = (edgeUV - minUV) / (maxUV - minUV);
                    return tex2D(_MainTex, mappedEdgeUV);
                }
            }
            ENDCG
        }
    }
}
