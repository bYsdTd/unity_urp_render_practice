Shader "SpatialVideo/SpatialVideoScreen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurTexture("Blur", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _PlaneSize("size of the Back Plane", Vector) = (1920, 1080, 0, 0)
        _PlanePosition ("Position of the Back Plane", Vector) = (0,0,0,0)
        _PlaneNormal("Normal of the Back Plane", Vector) = (0,0,1,0)
        _Layout("2D or LR or TB", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        
        LOD 100
        ZWrite On // 开启深度写入
        ZTest LEqual // 设置深度测试为小于等于
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        struct appdata
        {
            float3 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float2 blurUV : TEXCOORD1;
            float2 rawUV : TEXCOORD2;
            float4 vertex : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        TEXTURE2D(_BlurTexture);
        SAMPLER(sampler_BlurTexture);

        TEXTURE2D(_MaskTex);
        SAMPLER(sampler_MaskTex);

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float2 _PlaneSize;
        float3 _PlanePosition;
        int _Layout; // 0 2d; 1 LR; 2 TB
        float4x4 _PlaneWorldToLocalMatrix;
        float3 _PlaneNormal;
        CBUFFER_END

        // 不考虑旋转
        float2 ComputeUV(appdata v)
        {
            // 获取摄像机位置和前平面顶点的世界空间位置
            float3 worldPos = TransformObjectToWorld(v.vertex);
            float3 cameraPos = _WorldSpaceCameraPos;
            // 计算从摄像机到前平面顶点的射线方向
            float3 rayDir = normalize(worldPos - cameraPos);
            
            // 计算射线与后平面的交点
            // 假设后平面垂直于z轴，我们可以直接计算z轴上的距离
            float t = (_PlanePosition.z - cameraPos.z) / rayDir.z;
            float3 hitPoint = cameraPos + rayDir * t;
            
            // 根据后平面的位置和大小计算UV
            return  (hitPoint.xy - _PlanePosition.xy) / _PlaneSize + 0.5;
        }

        // 考虑旋转的版本
        float2 ComputeUV2(appdata v)
        {
            // _PlaneNormal = float3(0,0,1);
            // 获取摄像机位置和前平面顶点的世界空间位置
            float3 worldPos = TransformObjectToWorld(v.vertex);
            float3 cameraPos = _WorldSpaceCameraPos;

            // 计算从摄像机到前平面顶点的射线方向
            float3 rayDir = normalize(worldPos - cameraPos);

            // 后平面的法线和一个点P0（假设是平面的中心位置）
            float3 planeNormal = _PlaneNormal;
            float3 planePoint = _PlanePosition;

            // 计算射线与平面的交点t
            float denom = dot(planeNormal, rayDir);
            if (abs(denom) > 1e-6) // 避免除以零
            {
                float t = dot(planePoint - cameraPos, planeNormal) / denom;
                float3 hitPoint = cameraPos + t * rayDir;

                // 将交点转换为后平面的局部坐标系中
                float3 localHitPoint = mul(_PlaneWorldToLocalMatrix, float4(hitPoint, 1)).xyz;

                // 假设后平面局部坐标系与UV对齐，并且局部坐标系的范围是[-0.5, 0.5]，则可以直接使用localHitPoint计算UV
                return  localHitPoint.xy + float2(0.5, 0.5);
            }
            else
            {
                // 射线与平面平行，没有交点
                return  float2(0.5, 0.5); // 或其他默认值
            }    
        }
        
        v2f vert (appdata v)
        {
            v2f o;
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            
            o.vertex = TransformObjectToHClip(v.vertex);
            o.rawUV = TRANSFORM_TEX(v.uv, _MainTex);
            o.uv = ComputeUV2(v);
            o.blurUV = o.uv;
            
            if (_Layout == 1)
            {
                o.uv.x = o.uv.x * 0.5 + step(0.5, unity_StereoEyeIndex) * 0.5;
            }
            else if (_Layout == 2)
            {
                o.uv.y = o.uv.y * 0.5 + step(0.5, unity_StereoEyeIndex) * 0.5;
            }
            
            return o;
        }

        void HandleCorner(v2f i)
        {
            float2 texize = float2(1920, 1080);
            float2 texelPos = i.rawUV * texize;
            
            float2 corner = float2(0.05, 0.05) * texize;
            float cornerSize = max(corner.x, corner.y);
                            // 左下
            if (texelPos.x < cornerSize && texelPos.y < cornerSize)
            {
                float2 anchorPos = float2(cornerSize, cornerSize);
                float distence = sqrt(dot(texelPos - anchorPos, texelPos - anchorPos));
                if (distence > cornerSize)
                {
                    discard;
                }
            }

            // 左上
            if (texelPos.x < cornerSize && texelPos.y > texize.y -  cornerSize)
            {
                float2 anchorPos = float2(cornerSize, texize.y - cornerSize);
                float distence = sqrt(dot(texelPos - anchorPos, texelPos - anchorPos));
                if (distence > cornerSize)
                {
                    discard;
                }
            }

            // 右下
            if (texelPos.x > texize.x - cornerSize && texelPos.y < cornerSize)
            {
                float2 anchorPos = float2(texize.x - cornerSize, cornerSize);
                float distence = sqrt(dot(texelPos - anchorPos, texelPos - anchorPos));
                if (distence > cornerSize)
                {
                    discard;
                }
            }

            // 右上
            if (texelPos.x > texize.x - cornerSize && texelPos.y > texize.y - cornerSize)
            {
                float2 anchorPos = float2(texize.x - cornerSize, texize.y - cornerSize);
                float distence = sqrt(dot(texelPos - anchorPos, texelPos - anchorPos));
                if (distence > cornerSize)
                {
                    discard;
                }
            }

            // if (blurUV.y < 0 || blurUV.y > 1 || blurUV.x < 0 || blurUV.x > 1)
            // {
            //     discard;
            // }
        }
        
        float4 frag (v2f i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

            // return SAMPLE_TEXTURE2D(_BlurTexture, sampler_BlurTexture, i.rawUV);
            
            float2 uv = i.uv;
            float2 blurUV = i.blurUV;

            HandleCorner(i);
            
            // 中心保持区域的UV范围
            float2 minUV = float2(0, 0);
            float2 maxUV = float2(1, 1);
            
            // 如果在中心保持区域内，直接映射UV到原图
            // blurUV才是单目的0-1的uv,用它来判断区域
            if (blurUV.x >= minUV.x && blurUV.x <= maxUV.x && blurUV.y >= minUV.y && blurUV.y <= maxUV.y)
            {
                float2 remappedUV = (blurUV - minUV) / (maxUV - minUV);

                // 用贴图混合
                float lerp = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, remappedUV).r;
                float4 blur = SAMPLE_TEXTURE2D(_BlurTexture, sampler_BlurTexture, remappedUV);
                // 屏幕uv用考虑双目重新计算过的uv
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                col = blur * lerp + col * (1-lerp);
                
                return col;
            }
            else
            {
                // 计算UV到中心保持区域边缘的方向
                float2 center = (minUV + maxUV) * 0.5;
                float2 dir = normalize(blurUV - center);
                
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
                return SAMPLE_TEXTURE2D(_BlurTexture, sampler_BlurTexture, mappedEdgeUV);
            }
        }
        
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
