Shader "Custom/UVAnim" {
       Properties {
            _Color ("Color", Color) = (1,1,1,1)
            _MainTex ("Texture", 2D) = "white" { }
        }
        SubShader {
         	ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha
			Tags {"Queue"="Transparent"}
           Pass {

		        CGPROGRAM
		
		        #pragma vertex vert
		        #pragma fragment frag
		
		
		        #include "UnityCG.cginc"
		
		        float4 _Color;
		        sampler2D _MainTex;
		
		        struct v2f {
		            float4 pos : SV_POSITION;
		            float2 uv : TEXCOORD0;
		        };
		
		        float4 _MainTex_ST;
		        
		        float Rand01(){
		        	return frac(_Time.y * 100);
		        }
		
		        v2f vert (appdata_base v)
		        {
		            v2f o;
		            o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
		            o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
		           // o.uv = v.texcoord;
		            return o;
		        }
		
		        float4 frag(v2f i) : COLOR
		        {
		       	 	return tex2D(_MainTex, i.uv) * _Color;
		        }
		        ENDCG

            }
        }
        Fallback "VertexLit"
    }
