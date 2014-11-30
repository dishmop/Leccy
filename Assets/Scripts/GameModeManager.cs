using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class GameModeManager : MonoBehaviour {
	public static GameModeManager singleton = null;
	
	public GameObject sidePanel;
	public GameObject startGameDlg;
	public GameObject levelCompleteDlg;
	public GameObject gameCompleteDlg;
	public GameObject levelStartMessageDlg;
	
	
	public enum GameMode{
		kStart,
		kStartEditor,
		kTitleScreen,
		kPlayLevelInit,
		kPlayLevel,
		kLevelComplete,
		kGameComplete
	};
	
	public bool	enableEditor;
	public bool restartLevel;
	public bool nextLevel;
	public bool quitGame;
			
	public GameMode gameMode = GameMode.kStart;
	
	int 	triggersTriggered = 0;
	int 	numLevelTriggers = 0;
	
	
	bool startGame = false;
	
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
		
		gameMode = enableEditor ? GameMode.kStartEditor : GameMode.kStart;
	}	
	
	public void RestartLevel(){
		restartLevel = true;
	}
	
	public void NextLevel(){
		nextLevel = true;
	}
	
	public void QuitGame(){
		quitGame = true;
	}
	
	
	public void StartGame(){
		startGame = true;
		
	}
	
	public void TriggerComplete(){
		triggersTriggered++;
	}
	
	public void RegisterLevelTrigger(){
		// If below zero (i.e. no triggers in level) step it up to 0 before adding one
		numLevelTriggers++;
	}	
	
	public void UnregisterLevelTrigger(){
		// If below zero (i.e. no triggers in level) step it up to 0 before adding one
		numLevelTriggers--;
	}		
	

		
	void OnDestroy(){
		singleton = null;
	}	
	
	void BulkUpdate(){
		UI.singleton.GameUpdate();
		Circuit.singleton.GameUpdate();
		UI.singleton.LateGameUpdate();
		Simulator.singleton.GameUpdate();
		HandleTriggers();
	}
	
	bool IsLevelComplete(){
		return (numLevelTriggers != 0 && triggersTriggered == numLevelTriggers);
	}
	
	void HandleTriggers(){
		// Reset this as it must be reevaualted every frame
		triggersTriggered = 0;
	}	
	
	
	
	

	// FixedUpdate is called once per frame
	void FixedUpdate () {
	
		switch (gameMode){
			case GameMode.kStart:
				LevelManager.singleton.currentLevelIndex = 0;
				LevelManager.singleton.LoadLevel();
				sidePanel.GetComponent<PanelController>().ForceDeactivate();
				startGameDlg.SetActive(true);
				levelCompleteDlg.SetActive(false);
				gameCompleteDlg.SetActive(false);
				EventSystem.current.SetSelectedGameObject(startGameDlg);
				levelStartMessageDlg.SetActive(false);
				gameMode =GameMode.kTitleScreen;
				break;
			case GameMode.kStartEditor:
				sidePanel.GetComponent<PanelController>().ForceDeactivate();
				sidePanel.GetComponent<PanelController>().Activate();
				levelCompleteDlg.SetActive(false);
				gameMode =GameMode.kPlayLevelInit;
				gameCompleteDlg.SetActive(false);
				levelStartMessageDlg.SetActive(false);
				break;				
			case GameMode.kTitleScreen:
				if (startGame){		
					LevelManager.singleton.currentLevelIndex++;
					LevelManager.singleton.LoadLevel();
					startGameDlg.SetActive(false);
					gameMode = GameMode.kPlayLevelInit;
				}
				break;
			case GameMode.kPlayLevelInit:
				gameMode = GameMode.kPlayLevel;
				levelCompleteDlg.SetActive(false);
				gameCompleteDlg.SetActive(false);
				sidePanel.GetComponent<PanelController>().Activate();
				levelStartMessageDlg.SetActive(true);	
			
				break;	
			case GameMode.kPlayLevel:
				if (IsLevelComplete()){
					sidePanel.GetComponent<PanelController>().Deactivate();
					if (LevelManager.singleton.IsOnLastLevel()){
						gameCompleteDlg.SetActive(true);
					}
					else{
						levelCompleteDlg.SetActive(true);
					}
					gameMode = GameMode.kLevelComplete;
				
				}
				break;	
				
			case GameMode.kLevelComplete:
				if (restartLevel){
					LevelManager.singleton.LoadLevel();
					restartLevel = false;
					gameMode = GameMode.kPlayLevelInit;
				}
				if (nextLevel){
					LevelManager.singleton.currentLevelIndex++;
					LevelManager.singleton.LoadLevel();
					gameMode = GameMode.kPlayLevelInit;
				}
			   if (quitGame){
					gameMode = GameMode.kStart;
				}
				break;	
		}
		
		// If we are not in editor mode, then we should honour the anchors
		if (!enableEditor){
			UI.singleton.honourAnchors = true;
		}
		
		// Reset the triggers
		restartLevel = false;
		nextLevel = false;
		quitGame = false;		
			
		BulkUpdate ();
		
	}
	
}
