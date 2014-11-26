using UnityEngine;
using System;
using System.Collections;

public class UI : MonoBehaviour {

	public static UI singleton;
	
	public AudioSource placeElementSound;
	public AudioSource removeElementSound;
	public AudioSource failSound;

	string 		selectedPrefabId;
	GameObject	ghostElement;
	GridPoint	thisPoint;
	GridPoint	otherPoint;
	Vector3		worldPos;
	bool		buttonIsHeld;
	GridPoint	lastPoint;
	GridPoint	lastOtherPoint;
	
	bool		isInUI;
	
	// Temporary debug thing
	GUITextDisplay	guiTextDisplay;
	
	
	public void SetSelectedPrefabId(string id){
		selectedPrefabId = id; 
		GameObject.Destroy (ghostElement);
		
		// We can't get the factory to instantate it otherwise it will reduce the number of the we have left
		GameObject prefab = ElementFactory.singleton.GetPrefab(selectedPrefabId);
		ghostElement = Instantiate(prefab) as GameObject;
		ghostElement.transform.parent = transform;
		ghostElement.GetComponent<CircuitElement>().SetAlpha(0.35f);
		ghostElement.SetActive(true);	
	}
	
	public void OnEnterUI(){
		Debug.Log("OnEnterUI()");
		isInUI = true;
	}
	
	public void OnExitUI(){
		Debug.Log("OnExitUI()");
		isInUI = false;
	}


	// Use this for initialization
	void Awake () {
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
		
	}
	
	
	void Start(){
		guiTextDisplay = new GUITextDisplay(400f, 200f, 500f, 20f);
	}
	
	void OnDestroy () {
		singleton = null;
	
	}
	
	
	void CalcCurrentGridPoints(){
		// Get mouse pointer position in world and circuite space
		Vector3 mousePos = Input.mousePosition;
		mousePos.z = transform.position.z - Camera.main.transform.position.z;
		worldPos = Camera.main.ScreenToWorldPoint( mousePos);
		thisPoint = new GridPoint((int)(worldPos.x + 0.5f), (int)(worldPos.y + 0.5f));
		if (!Grid.singleton.IsPointInGrid(thisPoint) || isInUI){
			thisPoint = null;
		}
		if (thisPoint == null) return;		
		
		// Test to see if we are nearly eleecting another point (i.e. we asre selecting a connection). 
		float distThreshold = 0.15f;
		
		otherPoint = null;
		float xDiff = thisPoint.x - worldPos.x;
		float yDiff = thisPoint.y - worldPos.y;
		if (Mathf.Abs (xDiff) < distThreshold && Mathf.Abs (yDiff) > distThreshold){
			if (yDiff > 0){
				otherPoint = new GridPoint(thisPoint.x, thisPoint.y - 1);
			}
			else{
				otherPoint = new GridPoint(thisPoint.x, thisPoint.y + 1);
			}
		}
		else if (Mathf.Abs (xDiff) > distThreshold && Mathf.Abs (yDiff) < distThreshold){
			if (xDiff > 0){
				otherPoint = new GridPoint(thisPoint.x - 1, thisPoint.y );
			}
			else{
				otherPoint = new GridPoint(thisPoint.x + 1, thisPoint.y);
			}
		}
				

		if (otherPoint == null ||!Grid.singleton.IsPointInGrid(otherPoint) || isInUI){
			otherPoint = null;
		}		
	}

	
	void Update(){
	
		CalcCurrentGridPoints();
		
		if (ghostElement == null) Debug.LogError ("Ghost is null!?!");
		
		
		// Deal with the ghost element
		ghostElement.SetActive(thisPoint != null);
		CircuitElement ghostElementComp = ghostElement.GetComponent<CircuitElement>();
		ghostElementComp.SetGridPoint(thisPoint, -5);
		ghostElementComp.RebuildMesh();
		ghostElementComp.SetOtherGridPoint(otherPoint);		
		
		
		
		// Placed elements are dealt with differntly than drawn ones
		switch (ElementFactory.singleton.GetPrefab(selectedPrefabId).GetComponent<CircuitElement>().uiType){
			case CircuitElement.UIType.kPlace:
				HandlePlacedElementInput();
				break;
			case CircuitElement.UIType.kDraw:
				HandleDrawnElementInput();
				break;
			case CircuitElement.UIType.kErase:
				HandleEraserElementInput();
				break;
		}
		lastPoint = thisPoint;
		lastOtherPoint = otherPoint;
					
	}
	
	bool[] SaveOldConnections(CircuitElement existingElement){
		bool[] oldConnections = new bool[4];
		Array.Copy(existingElement.isConnected, oldConnections, 4);
		return oldConnections;
	}
	
	void AttemptToReestablishConnections(CircuitElement thisElement, bool[] oldConnections){
		// If there were connections before, try and set them up again
		if (oldConnections != null){
			for (int dir = 0; dir < 4; ++dir){
				if (oldConnections[dir]){
					CircuitElement otherElement = Circuit.singleton.GetElement(thisElement.GetGridPoint() + Circuit.singleton.offsets[dir]);
					if (otherElement){
						// We only want to do this if our new element is partial to it
						bool ok = thisElement.SuggestInvite(otherElement);
						if (ok) otherElement.SuggestInvite(thisElement);
						
					}
				}
			}
		}
	}

