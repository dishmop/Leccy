using UnityEngine;
using System.Collections;

public class Tut2CircuitWideAnchors : TutTriggerBase {

	float timeStart;
	float duration = 2;
	
	enum State{
		kTesting,
		kAchieved
	}
	
	State state = State.kTesting;
	

	// Use this for initialization
	protected override void OnEnable () {
		base.OnEnable();
		
		GameModeManager.singleton.disableLevelCompletion = true;
		
		GridPoint[] ammeterGridPoints = new GridPoint[9];
		
		// Vertcal
		ammeterGridPoints[0] = new GridPoint(10, 12);
		ammeterGridPoints[1] = new GridPoint(10, 9);
		ammeterGridPoints[2] = new GridPoint(10, 6);
		ammeterGridPoints[3] = new GridPoint(13, 12);
		ammeterGridPoints[4] = new GridPoint(13, 6);
		
		// Horizontal
		ammeterGridPoints[5] = new GridPoint(11, 13);
		ammeterGridPoints[6] = new GridPoint(12, 8);
		ammeterGridPoints[7] = new GridPoint(14, 9);
		ammeterGridPoints[8] = new GridPoint(14, 5);
		
		for (int i = 0; i < 9; ++i){
			CircuitElement element = Circuit.singleton.GetElement(ammeterGridPoints[i]);			if (element != null){
				UI.singleton.RemoveElement( element.gameObject);
			}
			GameObject newElement = ElementFactory.singleton.InstantiateElement("Ammeter");
			newElement.GetComponent<CircuitElementAmmeter>().orient = (i < 5) ? 2 : 1; 
			newElement.GetComponent<CircuitElementAmmeter>().hasTarget = true;
			newElement.GetComponent<CircuitElementAmmeter>().targetAmp = 1;
			UI.singleton.PlaceElement(newElement, ammeterGridPoints[i]);	
			Circuit.singleton.anchors[ammeterGridPoints[i].x, ammeterGridPoints[i].y].isAnchored[Circuit.kCentre] = true;	
		}
		
		ElementFactory.singleton.SetStock("Wire", 50);
		state = State.kTesting;

	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (!IsActive()) return;
		
		int numAchieved = 0;
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			if (go.GetComponent<CircuitElementAmmeter>() != null){
				if (go.GetComponent<CircuitElementAmmeter>().IsOnTarget() && go.GetComponent<CircuitElementAmmeter>().buttonActivated){
					numAchieved++;
				}
			}
		}
		
		switch (state){
			case State.kTesting:{
				if (numAchieved == 9){
					state = State.kAchieved;
					timeStart = Time.time;
				}
				break;
			}
			case State.kAchieved:{
				if (numAchieved != 9){
					state = State.kTesting;
				}
				if (Time.time > timeStart + duration){
					triggerHandle.Invoke();
					Deactivate();
				}
			
				break;
			}
		}
	
	}
}
