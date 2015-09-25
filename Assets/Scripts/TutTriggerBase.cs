using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class TutTriggerBase : MonoBehaviour {
	public UnityEvent triggerHandle;
	bool  isActive;
	
	protected virtual void OnEnable(){
		isActive = true;
		SetObjectAlpha(1);
	}
	
	protected void Deactivate(){
		isActive = false;
		SetObjectAlpha(0.5f);
	}
	
	protected bool IsActive(){
		return isActive;
	}
	

	

	
	void SetObjectAlpha(float a){
		// Find the tutorial object
		Transform trans = transform;
		while (trans != null && trans.GetComponent<TutorialObject>() == null){
			trans = trans.parent;
		}
		if (trans.GetComponent<TutorialObject>() != null){
			trans.GetComponent<TutorialObject>().SetAlpha(a);
		}
		
	}
}
