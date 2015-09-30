using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tutorial : MonoBehaviour {

	public static Tutorial singleton = null;
	public Dictionary<string, GameObject> tutorialObjects = new Dictionary<string, GameObject>();
	public Bounds bounds = new Bounds();
	
	public bool hasBeenExplosion = false;
	public GameObject theRulesGO;
	
	bool hasBounds;
	
	bool hasBeenExplosionInt;
	public bool hasDoneOneResistorTut;
	
	public void Deactivate(){
		foreach (GameObject go in tutorialObjects.Values){
			if (go.name != "The_Rules"){
				go.SetActive(false);
			}
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
//		Debug.Log ("Tutorial start");
		foreach (Transform child in transform){
//			Debug.Log (child.gameObject.name);
			tutorialObjects.Add(child.gameObject.name, child.gameObject);
		}
		theRulesGO.SetActive(true);
		theRulesGO.GetComponent<TheRules>().DisableRule(TheRules.Rules.kAll);
		
		RescaleFont(transform, 2, 2);
	}
	
	void FixedUpdate(){	
		bounds = new Bounds();
		hasBounds = false;
		AddToBounds(transform);
		
		hasBeenExplosion = hasBeenExplosionInt;
		hasBeenExplosionInt = false;
		
	}
	
	void AddToBounds(Transform trans){
		foreach (Transform child in trans){
			if (child.gameObject.activeSelf){
				Renderer renderer = child.GetComponent<Renderer>();
				if (renderer != null){
					if (!hasBounds){
						bounds = renderer.bounds;
						hasBounds = true;
					}
					else{
						bounds.Encapsulate(renderer.bounds);
					}
				}
				AddToBounds (child);
			}
		}
	}
	
	void RescaleFont(Transform trans, float fontScale, float scaleScale){
		foreach (Transform child in trans){
			TextMesh textMesh = child.GetComponent<TextMesh>();
			if (textMesh != null){
				if (textMesh.fontSize == 50) textMesh.fontSize = 55;
				textMesh.fontSize = Mathf.RoundToInt(textMesh.fontSize / fontScale);
				child.localScale = child.localScale * scaleScale;
				RescaleFont(child, fontScale, 1);
			}
			else{
				RescaleFont(child, fontScale, scaleScale);
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
