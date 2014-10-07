Shader "Custom/VertexColorLerp" {
	Properties{
		_Color ("Solid Color (A-Blend)", Color) = (1, 1, 1)
		_Number ("Number", Range(0,1)) = 1
		_Number ("Number", Float) = 1
	}

	SubShader {
		BindChannels{
			Bind "vertex", vertex
			Bind "color", color
		}
		Pass{		
			SetTexture[_]{
				ConstantColor [_Color]
				combine constant Lerp(constant) primary
			}	
				
		}
		
	} 
}
