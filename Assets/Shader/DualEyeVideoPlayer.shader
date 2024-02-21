Shader "Wangsd/DualEyeVideoPlayer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _UV0("uv0 / leftbottom", vector) = (0, 0, 0, 0)
        _UV1("uv1 / rightbottom", vector) = (0, 1, 0, 0)
        _UV2("uv2 / lefttop", vector) = (1, 1, 0, 0)
        _UV3("uv3 / righttop", vector) = (1, 0, 0, 0)
        _UVShiftValue("UVShift", float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Cull Off
        
        Blend SrcAlpha OneMinusSrcAlpha 

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint id : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 eyeIndex : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float2 rawUV : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _BlurVideoTexture;

            sampler2D _MaskTex;

            float4 _UV0;
            float4 _UV1;
            float4 _UV2;
            float4 _UV3;
            
            float _UVShiftValue;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.rawUV = TRANSFORM_TEX(v.uv, _MainTex);

                // o.eyeIndex = float2(unity_StereoEyeIndex, 0);
                o.eyeIndex = float2(1, 0);
                
                if (0 == v.id)
                {
                    o.uv = _UV0.xy;
                }
                else if (1 == v.id)
                {
                    o.uv = _UV1.xy;
                }
                else if (2 == v.id)
                {
                    o.uv = _UV2.xy;
                }
                else if (3 == v.id)
                {
                    o.uv = _UV3.xy;
                }
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture

                float2 texize = float2(3840, 2160);
                float2 texelPos = i.rawUV * texize;

                float2 texelSize = float2(1.0 / 7680.0, 1.0 / 2160.0);

                float2 corner = float2(0.05, 0.05) * float2(3840, 2160);
                float cornerSize = max(corner.x, corner.y);
                
                
                float2 uv = i.uv;

                uv *= float2(0.5, 1);
                
                
                if (0 == i.eyeIndex.x)
                {
                    uv.x += (texelSize.x * _UVShiftValue * -1);
                }
                else if (1 == i.eyeIndex.x)
                {
                    uv.x += 0.5;
                    uv.x += (texelSize.x * _UVShiftValue);
                }
                
                fixed4 col = tex2D(_MainTex, uv);
                fixed4 blur = tex2D(_BlurVideoTexture, uv);

                // bool needBlur = false;
                // float t = 0;
                // fixed4 blur = 0;
                //
                // int power = 3;
                
                // if (texelPos.x < corner.x * 3)
                // {
                //     needBlur = true;
                //     t = texelPos.x / (corner.x * 3);
                //     blur = tex2D(_BlurVideoTexture, uv);
                // }
                //
                // if (texelPos.x > texize.x - corner.x * power )
                // {
                //     needBlur = true;
                //     t = (texize.x - texelPos.x) / (corner.x * 3);
                //     blur = tex2D(_BlurVideoTexture, uv);
                // }
                //
                // if (texelPos.y < corner.y * power)
                // {
                //     needBlur = true;
                //     t = texelPos.y / (corner.y * power);
                //     blur = tex2D(_BlurVideoTexture, uv);
                // }
                //
                // if (texelPos.y > texize.y - corner.y * power )
                // {
                //     needBlur = true;
                //     t = (texize.y - texelPos.y) / (corner.y * 3);
                //     blur = tex2D(_BlurVideoTexture, uv);
                // }
                //
                //
                // if (needBlur)
                // {
                //     t = 3 * t * t - 2 * t * t * t;
                //     col = col*t + blur*(1 - t);
                // }

                float t =  tex2D(_MaskTex, i.rawUV).r;
                col = blur*t + col*(1-t);
                
                
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

                if (uv.y < 0 || uv.y > 1)
                {
                    discard;
                  //  return float4(0, 0, 0, 1);
                }
                else if (0 == i.eyeIndex.x)
                {
                    if (uv.x < 0 || uv.x > 0.5)
                    {
                        discard;
                     //   return float4(0, 0, 0, 1);
                    }
                }
                else if (1 == i.eyeIndex.x)
                {
                    if (uv.x < 0.5 || uv.x > 1)
                    {
                        discard;
                     //   return float4(0, 0, 0, 1);
                    }
                }
                
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