	void HandleDrawnElementInput(){
	
		buttonIsHeld = (Input.GetMouseButton(0) && !Input.GetKey (KeyCode.LeftControl));
		
		// If the buttons is not down, there is nothing to do
		if (thisPoint == null || !buttonIsHeld){
			return;
		}
		
		// Check if we have only just pressed it down
		bool buttonIsClicked = (Input.GetMouseButtonDown(0) && !Input.GetKey (KeyCode.LeftControl));
		
		GridPoint[] gridPath = null;
		
		 if (buttonIsClicked){
			gridPath = new GridPoint[1];
			gridPath[0] = thisPoint;
		 }
		else if (lastPoint != null && !thisPoint.IsEqual(lastPoint)){
			gridPath = CalcGridPath(thisPoint, lastPoint);
		 }
		

		if (gridPath != null)
		{
			for (int i = 0; i < gridPath.GetLength(0); ++i){
				GameObject existingElement = Circuit.singleton.GetGameObject(gridPath[i]);
				
				bool[] oldConnections = null;
				
				// If there is one there already
				if (existingElement != null){
					oldConnections = SaveOldConnections(existingElement.GetComponent<CircuitElement>());
					
					// If this is not the same kind of element - then replace it
					if (existingElement.GetComponent<SerializationID>().id != selectedPrefabId){
						existingElement.GetComponent<CircuitElement>().RemoveConnections();
						Destroy (Circuit.singleton.GetGameObject(gridPath[i]));
						Circuit.singleton.RemoveElement(gridPath[i]);
						GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
						PlaceElement(newElement, gridPath[i]);
					}
				}
				else{
					GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
					PlaceElement(newElement, gridPath[i]);
				}
				CircuitElement thisElement = Circuit.singleton.GetElement(gridPath[i]);
				AttemptToReestablishConnections(thisElement, oldConnections);
				
				// If we have a next one, make a connection
				if (i > 0){
					CircuitElement lastElement =  Circuit.singleton.GetElement(gridPath[i-1]);
					thisElement.SuggestInvite(lastElement);
					lastElement.SuggestInvite(thisElement);

				}
			}
			
		}
		

	}
	
		
	void HandlePlacedElementInput(){
		// If not on the grid, then nothing to do
		if (thisPoint == null) return;
		
		if (Input.GetMouseButtonDown(0) && !Input.GetKey (KeyCode.LeftControl)){
			GameObject existingElement = Circuit.singleton.GetGameObject(thisPoint);
			
			// If there is one there already
			if (existingElement){
				bool[] oldConnections = null;
				
				// Check if we should simply OnClick() the element which is there
				GameObject prefab = ElementFactory.singleton.GetPrefab(selectedPrefabId);
				if (existingElement.GetComponent<CircuitElement>().ShouldClick(prefab)){
					existingElement.GetComponent<CircuitElement>().OnClick();
					
					// Change the prefab too
					prefab.GetComponent<CircuitElement>().OnClick();
					PrefabManager.OnChangePrefab(prefab);
					SetSelectedPrefabId(selectedPrefabId);
					
				}
				// Othewise, remove it and add a new one
				else{
					oldConnections = SaveOldConnections(existingElement.GetComponent<CircuitElement>());
					existingElement.GetComponent<CircuitElement>().RemoveConnections();
					Destroy (Circuit.singleton.GetGameObject(thisPoint));
					Circuit.singleton.RemoveElement(thisPoint);
					GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
					PlaceElement(newElement, thisPoint);
					AttemptToReestablishConnections(newElement.GetComponent<CircuitElement>(), oldConnections);
					
				}
			}
			else{
				GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
				PlaceElement(newElement, thisPoint);
			}
			
		}
	}
	
	void PlaceElement(GameObject newElement, GridPoint thisPoint){
		Circuit.singleton.PlaceElement(newElement, thisPoint);
		placeElementSound.Play ();
	
	}
	
