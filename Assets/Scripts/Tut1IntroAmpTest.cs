using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class Tut1IntroAmpTest : TutTriggerBase {

	
	// Update is called once per frame
	void FixedUpdate () {
		
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			CircuitElementVoltmeter vm = go.GetComponent<CircuitElementVoltmeter>();
			if (vm == null) continue;
			
			if (IsActive() && MathUtils.FP.Feq(vm.GetVoltageDiff(), 1)){
				Tutorial.singleton.Deactivate();
				triggerHandle.Invoke();
				Deactivate();
			}
		}

	
	}
	
}
