using UnityEngine;
using System.Collections;
using System.Text;

public class Tut2CircuitSingleLoopFlow : TutTriggerBase {
	float voltage = 0;
	
	// Use this for initialization
	protected override void OnEnable(){
		base.OnEnable();
		SetupText();
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		SetupText();
		
	}
	
	
	void SetupText(){
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			CircuitElementVoltmeter vm = go.GetComponent<CircuitElementVoltmeter>();
			if (vm != null){	
				voltage = vm.GetVoltageDiff();
				break;
			}
		}
		
		int voltageInt = Mathf.RoundToInt(voltage);
		
		string ampText = (voltageInt != 1) ? voltageInt.ToString() + " amps" : "1 amp";
				
		StringBuilder sb = new StringBuilder();
		
		sb.Append("The speed of the blobs indicates the size\n");
		sb.Append("of the current. If the current flows around\n");
		sb.Append("a single loop, it will be the same everwhere; \n");
		sb.Append(ampText + " in our case.\n");

		transform.FindChild("MainText").GetComponent<TextMesh>().text = sb.ToString();
	}
	
}
