Shader "unitycookie/tut/introduction/1_FlatColor"{
	Properties{
		_Color ("Color", Color) = (1.0, 1.0, 1.0)
	}
	SubShader{
		Pass{
			CGPROGRAM
				// Pragmas
				#pragma vertex vert
				#pragma fragment frag
				
				// User defined variables
				uniform float4 _Color;
				
				// Base input structs
				struct VertexInput{
					float4 vertex : POSITION;
				};
				
				struct VertexOutput{
					float4 pos: SV_POSITION;
				};
				
				// Vertex function
				VertexOutput vert(VertexInput v){
					VertexOutput o;
					o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
					return o;
					
				}
				
				// Fragment function
				float4 frag(VertexOutput i) : COLOR {
					return _Color;
				}
				
			ENDCG
		}
	}
	
	// Fallback comment out during development
	Fallback	"Diffuse"
}