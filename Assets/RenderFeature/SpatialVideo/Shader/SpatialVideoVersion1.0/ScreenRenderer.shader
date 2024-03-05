Shader "Custom/ScreenRenderer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _MaskTex2("Mask", 2D) = "white" {}
        _PlaneWidth ("Width of the Back Plane", Float) = 1.0
        _PlaneHeight ("Height of the Back Plane", Float) = 1.0
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
                float2 rawUV : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _BlurVideoTexture;
            sampler2D _ExtendVideoTexture;
            sampler2D _MaskTex;
            sampler2D _MaskTex2;
            
            float _PlaneWidth;
            float _PlaneHeight;
            float4 _PlanePosition;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.rawUV = o.uv;
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
                o.uv.x = (hitPoint.x - _PlanePosition.x) / _PlaneWidth + 0.5;
                o.uv.y = (hitPoint.y - _PlanePosition.y) / _PlaneHeight + 0.5;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (i.uv.x < 0.0 || i.uv.x > 1.0 || i.uv.y < 0.0 || i.uv.y > 1.0)
                {
                    discard; // 超出范围，discard
                }
                
                // sample the texture
                fixed4 col = tex2D(_ExtendVideoTexture, i.uv);
                fixed4 blur = tex2D(_BlurVideoTexture, i.uv);

                // 用贴图混合
                float lerp = tex2D(_MaskTex2, i.uv).r;
                col = blur * lerp + col * (1-lerp);
                
                // blend col and blur
                // // 定义UV的边界
                // float2 uvMin = float2(0.25, 0.25);
                // float2 uvMax = float2(0.75, 0.75);
                //
                // // 使用smoothstep计算边界内外的过渡值
                // float edge0 = 0.24; // 稍微小于0.25，确保平滑过渡
                // float edge1 = 0.76; // 稍微大于0.75，确保平滑过渡
                //
                // // 计算U和V方向上的混合因子
                // float blendU = smoothstep(edge0, uvMin.x, i.uv.x) * (1.0 - smoothstep(uvMax.x, edge1, i.uv.x));
                // float blendV = smoothstep(edge0, uvMin.y, i.uv.y) * (1.0 - smoothstep(uvMax.y, edge1, i.uv.y));
                //
                // // 计算最终的混合因子，如果像素在UV的0.25到0.75范围内，blendFactor接近1，否则接近0
                // float blendFactor = blendU * blendV;
                //
                // // 根据混合因子混合col和blur
                // col = lerp(blur, col, blendFactor);
                
                
                // float t = tex2D(_MaskTex, i.rawUV).r;
                // col = blur*t + col*(1-t);

                
                return col;
            }
            ENDCG
        }
    }
}
