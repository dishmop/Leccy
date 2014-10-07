Shader "Custom/BlendGreyLightmap" {
	Properties{
		_MainTex ("Texture 1 (A - Lightmap)", 2D) = ""
		_Texture2 ("Texxture 2 (A - Mask)", 2D) = ""
		
	}
	
	Category{
		BindChannels{
			Bind "vertex", vertex
			Bind "texcoord", texcoord0
			Bind "texcoord1", texcoord1
			Bind "texcoord", texcoord2
			Bind "texcoord1", texcoord3
		}
		
		// iPhone 3Gs and Later
		SubShader{
			Pass{
				SetTexture[_MainTex] 
				SetTexture[_Texture2] {combine previous, texture}
				SetTexture[_Texture2] {combine texture Lerp(previous) previous}
				SetTexture[_MainTex]  {combine previous * texture alpha}
			}
		}
	}

}
