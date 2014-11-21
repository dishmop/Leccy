using UnityEngine;
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
		ghostElement.GetComponent<CircuitElement>().SetAlpha(0.25f);
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
		ghostElement.GetComponent<CircuitElement>().SetGridPoint(thisPoint);
		
		
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

	void HandleDrawnElementInput(){
	
		buttonIsHeld = (Input.GetMouseButton(0) && !Input.GetKey (KeyCode.LeftControl));
		
		// If the buttons is not down, there is nothing to do
		if (thisPoint == null || !buttonIsHeld){
			return;
		}
		
		// Check if we should be placing a wire in the new position
		bool buttonIsClicked = (Input.GetMouseButtonDown(0) && !Input.GetKey (KeyCode.LeftControl));

		if (thisPoint != lastPoint || buttonIsClicked)
		{
			GameObject existingElement = Circuit.singleton.GetGameObject(thisPoint);
			
			// If there is one there already
			if (existingElement){
				// If this is not the same kind of element - then replaces it
				if (existingElement.GetComponent<SerializationID>().id != selectedPrefabId){
					Destroy (Circuit.singleton.GetGameObject(thisPoint));
					Circuit.singleton.RemoveElement(thisPoint);
					GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
					Circuit.singleton.PlaceElement(newElement, thisPoint);
				}
			}
			else{
				GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
				Circuit.singleton.PlaceElement(newElement, thisPoint);
			}
		}
		
		// if we have just transitioned to a new point (and button held at both points, we should now get these two
		// elements to invite each other to connect to keep things symetrical
		if (thisPoint != lastPoint){
			CircuitElement lastElement =  Circuit.singleton.GetElement(lastPoint);
			CircuitElement thisElement =  Circuit.singleton.GetElement(lastPoint);
			lastElement.SuggestInvite(thisElement);
			thisElement.SuggestInvite(lastElement);
		}
		
				
						
		
	}
		
	void HandlePlacedElementInput(){
		// If not on the grid, then nothing to do
		if (thisPoint == null) return;
		
		if (Input.GetMouseButtonDown(0) && !Input.GetKey (KeyCode.LeftControl)){
			GameObject existingElement = Circuit.singleton.GetGameObject(thisPoint);
			
			// If there is one there already
			if (existingElement){
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
					Destroy (Circuit.singleton.GetGameObject(thisPoint));
					Circuit.singleton.RemoveElement(thisPoint);
					GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
					Circuit.singleton.PlaceElement(newElement, thisPoint);
				}
			}
			else{
				GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
				Circuit.singleton.PlaceElement(newElement, thisPoint);
			}
			
		}
	}
}
