using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class GameModeManager : MonoBehaviour {
	public static GameModeManager singleton = null;
	
	public GameObject sidePanel;
	public GameObject levelInfo;
	public GameObject telemetryPanel;
	public GameObject startGameDlg;
	public GameObject splashScreenDlg;
	public GameObject levelCompleteDlg;
	public GameObject gameCompleteDlg;
	public GameObject levelStartMessageDlg;
	public GameObject ammeterEffect;
	public GameObject voltMeterEffect;
	
	float endOfGameTime = -100f;
	float endOfGameLifeTime = 3f;
	
	float gameStartTime = 0;
	
	public static string playerNameKey = "PlayerName";
	
	
	public enum GameMode{
		kNone,
		kSplash,
		kStart,
		kStartEditor,
		kTitleScreen,
		kPlayLevelInit,
		kPlayLevel,
		kLevelCompleteWait,
		kLevelComplete,
		kGameComplete,
		kQuitGame,
		kReallyQuitGame,
	};
	
	public bool	enableEditor;
	public bool restartLevel;
	public bool nextLevel;
	public bool quitGame;
	public bool reallyQuitGame;
	public bool startGame;		
	
	GameMode gameMode = GameMode.kNone;
	GameMode lastGameMode = GameMode.kNone;
	
	int 	triggersTriggered = 0;
	int 	numLevelTriggers = 0;
	
	float levelCompletewWaitStartTime;
	float levelCompletewWaitDuration = 1f;
	
	

	
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
		
		gameMode = enableEditor ? GameMode.kStartEditor : GameMode.kNone;
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
	
	public void ReallyQuitGame(){
		reallyQuitGame = true;
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
	
	
	public void ResetGameTime(){
		gameStartTime = Time.fixedTime;
	}
	
	// For the game to be at a specific time
	public void ForceSetGameTime(float nowTime){
		gameStartTime = Time.fixedTime - nowTime;
		
	}	
	

		
	void OnDestroy(){
		singleton = null;
	}	
	
	public void BulkGameUpdate(){
		UI.singleton.GameUpdate();
		if (Telemetry.singleton.mode == Telemetry.Mode.kPlayback){
			Telemetry.singleton.GameUpdate();
		}
		Circuit.singleton.GameUpdate();
		UI.singleton.LateGameUpdate();
		// Only rerun the simulation if the circuit has changed since last time
		if (Circuit.singleton.IsDirty()){
			Telemetry.singleton.RegisterEvent(Telemetry.Event.kCircuitChanged);
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
	
	void HandleTelemetryUI(){
		telemetryPanel.SetActive(Telemetry.singleton.enableTelemetry);
		telemetryPanel.transform.FindChild ("TelemetryFrame").FindChild("TelemetryPlayback").gameObject.SetActive(Telemetry.singleton.mode == Telemetry.Mode.kPlayback);
		telemetryPanel.transform.FindChild ("TelemetryFrame").FindChild("TelemetryRecord").gameObject.SetActive(Telemetry.singleton.mode == Telemetry.Mode.kRecord && Telemetry.singleton.HasFile());
		
		
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
			levelInfo.transform.FindChild("CurrentLevel").GetComponent<Text>().text = "Current Level: " + LevelManager.singleton.currentLevelIndex + " / " + (LevelManager.singleton.levelsToLoad.GetLength(0)-1) + ": " + LevelManager.singleton.GetCurrentLevelName()	;
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
	
	void OnActivateTitle(){
		if (PlayerPrefs.HasKey(playerNameKey)){
			string name = PlayerPrefs.GetString(playerNameKey);
			startGameDlg.transform.FindChild("InputField").FindChild("Text").GetComponent<Text>().text = name;
		}
		if (startGameDlg.transform.FindChild("InputField").FindChild("Text").GetComponent<Text>().text != ""){
			startGameDlg.transform.FindChild("InputField").FindChild("Placeholder").GetComponent<Text>().enabled = false;
		}
		
		
		
	}
	
	

	// FixedUpdate is called once per frame
	public void FixedUpdate () {
	
		BulkGameUpdate ();

		HandleTelemetryUI();
		
		// Handle quitting the application - if we are no in the quit game state - then go there first
		// which shuts the level down properly
		// Then go to the really quite game state which actually closes the application
		if (reallyQuitGame){
			if (gameMode != GameMode.kQuitGame && gameMode != GameMode.kStart && gameMode != GameMode.kStartEditor && gameMode != GameMode.kReallyQuitGame){
				gameMode = GameMode.kQuitGame;
			}
		}
		
	
		switch (gameMode){
			case GameMode.kNone:
				gameMode = GameMode.kSplash;
				break;
			case GameMode.kSplash:
				splashScreenDlg.SetActive(true);
				UI.singleton.HideMousePointer();
				if (startGame || !Telemetry.singleton.enableTelemetry || Telemetry.singleton.mode == Telemetry.Mode.kPlayback){	
					gameMode = GameMode.kStart;
					splashScreenDlg.SetActive(false);
				}
				break;
		case GameMode.kStart:
				sidePanel.GetComponent<PanelController>().ForceDeactivate();
				if (!Telemetry.singleton.enableTelemetry || Telemetry.singleton.mode == Telemetry.Mode.kRecord){
					startGameDlg.SetActive(true);
					OnActivateTitle();	
				}
				levelCompleteDlg.SetActive(false);
				gameCompleteDlg.SetActive(false);
				EventSystem.current.SetSelectedGameObject(startGameDlg);
				levelStartMessageDlg.SetActive(false);
				Camera.main.transform.FindChild("Quad").gameObject.SetActive(false);
				if (!Telemetry.singleton.enableTelemetry || Telemetry.singleton.mode == Telemetry.Mode.kRecord) LevelManager.singleton.LoadLevel(0);
				if (reallyQuitGame){
					gameMode =GameMode.kReallyQuitGame;
				}
				else{
					gameMode = GameMode.kTitleScreen;
				}
				
				break;
			case GameMode.kStartEditor:
				sidePanel.GetComponent<PanelController>().ForceDeactivate();
				sidePanel.GetComponent<PanelController>().Activate();
				Camera.main.transform.FindChild("Quad").gameObject.SetActive(false);
				levelCompleteDlg.SetActive(false);
				if (reallyQuitGame){
					gameMode =GameMode.kReallyQuitGame;
				}
				else{
					gameMode = GameMode.kPlayLevelInit;
				}				

				gameCompleteDlg.SetActive(false);
				levelStartMessageDlg.SetActive(false);
				ResetGameTime ();
				break;				
			case GameMode.kTitleScreen:
				UI.singleton.HideMousePointer();
				ResetGameTime ();
				if (startGame){		
					startGameDlg.SetActive(false);
					ResetGameTime ();
					Telemetry.singleton.RegisterEvent(Telemetry.Event.kNewGameStarted);
					string name = startGameDlg.transform.FindChild("InputField").FindChild("Text").GetComponent<Text>().text;
					string safeName = Regex.Replace(name, "[^A-Za-z0-9] ","-");	
					PlayerPrefs.SetString(playerNameKey, safeName);
					string nameString = "My name is " + (name == "" ? "NONE-GIVEN" : safeName);
					Telemetry.singleton.RegisterEvent(Telemetry.Event.kUserComment, nameString);
					gameMode = GameMode.kPlayLevelInit;
				}
				// When in the title screen do regular uploads of files to the server (if there are any to upload)
				ServerUpload.singleton.GameUpdate();
				break;
			case GameMode.kPlayLevelInit:
				gameMode = GameMode.kPlayLevel;
				sidePanel.GetComponent<PanelController>().Activate();
				levelStartMessageDlg.SetActive(true);	
				if ((!Telemetry.singleton.enableTelemetry || Telemetry.singleton.mode == Telemetry.Mode.kRecord) && !enableEditor) LevelManager.singleton.LoadLevel();
				Telemetry.singleton.RegisterEvent(Telemetry.Event.kLevelStarted);
				
				ResetSidePanel();
				AudioListener.volume = 1f;
			
				break;	
			case GameMode.kPlayLevel:
				levelCompleteDlg.SetActive(false);
				gameCompleteDlg.SetActive(false);
				Camera.main.transform.FindChild("Quad").gameObject.SetActive(false);					
				if (IsLevelComplete() && !enableEditor){
					levelCompletewWaitStartTime = Time.fixedTime;
					gameMode = GameMode.kLevelCompleteWait;
					Telemetry.singleton.RegisterEvent(Telemetry.Event.kLevelCompleteWait);
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
						Telemetry.singleton.RegisterEvent(Telemetry.Event.kGameComplete);
					}
					else{
						Telemetry.singleton.RegisterEvent(Telemetry.Event.kLevelComplete);
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
				Telemetry.singleton.RegisterEvent(Telemetry.Event.kGameFinished);	
				LevelManager.singleton.currentLevelIndex = 1;
				if (enableEditor)
					gameMode = GameMode.kStartEditor;
				else
					gameMode = GameMode.kStart;
				break;
			case GameMode.kReallyQuitGame:
				// Need to wait for any server uploads to finish trying
				ServerUpload.singleton.ForceUpload();
				while (!ServerUpload.singleton.CanQuit()){
					ServerUpload.singleton.GameUpdate();
				}
				AppHelper.Quit();
				gameMode = GameMode.kStart;
				reallyQuitGame = false;
				break;
				
				
		}
		
		// Do telemetry recording
		if (Telemetry.singleton.mode == Telemetry.Mode.kRecord){
			Telemetry.singleton.GameUpdate();
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
		
		// Register any state change events
		Telemetry.singleton.RegisterEvent((Telemetry.Event)((int)Telemetry.Event.kUIStateNone + (int)gameMode));
		if (lastGameMode != gameMode){
		}
		lastGameMode = gameMode;	
	}
	
	public void SetUIState(int state){
		gameMode = (GameMode)state;
	}
	
	bool ShouldPlayEndOfGameEffects(){
		return Time.fixedTime < endOfGameTime + endOfGameLifeTime;
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
		endOfGameTime = Time.fixedTime;
		
		
	}
	
}
