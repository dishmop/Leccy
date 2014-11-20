Shader "Custom/SolidColor" {

	Properties{
		// name of property, string to appear in inspector, type of the property
		_Color ("Color", Color) = (1, 0, 0)
	}

	SubShader {
        ZTest Always
        
        Color[_Color]
		Pass{

		}
		
	} 
}
