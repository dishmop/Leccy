using UnityEngine;
using System.Collections;
using System.Text;

public class Tut2CircuitNewCurrentParallelFirstMeasure : TutTriggerBase {


	// Use this for initialization
	protected override void OnEnable () {
		base.OnEnable();
		
		// Check how many resisitors are i nthe circuit and adjust our inventory accordingly.
		int count = 0;
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			
			CircuitElementResistor resistor = go.GetComponent<CircuitElementResistor>();
			if (resistor != null) count++;
		}
		int inventoryResisotrCount = 2 - count;
		ElementFactory.singleton.SetStock("Resistor", inventoryResisotrCount);
		
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (!IsActive()) return;
		
		
		// To be sure we have suuceeded, the "anchored" (and only) voltmeter should read 1
		// The anchored ammeter should read 2
		// The "new" voltemeter should read 1
		CircuitElementAmmeter anchoredAmmeter = null;
		CircuitElementAmmeter newAmmeter = null;
		CircuitElementVoltmeter anchoredVoltMeter = null;
		
		
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			
			CircuitElementVoltmeter voltmeter = go.GetComponent<CircuitElementVoltmeter>();
			if (voltmeter != null){
				GridPoint point = voltmeter.GetGridPoint();
				Circuit.AnchorData data = Circuit.singleton.GetAnchors(point);
				if (data.isAnchored[Circuit.kCentre]){
					anchoredVoltMeter = voltmeter;
				}
			}
			CircuitElementAmmeter ammeter = go.GetComponent<CircuitElementAmmeter>();
			if (ammeter != null){
				GridPoint point = ammeter.GetGridPoint();
				Circuit.AnchorData data = Circuit.singleton.GetAnchors(point);
				if (data.isAnchored[Circuit.kCentre]){
					anchoredAmmeter = ammeter;
				}
				else{
					newAmmeter = ammeter;
				}
			}
		}
		
		if (anchoredVoltMeter != null && anchoredAmmeter != null && newAmmeter != null){
//			Debug.Log ("anchoredVoltMeter = " + anchoredVoltMeter.GetVoltageDiff() + ", newAmmeter =" + newAmmeter.GetMaxCurrent() + ", anchoredAmmeter = " + anchoredAmmeter.GetMaxCurrent()); 
			if (MathUtils.FP.Feq(anchoredVoltMeter.GetVoltageDiff(), 1f, 0.01f) &&
			    MathUtils.FP.Feq(newAmmeter.GetMaxCurrent(), 1f, 0.01f) &&
			    MathUtils.FP.Feq(anchoredAmmeter.GetMaxCurrent(), 2f, 0.01f)){
				triggerHandle.Invoke();
				newAmmeter.TriggerTargetEffect();
				Deactivate();
				
			}
		}
		
		
	
	}
	
}
