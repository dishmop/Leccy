using UnityEngine;
using System.Collections;

public class Tut2CircuitExplode : TutTriggerBase {


	
	// Update is called once per frame
	void FixedUpdate () {
		if (Tutorial.singleton.hasBeenExplosion){
			triggerHandle.Invoke();
		}
		

	
	
	}
}
