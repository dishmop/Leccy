Shader "Custom/PlasmaBalls" {
	Properties {
		_CentreColor0 ("CentreColor0", Color) = (0.0, 0.0, 0.0)
		_OuterColor0 ("OuterColor0", Color) = (0.0, 1.0, 0.0)
		_CentreColor1 ("CentreColor1", Color) = (0.0, 0.0, 0.0)
		_OuterColor1 ("OuterColor1", Color) = (0.0, 1.0, 0.0)
		_CentreColor2 ("CentreColor2", Color) = (0.0, 0.0, 0.0)
		_OuterColor2 ("OuterColor2", Color) = (0.0, 1.0, 0.0)
		_CentreColor3 ("CentreColor3", Color) = (0.0, 0.0, 0.0)
		_OuterColor3 ("OuterColor3", Color) = (0.0, 1.0, 0.0)
		
		
		_AlphaRadius ("AlphaRadius", Range(0, 1)) = 0.1
		_AlphaRadius ("AlphaRadius", Float) = 0.1
		_ColRadius ("ColRadius", Range(0,1)) = 0.1
		_ColRadius ("ColRadius", Float) = 0.1
		_Spacing ("Spacing", Range(0, 20)) = 10
		_Spacing ("Spacing", Float) = 10
		_DoBalls ("Do Balls", Range(0, 1)) = 1

		_Speed0 ("Speed0", Range(0,10)) = 0
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
		
		uniform float4 _CentreColor0;
		uniform float4 _OuterColor0;
		uniform float4 _CentreColor1;
		uniform float4 _OuterColor1;
		uniform float4 _CentreColor2;
		uniform float4 _OuterColor2;
		uniform float4 _CentreColor3;
		uniform float4 _OuterColor3;
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
		
		
		float4 CalcCol(float dist, float4 centreColor, float4 outerColor){
			// These should be global constants
			float4 white = float4(1, 1, 1, 1);
			float4 black = float4(0, 0, 0, 1);
	
			float4 alpha = lerp(white, black, clamp(dist / _AlphaRadius, 0, 1));
			float4 col = lerp(centreColor, outerColor, clamp(dist / _ColRadius, 0, 1));
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
			float val0 = floor(x) * 0.5;
			
			
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
		
		// This is a square function
		float TriangleFunc2(float x){
			float xx = frac(x);
			float ret =  1 - abs(xx - 0.5)*2.0;
			return ret * ret;
		}
		
		// Integral of the above function
		float TriangleFunc2Intg(float x){
			// First work out number of entire cycles ad multiply by area under one cycle (0.5)
			float val0 = floor(x) * 0.3333333;
			
			
			// Now with the bit that is left, see if we are below 0.5
			float val1 = 0;
			float xx = frac(x);
			if (xx < 0.5){
				val1 = 4 * xx * xx * xx / 3;
			}
			else{
				xx -= 0.5;
				val1 = 0.5 * 0.33333333 + xx - 4 * xx * xx * xx / 3;
			}
			return val0 + val1;
		}		
			
		
		
		
		float4 frag(v2f i) : COLOR
		{
			
			
			float4 xRepeatedUV = frac(i.uv * float4(5, 1, 0, 0)) * float4(0.2, 1, 0, 0);
			
			// Work out the speed on our part of the track
			float speedParam = 0;
			float4 centreCol = 0;
			float4 outerCol = 0;
			int intValX = floor(i.uv.x * 5);
			switch(intValX){
			 case 0:
			 	centreCol = _CentreColor0;
			 	outerCol = _OuterColor0;
			 	speedParam = _Speed0;
			 	break;
			 case 1:
			 	centreCol = _CentreColor1;
			 	outerCol = _OuterColor1;
			 	speedParam = _Speed1;
			 	break;
			 case 2:
			 	centreCol = _CentreColor2;
			 	outerCol = _OuterColor2;
			 	speedParam = _Speed2;
			 	break;
			 case 3:
			 	centreCol = _CentreColor3;
			 	outerCol = _OuterColor3;
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
			
			// Lets rotate the origina around a little
		//	float4 newOrigin = originOffset;
		//	newOrigin.x = originOffset.x + originOffset.x  * sin(1000*_Time.y/(2*3.14159));
			
			// Move so that origin is over correct place in texture and scale up so there is alarge gap between unit squares
			float4 scaledUV = (timeModdedUV - originOffset) * _Spacing;
			float4 lastScaledUV = (lastTimeModdedUV - originOffset) * _Spacing;
			
			
			// For the moment don't blur:
			//return CalcCol( TriangleFunc(scaledUV.x) + TriangleFunc(scaledUV.y));
			
			
			// We now assume the ball is centred at origin and has radius 1
			
			float distDelta = (scaledUV.y - lastScaledUV.y);
			if (abs(distDelta) > 0.001){
				return CalcCol( 0.75 * (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y)) / distDelta), centreCol, outerCol);
			}
			else{
				return CalcCol( 0.75 * (TriangleFunc(scaledUV.x) + TriangleFunc(scaledUV.y)), centreCol, outerCol);
			}
			
			/*
			float distDelta = (scaledUV.y - lastScaledUV.y);
			if (abs(distDelta) > 0.001){
				return CalcCol( 2.75 * (TriangleFunc2(scaledUV.x) + (TriangleFunc2Intg(scaledUV.y) - TriangleFunc2Intg(lastScaledUV.y)) / distDelta), centreCol, outerCol);
			}
			else{
				return CalcCol( 2.75 * (TriangleFunc2(scaledUV.x) + TriangleFunc2(scaledUV.y)), centreCol, outerCol);
			}*/
			
		}
		

	
		
		ENDCG
	}
	} 
	FallBack "Diffuse"
}
