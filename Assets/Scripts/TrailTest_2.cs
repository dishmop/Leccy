using UnityEngine;
using System.Collections;

public class TrailTest_2 : MonoBehaviour {

	
	// Where to draw
	public GameObject	screenGO;
	
	// The pipe
	public float		pipeWidthPercOfScreen = 80;
	public float		pipeHeightPercOfPipeLength = 20;
	public float		pipeRealLength = 1f;
	
	// The blur
	public float		blurDist = 0.1f;
	
	
	// Cacl these from pipe size, given it is centred
	int pipeWidth;
	int pipeHeight;
	int	pipeLeft;
	int pipeBottom;
	int pipeRight;
	int pipeTop;
	
	// Our own local copy of the pixel dasta
	int			width;
	int 		height;
	Color[]		pixelData;
	
	
	
	// Private members
	Material 	screenMaterial;
	Texture2D	screenTexture;
	

	// Use this for initialization
	void Start () {
		screenMaterial = screenGO.GetComponent<MeshRenderer>().material;
		screenTexture = screenMaterial.GetTexture(0) as Texture2D;	
		width = screenTexture.width;
		height = screenTexture.height;
		pixelData = new Color[width * height];
		
		ClearScreen();		
		CalcPipeStats();
	
	}
	
	void ClearScreen(){
		for (int i = 0; i < width * height; ++i){
			pixelData[i] = Color.black;
		}
	}
	
	void CalcPipeStats(){
		int centreX = width / 2;
		int centreY = height / 2;
		pipeWidth 	= (int)(pipeWidthPercOfScreen * width / 100f);
		pipeHeight 	= (int)(pipeHeightPercOfPipeLength * (pipeWidthPercOfScreen * width / 100f) / 100f);
		pipeLeft 	= centreX - pipeWidth / 2;
		pipeBottom 	= centreY - pipeHeight / 2;
		pipeRight 	= centreX + pipeWidth / 2;
		pipeTop		= centreY + pipeHeight / 2;
	}
	
	// Draw a white line around the outside of the pipe
	void DrawPipe(){
		// top and bottom edge
		for (int x = pipeLeft-1; x < pipeRight + 1; ++x){
			SetPixel(x, pipeBottom-1, Color.white);
			SetPixel(x, pipeTop, Color.white);
		}
		// Left and right edges
		for (int y = pipeBottom-1; y < pipeTop + 1; ++y){
			SetPixel(pipeLeft - 1, y, Color.white);
			SetPixel(pipeRight,  y, Color.white);
		}		
	}
	
	void SetPixel(int x, int y, Color col){
		if (x < 0 || x >= width || y < 0 || y >= height) return;
		
		pixelData[x + y * width] = col;
	}
	
	Color GetPixel(int x, int y){
		if (x < 0 || x >= width || y < 0 || y >= height) return Color.black;
		return pixelData[x + y * width];
	}
	
	// Draws a pixel with additive blending 	
	void AddPixel(int x, int y, Color addCol){
		int index = x + y * width;
		pixelData[index] += addCol;
	}

	
	
	void UploadPixels(){
		screenTexture.SetPixels(pixelData);
		screenTexture.Apply();		
		
	}
	
	
	float Frac(float val){
		return val - Mathf.Floor (val);
	}
	
	float CalcColorVal(float x, float y, float power){
		float xx = Mathf.Abs(Frac(x + 0.5f) - 0.5f);
		float yy = Mathf.Abs(Frac(y + 0.5f) - 0.5f);
		return Mathf.Pow(xx + yy, -power) - Mathf.Pow (0.5f, -power);
	}
	
	
	float CalcColorValIntg(float x, float y, float power){
	/*
		float y1 = y + 0.5f;
		float y2 = y1 - Mathf.Floor (y1);
		float y3 = Mathf.Abs(y2 - 0.5f);
		float yy = y3;
		*/
		float yy = Mathf.Abs(Frac(y + 0.5f) - 0.5f);
		
		
		// If the power is 1 then the integral is different
		if (Mathf.Abs(power - 1.0f) < 0.0001f){
			// Haven't worked this one out yet
			return 0;
		}
		else{
			float constant = -Mathf.Pow(yy, 1f - power)/(1f - power) + Mathf.Pow(0.5f, -power) * (yy);
			float halfCycleArea = constant + Mathf.Pow(0.5f + yy, 1f - power) / (1f - power) - Mathf.Pow(0.5f, -power) * (0.5f+yy);
			float val0 = 2f * halfCycleArea * Mathf.Floor(x);
			
			// now suppose we are in the first half
			float val1 = 0f;
			
			float xx = x - Mathf.Floor(x);
			
			
			if (xx < 0.5f){
				val1 = Mathf.Pow(xx + yy, 1f - power)/(1f - power) - Mathf.Pow(0.5f, -power) * (xx+yy) + constant;
			}
			else{
				float uu = 1f - xx;
				val1 = 2f * halfCycleArea - Mathf.Pow( (uu + yy), 1f - power)/(1f - power) + Mathf.Pow(0.5f, -power) * (uu+yy) - constant;
			}
			
			return val0 +val1;
			
		}
	}	
	
	void DrawPlasma(){
		float pipeCentreX = (pipeLeft + pipeRight) / 2f;
		float pipeCentreY = (pipeTop + pipeBottom) /2f;
		
		/*
		for (int y = (int)(pipeCentreY + 20f); y < (int)(pipeCentreY + 20f) + 1; ++y){
			for (int x = (int)(pipeCentreX) + -10; x < (int)(pipeCentreX) + 20; ++x){
				*/
				
		for (int y = pipeBottom; y < pipeTop; ++y){
			for (int x = pipeLeft; x < pipeRight; ++x){
			
			 
		/*	 int x = (int)(pipeCentreX);
			 int y = (int)(pipeCentreY);
			{
			 {
			 */
				
						
				float val = 0;
				if (Mathf.Abs(blurDist) < 0.001f) {
					val = CalcColorVal((x - pipeCentreX) / pipeHeight, ((y - pipeCentreY)/ pipeHeight), 0.1f);
				}
				else{
					float val0 = CalcColorValIntg((x - pipeCentreX) / pipeHeight + blurDist, ((y - pipeCentreY)/ pipeHeight), 0.1f);
					float val1 = CalcColorValIntg((x - pipeCentreX) / pipeHeight, ((y - pipeCentreY)/ pipeHeight), 0.1f);
					
					val = ( val0 - val1) / blurDist;
				}
				
				
				Color col = Color.Lerp (Color.black, Color.white, val);
				SetPixel(x, y, col);
			}
		}
	
	}


	
	// Update is called once per frame
	void Update () {
		ClearScreen();
		CalcPipeStats();
		DrawPipe();
		DrawPlasma();

		
		
		UploadPixels();
	}
}
