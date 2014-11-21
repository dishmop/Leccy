using UnityEngine;
using System.Collections;

public class UI : MonoBehaviour {

	public static UI singleton;

	string 		selectedPrefabId;
	GameObject	ghostElement;
	
	bool	isInUI;
	
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
	

	
	void Update(){
		
		// Get mouse pointer position in world and circuite space
		Vector3 mousePos = Input.mousePosition;
		mousePos.z = transform.position.z - Camera.main.transform.position.z;
		Vector3 worldPos = Camera.main.ScreenToWorldPoint( mousePos);
		GridPoint newPoint = new GridPoint((int)(worldPos.x + 0.5f), (int)(worldPos.y + 0.5f));
		
		// Only do this UI stuff if not on the button UI panel
		if (!isInUI && Grid.singleton.IsPointInGrid(newPoint)){
			// Deal with the ghost element
			ghostElement.GetComponent<CircuitElement>().SetGridPoint(newPoint);
			ghostElement.SetActive (true);
			
			if (Input.GetMouseButtonDown(0) && !Input.GetKey (KeyCode.LeftControl)){
				GameObject existingElement = Circuit.singleton.GetGameObject(newPoint);
				
				// If there is one there already
				if (existingElement){
					// Chec, if it is of the same type that we are about to place, then don;t add a new
					// one, just click this
					if (existingElement.GetComponent<SerializationID>().id == selectedPrefabId){
						existingElement.GetComponent<CircuitElement>().OnClick();
					}
					// Othewise, remove it and add a new one
					else{
						Circuit.singleton.RemoveElement(newPoint);
						GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
						Circuit.singleton.PlaceElement(newElement, newPoint);
					}
				}
				else{
					GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
					Circuit.singleton.PlaceElement(newElement, newPoint);
				}
			
			}
		}
		else{
			ghostElement.SetActive (false);
		}	
		
		
		
			
					
	}
}
