using UnityEngine;
using System;
using System.Collections;

public class PanelController : MonoBehaviour {

	// Moving the side panel in and out
	public SpringValue	pos = null;
	public SpringValue.Mode 	mode = SpringValue.Mode.kAsymptotic;
	
	
	public void Activate(){
		pos.Set (0f);
	}
	
	public void Deactivate(){
		RectTransform rectTranform = GetComponent<RectTransform>();
		Vector3[] corners = new Vector3[4];
		rectTranform.GetWorldCorners(corners);
		float value = Mathf.Abs (corners[0].x - corners[2].x);
		
		pos.Set (value);
	}
	
	
	public void ForceActivate(){
		pos.Force (0f);
	}
	
	public void ForceDeactivate(){
		RectTransform rectTranform = GetComponent<RectTransform>();
		Vector3[] corners = new Vector3[4];
		rectTranform.GetWorldCorners(corners);
		float value = Mathf.Abs (corners[0].x - corners[2].x);

		pos.Force (value);
	}

	
	
	void Awake(){
		pos = new SpringValue(0f, mode);	
	}
	
	// Update is called once per frame
	void Update () {
		pos.Update();
		SetPanelPos();
	}
	
	void SetPanelPos(){
		RectTransform rectTranform = GetComponent<RectTransform>();
		rectTranform.offsetMin = new Vector2(-pos.GetValue(), 0f);
		rectTranform.offsetMax = new Vector2(-pos.GetValue(), 0f);
	}
}
