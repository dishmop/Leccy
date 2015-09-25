using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tutorial : MonoBehaviour {

	public static Tutorial singleton = null;
	public Dictionary<string, GameObject> tutorialObjects = new Dictionary<string, GameObject>();

	public void Deactivate(){
		foreach (GameObject go in tutorialObjects.Values){
			go.SetActive(false);
		}
	}
	// Use this for initialization
	void Awake () {
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
	}
	
	
	void OnDestroy () {
		singleton = null;
	}
	
	void Start(){
		foreach (Transform child in transform){
			tutorialObjects.Add(child.gameObject.name, child.gameObject);
		}
	}
}
