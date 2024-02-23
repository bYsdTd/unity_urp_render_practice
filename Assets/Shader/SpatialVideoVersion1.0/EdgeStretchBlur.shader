Shader "Custom/EdgeStretchWithBlur"
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
            float4 _MainTex_TexelSize;

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

            fixed4 SampleAndBlur(float2 uv, float2 dir, float2 center, float2 minUV, float2 maxUV)
            {
                // // 计算模糊采样半径
                // float radius = 5.0 * length(_MainTex_TexelSize.xy);
                // fixed4 blurColor = fixed4(0, 0, 0, 0);
                //
                // // 进行多点采样
                // for (int i = -2; i <= 2; ++i)
                // {
                //     float lerpFactor = (float)i / 2.0;
                //     float2 sampleUV = uv + dir * radius * lerpFactor;
                //     // 计算边缘映射
                //     float2 edgeUV = sampleUV;
                //     if (abs(dir.x) > abs(dir.y))
                //     {
                //         float edgeX = dir.x > 0 ? maxUV.x : minUV.x;
                //         edgeUV = center + (edgeX - center.x) / dir.x * dir;
                //     }
                //     else
                //     {
                //         float edgeY = dir.y > 0 ? maxUV.y : minUV.y;
                //         edgeUV = center + (edgeY - center.y) / dir.y * dir;
                //     }
                //     float2 mappedEdgeUV = (edgeUV - minUV) / (maxUV - minUV);
                //     blurColor += tex2D(_MainTex, mappedEdgeUV);
                // }
                //
                // blurColor /= 5.0;
                // return blurColor;
                
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
                
                // 在扩展位置周围进行多点采样以实现模糊效果
                fixed4 blurColor = fixed4(0, 0, 0, 0);
                int sampleCount = 25;
                float blurRadius = 0.005;
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        float2 offset = float2(x, y) * blurRadius;
                        blurColor += tex2D(_MainTex, mappedEdgeUV + offset);
                    }
                }
                blurColor /= sampleCount;

                return blurColor;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                // 定义中心保持区域的UV范围
                float2 minUV = float2(0.25, 0.25);
                float2 maxUV = float2(0.75, 0.75);
                float2 center = (minUV + maxUV) * 0.5;

                // 如果在中心保持区域内，直接映射UV到原图
                if (uv.x >= minUV.x && uv.x <= maxUV.x && uv.y >= minUV.y && uv.y <= maxUV.y)
                {
                    float2 remappedUV = (uv - minUV) / (maxUV - minUV);
                    return tex2D(_MainTex, remappedUV);
                }
                else
                {
                    // 边缘区域应用模糊
                    return SampleAndBlur(uv, normalize(uv - center), center, minUV, maxUV);
                }
            }
            ENDCG
        }
    }
}
