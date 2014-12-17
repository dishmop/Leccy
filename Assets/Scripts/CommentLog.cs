using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CommentLog : MonoBehaviour, TelemetryListener {
	public GameObject sampleFontSize;
	
	public bool showUserComments;
	public bool showcursor;
	public bool showCircuitChange;
	public bool showUIState;
	public bool showGameState;
	public bool showUnkown;
	
	List<string>	displayStrings = new List<string>();
	
	

	// Use this for initializati	on
	void Start () {
		Telemetry.singleton.RegisterListener(this);
	
	}
	
	// Update is called once per frame
	void Update () {

		
	
	}
	
	public void ShowType(Telemetry.EventType type, bool show){
		switch (type){
			case Telemetry.EventType.kCircuitChange:{
				showCircuitChange = show;
				break;
			}
			case Telemetry.EventType.kCursor:{
				showcursor = show;
				break;
			}
			case Telemetry.EventType.kGameState:{
				showGameState = show;
				break;
			}
			case Telemetry.EventType.kUIState:{
				showUIState = show;
				break;
			}
			case Telemetry.EventType.kUnknown:{
				showUserComments = show;
				break;
			}
			case Telemetry.EventType.kUserComments:{
				showCircuitChange = show;
				break;
			}
		}
	}
	
	public void OnEvent(Telemetry.Event e, string text){
		if (!Telemetry.singleton.IsTextEvent(e)){
			Debug.LogError ("Trying to write out non text event with text");
			return;
		}
		if (!showUserComments && Telemetry.singleton.GetEventType(e) == Telemetry.EventType.kUserComments){
			return;
		}	
		string newString = PlaybackTime.FormatTime(GameModeManager.singleton.GetGameTime()) + ": " + e.ToString().Substring (1, e.ToString().Length-1)  + ": " + text;
		displayStrings.Add(newString);
		UpdateText();
	}
	
	public void OnEvent(Telemetry.Event e){
		if (Telemetry.singleton.IsTextEvent(e)){
			Debug.LogError ("Trying to write out text event with no text");
			return;
		}

		if (!showcursor && Telemetry.singleton.GetEventType(e) == Telemetry.EventType.kCursor){
			return;
		}
		if (!showCircuitChange && Telemetry.singleton.GetEventType(e) == Telemetry.EventType.kCircuitChange){
			return;
		}	
		if (!showUIState && Telemetry.singleton.GetEventType(e) == Telemetry.EventType.kUIState){
			return;
		}
		if (!showUnkown && Telemetry.singleton.GetEventType(e) == Telemetry.EventType.kUnknown){
			return;
		}
		if (!showGameState && Telemetry.singleton.GetEventType(e) == Telemetry.EventType.kGameState){
			return;
		}
		
		string newString  = PlaybackTime.FormatTime(GameModeManager.singleton.GetGameTime()) + ": " + e.ToString().Substring (1, e.ToString().Length-1);
		
		displayStrings.Add(newString);
		UpdateText();
	}
	
	public void OnNewGame(){
		displayStrings.Clear();
	}
	void UpdateText(){
		int maxLines = CalcMaxLinesAllowed();
		while (displayStrings.Count > maxLines){
			displayStrings.RemoveAt (0);
		}
		// Use StringBuilder for concatenation in tight loops.
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		
		for (int i = 0; i < displayStrings.Count; ++i){
			sb.AppendLine(displayStrings[i]);
		}
		GetComponent<Text>().text = sb.ToString();
		
			
	}
	
	int CalcMaxLinesAllowed(){
		Rect rect = GetComponent<RectTransform>().rect;
		int fontSize = GetComponent<Text>().fontSize;
		return (int)(rect.height / (float)fontSize);
	}
}
