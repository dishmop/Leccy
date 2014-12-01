using UnityEngine;
using System.Collections;

public class MouseDown : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void OnMouseDown(){
		CircuitElement script = GetComponentInParent<CircuitElement>();
		if (script) script.OnMouseDown();

	}
	
	void OnMouseOver(){
		CircuitElement script = GetComponentInParent<CircuitElement>();
		if (script) script.OnMouseOver();

	}	
}
