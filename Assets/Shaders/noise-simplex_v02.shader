Shader "Custom noise/Noise-Simplex02" {
Properties {
	_Freq ("Frequency", Float) = 20
	_Speed ("Speed", Float) = 0
	_StaticSpeed ("StaticSpeed", Float) = 0.3
	_Color0 ("Color0", Color) = (1.0, 1.0, 1.0)
	_Color1 ("Color1", Color) = (1.0, 1.0, 1.0)
	_BkColor0 ("BkColor0", Color) = (0.0, 0.0, 0.0)
	_BkColor1 ("BkColor1", Color) = (0.0, 0.0, 0.0)
//	_DetailScale("Detail scale", Float) = 2
	_Overlap ("Overlap", Range(0,1)) = 0
	_Overlap ("Overlap", Float) = 0
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
			_Speed,
			_StaticSpeed
//			_DetailScale
			;
			
		uniform float4 _Color0;
		uniform float4 _BkColor0;
		uniform float4 _Color1;
		uniform float4 _BkColor1;
		
		uniform float _Overlap = 0.1;
		
		v2f vert(appdata_base v)
		{
			v2f o;

			o.pos =	mul(UNITY_MATRIX_MVP, v.vertex);
			
			o.uv = v.texcoord;

			
			return o;
		}
		
		float4 frag(v2f i) : COLOR
		{
			// Work out the main noise evalue
			float4 noisePos;
			float3 upperNoisePos;	
			
			// Work out positions in texture we should be sampling
			// Make the y component map to a circle in Yz space
			noisePos.x = i.uv.x;
			const float pi = 3.14159;
			float scaledY = _Time.y * _Speed + i.uv.y * 2 * pi;
			noisePos.y = sin(scaledY);
			noisePos.w = cos(scaledY);
			
			noisePos.x *= _Freq;
			noisePos.y *= 0.5*_Freq/pi;
			noisePos.w *= 0.5*_Freq/pi;
			
			
			
			// Make them all animate in the same way
			//noisePos.y += _Time.y * _Speed;
			noisePos.z += _Time.y * _StaticSpeed;
			
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