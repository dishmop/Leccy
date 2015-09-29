using UnityEngine;
using System.Collections;
using System.Text;

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
	
	void SetupText(){
		float voltage = 0;
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			CircuitElementVoltmeter vm = go.GetComponent<CircuitElementVoltmeter>();
			if (vm != null){	
				voltage = vm.GetMaxVoltage();
				break;
			}
		}
		int voltageInt = Mathf.RoundToInt(voltage);
		
		string ampText = (voltageInt != 1) ? voltageInt.ToString() + " amps" : "1 amp";
		

		StringBuilder sb = new StringBuilder();
		sb.Append ("I've added an \"Ammeter\"\n");
		sb.Append ("into the circuit.\n");
		sb.Append ("\n");
		sb.Append ("This shows the flow is " + ampText + "\n");
		sb.Append ("\n");
		sb.Append ("Ammeters look a bit like\n");
		sb.Append ("voltmeters - except\n");
		sb.Append ("they let the current\n");
		sb.Append ("flow through them.\n");

	}
}
