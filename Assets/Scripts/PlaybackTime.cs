using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class PlaybackTime : MonoBehaviour {

	public static string FormatTime(float time){
		TimeSpan timeSpan = TimeSpan.FromSeconds(time);
		return string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
	}
	
	
	// Update is called once per frame
	void Update () {
	
		//float gameTime = Telemetry.singleton.GetPlaybackTime();
		float gameTime = GameModeManager.singleton.GetGameTime();

		GetComponent<Text>().text = FormatTime(gameTime);
		
	}
	
}
