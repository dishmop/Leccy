using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LeccyUIButton : MonoBehaviour {

	public GameObject circuitElementPrefab;

	// Use this for initialization
	void Start () {
		if (circuitElementPrefab){
			transform.FindChild("Text").GetComponent<Text>().text = circuitElementPrefab.GetComponent<CircuitElement>().GetUIString();
			transform.FindChild("Button").FindChild("UIMesh").GetComponent<UIMesh>().prefabMesh = circuitElementPrefab.GetComponent<CircuitElement>().GetUIMehsPrefab();
		}
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
