using UnityEngine;
using System.Collections;

public class Grid : MonoBehaviour {

	public int gridWidth;
	public int gridHeight;
	public float squareWidth = 1f;
	public float squareHeight = 1f;	
	public GameObject gridSquarePrefab;
	public GameObject highlightPrefab;
	public GameObject eraseHighlightPrefab;	
	
	GUITextDisplay	guiTextDisplay;
	
	
	GameObject highlightPrefabToUse;	
	
	//GameObject[,]	gridSquares;
	
	GameObject		highlightSquare;
	GridPoint		selection = new GridPoint();
	GridPoint		newSelection = new GridPoint();
	GridPoint		otherSelection = new GridPoint();
	GridPoint		otherNewSelection = new GridPoint();
	bool			enableEraseHighlight =false;
	
	public void SetSelected(GridPoint newPoint, GridPoint otherPoint){
		newSelection = new GridPoint(newPoint);
		otherNewSelection = new GridPoint(otherPoint);
		
	}
	
	public void EnableEraseHighlightMode(bool enable){
	
		enableEraseHighlight = enable;
		if (enable){
			highlightPrefabToUse = eraseHighlightPrefab;
		}
		else{
			highlightPrefabToUse = highlightPrefab;
		}
		
	}
	
	void Awake(){
	}
	

	// Use this for initialization
	void Start () {

		highlightPrefabToUse = highlightPrefab;
		
		for (int x = 0; x < gridWidth; ++x){
			for (int y = 0; y < gridHeight; ++y){
				GameObject gridSquare = Instantiate(
					gridSquarePrefab, 
					new Vector3(x * squareWidth, y * squareHeight, 0), 
					Quaternion.identity)
					as GameObject;
				gridSquare.transform.parent = transform;
			}
			
		}
		
		guiTextDisplay = new GUITextDisplay(10f, 200f, 500f, 20f);
	
	}
	
	
	// Update is called once per frame
	void Update () {
			
		if (newSelection.x != selection.x || newSelection.y != selection.y ||
		    otherNewSelection.x != otherSelection.x || otherNewSelection.y != otherSelection.y){
		    
			GameObject.Destroy(highlightSquare);
			
			if (newSelection.x > 0 && newSelection.x < gridWidth && newSelection.y > 0 && newSelection.y < gridHeight)
			{
				selection = newSelection;
			}
			else
			{
				selection = new GridPoint();
			}
			
			if (otherNewSelection.x > 0 && otherNewSelection.x < gridWidth && otherNewSelection.y > 0 && otherNewSelection.y < gridHeight)
			{
				otherSelection = otherNewSelection;
			}
			else
			{
				otherSelection = new GridPoint();
			}			
			
			if (selection.IsValid()){
				if (!enableEraseHighlight || !otherSelection.IsValid ()){
					highlightSquare = Instantiate(
						highlightPrefabToUse, 
						new Vector3(selection.x, selection.y, highlightPrefab.transform.position.z), 
						Quaternion.identity)
						as GameObject;
					highlightSquare.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
					highlightSquare.transform.parent = transform;
				}
				// This is if we have a connection highlighted
				else{
					highlightSquare = Instantiate(
						highlightPrefabToUse, 
						new Vector3((selection.x + otherSelection.x) * 0.5f, (selection.y + otherSelection.y) * 0.5f, highlightPrefab.transform.position.z), 
						Quaternion.identity)
						as GameObject;
					highlightSquare.transform.parent = transform;
					if (selection.x == otherSelection.x)
						highlightSquare.transform.localScale = new Vector3(0.5f, 1f, 1f);
					else
						highlightSquare.transform.localScale = new Vector3(1f, 0.5f, 1f);
				}
			}

		}
		
	
	}
	
	void OnGUI () {
		guiTextDisplay.GUIResetTextLayout();
		guiTextDisplay.GUIPrintText( "Selected Gird Position: " + selection.x + ", " + selection.y, Color.yellow);
		
	}
	

}
