﻿Shader "Custom/SolidColor" {

	Properties{
		// name of property, string to appear in inspector, type of the property
		_Color ("Color", Color) = (1, 0, 0)
	}

	SubShader {
		Color[_Color]
		Pass{}
		
	} 
}