using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameModeManager : MonoBehaviour {
	public static GameModeManager singleton = null;
	
	public GameObject sidePanel;
	public GameObject levelInfo;
	public GameObject startGameDlg;
	public GameObject levelCompleteDlg;
	public GameObject gameCompleteDlg;
	public GameObject levelStartMessageDlg;
	public GameObject ammeterEffect;
	public GameObject voltMeterEffect;
	
	float endOfGameTime = -100f;
	float endOfGameLifeTime = 3f;
	
	float gameStartTime = 0;
	
	
	public enum GameMode{
		kStart,
		kStartEditor,
		kTitleScreen,
		kPlayLevelInit,
		kPlayLevel,
		kLevelCompleteWait,
		kLevelComplete,
		kGameComplete,
		kQuitGame
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
	
	public float GetGameTime(){
		return Time.fixedTime - gameStartTime;
	}
	
	
	

		
	void OnDestroy(){
		singleton = null;
	}	
	
	public void BulkGameUpdate(){
		UI.singleton.GameUpdate();
		Circuit.singleton.GameUpdate();
		Telemetry.singleton.GameUpdate();
		UI.singleton.LateGameUpdate();
		// Only rerun the simulation if the circuit has changed since last time
		if (Circuit.singleton.IsDirty()){
			Simulator.singleton.GameUpdate();
			Circuit.singleton.ResetDirty();
		}

	}
	
	void HandleSideButtons(){
		Transform panelTranform = sidePanel.transform.FindChild("BottomPanel");
		panelTranform.FindChild ("PreviousLevelButton").GetComponent<Button>().interactable = (LevelManager.singleton.currentLevelIndex > 1);
		panelTranform.FindChild ("SaveLevelButton").gameObject.SetActive(enableEditor);
		panelTranform.FindChild ("ClearLevelButton").gameObject.SetActive(enableEditor);
		panelTranform.FindChild ("ResaveAllButton").gameObject.SetActive(enableEditor);
		panelTranform.FindChild ("RefreshAllButton").gameObject.SetActive(enableEditor);
	}
	
	bool IsLevelComplete(){
		return (numLevelTriggers != 0 && triggersTriggered == numLevelTriggers);
	}
	
	void ResetTriggerCount(){
		// Reset this as it must be reevaualted every frame
		triggersTriggered = 0;
	}	
	
	void HandleLevelInfo(){
		if (gameMode != GameMode.kTitleScreen){
			levelInfo.transform.FindChild("CurrentLevel").GetComponent<Text>().text = "Current Level: " + LevelManager.singleton.currentLevelIndex + " / " + (LevelManager.singleton.levelsToLoad.GetLength(0)-1);
			levelInfo.transform.FindChild("TriggersActivated").GetComponent<Text>().text = "Triggers Activated: " + triggersTriggered + " / " + numLevelTriggers;
		}
		else{
			levelInfo.transform.FindChild("CurrentLevel").GetComponent<Text>().text = "";
			levelInfo.transform.FindChild("TriggersActivated").GetComponent<Text>().text = "";
		}
		
		Vector2 offsetMin = levelInfo.transform.FindChild("CurrentLevel").GetComponent<RectTransform>().offsetMin;
		offsetMin.x = sidePanel.GetComponent<RectTransform>().offsetMin.x;
		levelInfo.transform.FindChild("CurrentLevel").GetComponent<RectTransform>().offsetMin= offsetMin;
		
		
	}
	
	void ResetSidePanel(){
		sidePanel.transform.FindChild("ElementSelectPanel").GetComponent<ElementSelectPanel>().Cleanup();
		sidePanel.transform.FindChild("ElementSelectPanel").GetComponent<ElementSelectPanel>().Start();

	}
	
	

	// FixedUpdate is called once per frame
	public void FixedUpdate () {
	
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
				gameStartTime = Time.fixedTime;
				break;				
			case GameMode.kTitleScreen:
				UI.singleton.HideMousePointer();
				if (startGame){		
					startGameDlg.SetActive(false);
					gameStartTime = Time.fixedTime;
					Telemetry.singleton.RegisterEvent(Telemetry.TelemetryEvent.kNewGameStarted);
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
				Telemetry.singleton.RegisterEvent(Telemetry.TelemetryEvent.kLevelStarted);
				
				ResetSidePanel();
				AudioListener.volume = 1f;
			
				break;	
			case GameMode.kPlayLevel:
				if (IsLevelComplete() && !enableEditor){
					levelCompletewWaitStartTime = Time.fixedTime;
					gameMode = GameMode.kLevelCompleteWait;
					Telemetry.singleton.RegisterEvent(Telemetry.TelemetryEvent.kLevelCompleteWait);
				}
				if (quitGame){
					gameMode = GameMode.kQuitGame;
				}				
			break;	
			case GameMode.kLevelCompleteWait:				
				if (Time.fixedTime > levelCompletewWaitStartTime + levelCompletewWaitDuration){
					Camera.main.transform.FindChild("Quad").gameObject.SetActive(true);			
					sidePanel.GetComponent<PanelController>().Deactivate();
					if (LevelManager.singleton.IsOnLastLevel()){
						gameCompleteDlg.SetActive(true);
						TriggerEndOfGameEffects();
						Telemetry.singleton.RegisterEvent(Telemetry.TelemetryEvent.kGameComplete);
					}
					else{
						Telemetry.singleton.RegisterEvent(Telemetry.TelemetryEvent.kLevelComplete);
						levelCompleteDlg.SetActive(true);
					}
					gameMode =  GameMode.kLevelComplete;
					Camera.main.transform.FindChild("Quad").gameObject.SetActive(true);			
				}
				break;
				
			case GameMode.kLevelComplete:
				Camera.main.transform.FindChild("Quad").gameObject.SetActive(!ShouldPlayEndOfGameEffects());	
				if (!ShouldPlayEndOfGameEffects() && AudioListener.volume > 0){
					AudioListener.volume = AudioListener.volume - 0.01f;
				}
				if (restartLevel){
					restartLevel = false;
					gameMode = GameMode.kPlayLevelInit;
				}
				if (nextLevel){
					LevelManager.singleton.currentLevelIndex++;
					gameMode = GameMode.kPlayLevelInit;
				}
  				if (quitGame){
					gameMode = GameMode.kQuitGame;
				}
				break;	
			case GameMode.kQuitGame:
				Telemetry.singleton.RegisterEvent(Telemetry.TelemetryEvent.kGameFinished);		
				gameMode = GameMode.kStart;
				break;
				
		}
		
		HandleSideButtons();
		HandleLevelInfo();
		UpdateEndOfGameEffects();
		
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
	
	bool ShouldPlayEndOfGameEffects(){
		return Time.realtimeSinceStartup < endOfGameTime + endOfGameLifeTime;
	}
	
	
	void UpdateEndOfGameEffects(){
		if (ShouldPlayEndOfGameEffects()){
			Rect bounds = Circuit.singleton.bounds;
			Vector3 randPos = new Vector3(Random.Range (bounds.min.x, bounds.max.x), Random.Range (bounds.min.y, bounds.max.y), 0f);
			int val = (int)(Random.Range (0, 5));
			switch(val){
				case 0: 
					Instantiate(ammeterEffect, randPos, Quaternion.identity);
					break;
				case 1: 
					Instantiate(voltMeterEffect, randPos, Quaternion.identity);
					break;
					
			}
			
		}
	}
	
	
	void TriggerEndOfGameEffects(){
		Camera.main.GetComponent<CamControl>().CentreCamera();
		endOfGameTime = Time.realtimeSinceStartup;
		
		
	}
	
}
