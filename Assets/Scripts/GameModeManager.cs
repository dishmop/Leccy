using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
		kLevelCompleteWait,
		kLevelComplete,
		kGameComplete
	};
	
	public bool	enableEditor;
	public bool restartLevel;
	public bool nextLevel;
	public bool quitGame;
	public bool startGame;		
	
	public GameMode gameMode = GameMode.kStart;
	
	int 	triggersTriggered = 0;
	int 	numLevelTriggers = 0;
	
	float levelCompletewWaitStartTime;
	float levelCompletewWaitDuration = 1f;
	
	

	
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
	
	void BulkGameUpdate(){
		UI.singleton.GameUpdate();
		Circuit.singleton.GameUpdate();
		UI.singleton.LateGameUpdate();
		Simulator.singleton.GameUpdate();

	}
	
	void HandleSideButtons(){
		Transform panelTranform = sidePanel.transform.FindChild("BottomPanel");
		panelTranform.FindChild ("PreviousLevelButton").GetComponent<Button>().interactable = (LevelManager.singleton.currentLevelIndex > 1);
		panelTranform.FindChild ("SaveLevelButton").gameObject.SetActive(enableEditor);
		panelTranform.FindChild ("ClearLevelButton").gameObject.SetActive(enableEditor);
	}
	
	bool IsLevelComplete(){
		return (numLevelTriggers != 0 && triggersTriggered == numLevelTriggers);
	}
	
	void ResetTriggerCount(){
		// Reset this as it must be reevaualted every frame
		triggersTriggered = 0;
	}	
	
	
	
	

	// FixedUpdate is called once per frame
	void FixedUpdate () {
	
		BulkGameUpdate ();
	
		switch (gameMode){
			case GameMode.kStart:
				sidePanel.GetComponent<PanelController>().ForceDeactivate();
				startGameDlg.SetActive(true);
				levelCompleteDlg.SetActive(false);
				gameCompleteDlg.SetActive(false);
				EventSystem.current.SetSelectedGameObject(startGameDlg);
				levelStartMessageDlg.SetActive(false);
				Camera.main.transform.FindChild("Quad").gameObject.SetActive(false);
				LevelManager.singleton.LoadLevel(0);
				AudioListener.pause = true;
				gameMode =GameMode.kTitleScreen;
				break;
			case GameMode.kStartEditor:
				sidePanel.GetComponent<PanelController>().ForceDeactivate();
				sidePanel.GetComponent<PanelController>().Activate();
				Camera.main.transform.FindChild("Quad").gameObject.SetActive(false);
				levelCompleteDlg.SetActive(false);
				gameMode =GameMode.kPlayLevelInit;
				gameCompleteDlg.SetActive(false);
				levelStartMessageDlg.SetActive(false);
				break;				
			case GameMode.kTitleScreen:
				if (startGame){		
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
				Camera.main.transform.FindChild("Quad").gameObject.SetActive(false);					
				if (!enableEditor) LevelManager.singleton.LoadLevel();
				AudioListener.pause = false;
			
				break;	
			case GameMode.kPlayLevel:
				if (IsLevelComplete() && !enableEditor){
					levelCompletewWaitStartTime = Time.fixedTime;
					gameMode = GameMode.kLevelCompleteWait;
				
				}
				if (quitGame){
					gameMode = GameMode.kStart;
				}				
			break;	
			case GameMode.kLevelCompleteWait:				
				if (Time.fixedTime > levelCompletewWaitStartTime + levelCompletewWaitDuration){
					Camera.main.transform.FindChild("Quad").gameObject.SetActive(true);			
					sidePanel.GetComponent<PanelController>().Deactivate();
					if (LevelManager.singleton.IsOnLastLevel()){
						gameCompleteDlg.SetActive(true);
					}
					else{
						levelCompleteDlg.SetActive(true);
					}
					gameMode =  GameMode.kLevelComplete;
					Camera.main.transform.FindChild("Quad").gameObject.SetActive(true);			
				}
				break;
				
			case GameMode.kLevelComplete:
				if (restartLevel){
					restartLevel = false;
					gameMode = GameMode.kPlayLevelInit;
				}
				if (nextLevel){
					LevelManager.singleton.currentLevelIndex++;
					gameMode = GameMode.kPlayLevelInit;
				}
			   if (quitGame){
					gameMode = GameMode.kStart;
				}
				break;	
		}
		
		HandleSideButtons();
		
		// If we are not in editor mode, then we should honour the anchors
		if (!enableEditor){
			UI.singleton.honourAnchors = true;
		}
		ResetTriggerCount();
		
		// Reset the triggers
		startGame = false;
		restartLevel = false;
		nextLevel = false;
		quitGame = false;		
			

		
	}
	
}
