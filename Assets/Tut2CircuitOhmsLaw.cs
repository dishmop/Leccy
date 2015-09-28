using UnityEngine;
using System.Collections;

public class Tut2CircuitOhmsLaw : TutTriggerBase {

	// Use this for initialization
	protected override void OnEnable(){
		base.OnEnable();
		UpdateCirclePos();
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		UpdateCirclePos();
	
	}
	
	void UpdateCirclePos(){
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			CircuitElementResistor resistor = go.GetComponent<CircuitElementResistor>();
			if (resistor == null) continue;
			
			Vector3 pos = new Vector3(resistor.GetGridPoint().x, resistor.GetGridPoint().y, transform.position.z);
			transform.FindChild("UICircle").gameObject.SetActive(true);
			transform.FindChild("UICircle").position = pos;
			return;
		}
		// If it got through to here then there is no resistor
		transform.FindChild("UICircle").gameObject.SetActive(false);
	}
	
}
