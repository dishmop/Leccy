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
		
		Vector3 mousePos = Input.mousePosition;
		mousePos.z = transform.position.z - Camera.main.transform.position.z;
		Vector3 worldPos = Camera.main.ScreenToWorldPoint( mousePos);
		GridPoint newPoint = new GridPoint((int)(worldPos.x + 0.5f), (int)(worldPos.y + 0.5f));
		
		if (!isInUI && Grid.singleton.IsPointInGrid(newPoint)){
			
			ghostElement.GetComponent<CircuitElement>().SetGridPoint(newPoint);
			ghostElement.transform.position = new Vector3(newPoint.x, newPoint.y, ghostElement.transform.position.z);
			ghostElement.SetActive (true);
		}
		else{
			ghostElement.SetActive (false);
		}		
	}
}
