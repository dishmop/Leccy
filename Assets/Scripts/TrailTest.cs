using UnityEngine;
using System.Collections;

public class TrailTest : MonoBehaviour {

	
	// Where to draw
	public GameObject	screenGO;
	
	// The pipe
	public float		pipeWidthPercOfScreen = 80;
	public float		pipeHeightPercOfPipeLength = 20;
	public float		pipeRealLength = 1f;
	
	// the Sim
	public float		simSampleInterval = 1f;
	public float		simTimeStep = 0.001f;
	
	// the ball
	public float		ballRadiusPerc = 40f;
	public float		ballSpeed = 0f;
	public float		ballRPS = 10f;
	public float		ballsPerPipe = 5;
	
	
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
	
	void DrawBall(float time, float xOffsetFrac){
	
	
		float radius = ballRadiusPerc * pipeHeight / 100f;

		
		// Create  a bright spot
		Vector2 ballCentre = new Vector2(pipeLeft + (xOffsetFrac + time * ballSpeed) * pipeWidth / pipeRealLength, (pipeTop + pipeBottom) * 0.5f);
		
		DrawSpot(ballCentre, radius, ballRPS, 0f, time, new Color(1f, 0.5f, 0.5f));
		DrawSpot(ballCentre, radius, -ballRPS, 0.5f, time, new Color(1f, 0.5f, 0.5f));	
	//	DrawSpot(ballCentre, radius, ballRPS * 2f, 0f, time, new Color(0f, 1f, 0.5f));
	//	DrawSpot(ballCentre, radius, -ballRPS * 2f, 0.5f, time, new Color(0f, 1f, 0.5f));
		
	}
	
	void DrawSpot(Vector2 centre, float radius, float rps, float phase, float time, Color col ){
		
		// Spot posiion
		Vector2 spotPos = new Vector2();
		
		spotPos.x = centre.x + radius * Mathf.Sin ((phase + time * rps) * 2 * Mathf.PI);
		spotPos.y = centre.y + radius * Mathf.Cos ((phase + time * rps) * 2 * Mathf.PI);
		
		SetPixel((int)Mathf.Round(spotPos.x), (int)Mathf.Round (spotPos.y), col);
		
		
		
	}
	
	void UploadPixels(){
		screenTexture.SetPixels(pixelData);
		screenTexture.Apply();		
		
	}
	
	void DrawBalls(float time){
		
		for (float i = -10*ballsPerPipe; i < 10*ballsPerPipe; ++i){
			DrawBall (time, i / ballsPerPipe);
		}
		
		DrawBall (time, 0);
		
	}
	
	void DrawWaves(){
		float epsilon = 0.0001f;
		if (pipeRealLength <epsilon) return;
		if (simSampleInterval < epsilon) return;
		if (simTimeStep < epsilon) return;
		
		for (float t = 0; t < simSampleInterval; t += simTimeStep){
			DrawBalls (t);
		}
	}
	
	// Update is called once per frame
	void Update () {
		ClearScreen();
		CalcPipeStats();
		DrawPipe();
		DrawWaves();

		UploadPixels();
	}
}
