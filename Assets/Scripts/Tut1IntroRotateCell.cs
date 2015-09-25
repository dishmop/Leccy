using UnityEngine;
using System.Collections;

public class Tut1IntroRotateCell : TutTriggerBase {

	public enum Status{
		kUnset,
		kNotTwoCells,
		kTwoCellsNotConnected,
		kTwoCellsAligned,
		kTwoCellsOppAligned,
		kTwoCellsSideAlligned
	};
	
	Status status = Status.kUnset;


	
	// Update is called once per frame
	void FixedUpdate () {
		int count = 0;
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			CircuitElementCell cell = go.GetComponent<CircuitElementCell>();
			if (cell == null) continue;
			
			count++;
		}
		
		if (count != 2){
			status = Status.kUnset;
		}
		else{
			int count2 = 0;
			// Check if we have wires going all the way round
			foreach (GameObject go in Circuit.singleton.elements){
				if (go == null) continue;
				
				count2++;
			}
			Debug.Log("count2 = " + count2);
		}
		
	
	}
}
