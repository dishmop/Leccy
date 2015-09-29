using UnityEngine;
using System.Collections;

public class Tut2CircuitNewCurrent : TutTriggerBase {

	public string seriesFirstName;
	public string parallelFirstName;
	public string finalName;
	
	
	// Use this for initialization
	protected override void OnEnable () {
		base.OnEnable();
		
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			if (go.GetComponent<CircuitElementVoltmeter>() != null){
				go.GetComponent<CircuitElementVoltmeter>().hasTarget = false;
				break;
			}
		}
		
		
		
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (!IsActive()) return;
	
		CircuitElementAmmeter ammeter = null;
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			if (go.GetComponent<CircuitElementAmmeter>() != null){
				ammeter = go.GetComponent<CircuitElementAmmeter>();
				break;
			}
		}
		
		if (ammeter == null) return;
		
		if (seriesFirstName != "" && parallelFirstName != ""){
			Tutorial.singleton.hasDoneOneResistorTut = false;
		}
		else{
			Tutorial.singleton.hasDoneOneResistorTut = true;
		}
		
		
		if (seriesFirstName != "" && MathUtils.FP.Feq(ammeter.GetMaxCurrent(), 0.5f)){
			Tutorial.singleton.Deactivate();
			GameModeManager.singleton.LoadLevelByNameQuiet(seriesFirstName);
			
		}
		if (parallelFirstName != "" && MathUtils.FP.Feq(ammeter.GetMaxCurrent(), 2f)){
			Tutorial.singleton.Deactivate();
			GameModeManager.singleton.LoadLevelByNameQuiet(parallelFirstName);
		}
		

			
		
		
		
		
		
	}
}
