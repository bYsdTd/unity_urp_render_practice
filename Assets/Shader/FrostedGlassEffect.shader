Shader "Custom/FrostedGlassEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurAmount ("Blur Amount", Range(0, 1)) = 0.5
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

            sampler2D _MainTex;
            float _BlurAmount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 col = float4(1,0,0,0);

                // // Simple box blur
                // int samples = 9;
                // float blur = _BlurAmount * 0.002; // Blur size
                // for(int x = -1; x <= 1; ++x)
                // {
                //     for(int y = -1; y <= 1; ++y)
                //     {
                //         float2 sampleUv = uv + float2(x, y) * blur;
                //         col += tex2D(_MainTex, sampleUv);
                //     }
                // }
                //
                // col /= samples;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
