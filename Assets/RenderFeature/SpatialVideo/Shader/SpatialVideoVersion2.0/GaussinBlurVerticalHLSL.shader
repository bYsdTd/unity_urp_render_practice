Shader "Custom/GaussinBlurVerticalHLSL"
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
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        struct Attributes
        {
            float4 positionOS       : POSITION;
            float2 uv               : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D_X(_MainTex);
        SAMPLER(sampler_MainTex);
        
        TEXTURE2D_X(_SourceTextureVertical);
        SAMPLER(sampler_SourceTextureVertical);
        
        CBUFFER_START(UnityPerMaterial)
        float4 _SourceTextureSize;
        float4 _MainTex_ST;
        int _Layout; // 0 2d; 1 LR; 2 TB
        CBUFFER_END
        
        Varyings vert (Attributes v)
        {
            Varyings o;
            // o.positionCS = TransformObjectToHClip(v.positionOS);
            o.positionCS = v.positionOS;
            o.texcoord = TRANSFORM_TEX(v.uv, _MainTex);

            if (_Layout == 1)
            {
                o.texcoord.x = o.texcoord.x * 0.5 + step(0.5, unity_StereoEyeIndex) * 0.5;
            }
            else if (_Layout == 2)
            {
                o.texcoord.y = o.texcoord.y * 0.5 + step(0.5, unity_StereoEyeIndex) * 0.5;
            }
            
            return o;
        }

        float2 GetSourceTexelSize()
        {
            return float2(1.0 / _SourceTextureSize.x, 1.0 / _SourceTextureSize.y);
        }

        float4 BloomVerticalPassFragment (float2 uv)  {
			float3 color = 0.0;
			float offsets[] = {
				-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
			};
			float weights[] = {
				0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
				0.19459459, 0.12162162, 0.05405405, 0.01621622
			};
			for (int i = 0; i < 9; i++) {
				float offset = offsets[i] * GetSourceTexelSize().y;
				 float4 c = SAMPLE_TEXTURE2D_X(_SourceTextureVertical, sampler_SourceTextureVertical, uv + float2(0.0, offset));
				color += c.rgb * weights[i];
			}
			return float4(color, 1.0);
		}

        float4 frag (Varyings i) : SV_Target
        {
            // i.uv.y = 1 - i.uv.y;
            
            // sample the texture
			// return SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.texcoord);

            float4 col = BloomVerticalPassFragment(i.texcoord);

            return col;
        }
        
        ENDHLSL

        Pass
        {
            ZTest Off
            Cull Off
            Name "GaussinBlurVerticalHLSL"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}