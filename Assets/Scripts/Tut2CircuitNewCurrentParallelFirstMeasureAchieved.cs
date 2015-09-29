using UnityEngine;
using System.Collections;
using System.Text;

public class Tut2CircuitNewCurrentParallelFirstMeasureAchieved : TutTriggerBase {

	
	// Use this for initialization
	protected override void OnEnable () {
		base.OnEnable();
		
		SetupButtonVisibility();
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
	
		SetupButtonVisibility();
		
	
	}
	
	void SetupButtonVisibility(){
		if (Tutorial.singleton.hasDoneOneResistorTut){
			transform.FindChild("ContinueButtonFinish").gameObject.SetActive(true);
			transform.FindChild("ContinueButtonNext").gameObject.SetActive(false);
		}
		else{
			transform.FindChild("ContinueButtonFinish").gameObject.SetActive(false);
			transform.FindChild("ContinueButtonNext").gameObject.SetActive(true);
		}
		
	}
}
