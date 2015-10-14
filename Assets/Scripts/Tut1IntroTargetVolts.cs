using UnityEngine;
using System.Collections;

public class Tut1IntroTargetVolts : TutTriggerBase {

	GameObject voltMeter = null;
	float timeStart;
	float duration = 3;
	
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
		SetVoltTarget(3);
		state = State.kTarget1;
		transform.FindChild("horizontal_arrow").gameObject.SetActive(true);
		
		transform.FindChild("MainText").GetComponent<TextMesh>().text = "Sometimes the voltmeter\nhas a target voltage.\nThis is displayed in\nthe circle.\n\nWhen the target is\nacheived - it makes\nan annoying noise.\n\nTry it!";
		UI.singleton.elementSelectPanel.GetComponent<ElementSelectPanel>().SetSelection("Cell");
		

	}
	
	// Update is called once per frame
	void Update () {
		switch (state){
			case State.kTarget1:{
				if (voltMeter.GetComponent<CircuitElementVoltmeter>().IsOnTarget()){
					if (!voltMeter.GetComponent<CircuitElementVoltmeter>().buttonActivated){
						transform.FindChild("MainText").GetComponent<TextMesh>().text = "Pheweee - that's pretty\nannoying! - click in\nthe target circle\nto turn it off.";
					}
					else{
						transform.FindChild("MainText").GetComponent<TextMesh>().text = "That's better!";
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
					transform.FindChild("MainText").GetComponent<TextMesh>().text = "That's better!\nNow try a new one.";
					SetVoltTarget(5);
				}
				break;
			}
			case State.kTarget2:{
				if (voltMeter.GetComponent<CircuitElementVoltmeter>().IsOnTarget()){
					if (!voltMeter.GetComponent<CircuitElementVoltmeter>().buttonActivated){
						transform.FindChild("MainText").GetComponent<TextMesh>().text = "Achieved!\nNow click in\nthe target circle";
					}
					else{
						transform.FindChild("MainText").GetComponent<TextMesh>().text = "Thanks";
						state = State.kAchieved2;
						timeStart = Time.time;
					}
				}
				break;
			}
			case State.kAchieved2:{
				if (Time.time > timeStart + duration){
					state = State.kTarget3;
					transform.FindChild("MainText").GetComponent<TextMesh>().text = "Thanks\nOne more....";
					SetVoltTarget(1);
				}
				break;
			}	
			case State.kTarget3:{
				if (voltMeter.GetComponent<CircuitElementVoltmeter>().IsOnTarget()){
					if (!voltMeter.GetComponent<CircuitElementVoltmeter>().buttonActivated){
						transform.FindChild("MainText").GetComponent<TextMesh>().text = "Achieved!";
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
