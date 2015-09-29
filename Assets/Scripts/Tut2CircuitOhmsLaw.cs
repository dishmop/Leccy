using UnityEngine;
using System.Collections;
using System.Text;

public class Tut2CircuitOhmsLaw : TutTriggerBase {
	float voltage = 0;
	
	// Use this for initialization
	protected override void OnEnable(){
		base.OnEnable();
		
		UpdateCirclePos();
		SetupText();
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		UpdateCirclePos();
		SetupText();
	
	}
	
	void UpdateCirclePos(){
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			CircuitElementVoltmeter vm = go.GetComponent<CircuitElementVoltmeter>();
			if (vm != null){	
				voltage = vm.GetVoltageDiff();
			}
			CircuitElementResistor resistor = go.GetComponent<CircuitElementResistor>();
			if (resistor == null) continue;
			
			Vector3 pos = new Vector3(resistor.GetGridPoint().x, resistor.GetGridPoint().y, transform.position.z);
			transform.FindChild("UICircle").gameObject.SetActive(true);
			transform.FindChild("UICircle").position = pos;
			return;
		}
		// If it got through to here then there is no resistor
		transform.FindChild("UICircle").gameObject.SetActive(false);
	}
	
	void SetupText(){
	
		string voltageStr = Mathf.RoundToInt(voltage).ToString();
		
	
		StringBuilder sb = new StringBuilder();
		sb.Append("All resistors have a \"Resistance\".\n");
		sb.Append("There is a relationship betwen the\n");
		sb.Append("resistance, the current flowing \n");
		sb.Append("through it and the voltage accross it. \n");
		sb.Append("This is given by \"Ohms law\": \n");
		sb.Append("Voltage = Resistance X Current\n");
		sb.Append("\n");
		sb.Append("In our case the current is " + voltageStr + " and \n");
		sb.Append("the voltage across it is " + voltageStr + ", so we \n");
		sb.Append("must have a 1 ohm resistor:\n");
		sb.Append(Mathf.RoundToInt(voltage) + " = " + voltageStr + " X 1\n");
		transform.FindChild("MainText").GetComponent<TextMesh>().text = sb.ToString();
	}
	
}
