Shader "Custom/PlasmaBalls" {
	Properties {
		_CentreColor ("CentreColor", Color) = (0.0, 0.0, 0.0)
		_OuterColor ("OuterColor", Color) = (0.0, 1.0, 0.0)
		_AlphaRadius ("AlphaRadius", Range(0, 1)) = 0.1
		_AlphaRadius ("AlphaRadius", Float) = 0.1
		_ColRadius ("ColRadius", Range(0,1)) = 0.1
		_ColRadius ("ColRadius", Float) = 0.1
		_Spacing ("Spacing", Range(0, 20)) = 10
		_Spacing ("Spacing", Float) = 10
		_DoBalls ("Do Balls", Range(0, 1)) = 1
		_Speed0 ("Speed0", Range(0,1)) = 0
		_Speed0 ("Speed0", Float) = 0
		_Speed1 ("Speed1", Float) = 0
		_Speed2 ("Speed2", Float) = 0
		_Speed3 ("Speed3", Float) = 0		
	}
	SubShader {
	Pass {
		//Tags { "RenderType" = "Transparent" }
		CGPROGRAM
		
		#pragma target 3.0		
				
		#pragma vertex vert
		#pragma fragment frag
		
		 #include "UnityCG.cginc"
		
		struct v2f {
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
		};
		
		uniform float4 _CentreColor;
		uniform float4 _OuterColor;
		uniform float _AlphaRadius;
		uniform float _ColRadius;
		uniform float _Spacing;
		uniform float _DoBalls;
		uniform float _Speed0;
		uniform float _Speed1;
		uniform float _Speed2;
		uniform float _Speed3;
		


		
		v2f vert(appdata_base v)
		{
			v2f o;

			o.pos =	mul(UNITY_MATRIX_MVP, v.vertex);
			
			o.uv = v.texcoord;

			
			return o;
		}
		
		
		float4 CalcCol(float dist){
			// These should be global constants
			float4 white = float4(1, 1, 1, 1);
			float4 black = float4(0, 0, 0, 1);
	
			float4 alpha = lerp(white, black, clamp(dist / _AlphaRadius, 0, 1));
			float4 col = lerp(_CentreColor, _OuterColor, clamp(dist / _ColRadius, 0, 1));
			return col * alpha;
		}	
		
		// Returns function with straight slopes and period 1 where:
		// f (0) = 0
		// f(0.5) = 1
		// f(1) = 0
		// etc..
		float TriangleFunc(float x){
			float xx = frac(x);
			return 1 - abs(xx - 0.5)*2.0;
		}
		
		
		// Integral of the above function
		float TriangleFuncIntg(float x){
			// First work out number of entire cycles ad multiply by area under one cycle (0.5)
			float val0 = int(x) * 0.5;
			
			
			// Now with the bit that is left, see if we are below 0.5
			float val1 = 0;
			float xx = frac(x);
			if (xx < 0.5){
				val1 = xx * xx;
			}
			else{
				xx -= 0.5;
				val1 = 0.25 + xx - xx*xx;
			}
			return val0 + val1;
		}
		
		
		
		float4 frag(v2f i) : COLOR
		{
			
			
			float4 xRepeatedUV = frac(i.uv * float4(5, 1, 0, 0)) * float4(0.2, 1, 0, 0);
			
			// Work out the speed on our part of the track
			float speedParam = 0;
			int intValX = floor(i.uv.x * 5);
			switch(intValX){
			 case 0:
			 	speedParam = _Speed0;
			 	break;
			 case 1:
			 	speedParam = _Speed1;
			 	break;
			 case 2:
			 	speedParam = _Speed2;
			 	break;
			 case 3:
			 	speedParam = _Speed3;
			 	break;
			 case 4:
			 	// do nothing
			 	break;
			 }		
	
			// move with time
			float frameInterval = 1/30.0;
			float4 timeModdedUV = xRepeatedUV + float4(0, (_Time.y + frameInterval) * speedParam, 0, 0);
			
			// Get location of time at previous frame
			float4 lastTimeModdedUV = xRepeatedUV + float4(0, _Time.y * speedParam, 0, 0);
			
		 		
			
			

			// Transform based on size, movement and spacing
			float4 originOffset = float4(0.05, 0.00, 0, 0);
			
			// Move so that origin is over correct place in texture and scale up so there is alarge gap between unit squares
			float4 scaledUV = (timeModdedUV - originOffset) * _Spacing;
			float4 lastScaledUV = (lastTimeModdedUV - originOffset) * _Spacing;
			
			// We now assume the ball is centred at origin and has radius 1
			float dist = 0;
			if (_DoBalls > 0.5) {
				// Repeate each period
				float4 transUV = frac(scaledUV + 0.5  ) - 0.5;
				float distSq = transUV.x * transUV.x + transUV.y * transUV.y;
				dist = sqrt(distSq);
			}
			else{
			
				float val = TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y)- TriangleFuncIntg(lastScaledUV.y)) / (scaledUV.y - lastScaledUV.y);
				return CalcCol(val);
			/*
			
				// Repeate each period
				float4 transUV = frac(scaledUV + 0.5  ) - 0.5;
				float4 lastTransUV = 0;
				
				// Only dp this calc if we are within 0.5 texels of current
				if (abs(scaledUV.y - lastScaledUV.y) < 0.5){
					lastTransUV = frac(lastScaledUV + 0.5  ) - 0.5;
				}
				
				// If we are either side of the peak, then need to include tha too
				if (transUV.y > 0 && lastTransUV.y < 0){
					float dist = abs(transUV.y - lastTransUV.y);
					float thisDist = abs(transUV.x) + abs(transUV.y);
					float lastDist = abs(lastTransUV.x) + abs(lastTransUV.y);
					float midDist = abs(transUV.x);
										 					
					float colDist0 = 0.5*(thisDist + midDist) * abs(transUV.y) / dist;
					float colDist1 = 0.5*(lastDist + midDist) * abs(lastTransUV.y) / dist;
					return CalcCol(colDist0 + colDist1);
					
				
					
				}
				else if (transUV.y <0 &&  lastTransUV.y > 0){
					
					
					float dist = 1 - abs(transUV.y - lastTransUV.y);
					float thisDist = abs(transUV.x) + abs(transUV.y);
					float lastDist = abs(lastTransUV.x) + abs(lastTransUV.y);
					float midDist = abs(transUV.x) + 0.5;
										 					
					float colDist0 = 0.5*(thisDist + midDist) * abs(-0.5 - transUV.y) / dist;
					float colDist1 = 0.5*(lastDist + midDist) * abs(0.5 - lastTransUV.y) / dist;
					return CalcCol(colDist0 + colDist1);
					
				
					
				}				
				else{
					float dist = abs(transUV.y - lastTransUV.y);			
					float thisDist = abs(transUV.x) + abs(transUV.y);
					float lastDist = abs(lastTransUV.x) + abs(lastTransUV.y);
					
					float calcDist = 0.5 * (thisDist + lastDist) * dist / dist;
					return CalcCol(calcDist);
				}
				*/
			}
			return CalcCol(dist);
		}
		

	
		
		ENDCG
	}
	} 
	FallBack "Diffuse"
}
