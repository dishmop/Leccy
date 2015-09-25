using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class ClickToContinue : TutTriggerBase {
	public UnityEvent triggerHandler;
	
	float startTime = 0;
	float waitDuration = 0;//2;
	float pulsesPerSec = 5f;
	
	// Use this for initialization
	protected override void OnEnable(){
		base.OnEnable();
		startTime = Time.time;
		GetComponent<Renderer>().enabled = false;
	}	
	
	// Update is called once per frame
	void FixedUpdate () {
		float age = Time.time - (startTime + waitDuration);
		
		float visibility = 0;
		if (age >= 0 && IsActive()){
			GetComponent<Renderer>().enabled = true;
			visibility = 0.25f - 0.2f * Mathf.Cos(age * pulsesPerSec / 2 * Mathf.PI);
		
			Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Bounds bounds = GetComponent<Renderer>().bounds;
			mouseWorldPos.z = bounds.min.z;
			if (bounds.Contains(mouseWorldPos)){
				visibility = 0.8f - 0.2f * Mathf.Cos(age * pulsesPerSec / 2 * Mathf.PI);
				UI.singleton.HideMousePointer();
				if (Input.GetMouseButtonDown(0)){
					triggerHandler.Invoke();
					Deactivate();
				}
			}
		}
		Color col = GetComponent<TextMesh>().color;
		col.a = visibility;
		GetComponent<TextMesh>().color = col;
		
	}
	

}
