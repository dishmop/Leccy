Shader "Custom/SolidColor_FG" {
       Properties {
            _BaseColor ("BaseColor", Color) = (1,1,1,1)
            _Color ("Color", Color) = (1,1,1,1)
        }
        SubShader {
        	ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha
			Tags {"Queue"="Overlay"}
			
            Pass {
			    CGPROGRAM
		
		        #pragma vertex vert
		        #pragma fragment frag

		        #include "UnityCG.cginc"
		        
		
		        float4 _BaseColor;
		        float4 _Color;
		    
		
		        struct v2f {
		            float4 pos : SV_POSITION;
		            float2 uv : TEXCOORD0;
		        };
		
		
		        v2f vert (appdata_base v)
		        {
		            v2f o;
		            o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
		            return o;
		        }
		
		        float4 frag(v2f i) : COLOR
		        {

		        	return (_BaseColor * _Color);
		        }
		        ENDCG

            }
        }
        Fallback "VertexLit"
    }
