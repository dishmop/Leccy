using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LeccyUIButton : MonoBehaviour, PrefabListener {

	public GameObject circuitElementPrefab;
	
	
	void Awake(){
		PrefabManager.AddListener(this);
	}
	
	void OnDestroy(){
		PrefabManager.RemoveListener(this);
	}
	
	public void OnChangePrefab(GameObject preFab){
		if (preFab == circuitElementPrefab){
			ConfigureButton();
		}
	}
	
	
	void ConfigureButton(){
		if (circuitElementPrefab){
			transform.FindChild ("TextFrame").FindChild("Text").GetComponent<Text>().text = circuitElementPrefab.GetComponent<CircuitElement>().GetUIString();
			transform.FindChild("ButtonFrame").FindChild("Button").FindChild("UIMesh").GetComponent<UIMesh>().SetPrefabMesh(circuitElementPrefab.GetComponent<CircuitElement>().GetDisplayMesh());
		}
	}
		

	// Use this for initialization
	void Start () {
		ConfigureButton();
		
	}
	

}
