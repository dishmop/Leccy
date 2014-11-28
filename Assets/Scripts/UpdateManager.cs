using UnityEngine;
using System.Collections;

public class UpdateManager : MonoBehaviour {

	// this class is used to call Update on the various systems in a prefdefined order
	
	// FixedUpdate is called once per frame
	void FixedUpdate () {
		Circuit.singleton.GameUpdate();
		UI.singleton.GameUpdate();
		Simulator.singleton.GameUpdate();
	 
	}
}
