using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Tut1IntroAddCells : TutTriggerBase {
	
	

	// Update is called once per frame
	void FixedUpdate () {
	
		int count = 0;
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			CircuitElementCell cell = go.GetComponent<CircuitElementCell>();
			if (cell == null) continue;
			
			count++;
		}
		
		if (count == 2){
			Tutorial.singleton.Deactivate();
			triggerHandle.Invoke();
		}
	}
}
