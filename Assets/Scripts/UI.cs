using UnityEngine;
using System;
using System.Collections;

public class UI : MonoBehaviour {

	public static UI singleton;

	string 		selectedPrefabId;
	GameObject	ghostElement;
	GridPoint	thisPoint;
	bool		buttonIsHeld;
	GridPoint	lastPoint;
	
	bool		isInUI;
	
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
	
	void OnDestroy () {
		singleton = null;
	
	}
	
	
	GridPoint CalcCurrentGridPoint(){
		// Get mouse pointer position in world and circuite space
		Vector3 mousePos = Input.mousePosition;
		mousePos.z = transform.position.z - Camera.main.transform.position.z;
		Vector3 worldPos = Camera.main.ScreenToWorldPoint( mousePos);
		GridPoint point = new GridPoint((int)(worldPos.x + 0.5f), (int)(worldPos.y + 0.5f));
		if (!Grid.singleton.IsPointInGrid(point) || isInUI){
			point = null;
		}
		return point;
	}

	
	void Update(){
	
		thisPoint = CalcCurrentGridPoint();
		
		
		// Deal with the ghost element
		ghostElement.SetActive(thisPoint != null);
		ghostElement.GetComponent<CircuitElement>().SetGridPoint(thisPoint, -5);
		ghostElement.GetComponent<CircuitElement>().RebuildMesh();
		
		
		// Placed elements are dealt with differntly than drawn ones
		switch (ElementFactory.singleton.GetPrefab(selectedPrefabId).GetComponent<CircuitElement>().uiType){
			case CircuitElement.UIType.kPlace:
				HandlePlacedElementInput();
				break;
			case CircuitElement.UIType.kDraw:
				HandleDrawnElementInput();
				break;
		}
		lastPoint = thisPoint;
					
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
		
		// Check if we should be placing a wire in the new position
		bool buttonIsClicked = (Input.GetMouseButtonDown(0) && !Input.GetKey (KeyCode.LeftControl));
		
		GridPoint[] gridPath = null;
		
		 if (buttonIsClicked){
			gridPath = new GridPoint[1];
			gridPath[0] = thisPoint;
		 }
		 else if (!thisPoint.IsEqual(lastPoint)){
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
					
					// If this is not the same kind of element - then replaces it
					if (existingElement.GetComponent<SerializationID>().id != selectedPrefabId){
						existingElement.GetComponent<CircuitElement>().RemoveConnections();
						Destroy (Circuit.singleton.GetGameObject(gridPath[i]));
						Circuit.singleton.RemoveElement(gridPath[i]);
						GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
						Circuit.singleton.PlaceElement(newElement, gridPath[i]);
					}
				}
				else{
					GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
					Circuit.singleton.PlaceElement(newElement, gridPath[i]);
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
					Circuit.singleton.PlaceElement(newElement, thisPoint);
					AttemptToReestablishConnections(newElement.GetComponent<CircuitElement>(), oldConnections);
					
				}
			}
			else{
				GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
				Circuit.singleton.PlaceElement(newElement, thisPoint);
			}
			
		}
	}
	
	GridPoint[] CalcGridPath(GridPoint prevPoint, GridPoint nextPoint){
		
		if (prevPoint.IsEqual(nextPoint)){
			Debug.LogError("Trying to draw from and to the same point!");
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
}
