using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class IntroAmpTest : MonoBehaviour {
	public UnityEvent finalTrigger;
	public GameObject addCellText;
	
	public enum State{
		kTestVoltage,
		kAddCell,
		kRotateCell,
		kFinish
		
	}
	public State state;

	// Use this for initialization
	void OnEnable () {
		state = State.kTestVoltage;
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		
		switch (state){
			case State.kTestVoltage:{
				foreach (GameObject go in Circuit.singleton.elements){
					if (go == null) continue;
					CircuitElementVoltmeter vm = go.GetComponent<CircuitElementVoltmeter>();
					if (vm == null) continue;
					
					if (MathUtils.FP.Feq(vm.GetVoltageDiff(), 1)){
						state = State.kAddCell;
						// Deactivate all apart from this one
						Tutorial.singleton.Deactivate(gameObject);
						addCellText.SetActive(true);
					}
				}
				break;
			}
		}

	
	}
}
