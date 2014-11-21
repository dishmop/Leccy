Shader "Custom/PlasmaBalls" {
	Properties {
		_Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Col0 ("Col0", Color) = (0.0, 0.0, 0.0)
		_Col1 ("Col1", Color) = (0.0, 0.0, 0.0)
		_Col2 ("Col2", Color) = (0.0, 0.0, 0.0)
		
		
		_AlphaRadius ("AlphaRadius", Range(0, 1)) = 0.1
		_AlphaRadius ("AlphaRadius", Float) = 0.1
		_ColRadius ("ColRadius", Range(0,1)) = 0.1
		_ColRadius ("ColRadius", Float) = 0.1
		_Spacing ("Spacing", Range(0, 20)) = 10
		_Spacing ("Spacing", Float) = 10
		_DoBalls ("Do Balls", Range(0, 1)) = 1

		_Speed0 ("Speed0", Range(0,15)) = 0
		_Speed0 ("Speed0", Float) = 0
		_Speed1 ("Speed1", Float) = 0
		_Speed2 ("Speed2", Float) = 0
		_Speed3 ("Speed3", Float) = 0	
		
		_Seperation0 ("Seperation0", Float) = 1
		_Seperation1 ("Seperation1", Float) = 1
		_Seperation2 ("Seperation2", Float) = 1
		_Seperation3 ("Seperation3", Float) = 1	
		
		_Voltage0("Voltage0", Float) = 0
		_Voltage1("Voltage1", Float) = 0
		_Voltage2("Voltage2", Float) = 0
		_Voltage3("Voltage3", Float) = 0
		
	//	_Voltae	("Voltage0", Float) = 0;				
	}
	SubShader {
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
		Tags {"Queue"="Transparent"}
		
	Pass {
		//Tags { "RenderType" = "Transparent" }
		CGPROGRAM
		#pragma target 3.0		
#pragma profileoption NumInstructionSlots=65534
#pragma profileoption NumMathInstructionSlots=65534
				
		#pragma vertex vert
		#pragma fragment frag
		
		 #include "UnityCG.cginc"
		
		struct v2f {
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
		};
		
		uniform float4 _Color;
		uniform float4 _Col0;
		uniform float4 _Col1;
		uniform float4 _Col2;
		uniform float4 _OuterColor3;
		uniform float _AlphaRadius;
		uniform float _ColRadius;
		uniform float _Spacing;
		uniform float _DoBalls;
		uniform float _Speed0;
		uniform float _Speed1;
		uniform float _Speed2;
		uniform float _Speed3;
		uniform float _Seperation0;	
		uniform float _Seperation1;
		uniform float _Seperation2;
		uniform float _Seperation3;
		uniform float _Voltage0;
		uniform float _Voltage1;
		uniform float _Voltage2;
		uniform float _Voltage3;
		
		
		v2f vert(appdata_base v)
		{
			v2f o;
			
			// Changes thickness of wires - Must change this in two places!
			float gamma =1;
			if (v.vertex.x > 0)
				v.vertex.x =  0.5 * pow(2 * v.vertex.x, gamma);
			else
				v.vertex.x =  -0.5 * pow(-2 * v.vertex.x, gamma);
			
			if (v.vertex.y > 0)
				v.vertex.y =  0.5 * pow(2 * v.vertex.y, gamma);
			else
				v.vertex.y =  -0.5 * pow(-2 * v.vertex.y, gamma);
			/*
			if (abs() < 0.5 - epsilon)
				v.vertex.x *= 1;
			if (abs(v.vertex.y) < 0.5 - epsilon)
				v.vertex.y *= 1;	
*/
			o.pos =	mul(UNITY_MATRIX_MVP, v.vertex);
			
			o.uv = v.texcoord;

			
			return o;
		}
		

		
		float4 CalcCol(float dist, float4 col){
			// These should be global constants
			float4 black = float4(0, 0, 0, 1);
		
			//return DCColorLerp(black, col , dist);
	
			return lerp(black, col , clamp(dist, 0, 1));
		}	
		
		// Returns function with straight slopes and period 1 where:
		// f (0) = 0
		// f(0.5) = 1
		// f(1) = 0
		// etc..
		float TriangleFunc(float x){
			float xx = frac(x);
			return abs(xx - 0.5)*2.0;
		}
		
		
		// Integral of the above function
		float TriangleFuncIntg(float x){
			// First work out number of entire cycles ad multiply by area under one cycle (0.5)
			float val0 = floor(x) * 0.5;
			
			
			// Now with the bit that is left, see if we are below 0.5
			float val1 = 0;
			float xx = frac(x);
			if (xx < 0.5){
				val1 = xx - xx * xx;
			}
			else{
				xx -= 0.5;
				val1 = 0.25 + xx*xx;
			}
			return val0 + val1;
		}
		
		// This is a square function
		float TriangleFunc2(float x){
		
			float xx = frac(x);
			float yy =  xx - 0.5;
			return yy * yy * 4;
		}
		
		// Integral of the above function
		float TriangleFunc2Intg(float x){
			// First work out number of entire cycles ad multiply by area under one cycle (0.5)
			float val0 = floor(x) /3;
			
			float xx = frac(x);
			float val1 = (4.0/3.0) * xx * xx * xx - 2.0 * xx *xx + xx;
			return val0 + val1;

		}	

		// etc..
		float TriangleFunc3(float x){
			float xx = frac(x);
			float yy =  xx - 0.5;
			return yy * yy * yy * yy * 16;
		}			
			
		
		// Integral of the above function
		float TriangleFunc3Intg(float x){
			// First work out number of entire cycles ad multiply by area under one cycle (0.5)
			float val0 = floor(x) /10;
			
			float xx = frac(x);
			float val1 = (16.0/5.0) * xx * xx * xx * xx  * xx  - 2.0 * xx *xx + xx;
			return val0 + val1;

		}		
		
		float TriangleFuncN(float x, float power){
			float xx = fmod(x + 0.5, 1);
			return 1/pow(abs(xx - 0.5), power) -1/pow(0.3, power);
		}
		
		/*
		float TriangleFuncNXY(float x, float y, float power){
			float thing = 0.5;
			float xx = 0;//abs(frac(x+thing)-thing);
			float yy = 0.5 - abs(frac(y) - thing);
			//float dist = sqrt(xx*xx+yy*yy);
			float dist = xx+yy;
			return pow(dist, -power) -pow(0.5, -power);
		}	
		*/
		
		float TriangleFuncNXY(float x, float y, float power){
			float xx = abs(frac(x + 0.5) - 0.5);
			float yy = abs(frac(y + 0.5) - 0.5);
			float dist = xx+yy;
			return pow(dist, -power) -pow(0.5, -power);
		}	

		float TriangleFuncNXYIntgY(float x, float y, float power){
			float xx = abs(frac(x + 0.5) - 0.5);
		
			// If the power is 1 then the integral is different
			if (abs(power - 1.0) < 0.0001){
				// Haven't worked this one out yet
				return 0;
			}
			else{
				float constant = -pow(xx, (1 - power))/(1 - power) + pow(0.5f, -power) * (xx);
				float halfCycleArea = constant + pow(xx + 0.5, (1 - power)) / (1 - power) - pow(0.5, -power) * (0.5+xx);
				float val0 = 2 * halfCycleArea * floor(y);
				
				// now suppose we are in the first half
				float val1 = 0;
				
				float yy = y - floor(y);
				
				
				if (yy < 0.5){
					val1 = pow(xx + yy, (1 - power))/(1 - power) - pow(0.5, -power) * (yy+xx) + constant;
				}
				else{
					float uu = 1 - yy;
					val1 = 2 * halfCycleArea - pow((xx + uu), 1 - power)/(1 - power) + pow(0.5, -power) * (uu+xx) - constant;
				}
				
				return val0 +val1;

			}
			
		}					
		
		
		
		float4 frag(v2f i) : COLOR
		{
			// Changes thickness of wires - Must change this in two places!
			float gamma = 1;
			
			float wireThickness = 2 * 0.5 * pow(2 * 0.05, gamma);
			
			float scaleX = 0.1 / wireThickness;
			
			
			
			float4 xRepeatedUV = frac(i.uv * float4(5, 1, 0, 0)) * float4(0.2, 1, 0, 0);
			
			xRepeatedUV.x /= scaleX;
			
						
			// Work out the speed on our part of the track
			float speedParam = 0;
			float seperationParam = 1;

			float voltage = 0;
			
			int intValX = floor(i.uv.x * 5);
			switch(intValX){
			 case 0:
			 	speedParam = _Speed0;
			 	seperationParam = _Seperation0;
			 	voltage = _Voltage0;
			 	break;
			 case 1:
			 	speedParam = _Speed1;
			 	seperationParam = _Seperation1;
			 	voltage = _Voltage1;
			 	break;
			 case 2:
			 	speedParam = _Speed2;
			 	seperationParam = _Seperation2;
			 	voltage = _Voltage2;
			 	break;
			 case 3:
			 	speedParam = _Speed3;
			 	seperationParam = _Seperation3;
			 	voltage = _Voltage3;
			 	break;
			 case 4:
			 	// do nothing
			 	break;
			}	
			// Maximum blue value
			if (abs(speedParam) > 15) speedParam = 15 * speedParam / abs(speedParam);
	
			// move with time
			float frameInterval = 1/30.0;
			
			
			
			// Add a bit on so the blue is never zero (this is  aproblem ifg speed ends up setting this to zero!)
			float4 timeModdedUV = xRepeatedUV + float4(0, (_Time.y + frameInterval) * speedParam + 0.0001f, 0, 0);
			
			// Get location of time at previous frame
			float4 lastTimeModdedUV = xRepeatedUV + float4(0, _Time.y * speedParam, 0, 0);
			
			
			
			/*
			
			float thisSpeedParam = 1;
			if (speedParam < 0) thisSpeedParam = -1;
			
			float4 timeModdedUV = xRepeatedUV + float4(0, (_Time.y + frameInterval) * thisSpeedParam, 0, 0);
			
			// Get location of time at previous frame
			float4 lastTimeModdedUV = xRepeatedUV + float4(0, _Time.y * thisSpeedParam, 0, 0);
			*/
			

			// Transform based on size, movement and spacing
			float4 originOffset = float4(0.05, 0.00, 0, 0) / scaleX;
			
			// Lets rotate the origina around a little
		//	float4 newOrigin = originOffset;
		//	newOrigin.x = originOffset.x + originOffset.x  * sin(1000*_Time.y/(2*3.14159));
			
			// Move so that origin is over correct place in texture and scale up so there is alarge gap between unit squares
			float4 vecSpacing = float4(_Spacing * 3, _Spacing, _Spacing, _Spacing);
			float4 scaledUV = (timeModdedUV - originOffset) * vecSpacing;
			float4 lastScaledUV = (lastTimeModdedUV - originOffset) * vecSpacing;
			
			scaledUV.y *= seperationParam;
			
			lastScaledUV.y *= seperationParam;
			
			// For the moment don't blur:
			//return CalcCol( TriangleFunc(scaledUV.x) + TriangleFunc(scaledUV.y));
			
			
			// We now assume the ball is centred at origin and has radius 1
			
			//if (i.uv.y < 0.05 || i.uv.y > 0.95) return 1;
			
		//	float amp = 0.5 - cos(speedParam * (i.uv.y) * 2 * 3.14159265);
			
			
			
			
			float distDelta = (scaledUV.y - lastScaledUV.y);
			float multVertical = 1;
			//return CalcCol( 0.5 * ((TriangleFunc(scaledUV.x * multVertical) + TriangleFunc(scaledUV.y))), centreCol, outerCol);
			float4 col0;
			float4 col1;
			float4 col2;
			float4 col3;
			float4 col4;
			
			float val0 = 0;	
			float val1 = 0;
			float val2 = 0;
			float val3 = 0;
			float val4 = 0;
			float val5 = 0;
			float val6 = 0;
			
			float mul0 = 0.5;
			float sub0 = 0 ;
			float power = 0.2;
			float amp = clamp(abs(speedParam) / 15 + 1, 1, 2);
			
//				float val0 = - sub0 + mul0 * (TriangleFunc2((scaledUV.x * multVertical) + xOffset) + (TriangleFunc2Intg(scaledUV.y) - TriangleFunc2Intg(lastScaledUV.y)) / distDelta);
//				float val1 = - sub0 + mul0 * (TriangleFunc2((scaledUV.x * multVertical) - xOffset) + (TriangleFunc2Intg(scaledUV.y) - TriangleFunc2Intg(lastScaledUV.y)) / distDelta);
//				col0 =  CalcCol( val0 + val1, centreCol);
///				col1 =  CalcCol( -.50 + 0.5 * (TriangleFunc(scaledUV.x * multVertical) * (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y)) / distDelta), outerCol);
			// Voltage 0 ->1
			float epsilon = 0.0001;
			float4 retCol;
			if (voltage < 1-epsilon){
				float xOffset = 0.1;
				
				val0 = -1.6 + 0.9 * (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				val3 = -0.6 + 0.65 * (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				
				col0 = CalcCol(amp * val0, lerp(float4(-.25, -.25, -.25, 1), 0.85 * _Col0, frac(voltage)));
				col1 = CalcCol(amp * val3, _Col0);
				
				retCol =  col1+ col0;
			}
			// Voltage 1 -> 2
			else if (voltage < 2-epsilon){
			 
				val0 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x , lastScaledUV.y, power))/ distDelta;
				val3 = -0.5 + 0.5* (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				// Bits of the next one
				float xOffset = 0.2;		
				
				val4 = -1.5 + 1.05 * (TriangleFunc(scaledUV.x + xOffset) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				val5 = -1.5 + 1.05 * (TriangleFunc(scaledUV.x - xOffset)+ (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);		

				col0 =  CalcCol(amp * val0, _Col1);
				col1 = CalcCol(amp * val3, _Col0);
				col2 =  CalcCol(amp * (val4 + val5), lerp(float4(-.25, -.25, -.25, 1), 0.85 * _Col0, frac(voltage)));				
				
				retCol =  col0 + col1 + col2;
			}
			else if (voltage < 3-epsilon){
			
				float xOffset = lerp(0.15, 0.2, frac(voltage));		
				val0 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x + xOffset , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x + xOffset , lastScaledUV.y, power))/ distDelta;
				val1 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x - xOffset , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x - xOffset , lastScaledUV.y, power))/ distDelta;
				val3 = -0.5 + 0.5 * (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				val4 = -1.0 + 0.5 * (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				
				col0 =  CalcCol(amp * (val0 + val1), _Col1);
				col1 = CalcCol(amp * val3, _Col0);
				col2 =  CalcCol(amp * val4, lerp(float4(-.25, -.25, -.25, 1), 0.85 * _Col0, frac(voltage)));

				
				retCol =  col0 + col1 + col2;
			}
			else if (voltage < 4-epsilon){
				float xOffset1 = 0.2;		
				float xOffset2 = 0.1;							
				val0 = -sub0 + mul0 * 1.4 * (TriangleFuncNXYIntgY(scaledUV.x + xOffset1 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x + xOffset1 , lastScaledUV.y, power))/ distDelta;
				val1 = -sub0 + mul0 * 1.4 * (TriangleFuncNXYIntgY(scaledUV.x - xOffset1 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x - xOffset1 , lastScaledUV.y, power))/ distDelta;
				val2 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x , lastScaledUV.y, power))/ distDelta;
				val3 = -0.5 + 0.25* (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				val4 = -0.5 + 0.5* (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				val5 = -1.5 + 1 * (TriangleFunc(scaledUV.x + xOffset1) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				val6 = -1.5 + 1 * (TriangleFunc(scaledUV.x - xOffset1) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);			
					
				col0 =  CalcCol(amp * (val0 + val1), _Col1);
				col1 = CalcCol(amp * val3, _Col2);
				col2 = CalcCol(amp * val4, _Col0);
				col3 =  CalcCol(amp * val2, _Col2);	
				col4 =  CalcCol(amp * (val5 + val6), lerp(float4(-.25, -.25, -.25, 1), 0.85 * _Col2, frac(voltage)));					
				
				retCol =  col0 + col1 + col2 + col3 + col4;
			}
			else if (voltage < 5-epsilon){
				float xOffset0 = 0.1;		
				float xOffset1 = 0.2;		
				val0 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x + xOffset0 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x + xOffset0 , lastScaledUV.y, power))/ distDelta;
				val1 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x - xOffset0 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x - xOffset0 , lastScaledUV.y, power))/ distDelta;
				val2 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x + xOffset1 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x + xOffset1 , lastScaledUV.y, power))/ distDelta;
				val3 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x - xOffset1 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x - xOffset1 , lastScaledUV.y, power))/ distDelta;
				val4 = -0.5 + 0.35 * (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);			
				col0 = CalcCol(amp * (val0 + val1), _Col2);
				col1 = CalcCol(amp * (val2 + val3), _Col1);
				col2 = CalcCol(amp * val4, _Col2);
				col3 = CalcCol(amp * val4, lerp(float4(-.25, -.25, -.25, 1), 0.85 * _Col2, frac(voltage)));

				retCol =  col0 + col1 + col2 + col3;
				
			}
			else {//if (voltage < 6-epsilon){
				float xOffset0 = 0.1;		
				float xOffset1 = 0.2;		
				val0 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x + xOffset0 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x + xOffset0 , lastScaledUV.y, power))/ distDelta;
				val1 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x - xOffset0 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x - xOffset0 , lastScaledUV.y, power))/ distDelta;
				val2 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x + xOffset1 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x + xOffset1 , lastScaledUV.y, power))/ distDelta;
				val3 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x - xOffset1 , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x - xOffset1 , lastScaledUV.y, power))/ distDelta;
				val4 = -sub0 + mul0 * (TriangleFuncNXYIntgY(scaledUV.x , scaledUV.y, power) - TriangleFuncNXYIntgY(scaledUV.x , lastScaledUV.y, power))/ distDelta;
				val5 = -0.5 + 0.45* (TriangleFunc(scaledUV.x) + (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta);
				
				col0 = CalcCol(amp * (val0 + val1 + val2 + val3 + val4), _Col1);
				col2 = CalcCol(amp * val5, _Col2);
				retCol =  col0 + col2;
			}									
			

			return retCol * _Color;
			
			
		}
		

	
		
		ENDCG
	}
	} 
	FallBack "Diffuse"
}
