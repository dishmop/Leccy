using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlaybackButton : MonoBehaviour {

	public Telemetry.PlaybackState desiredState;

	// Use this for initialization
	public void OnClick () {
		Telemetry.singleton.SetPlayabckState(desiredState);
	
	}
	
	void Update(){
		transform.FindChild ("Image").GetComponent<Button>().interactable =  Telemetry.singleton.CanEnterPlaybackState(desiredState);
		
	}
}
