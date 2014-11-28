using UnityEngine;
using System.Collections;

public class GameModeManager : MonoBehaviour {
	public static GameModeManager singleton = null;
	
	public enum GameMode{
		kStart,
		kStartEditor,
		kTitleScreen,
		kPlayLevel,
		kLevelComplete,
		kGameComplete
	};
	
	public bool	enableEditor;
	public GameMode gameMode = GameMode.kStart;
	
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
		
		gameMode = enableEditor ? GameMode.kStartEditor : GameMode.kStart;
	}	
	
	
	void OnDestroy(){
		singleton = null;
	}	
	
	void BulkUpdate(){
		UI.singleton.GameUpdate();
		Circuit.singleton.GameUpdate();
		UI.singleton.LateGameUpdate();
		Simulator.singleton.GameUpdate();
	}


	// FixedUpdate is called once per frame
	void FixedUpdate () {
	
		switch (gameMode){
			case GameMode.kStart:
				LevelManager.singleton.currentLevelIndex = 0;
				LevelManager.singleton.LoadLevel();
				gameMode =GameMode.kTitleScreen;
				break;
			case GameMode.kStartEditor:
				gameMode =GameMode.kPlayLevel;
				break;				
			case GameMode.kTitleScreen:
				break;
			case GameMode.kPlayLevel:
				break;		
		}
		
		// If we are not in editor mode, then we should honour the anchors
		if (!enableEditor){
			UI.singleton.honourAnchors = true;
		}
			
		BulkUpdate ();
		
	}
	
}
