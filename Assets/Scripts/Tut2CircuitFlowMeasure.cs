using UnityEngine;
using System.Collections;

public class Tut2CircuitFlowMeasure : TutTriggerBase {

	// Use this for initialization
	protected override void OnEnable () {
		base.OnEnable();
		GridPoint ammeterGridPoint = new GridPoint(4, 11);
		GameObject existingElement = Circuit.singleton.GetElement(ammeterGridPoint).gameObject;
		UI.singleton.RemoveElement(existingElement);
		GameObject newElement = ElementFactory.singleton.InstantiateElement("Ammeter");
		newElement.GetComponent<CircuitElementAmmeter>().orient = 2;
		newElement.GetComponent<CircuitElementAmmeter>().hasTarget = false;
		UI.singleton.PlaceElement(newElement, ammeterGridPoint);
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
	
	}
}
