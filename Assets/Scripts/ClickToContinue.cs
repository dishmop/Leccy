using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class ClickToContinue : MonoBehaviour {
	public UnityEvent triggerHandler;
	
	float startTime = 0;
	float waitDuration = 2;
	float pulsesPerSec = 5f;
	bool isActive = false;
	
	// Use this for initialization
	void OnEnable () {
		startTime = Time.time;
		GetComponent<Renderer>().enabled = false;
		isActive = true;
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		float age = Time.time - (startTime + waitDuration);
		
		float visibility = 0;
		if (age >= 0 && isActive){
			GetComponent<Renderer>().enabled = true;
			visibility = 0.25f - 0.2f * Mathf.Cos(age * pulsesPerSec / 2 * Mathf.PI);
		
			Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			mouseWorldPos.z = 0;
			Bounds bounds = GetComponent<Renderer>().bounds;
			if (bounds.Contains(mouseWorldPos)){
				visibility = 0.8f - 0.2f * Mathf.Cos(age * pulsesPerSec / 2 * Mathf.PI);
				UI.singleton.HideMousePointer();
				if (Input.GetMouseButtonDown(0)){
					triggerHandler.Invoke();
					isActive = false;
				}
			}
		}
		Color col = GetComponent<TextMesh>().color;
		col.a = visibility;
		GetComponent<TextMesh>().color = col;
		
	}
}
