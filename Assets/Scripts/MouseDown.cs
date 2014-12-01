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
		CircuitElementAmmeter ampScript = GetComponentInParent<CircuitElementAmmeter>();
		if (ampScript) ampScript.OnMouseDown();

	}
	
	void OnMouseOver(){
		CircuitElementAmmeter ampScript = GetComponentInParent<CircuitElementAmmeter>();
		if (ampScript) ampScript.OnMouseOver();

	}	
}