	void HandleEraserElementInput(){
	
		// Test if we are able to erasing the thing if we were t press the button
		CircuitElement thisElement = null;
		CircuitElement otherElement = null;
		
		if (thisPoint != null) thisElement = Circuit.singleton.GetElement(thisPoint);
		if (otherPoint != null) otherElement = Circuit.singleton.GetElement(otherPoint);

		if (thisElement != null && otherElement != null){
			bool ok1 = thisElement.IsAmenableToSuggestion(otherElement);
			bool ok2 = otherElement.IsAmenableToSuggestion(thisElement);
			ghostElement.GetComponent<CircuitElement>().SetErrorState(!ok1 || !ok2);
		}
		else{
			ghostElement.GetComponent<CircuitElement>().SetErrorState(false);
		}
		
		buttonIsHeld = (Input.GetMouseButton(0) && !Input.GetKey (KeyCode.LeftControl));
		
		// If the buttons is not down, there is nothing more to do
		if (thisPoint == null || !buttonIsHeld){
			return;
		}
	
		
		// Check if we have only just pressed it down
		bool buttonIsClicked = (Input.GetMouseButtonDown(0) && !Input.GetKey (KeyCode.LeftControl));
		
		GridPoint[] gridPath = null;
		
		// If clicking a connection - then simple request that the connection be removed
		if (buttonIsClicked && otherPoint != null){
			if (thisElement != null && otherElement != null){
				bool ok1 = thisElement.SuggestUninvite(otherElement);
				bool ok2 = otherElement.SuggestUninvite(thisElement);
				if (ok1 && ok2)
					removeElementSound.Play ();
				else
					failSound.Play ();
			}
			
		}
		// Otherwise, remove the component itself
		else if (buttonIsClicked){
			gridPath = new GridPoint[1];
			gridPath[0] = thisPoint;
		}
		// If dragging from one point to the next
		else if (lastPoint != null && !thisPoint.IsEqual(lastPoint)){
			gridPath = CalcGridPath(lastPoint, thisPoint);
			
			// If our last thing was to delete a connection, then check fi the first two
			// elements of this list are that connection - if they are then don;t delete the first one
			if (lastOtherPoint != null && lastOtherPoint.IsEqual(gridPath[1]) && lastPoint.IsEqual(gridPath[0])){
				int numElements = gridPath.GetLength(0);
				GridPoint[] newPath = new GridPoint[numElements-1];
				for (int i = 0; i < numElements-1; ++i){
					newPath[i] = gridPath[i+1];
				}
				gridPath = newPath;
				
			}
			
		}
		// If we were deleting a connection and how now moved to the point itself, then delete the point
		else if (lastOtherPoint != null && otherPoint == null && thisPoint.IsEqual(lastPoint)){
			gridPath = new GridPoint[1];
			gridPath[0] = thisPoint;
		}
		
		
		if (gridPath != null)
		{
			for (int i = 0; i < gridPath.GetLength(0); ++i){
				GameObject existingElement = Circuit.singleton.GetGameObject(gridPath[i]);
								
				// If there is one there already
				if (existingElement != null){
					existingElement.GetComponent<CircuitElement>().RemoveConnections();
					Destroy (Circuit.singleton.GetGameObject(gridPath[i]));
					Circuit.singleton.RemoveElement(gridPath[i]);
				}
			}
			removeElementSound.Play ();
			
		}
		
		
	}	
	
	GridPoint[] CalcGridPath(GridPoint prevPoint, GridPoint nextPoint){
		
		if (prevPoint.IsEqual(nextPoint)){
			Debug.LogError("Trying to draw from and to the same point!");
			return null;
		}
		if (prevPoint == null || nextPoint == null){
			Debug.LogError("Trying to draw from or to a null point!");
			return null;
		}		
		
		int xDiff = nextPoint.x - prevPoint.x;
		int yDiff = nextPoint.y - prevPoint.y;
		
		// Work out how many elements the array should have
		int size = Mathf.Abs(xDiff) + Mathf.Abs(yDiff) + 1;
		GridPoint[] result = new GridPoint[size];
		
		// If we are not moving in x at all...
		int i = 0;
		if (Mathf.Abs(xDiff) > Mathf.Abs(yDiff)){
			int xInc = xDiff / Mathf.Abs (xDiff);
			int lastY = prevPoint.y;
			float grad = (float)yDiff/(float)xDiff;
			for (int x = prevPoint.x; x != nextPoint.x + xInc; x += xInc){
				int thisY = prevPoint.y + Mathf.RoundToInt((x-prevPoint.x) * grad);
				if (thisY != lastY){
					result[i++] = new GridPoint(x, lastY);
					lastY = thisY;
				}
				result[i++] = new GridPoint(x, thisY);	
			}
			
		}
		else{
			int yInc = yDiff / Mathf.Abs (yDiff);
			int lastX = prevPoint.x;
			float grad = (float)xDiff/(float)yDiff;
			for (int y = prevPoint.y; y != nextPoint.y + yInc; y += yInc){
				int thisX = prevPoint.x + Mathf.RoundToInt((y-prevPoint.y) * grad);
				if (thisX != lastX){
					result[i++] = new GridPoint(lastX, y);
					lastX = thisX;
				}					
				result[i++] = new GridPoint(thisX, y);
			}
		}
		
		return result;
	}	
	
	
	// Temprary debug stuff
	void OnGUI(){
		guiTextDisplay.GUIResetTextLayout();
		if (thisPoint != null){
			guiTextDisplay.GUIPrintText( "Selected Grid Position: " + thisPoint.x + ", " + thisPoint.y, Color.yellow);
			float selVolt = 0f;
			float selAmp = 0f;
			CircuitElement thisElement = Circuit.singleton.GetElement(thisPoint);
			if (thisElement){
				selVolt = thisElement.GetMaxVoltage();
				selAmp = thisElement.GetMaxCurrent();
					
			}
			guiTextDisplay.GUIPrintText( "Selection max stats: " + selVolt.ToString("0.000") + "V, " + selAmp.ToString("0.000") + "A", Color.yellow);
		}
	}
}
