Shader "Custom/PlasmaBalls_new" {
	Properties {


		_Speed0 ("Speed0", Range(0,15)) = 0
		_Speed0 ("Speed0", Float) = 0
		_Speed1 ("Speed1", Float) = 0
		_Speed2 ("Speed2", Float) = 0
		_Speed3 ("Speed3", Float) = 0	
		
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

				
		#pragma vertex vert
		#pragma fragment frag
		
		 #include "UnityCG.cginc"
		
		struct v2f {
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
		};
		
		uniform float _Speed0;
		uniform float _Speed1;
		uniform float _Speed2;
		uniform float _Speed3;
		uniform float _Voltage0;
		uniform float _Voltage1;
		uniform float _Voltage2;
		uniform float _Voltage3;
		
		
		v2f vert(appdata_base v)
		{
			v2f o;
			

			o.pos =	mul(UNITY_MATRIX_MVP, v.vertex);
			
			o.uv = v.texcoord;

			
			return o;
		}
		

		
		float4 CalcCol(float dist){
			// These should be global constants
			float4 black = float4(0, 0, 0, 1);
			float4 white = float4(1, 1, 1, 1);

	
			return lerp(black, white , clamp(dist, 0, 1));
		}	
		
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
		
		

		
		float TriangleFuncNXYFlat(float x, float y, float power, float flatSize){
			float xx = 0;
			if (abs(x) > flatSize)
			{	
				if (x > 0)
					xx = abs(frac(x-flatSize + 0.5) - 0.5);
				else
					xx = abs(frac(x+flatSize + 0.5) - 0.5);
			}
	
			float yy = abs(frac(y + 0.5) - 0.5);
			float dist = xx+yy;
			return pow(dist, -power) -pow(0.5, -power);
		}	

		
		float DCPowMinusZeropointtwo(float x)
		{
		//	return 0.4536 * x*x*x - 0.8983 * x*x + 1.4536 * x + 0.0164;

		//	float xx = 1-x;
			//return 1 - 0.2*xx + 0.12*xx*xx - 0.088*xx*xx*xx + 0.0704*xx*xx*xx*xx - 0.59136*xx*xx*xx*xx*xx;
			return pow(x, -0.2);
		}
		
		float4 DCPowOneMinusZeropointtwo(float4 x)
		{
//			return -17.004*x*x*x*x*x*x + 42.723*x*x*x*x*x - 42.414*x*x*x*x + 21.359*x*x*x - 6.0661*x*x + 1.9993*x + 0.0028;
			return pow(x, 1-0.2);
		}
		
		float TriangleFuncNXY(float x, float y, float power)
			{
			float xx = abs(frac(x + 0.5) - 0.5);
			float yy = abs(frac(y + 0.5) - 0.5);
			float dist = xx+yy;
			return pow(dist, -power) -pow(0.5, -power);
		}	

		float TriangleFuncNXYFlatIntgY(float x, float y, float power, float flatSize){
			float xAbs = abs(x);
			float adjustedX = (xAbs-flatSize) * (1 - 2 * (x < 0));
			
			float xx = (xAbs > flatSize) * abs(frac(adjustedX + 0.5) - 0.5);
			float yy = frac(y);
			float uu = 1 - yy;
			
			float4 pow1MinusIn = float4(xx, xx + 0.5, xx + yy, (xx + uu));
			float4 pow1MinusOut = DCPowOneMinusZeropointtwo(pow1MinusIn);
			
			float powHalfMinus = pow(0.5, -power);
			
		
			float constant = -pow1MinusOut[0]/(1 - power) + powHalfMinus * (xx);
			float halfCycleArea = constant + pow1MinusOut[1] / (1 - power) - powHalfMinus * (0.5+xx);
			float val0 = 2 * halfCycleArea * floor(y);
			
			// now suppose we are in the first half
			float val1 = 0;
			
			
			
			if (yy < 0.5){
				val1 = pow1MinusOut[2]/(1 - power) - powHalfMinus * (yy+xx) + constant;
			}
			else{
				val1 = 2 * halfCycleArea - pow1MinusOut[3]/(1 - power) + powHalfMinus * (uu+xx) - constant;
			}
			
			return val0 +val1;

		}					
		

		float TriangleFuncNXYFlatIntgY1(float x, float y, float power, float flatSize){
			float xAbs = abs(x);
			float adjustedX = (xAbs-flatSize) * (1 - 2 * (x < 0));
			
			float xx = (xAbs > flatSize) * abs(frac(adjustedX + 0.5) - 0.5);
			
			
		
			float yy = frac(y);
			float uu = 1 - yy;
			
			// Create a vector to do all the pow operations in two shots
			float4 pow1MinusIn = float4(xx, xx + 0.5, xx + yy, (xx + uu));
			float4 pow1MinusOut = DCPowOneMinusZeropointtwo(pow1MinusIn);
			
			
			float powhalfMinus = DCPowMinusZeropointtwo(0.5f);
			
			const float constant = -pow1MinusOut[0]/(1 - power) + powhalfMinus * (xx);

			float halfCycleArea = constant + pow1MinusOut[1] / (1 - power) - powhalfMinus * (0.5+xx);
			float val0 = 2 * halfCycleArea * floor(y);
			
			
			
			if (yy < 0.5){
				return val0 + pow1MinusOut[2]/(1 - power) - powhalfMinus * (yy+xx) + constant;
			}
			else{
				return val0 + 2 * halfCycleArea - pow1MinusOut[3]/(1 - power) + powhalfMinus * (uu+xx) - constant;
			}
				

			
		}						
			

		
		float4 frag(v2f i) : COLOR
		{

			float4 xRepeatedUV = frac(i.uv * float4(5, 1, 0, 0)) * float4(0.2, 1, 0, 0);

			
						
			// Work out the speed on our part of the track
	
			
			int intValX = floor(i.uv.x * 5);
			float4 speedArray =  float4(_Speed0, _Speed1, _Speed2, _Speed3);
			float4 voltArray =  float4(_Voltage0, _Voltage1, _Voltage2, _Voltage3);
			
			float speedParam = float(intValX == 0) * _Speed0 + float(intValX == 1) * _Speed1 + float(intValX == 2) * _Speed2 + float(intValX == 3) * _Speed3;
			float voltage = float(intValX == 0) * _Voltage0 + float(intValX == 1) * _Voltage1 + float(intValX == 2) * _Voltage2 + float(intValX == 3) * _Voltage3;
			float seperationParam = 1;
	
			// Maximum blur value
			// move with time
			float frameInterval = 1/30.0;
			
			
			
			// Add a bit on so the blur is never zero (otherwise the intergral fails - and more instructions to make it cope with zero blur)
			float4 timeModdedUV = xRepeatedUV + float4(0, (_Time.y + frameInterval) * speedParam + 0.0001f, 0, 0);
			
			// Get location of time at previous frame
			float4 lastTimeModdedUV = xRepeatedUV + float4(0, _Time.y * speedParam, 0, 0);
			
			
			// Transform based on size, movement and spacing
			float4 originOffset = float4(0.05, 0.00, 0, 0);
			
			
			// Move so that origin is over correct place in texture and scale up so there is alarge gap between unit squares
			float spacing = 2;
			float4 vecSpacing = float4(spacing * 3, spacing, spacing, spacing);
			float4 scaledUV = (timeModdedUV - originOffset) * vecSpacing;
			float4 lastScaledUV = (lastTimeModdedUV - originOffset) * vecSpacing;
			
			scaledUV.y *= seperationParam;
			
			lastScaledUV.y *= seperationParam;
					
			
			
			
			float distDelta = (scaledUV.y - lastScaledUV.y);
			float multVertical = 1;

			
			float mul0 = 0.5;
			float sub0 = 0 ;
			float power = 0.2;
			float amp = clamp(abs(speedParam) / 15 + 1, 1, 2);
			
			
			float epsilon = 0.0001;
			float4 retCol;
			
			float maxVolts = 5;
			float voltageScaled = voltage / maxVolts;
			//float val0 = -sub0 + mul0 * (TriangleFuncNXYFlatIntgY(scaledUV.x , scaledUV.y, power, voltageScaled) - TriangleFuncNXYFlatIntgY(scaledUV.x , lastScaledUV.y, power, voltageScaled))/ distDelta;
		//	float val0 = -sub0 + mul0 * (TriangleFuncNXYFlat(scaledUV.x , scaledUV.y, power, voltageScaled));
			//float val0 = (0.5+voltageScaled) *  TriangleFunc( scaledUV.x) * TriangleFunc( scaledUV.y);
			float val0 = (0.5+voltageScaled) *  (TriangleFunc( scaledUV.x) * (TriangleFuncIntg( scaledUV.y) - TriangleFuncIntg( lastScaledUV.y))) / distDelta;
			
			//val0 = 1.5-0.2 *  pow (val0, -2);
			//float val0 = -0.5 + voltageScaled + TriangleFunc(scaledUV.x) * (TriangleFuncIntg(scaledUV.y) - TriangleFuncIntg(lastScaledUV.y))/ distDelta;
			return  CalcCol(amp * val0);

			
			
		}
		

	
		
		ENDCG
	}
	} 
	FallBack "Diffuse"
}
