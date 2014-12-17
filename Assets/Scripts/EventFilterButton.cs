using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EventFilterButton : MonoBehaviour {

	public Telemetry.EventType eventType;
	public bool enableState;
	public Color enableColor;
	public Color disableColor;
	public GameObject eventLog;
	
	public void OnClick(){
		enableState = !enableState;
		SetupColor();
		SetupOtherObjects();
	}

	// Use this for initialization
	void Start () {
		SetupColor();
		SetupOtherObjects();
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void SetupColor(){
		GetComponent<Text>().color = enableState ? enableColor : disableColor;
	}
	
	void SetupOtherObjects(){
		eventLog.GetComponent<CommentLog>().ShowType(eventType, enableState);
		Telemetry.singleton.EnableStepType(eventType, enableState);
		
	}
}
