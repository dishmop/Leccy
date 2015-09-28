using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tutorial : MonoBehaviour {

	public static Tutorial singleton = null;
	public Dictionary<string, GameObject> tutorialObjects = new Dictionary<string, GameObject>();
	public Bounds bounds = new Bounds();
	public bool hasBeenExplosion = false;
	
	bool hasBeenExplosionInt;
	
	public void Deactivate(){
		foreach (GameObject go in tutorialObjects.Values){
			go.SetActive(false);
		}
	}
	
	public void Deactivate(GameObject exception){
		foreach (GameObject go in tutorialObjects.Values){
			if (go == exception) continue;
			go.SetActive(false);
		}
	}
	
	public void OnExplosion(){
		hasBeenExplosionInt = true;
	
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
	
	void FixedUpdate(){	
		bounds = new Bounds();
		AddToBounds(transform);
		
		hasBeenExplosion = hasBeenExplosionInt;
		hasBeenExplosionInt = false;
		
	}
	
	void AddToBounds(Transform trans){
		foreach (Transform child in trans){
			if (child.gameObject.activeSelf){
				Renderer renderer = child.GetComponent<Renderer>();
				if (renderer != null){
					Vector3 width = renderer.bounds.extents;
					bounds.Encapsulate(renderer.bounds);
				}
				AddToBounds (child);
			}
		}
	}
/*
	void AddChildrenToBounds(Transform trans){		
		string startName = "";
		foreach (Transform child in trans){
			if (child.gameObject.activeSelf){
				startName = child.gameObject.name.Substring(0, 3);
			}
		}
		
		foreach (Transform child in trans){
			if (child.gameObject.name.Substring(0, 3) == startName){
				bool wasActive = child.gameObject.activeSelf;
				child.gameObject.SetActive(true);
				Renderer renderer = child.GetComponent<Renderer>();
				if (renderer != null){
					Vector3 width = renderer.bounds.extents;
					bounds.Encapsulate(renderer.bounds);
				}
				AddToBounds (child);
				child.gameObject.SetActive(wasActive);
			}
		}
	}
	*/
}
