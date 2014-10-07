using UnityEngine;
//using System.Collections;

public class GUITextDisplay{

	float leftMargin = 	10f;
	float topMargin = 	70f;
	float lineHeight = 	20f;
	float maxLineLen = 	500f;
	
	// This gets reset at beginning of each call
	int lineNum = 0;	
	
	public GUITextDisplay(float left, float top, float maxLineLen, float lineHeight){
		leftMargin = 	left;
		topMargin = 	top;
		lineHeight = 	maxLineLen;
		maxLineLen = 	lineHeight;
	}



	
	public void GUIPrintText(string text, Color col){
		GUI.contentColor = col;
		GUI.Label (new Rect(leftMargin, topMargin + lineNum++ * lineHeight, maxLineLen, lineHeight), text);
	}
	
	public void GUIResetTextLayout(){
		lineNum = 0;
	}
}
