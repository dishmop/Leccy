Shader "Custom/UVAnim-real" {
       Properties {
            _Color1 ("Color 1", Color) = (1,1,1,1)
            _Color2 ("Color 2", Color) = (1,1,1,1)
            _MainTex ("Texture", 2D) = "white" { }
        }
        SubShader {
            Pass {

		        CGPROGRAM
		
		        #pragma vertex vert
		        #pragma fragment frag
		
		
		        #include "UnityCG.cginc"
		
		        float4 _Color1;
		        float4 _Color2;
		        sampler2D _MainTex;
		
		        struct v2f {
		            float4 pos : SV_POSITION;
		            float2 uv : TEXCOORD0;
		        };
		
		        float4 _MainTex_ST;
		
		        v2f vert (appdata_base v)
		        {
		            v2f o;
		            o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
		            //o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
		            o.uv = v.texcoord;
		            return o;
		        }
		
		        half4 frag (v2f i) : COLOR
		        {
		        	float a = sin(i.uv.x * 20 + _Time.y * 2.5) * 0.5 + 1.0;
		        	float b = sin(i.uv.y * 20 + _Time.y * 10.2) * 0.5 + 1.0;
		           return a * _Color1 + b * _Color2;
		        }
		        ENDCG

            }
        }
        Fallback "VertexLit"
    }
