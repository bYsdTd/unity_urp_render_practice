Shader "Wangsd/GaussinBlurHorizon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SourceTextureSize("TextureSize", Vector) = (100, 100, 0 , 0)
        _Layout("2D or LR or TB", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass 
        {
            Name "GaussinBlurHorizon"
            ZTest Off
            Cull Off
            
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            int _Layout; // 0 2d; 1 LR; 2 TB
            
            sampler2D _SourceTexture;

            float4 _SourceTextureSize;

            v2f vert (appdata v)
            {
                v2f o;
                //o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex = v.vertex;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                if (_Layout == 1)
                {
                    o.uv.x = o.uv.x * 0.5 + step(0.5, unity_StereoEyeIndex) * 0.5;
                }
                else if (_Layout == 2)
                {
                    o.uv.y = o.uv.y * 0.5 + step(0.5, unity_StereoEyeIndex) * 0.5;
                }
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float2 GetSourceTexelSize()
            {
                return float2(1.0 / _SourceTextureSize.x, 1.0 / _SourceTextureSize.y);
            }

            float4 BloomHorizontalPassFragment (float2 uv)  {
	            float3 color = 0.0;
	            float offsets[] = {
		            -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
	            };
	            float weights[] = {
		            0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
		            0.19459459, 0.12162162, 0.05405405, 0.01621622
	            };
	            for (int i = 0; i < 9; i++) {
		            float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
		            color += tex2D(_SourceTexture, uv + float2(offset, 0.0)).rgb * weights[i];
	            }
	            return float4(color, 1.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // i.uv.y = 1 - i.uv.y;
                
                // sample the texture
                // fixed4 col = tex2D(_MainTex, i.uv);

                fixed4 col = BloomHorizontalPassFragment(i.uv);
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
