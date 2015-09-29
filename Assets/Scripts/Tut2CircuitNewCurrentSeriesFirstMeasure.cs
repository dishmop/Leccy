using UnityEngine;
using System.Collections;
using System.Text;

public class Tut2CircuitNewCurrentSeriesFirstMeasure : TutTriggerBase {


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
		
		
		// To be sure we have suuceeded, the "anchored" voltmeter should read 1
		// The anchored (and only) ammeter should read 0.5
		// The "new" voltemeter should read 0.5
		CircuitElementAmmeter anchoredAmmeter = null;
		CircuitElementVoltmeter anchoredVoltMeter = null;
		CircuitElementVoltmeter newVoltMeter = null;
		
		
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			
			CircuitElementVoltmeter voltmeter = go.GetComponent<CircuitElementVoltmeter>();
			if (voltmeter != null){
				GridPoint point = voltmeter.GetGridPoint();
				Circuit.AnchorData data = Circuit.singleton.GetAnchors(point);
				if (data.isAnchored[Circuit.kCentre]){
					anchoredVoltMeter = voltmeter;
				}
				else{
					newVoltMeter = voltmeter;
				}
			}
			CircuitElementAmmeter ammeter = go.GetComponent<CircuitElementAmmeter>();
			if (ammeter != null){
				anchoredAmmeter = ammeter;
			}
		}
		
		if (anchoredVoltMeter != null && anchoredAmmeter != null && newVoltMeter != null){
			if (MathUtils.FP.Feq(anchoredVoltMeter.GetVoltageDiff(), 1f) &&
			    MathUtils.FP.Feq(newVoltMeter.GetVoltageDiff(), 0.5f) &&
				MathUtils.FP.Feq(anchoredAmmeter.GetMaxCurrent(), 0.5f)){
				triggerHandle.Invoke();
				newVoltMeter.TriggerTargetEffect();
				Deactivate();
				
			}
		}
		
		
	
	}
	
}
