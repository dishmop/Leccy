using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LeccyUIButton : MonoBehaviour {

	public GameObject circuitElementPrefab;

	// Use this for initialization
	void Start () {
		if (circuitElementPrefab){
			transform.FindChild ("TextFrame").FindChild("Text").GetComponent<Text>().text = circuitElementPrefab.GetComponent<CircuitElement>().GetUIString();
			transform.FindChild("ButtonFrame").FindChild("Button").FindChild("UIMesh").GetComponent<UIMesh>().SetPrefabMesh(circuitElementPrefab.GetComponent<CircuitElement>().GetUIMehsPrefab());
		}
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
