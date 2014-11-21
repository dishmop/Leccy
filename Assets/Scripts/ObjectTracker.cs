using UnityEngine;
using System.Collections;

public class ObjectTracker : MonoBehaviour {

	public string trackerName;
	
	void Awake(){
		Debug.Log (trackerName + gameObject.GetInstanceID().ToString() +  " Created");
	}
	
	void OnDestroy () {
		Debug.Log (trackerName + gameObject.GetInstanceID().ToString() + " Destroyed");
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
