﻿using UnityEngine;
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
				voltage = vm.GetMaxVoltage();
				break;
			}
		}
		
		int voltageInt = Mathf.RoundToInt(voltage);
		
		string ampText = (voltageInt != 1) ? voltageInt.ToString() + " amps" : "1 amp";
				
		StringBuilder sb = new StringBuilder();
		
		sb.Append("The speed of the blobs indicates the size of the current. If the current flows\n");
		sb.Append("around a single loop, it will be the same everwhere; " + ampText + " in our case.\n");

		transform.FindChild("MainText").GetComponent<TextMesh>().text = sb.ToString();
	}
	
}