using UnityEngine;
using System.Collections;

public class Tut2CircuitNewVoltages : TutTriggerBase {

	GameObject voltMeter = null;
	float timeStart;
	float duration = 2;
	byte[] storage = new byte[100*1024];
	
	enum State{
		kTarget1,
		kAchieved1,
		kTarget2,
		kAchieved2,
		kTarget3,
		kAchieved3,
		
	}
	
	State state = State.kTarget1;
	

	// Use this for initialization
	protected override void OnEnable () {
		base.OnEnable();
		GameModeManager.singleton.disableLevelCompletion = true;
		
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			if (go.GetComponent<CircuitElementVoltmeter>() != null){
				voltMeter = go;
				break;
			}
		}
		SetVoltTarget(2);
		state = State.kTarget1;
		transform.FindChild("horizontal_arrow").gameObject.SetActive(true);
		
		transform.FindChild("MainText").GetComponent<TextMesh>().text = "You've got a new target voltage. Add more cells to acheive it.\nI've changed the brown bits around a bit.";
		
		// Store the level as we know it
		LevelManager.singleton.CacheLevel(storage);
		
		// Change the anchors
//		
//		// Add anchors to everyhing we've made so far
//		for (int x = 0; x < Circuit.singleton.elements.GetLength(0); ++x){
//			for (int y = 0; y < Circuit.singleton.elements.GetLength(1); ++y){
//				if (Circuit.singleton.elements[x,y] != null && (Circuit.singleton.elements[x,y].GetComponent<CircuitElement>() != null)){
//					for (int i = 0; i < 5; ++i){
//						Circuit.singleton.anchors[x,y].isAnchored[i] = true;
//						
//					}
//					Circuit.singleton.anchors[x,y].isDirty = true;
//				}
//			}
//			
//		}
//		
//		
//		// Remove anchors from ther left hand edge
//		int xx = 4;
//		for (int y = 0; y < Circuit.singleton.elements.GetLength(1); ++y){
//			if (Circuit.singleton.elements[xx,y] != null && (Circuit.singleton.elements[xx,y].GetComponent<CircuitElementWire>() != null)){
//				for (int i = 0; i < 5; ++i){
//					Circuit.singleton.anchors[xx,y].isAnchored[i] = false;
//				}
//			}
//			
//		}
		
		Circuit.singleton.ForceDirty();
	}
	
	// Update is called once per frame
	void Update () {
		switch (state){
			case State.kTarget1:{
				if (voltMeter.GetComponent<CircuitElementVoltmeter>().IsOnTarget()){
					if (!voltMeter.GetComponent<CircuitElementVoltmeter>().buttonActivated){
					}
					else{
						transform.FindChild("horizontal_arrow").gameObject.SetActive(false);
						state = State.kAchieved1;
						timeStart = Time.time;
					}
				}
				break;
			}
			case State.kAchieved1:{
				if (Time.time > timeStart + duration){
					state = State.kTarget2;
					transform.FindChild("MainText").GetComponent<TextMesh>().text = "You've got a new target voltage. Add more cells to acheive it.\nI've changed the brown bits around a bit.\nNotice how the current went up too? - Here's another.";
					SetVoltTarget(4);
				}
				break;
			}
			case State.kTarget2:{
				if (voltMeter.GetComponent<CircuitElementVoltmeter>().IsOnTarget()){
					if (!voltMeter.GetComponent<CircuitElementVoltmeter>().buttonActivated){
					}
					else{
						state = State.kAchieved2;
						timeStart = Time.time;
					}
				}
				break;
			}
			case State.kAchieved2:{
				if (Time.time > timeStart + duration){
					state = State.kTarget3;
					transform.FindChild("MainText").GetComponent<TextMesh>().text = "You've got a new target voltage. Add more cells to acheive it.\nI've changed the brown bits around a bit.\nNotice how the current went up too? - Here's another.\nAnd one more....";
					SetVoltTarget(1);
				}
				break;
			}	
			case State.kTarget3:{
				if (voltMeter.GetComponent<CircuitElementVoltmeter>().IsOnTarget()){
					if (!voltMeter.GetComponent<CircuitElementVoltmeter>().buttonActivated){
					}
					else{
						state = State.kAchieved3;
						timeStart = Time.time;
					}
				}
				break;
			}	
			case State.kAchieved3:{
				if (Time.time > timeStart + duration){
					Tutorial.singleton.Deactivate();
					triggerHandle.Invoke();
					Deactivate();
				}
				break;
			}	
		}
	}		
	
	

	
	void SetVoltTarget(float target){
		if (voltMeter.GetComponent<CircuitElementVoltmeter>().targetVolts != target){
			voltMeter.GetComponent<CircuitElementVoltmeter>().targetVolts = target;
			voltMeter.GetComponent<CircuitElementVoltmeter>().hasTarget = true;
		}
			
	}
}
