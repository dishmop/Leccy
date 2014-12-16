using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SendButtonScript : MonoBehaviour {

	public GameObject textHolderObj;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public void OnClick(){
		Telemetry.singleton.RegisterEvent(Telemetry.Event.kUserComment, textHolderObj.GetComponent<Text>().text);
	
	}
}
