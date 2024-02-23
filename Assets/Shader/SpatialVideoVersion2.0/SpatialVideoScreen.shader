Shader "Custom/SpatialVideoScreen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _PlaneSize("size of the Back Plane", Vector) = (1920, 1080, 0, 0)
        _PlanePosition ("Position of the Back Plane", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        
        LOD 100
        ZWrite On
        
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BlurVideoTexture;
            sampler2D _ExtendVideoTexture;
            sampler2D _MaskTex;
            
            float2 _PlaneSize;
            float4 _PlanePosition;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // 获取摄像机位置和前平面顶点的世界空间位置
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 cameraPos = _WorldSpaceCameraPos;
                // 计算从摄像机到前平面顶点的射线方向
                float3 rayDir = normalize(worldPos - cameraPos);
                
                // 计算射线与后平面的交点
                // 假设后平面垂直于z轴，我们可以直接计算z轴上的距离
                float t = (_PlanePosition.z - cameraPos.z) / rayDir.z;
                float3 hitPoint = cameraPos + rayDir * t;
                
                // 根据后平面的位置和大小计算UV
                o.uv = (hitPoint.xy - _PlanePosition.xy) / _PlaneSize + 0.5;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                // 中心保持区域的UV范围
                float2 minUV = float2(0, 0);
                float2 maxUV = float2(1, 1);

                // 如果在中心保持区域内，直接映射UV到原图
                if (uv.x >= minUV.x && uv.x <= maxUV.x && uv.y >= minUV.y && uv.y <= maxUV.y)
                {
                    float2 remappedUV = (uv - minUV) / (maxUV - minUV);

                    // 用贴图混合
                    float lerp = tex2D(_MaskTex, remappedUV).r;
                    float4 col = tex2D(_MainTex, remappedUV);
                    float4 blur = tex2D(_BlurVideoTexture, remappedUV);
                    col = blur * lerp + col * (1-lerp);
                    return col;
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
                    return tex2D(_BlurVideoTexture, mappedEdgeUV);
                }
            }
            ENDCG
        }
    }
}
