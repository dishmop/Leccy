Shader "unitycookie/tut/beginner/2_Lambert" {
	Properties{
		_Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
	}
	
	SubShader{
		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			// user defned variables
			uniform float4 _Color;
			
			// Unity defined variables
			uniform float4 _LightColor0;
			//float4x4 	_Object2World;
			//float4x4 	_World2Object;
			//float4	_WorldSpaceLightPos0;
			
			// Base input structs
			struct VertexInput{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
			
			struct VertexOutput{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
			};
			 
			 
			// vertex function	
			VertexOutput vert(VertexInput v){
				VertexOutput o;
				
				
				float3 normal = mul(_Object2World, float4(v.normal.xyz,0.0));
				
				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				
				// do all calculations in object space
				//float4 lightPos = normalize(float4(_WorldSpaceLightPos0.xyz, 1.f));
				
				//float4 lightDir = float4(0.0, 0.0, 1.0, 0.0);
				//float4x4 lightToModel = (_Light2World0, _World2Object);
			//	lightPos = mul(_World2Object, lightPos);
				
				
//				float3 lightDirection = normalize(_WorldSpaceLightDirection0.xyz);
				
				float3 diffuseReflection = _Color.rgb * _LightColor0.rgb * dot(normal, lightDir);
				//float4
				
				o.col = float4(diffuseReflection, 1.0);
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				
				return o;
			}
			
			// Fragment function
			float4 frag(VertexOutput i) : COLOR{
				return i.col;
			}
			
			ENDCG
			
		}
		
	}
	
	// fallback "Diffuse"
}