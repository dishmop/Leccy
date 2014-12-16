using UnityEngine;
using System;
using System.Collections;

public class PanelController : MonoBehaviour {

	// Moving the side panel in and out
	public SpringValue			pos = null;
	public SpringValue.Mode 	mode = SpringValue.Mode.kAsymptotic;
	public GameObject			adjustCamera;
	
	
	public void Activate(){
//		pos.Set (0f);
//		if (adjustCamera != null) adjustCamera.GetComponent<CamControl>().ignoreSide = false;
		transform.FindChild ("Quad").gameObject.SetActive(false);
	}
	
	public void Deactivate(){
//		RectTransform rectTranform = GetComponent<RectTransform>();
//		Vector3[] corners = new Vector3[4];
//		rectTranform.GetWorldCorners(corners);
//		float value = Mathf.Abs (corners[0].x - corners[2].x);
//		
//		if (adjustCamera != null) adjustCamera.GetComponent<CamControl>().ignoreSide = true;
//		pos.Set (value);
		transform.FindChild ("Quad").gameObject.SetActive(true);
	}
	
	
	public void ForceActivate(){
//		pos.Force (0f);
//		if (adjustCamera != null) adjustCamera.GetComponent<CamControl>().ignoreSide = false;
		transform.FindChild ("Quad").gameObject.SetActive(false);
	}
	
	public void ForceDeactivate(){
//		RectTransform rectTranform = GetComponent<RectTransform>();
//		Vector3[] corners = new Vector3[4];
//		rectTranform.GetWorldCorners(corners);
//		float value = Mathf.Abs (corners[0].x - corners[2].x);
//		if (adjustCamera != null) adjustCamera.GetComponent<CamControl>().ignoreSide = true;
//		
//		pos.Force (value);
		transform.FindChild ("Quad").gameObject.SetActive(true);
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
