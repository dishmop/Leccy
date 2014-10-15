Shader "Custom noise/Noise-Simplex02" {
Properties {
	_Freq ("Frequency", Float) = 20
	_Speed0 ("Speed0", Float) = 0
	_Speed1 ("Speed1", Float) = 0
	_Speed2 ("Speed2", Float) = 0
	_Speed3 ("Speed3", Float) = 0
	_StaticSpeed0 ("StaticSpeed0", Float) = 0.3
	_StaticSpeed1 ("StaticSpeed1", Float) = 0.3
	_StaticSpeed2 ("StaticSpeed2", Float) = 0.3
	_StaticSpeed3 ("StaticSpeed3", Float) = 0.3
	_Color0 ("Color0", Color) = (1.0, 1.0, 1.0)
	_Color1 ("Color1", Color) = (1.0, 1.0, 1.0)
	_BkColor0 ("BkColor0", Color) = (0.0, 0.0, 0.0)
	_BkColor1 ("BkColor1", Color) = (0.0, 0.0, 0.0)
//	_DetailScale("Detail scale", Float) = 2
	_EnableSquashing ("Enable Squashing", Range(0, 1)) = 0
	_EnableSquashing ("Enable Squashing", Float) = 0

}

SubShader {
	Pass {
		//Tags { "RenderType" = "Transparent" }
		CGPROGRAM
		
		#pragma target 3.0
		
		#pragma vertex vert
		#pragma fragment frag
		
		#include "noiseSimplex.cginc"
		 #include "UnityCG.cginc"
		
		struct v2f {
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
//			float3 srcPos1 : TEXCOORD1;
		};
		
		uniform float
			_Freq,
			_Speed0,
			_Speed1,
			_Speed2,
			_Speed3,
			_StaticSpeed0,
			_StaticSpeed1,
			_StaticSpeed2,
			_StaticSpeed3			
//			_DetailScale
			;
				

			
		uniform float4 _Color0;
		uniform float4 _BkColor0;
		uniform float4 _Color1;
		uniform float4 _BkColor1;
		uniform float _EnableSquashing;
		

		
		v2f vert(appdata_base v)
		{
			v2f o;

			o.pos =	mul(UNITY_MATRIX_MVP, v.vertex);
			
			o.uv = v.texcoord;

			
			return o;
		}
		
		float2 CalcCentreXY(float2 xy, float2 corner, float2 up){
			const  float pi = 3.14159;
	
		 	float2 ret;
		 	float2 pos;
		 	float2 relPos = xy - corner;
		 	float relPosLen = sqrt(relPos.x * relPos.x + relPos.y * relPos.y);
			float2 relPosNorm = relPos * 1.0 / relPosLen;
		 	float angle = acos(relPosNorm.x * up.x + relPosNorm.y * up.y);
			// scale the angle
			angle = 0.1 * angle / pi;	
			ret.x = relPosLen;
			ret.y = angle;	 
			return ret;
		}
		
		float4 frag(v2f i) : COLOR
		{
			const  float pi = 3.14159;
	
			// Work out the main noise evalue
			float4 noisePos;
			float3 upperNoisePos;	
			
			
			// Work out positions in texture we should be sampling
			// Make the y component map to a circle in Yz space

			// Hmm, the thing fo going round in a figure of 8 does not wrk when scrolling in y
			// Though does work when undulating in z
			// So revert back to single cirle - and maybe add variation through static undulating multiplier
			
//			 Go round twice so we can join in the middle (actually, we do a figure of 8)
//			float intPart;
//			//float scaledY = fmod(_Time.y * _Speed + i.uv.y, intPart);
//			float scaledY = i.uv.y;
//			intPart = (int)scaledY;
//			scaledY = scaledY - intPart;
//			
//			// Reduce to between 0 and 1
//			bool isInUpperHalf = (scaledY > 0.5f);
//			
//			scaledY += _Time.y * _Speed;
//			
//			// Scale so it fits in a circle
//			scaledY *= 4 * pi;
//			
//			noisePos.y = sin(scaledY);
//			if (isInUpperHalf){
//				noisePos.w = 2.0 - cos(scaledY);	
//				noisePos.w = 10f;
//				noisePos.y = 10f;
//			}
//			else{
//				noisePos.w = cos(scaledY);	
//			}
			int intValX = (int)(i.uv.x * 5);

			float2 i2uv = i.uv.xy;
			float speedParam = 0;
			float staticSpeedParam = 0;
			switch(intValX){
			 case 0:
			 	speedParam = _Speed0;
			 	staticSpeedParam = _StaticSpeed0;
			 	break;
			 case 1:
			 	speedParam = _Speed1;
			 	staticSpeedParam = _StaticSpeed1;
			 	break;
			 case 2:
			 	speedParam = _Speed2;
			 	staticSpeedParam = _StaticSpeed2;
			 	break;
			 case 3:
			 	speedParam = _Speed3;
			 	staticSpeedParam = _StaticSpeed3;
			 	break;
			 case 4:
			 	speedParam = 1;
			 	staticSpeedParam = 0.5;
			 	
			 	// figure out the weighting for each channel
			 	float speedCop0 = _Speed0;
			 	float speedCop1 = _Speed1;
			 	float speedCop2 = _Speed2;
			 	float speedCop3 = _Speed3;
			 	
			 	// Channels speed
			 	float right2Up = 0;
			 	float down2Right = 0;
			 	float left2Down = 0;
			 	float up2Left = 0;
			 	float up2Down = 0;
			 	float left2Right = 0;
			 	
			 	// First attribute stuff to the leftright updown channels

			 	// If pushing in different directions
			 	if (speedCop0 * speedCop1 < 0){
			 		float speed = min(abs(speedCop0), abs(speedCop1));
			 		if (speedCop0 > 0){
		 				right2Up = speed;
		 				speedCop0 -= speed;
		 				speedCop1 += speed;
		 			}
		 			else{
		 				right2Up = -speed;
		 				speedCop0 += speed;
		 				speedCop1 -= speed;		 				
		 			}

			 	}
		 	
			 	// If pushing in different directions
			 	if (speedCop1 * speedCop2 < 0){
			 		float speed = min(abs(speedCop1), abs(speedCop2));
			 		if (speedCop1 > 0){
		 				down2Right = speed;
		 				speedCop1 -= speed;
		 				speedCop2 += speed;		 				
		 			}
		 			else{
		 				down2Right = -speed;
		 				speedCop1 += speed;
		 				speedCop2 -= speed;		 				
		 			}

			 	}
			 	// If pushing in different directions
			 	if (speedCop2 * speedCop3 < 0){
			 		float speed = min(abs(speedCop2), abs(speedCop3));
			 		if (speedCop2 > 0){
		 				left2Down = speed;
		 				speedCop2 -= speed;
		 				speedCop3 += speed;		 				
		 			}
		 			else{
		 				left2Down = -speed;
		 				speedCop2 += speed;
		 				speedCop3 -= speed;		 				
		 			}

			 	}	
			 	// If pushing in different directions
			 	if (speedCop3 * speedCop0 < 0){
			 		float speed = min(abs(speedCop3), abs(speedCop0));
			 		if (speedCop3 > 0){
		 				up2Left = speed;
		 				speedCop3 -= speed;
		 				speedCop0 += speed;		 				
		 			}
		 			else{
		 				up2Left = -speed;
		 				speedCop3 += speed;
		 				speedCop0 -= speed;		 				
		 			}

			 	}				 				 		
			 	
			 			 					 				 					 		
			 	
			 	// If pushing in different directions
			 	if (speedCop0 * speedCop2 < 0){
			 		float speed = min(abs(speedCop0), abs(speedCop2));
		 			up2Down = -speed;

			 	}
			 	// If pushing in different directions
			 	if (speedCop1 * speedCop3 < 0){
			 		float speed = min(abs(speedCop1), abs(speedCop3));
		 			left2Right = speed;

			 	}				 	
			 	// Calc the max speed
			 	float speed = _Speed0;
			 	if (speed < _Speed1) speed = _Speed1;
			 	if (speed < _Speed2) speed = _Speed2;
			 	if (speed < _Speed3) speed = _Speed3;
			 	
			 	speedParam = speed;	
			 	float speedMul = 1.f / speed;
			 	right2Up *= speedMul;
			 	down2Right *= speedMul;
			 	left2Down *= speedMul;
			 	up2Left *= speedMul;
			 	up2Down *= speedMul;
			 	left2Right *= speedMul;
			 	
			 	i2uv.x = 0;
			 	i2uv.y = 0;
			 	

			 	
			 	// From right to up
			 	i2uv += right2Up * CalcCentreXY(i.uv.xy, float2(0.9, 0.45), float2(0, 1));
			 	// From down to right
			 	i2uv += down2Right * CalcCentreXY(i.uv.xy, float2(0.9, 0.55), float2(-1, 0));
			 	// From left to down
			 	i2uv += left2Down * CalcCentreXY(i.uv.xy, float2(0.8, 0.55), float2(0, -1));
			 	// From up to left
			 	i2uv += up2Left * CalcCentreXY(i.uv.xy, float2(0.8, 0.45), float2(1, 0));

			 	// From up to down
			 	i2uv += up2Down * i.uv.xy;
			 	// From left to right
			 	i2uv.x += left2Right * i.uv.y;
			 	i2uv.y += left2Right * i.uv.x;
			 	

			 	break;
			}
			
			float scaledY = (_Time.y * speedParam + i2uv.y) * 4 * pi;

			noisePos.x = (i2uv.x * 5);
			noisePos.x = noisePos.x  - intValX;
			noisePos.x = noisePos.x / 5.0f;
		
			if (_EnableSquashing > 0.5){
				noisePos.y = sin(scaledY) / (0.2 + speedParam);
				noisePos.w = cos(scaledY) / (0.2 + speedParam);	
			}
			else{
				noisePos.y = sin(scaledY);
				noisePos.w = cos(scaledY);	
			}
			
			noisePos.x *= _Freq;
			noisePos.y *= 0.25*_Freq /pi;
			noisePos.w *= 0.25*_Freq/pi;
			
			
			
			// Make them all animate in the same way
			//noisePos.y += _Time.y * _Speed;
			noisePos.z += _Time.y * staticSpeedParam;
			
			//noisePos *= _Freq;
			
			
			float nsThis = snoise(noisePos) / 2 + 0.5f;
			float colTrans = i.uv.y;
			
			float4 bkCol = lerp(_BkColor0, _BkColor1, colTrans);
			float4 fgCol = lerp(_Color0, _Color1, colTrans);
			float4 col = lerp(bkCol, fgCol, nsThis);
			return col;
		}
		
		ENDCG
	}
}

}