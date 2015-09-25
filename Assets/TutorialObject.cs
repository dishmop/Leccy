using UnityEngine;
using System.Collections;

public class TutorialObject : MonoBehaviour {
	float startTime;
	float delay = 1;
	bool visible = true;

	void OnEnable(){
		startTime = Time.time;
		SetChildrenVisible(transform, false);
		visible = false;
	}
	
	void Update(){
		if (!visible && Time.time > startTime + delay){
			SetChildrenVisible(transform, true);
			visible = true;
			
		}
	}
	
	void SetChildrenVisible(Transform trans, bool visible){
		foreach (Transform child in trans){
			Renderer renderer = child.GetComponent<Renderer>();
			if (renderer != null){
				renderer.enabled = visible;
			}
			SetChildrenVisible(child, visible);
			
		}
	}
	
	

}
