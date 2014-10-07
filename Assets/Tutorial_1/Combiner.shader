Shader "Custom/Combiner" {
	Properties{
		// name of property, string to appear in inspector, type of the property
		_Color ("Color", Color) = (1, 0, 0)
		_MainTex("Texture", 2D) = "" {}
	}

	SubShader {
		Pass{			
			Color[_Color]
			SetTexture[_MainTex] {Combine primary alpha * texture}
		}
		
	} 
}
