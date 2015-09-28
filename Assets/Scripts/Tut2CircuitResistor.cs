using UnityEngine;
using System.Collections;

public class Tut2CircuitResistor : TutTriggerBase {

	void FixedUpdate(){
	
		if (!IsActive()) return;
		
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			if (go.GetComponent<CircuitElementResistor>() != null){
				if (MathUtils.FP.Feq (go.GetComponent<CircuitElementResistor>().GetMaxCurrent(), 1)){
					triggerHandle.Invoke();
					Deactivate();
				}
			}
		}
	}
}
