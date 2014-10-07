Shader "Custom noise/Noise-Simplex" {
Properties {
	_Freq ("Frequency", Float) = 20
	_Speed ("Speed", Float) = 0
	_StaticSpeed ("StaticSpeed", Float) = 0.3
	_Color ("Color", Color) = (1.0, 1.0, 1.0)
	_BkColor ("BkColor", Color) = (0.0, 0.0, 0.0)
	_DetailScale("Detail scale", Float) = 2
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
			float3 srcPos0 : TEXCOORD0;
			float3 srcPos1 : TEXCOORD1;
		};
		
		uniform float
			_Freq,
			_Speed,
			_StaticSpeed,	
			_DetailScale
			;
			
		uniform float4 _Color;
		uniform float4 _BkColor;
		
		v2f vert(appdata_base v)
		{
			v2f o;

			o.pos =	mul(UNITY_MATRIX_MVP, v.vertex);
			
			o.srcPos0 = mul(_Object2World, v.vertex).xyz;
			o.srcPos0 *= _Freq;
			o.srcPos0.y += _Time.y * _Speed;
			o.srcPos0.z += _Time.y * _StaticSpeed;
			
			
			o.srcPos1 = mul(_Object2World, v.vertex).xyz;
			o.srcPos1 *= _Freq;
			o.srcPos1.y += _Time.y * _Speed;
			o.srcPos1.z += _Time.y * _StaticSpeed / _DetailScale;
			
			return o;
		}
		
		float4 frag(v2f i) : COLOR
		{
			float ns = snoise(i.srcPos0) / 2 + 0.5f;
			float ns2 = snoise(_DetailScale * i.srcPos1) / 2 + 0.5f;

			return _BkColor + ns * ns2 * (_Color  - _BkColor);
//			return _BkColor + ns * (_Color  - _BkColor);
		}
		
		ENDCG
	}
}

}