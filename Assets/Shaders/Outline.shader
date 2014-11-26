Shader "Custom/Outline" {
       Properties {
            _BaseColor ("BaseColor", Color) = (1,1,1,1)
            _Color ("Color", Color) = (1,1,1,1)
            _Temperature ("Temperature", Range(0, 1)) = 0
            _Temperature ("Temperature", Float) = 0
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
		        
		
		        float4 _BaseColor;
		        float4 _Color;
		        float _Temperature;
		    
		
		        struct v2f {
		            float4 pos : SV_POSITION;
		            float2 uv : TEXCOORD0;
		        };
		
		
		        v2f vert (appdata_base v)
		        {
		            v2f o;
		            o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
		           // o.uv = v.texcoord;
		            return o;
		        }
		
		        float4 frag(v2f i) : COLOR
		        {
		        	float4 heatCol =  float4( _Temperature, pow(_Temperature, 20), pow(_Temperature, 40), 0);

		        	return (_BaseColor * _Color) +  heatCol;
		        }
		        ENDCG

            }
        }
        Fallback "VertexLit"
    }
