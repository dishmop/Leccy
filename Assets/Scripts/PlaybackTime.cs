using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlaybackTime : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
		GetComponent<Text>().text = Telemetry.singleton.GetPlaybackTime();
	
	}
}
