using UnityEngine;
using System.Collections;
using System;

public class OldUI : MonoBehaviour {

	public static OldUI singleton;

	public GridPoint	newDrawPoint = new GridPoint();
	public GridPoint	oldDrawPoint = new GridPoint();
	
	// Loading and saving
	public string 		levelToSave = "DefaultLevel";
	public TextAsset[]	levelsToLoad = new TextAsset[10];
	public int			currentLevelIndex = 0;
	public bool			loadLevelOnStartup = false;
	public bool			enableEditor = true;

	
	
	bool 				startupLevelLoaded = false;
	int 				triggersTriggered = 0;
	int					numLevelTriggers = 0;
	float				levelLoadFade = 0;
	float				buttonPulseAlpha = 0;
	int					levelCompleteMsgCountdown;
	
	
	
	enum GameMode{
		kNone,
		kStartScreen,
		kPlayGame,
		kLevelComplete,
		kGameComplete,
		kEditMode
	};
	GameMode		gameMode = GameMode.kNone;
	Rect			toolbarRect = new Rect(400, 25, 1000, 30);

	
	public enum InputMode{
		kWires,
		kCells,
		kResistors,
		kAmmeters,
		kErase,
		kLoadLevel,
		kToggleEdit,
		kSaveLevel,
		kClearAll,
		kBakeConnect,
		kBakeAll,
		kUnbake,
		kNumButtons
	};
	public int numNonEditButtons = 7;
	public InputMode inputMode;
	
	// Toolbar
	string[] toolbarStrings = {"Wires", "Cells", "Resistors", "Ammeter", "Eraser", "Reload Level", "Toggle edit", "Save Level", "Clear all", "Bake connect", "Bake All", "Unbake"};


		
	// Use this for initialization
	void Start () {
		singleton = this;
	
		gameMode = (enableEditor) ? GameMode.kEditMode : GameMode.kStartScreen;
		

	}
	
	bool IsPosInUI(Vector3 pos){
		return toolbarRect.Contains (pos);
	}
	
	// Update is called once per frame
	void Update() {
	
		buttonPulseAlpha = 0.75f + 0.25f * Mathf.Sin (10 * Time.realtimeSinceStartup);
	
		//Camera.main.transform.FindChild("Quad").gameObject.SetActive(gameMode == GameMode.kLevelComplete || gameMode == GameMode.kGameComplete);
		Camera.main.transform.FindChild("Quad").gameObject.SetActive(false);
		Camera.main.transform.FindChild("StartScreen").gameObject.SetActive(gameMode == GameMode.kStartScreen );
		
//		Grid.singleton.gameObject.SetActive(gameMode != GameMode.kStartScreen);
		Simulator.singleton.gameObject.SetActive(gameMode != GameMode.kStartScreen);
		
		// Ensure that when we do complete a level, it takes a little time before all the messages and buttons appear
		if (gameMode == GameMode.kPlayGame) levelCompleteMsgCountdown = 60;
		
		
		if (gameMode == GameMode.kLevelComplete || gameMode == GameMode.kGameComplete || gameMode == GameMode.kStartScreen){
			levelCompleteMsgCountdown--;
			return;
		}
	
		
		if (levelLoadFade > 0) levelLoadFade -= 0.01f;
		
			
//		// If we are in erase mode, put grid highligher in that mode
//		Grid.singleton.EnableEraseHighlightMode((inputMode == InputMode.kErase));
	
		// Track the mouse pointer highlight
//		Vector3 mousePos = Input.mousePosition;
		
//		// If inside the UI, then not active and nothing else to do
//		// as UI is hangled in OnGUI function
//		if (IsPosInUI(new Vector3(mousePos.x, Screen.height - mousePos.y, 0f))){
//			Grid.singleton.SetSelected(new GridPoint(), new GridPoint());
//			return;
//		}
//		mousePos.z = transform.position.z - Camera.main.transform.position.z;
//		Vector3 worldPos = Camera.main.ScreenToWorldPoint( mousePos);
//		
//		GridPoint newPoint = new GridPoint((int)(worldPos.x + 0.5f), (int)(worldPos.y + 0.5f));
//		
//		// We also get a secondary point (used for selecting a connetion)
//		float distThreshold = 0.15f;
//		GridPoint otherPoint = new GridPoint();
//		float xDiff = newPoint.x - worldPos.x;
//		float yDiff = newPoint.y - worldPos.y;
//		if (Mathf.Abs (xDiff) < distThreshold && Mathf.Abs (yDiff) > distThreshold){
//			if (yDiff > 0){
//				otherPoint = new GridPoint(newPoint.x, newPoint.y - 1);
//			}
//			else{
//				otherPoint = new GridPoint(newPoint.x, newPoint.y + 1);
//			}
//		}
//		else if (Mathf.Abs (xDiff) > distThreshold && Mathf.Abs (yDiff) < distThreshold){
//			if (xDiff > 0){
//				otherPoint = new GridPoint(newPoint.x - 1, newPoint.y );
//			}
//			else{
//				otherPoint = new GridPoint(newPoint.x + 1, newPoint.y);
//			}
//		}
//		
//
//		Grid.singleton.SetSelected(newPoint, otherPoint);
//		
//		switch (inputMode){
//			case InputMode.kResistors:
////				circuit.TrialElement(newPoint, resistorPrefab);
//				break;
//		
//		}
//		
		
		
		/*						
		// If the mouse button is down
		if (Input.GetMouseButton(0) && !Input.GetKey (KeyCode.LeftControl)){
			oldDrawPoint = newDrawPoint;
			newDrawPoint = new GridPoint(newPoint);
			
			// If the point we are drawing to has changed and the new one is valid
			if (!oldDrawPoint.IsEqual(newDrawPoint) && newDrawPoint.IsValid()){
			
				// Play an (indiscriminate) sound effect
				AudioSource source = gameObject.GetComponent<AudioSource>();
				source.Play();
				
				
				// If drawing wires
				if (inputMode == InputMode.kWires && (gameMode == GameMode.kEditMode || GetNumWiresRemaining() > 0)){
					if (oldDrawPoint.IsValid ()){
						circuit.AddWire(oldDrawPoint, newDrawPoint);
					}
					else{
						circuit.AddWire(newDrawPoint);
					}
				}

				// If drawing cells
				if (inputMode == InputMode.kCells ){
					if (gameMode == GameMode.kEditMode || GetNumCellsRemaining() > 0){
						if (oldDrawPoint.IsValid ()){
							circuit.AddCell(oldDrawPoint, newDrawPoint);
						}
						else{
							circuit.AddCell(newDrawPoint);
						}
					}
					else{
						circuit.ClickCell(newDrawPoint);
					}
					
				}
				
				// If drawing resistors
				if (inputMode == InputMode.kResistors && (gameMode == GameMode.kEditMode || GetNumResistorsRemaining() > 0)){
					if (oldDrawPoint.IsValid ()){
						circuit.AddResistor(oldDrawPoint, newDrawPoint);
					}
					else{
						circuit.AddResistor(newDrawPoint);
					}
					
				}
				// If drawing Ameter
				if (inputMode == InputMode.kAmmeters && (gameMode == GameMode.kEditMode || GetNumAmetersRemaining() > 0)){
					if (oldDrawPoint.IsValid ()){
						circuit.AddAmeter(oldDrawPoint, newDrawPoint);
					}
					else{
						circuit.AddAmeter(newDrawPoint);
					}
					
				}
				// If erasing
				if (inputMode == InputMode.kErase){
					if (oldDrawPoint.IsValid ()){
						circuit.Erase(oldDrawPoint, newDrawPoint);
					}
					else{
						// if we want to erase a connection
						if (otherPoint.IsValid ()){
							circuit.EraseConnection(newDrawPoint, otherPoint);
						}	
						else{
							circuit.Erase(newDrawPoint);
						}			
					}
				}
			}
		}
		else{
			// Set to invalid
			newDrawPoint = new GridPoint();
		}
		
		*/

	
	}

	
	
	void LateUpdate(){
		if  (gameMode == GameMode.kPlayGame && numLevelTriggers != 0 && triggersTriggered == numLevelTriggers){
			if (currentLevelIndex != levelsToLoad.Length-1){
				gameMode = GameMode.kLevelComplete;
			}
			else{
				gameMode = GameMode.kGameComplete;
			}
		}
		if (gameMode == GameMode.kLevelComplete && (numLevelTriggers == 0 || triggersTriggered != numLevelTriggers)){
			gameMode = GameMode.kPlayGame;	
		}
		

		// Reset this as it must be reevaualted every frame
		triggersTriggered = 0;
		
	}	
		
	void LoadLevel(int index){
		if (index < levelsToLoad.Length && levelsToLoad[index] != null){
			LevelSerializer.singleton.LoadLevel(levelsToLoad[index].name + ".bytes");
			inputMode = InputMode.kWires;
			levelLoadFade = 2;
			Simulator.singleton.ClearSimulation();
			Circuit.singleton.CalcBounds();
			Camera.main.GetComponent<CamControl>().CentreCamera();
		}
	}

		
	void OnGUI (){
	
		// For big labels accross the centre of the screen
		float labelWidth = 500f;
		float labelHeight = 50f;
		float borderWidth = 0.5f * (Screen.width - labelWidth);
		float borderHeight = 0.2f * (Screen.height - labelHeight);
		
		float buttonWidth = 150f;
		GUIStyle labelStyle = new GUIStyle();
		
		labelStyle.alignment = TextAnchor.UpperCenter;
		labelStyle.fontSize = (int)labelHeight;
		labelStyle.normal.textColor = Color.yellow;

		switch (gameMode)
		{
			case GameMode.kLevelComplete:
			{
				if (levelCompleteMsgCountdown < 0){
					GUI.Label(new Rect(borderWidth, Screen.height - borderHeight - labelHeight, labelWidth, labelHeight), "Level " + (currentLevelIndex+1) + " Complete!", labelStyle);
					
					GUIStyle buttonStyle = new GUIStyle(GUI.skin.GetStyle("button"));
					buttonStyle.fontSize = 20;
					Color useColor = Color.green;
					buttonStyle.normal.textColor = useColor;
				
					if (GUI.Button(new Rect(borderWidth, Screen.height - borderHeight - labelHeight + 100, buttonWidth, labelHeight), "Reload Level " + (currentLevelIndex+1), buttonStyle)){
						LoadLevel(currentLevelIndex);
					}
					if (GUI.Button(new Rect(Screen.width - borderWidth - buttonWidth, Screen.height - borderHeight - labelHeight + 100, buttonWidth, labelHeight), "Restart game", buttonStyle)){
						Application.LoadLevel(Application.loadedLevel);
					}
					useColor.a = buttonPulseAlpha;
					buttonStyle.normal.textColor = useColor;
					if (GUI.Button(new Rect(Screen.width/2 - buttonWidth/2, Screen.height - borderHeight - labelHeight + 100, buttonWidth, labelHeight), "Load next level", buttonStyle)){
						currentLevelIndex++;
						LoadLevel(currentLevelIndex);
					}
				}
			
				break;
			}
			case GameMode.kGameComplete:
			{
				if (levelCompleteMsgCountdown < 0){
					
					GUI.Label(new Rect(borderWidth, Screen.height - borderHeight - labelHeight, labelWidth, labelHeight), "Game Complete!!!", labelStyle);
					
					GUIStyle buttonStyle = new GUIStyle(GUI.skin.GetStyle("button"));
					buttonStyle.fontSize = 20;
					Color useColor = Color.green;
					buttonStyle.normal.textColor = useColor;
					
					if (GUI.Button(new Rect(borderWidth, Screen.height - borderHeight - labelHeight + 100, buttonWidth, labelHeight), "Reload Level " + (currentLevelIndex+1), buttonStyle)){
						LoadLevel(currentLevelIndex);
					}
					
					useColor.a = buttonPulseAlpha;
					buttonStyle.normal.textColor = useColor;
					if (GUI.Button(new Rect(Screen.width - borderWidth - buttonWidth, Screen.height - borderHeight - labelHeight + 100, buttonWidth, labelHeight), "Restart game", buttonStyle)){
						Application.LoadLevel(Application.loadedLevel);
					}					
				}
				break;
			}	
			case GameMode.kStartScreen:
			{
				GUIStyle buttonStyle = new GUIStyle(GUI.skin.GetStyle("button"));
				buttonStyle.fontSize = 34;
				Color useColor = Color.green;
				useColor.a = buttonPulseAlpha;
				
				buttonStyle.normal.textColor = useColor;
				
				
				//GUI.color = Color.green;
				
				float thisButtonWidth = 300;
				
				if (GUI.Button(new Rect(Screen.width/2 - thisButtonWidth/2, Screen.height/3 - labelHeight + 70, thisButtonWidth, labelHeight), "Start Game", buttonStyle)){
					if (loadLevelOnStartup && !startupLevelLoaded){
						if (levelsToLoad[currentLevelIndex] != null){
							LoadLevel(currentLevelIndex);
						}
						startupLevelLoaded = true;
						
					}
					gameMode = GameMode.kPlayGame;
				
				}
				
				break;
			}	
			case GameMode.kPlayGame:
			{
				// if we have just started a level
				if (levelLoadFade > 0){
					
					labelStyle.alignment = TextAnchor.UpperCenter;
					labelStyle.fontSize = (int)labelHeight;
					Color useCol = Color.green;
					useCol.a = levelLoadFade;
					labelStyle.normal.textColor = useCol;
					
					if (currentLevelIndex != levelsToLoad.Length - 1){
						GUI.Label(new Rect(borderWidth, Screen.height - borderHeight - labelHeight, labelWidth, labelHeight), "Level " + (currentLevelIndex+1) + ": " + levelsToLoad[currentLevelIndex].name, labelStyle);
					}
					else{
						GUI.Label(new Rect(borderWidth, Screen.height - borderHeight - labelHeight, labelWidth, labelHeight), "Final Level: " + levelsToLoad[currentLevelIndex].name, labelStyle);
					}
				}
				break;
			}
		}
		
		if (gameMode == GameMode.kEditMode || gameMode == GameMode.kPlayGame)
		{
		
			// If not in editor mode we only want to show a subset of the buttons
			int numButtons = gameMode == GameMode.kEditMode ? (int)InputMode.kNumButtons : numNonEditButtons;
			string[] useStrings = new string[numButtons];
			Array.Copy(toolbarStrings, useStrings, numButtons);
			
			// If not in edit mode, append the number of elements let to use
			if (gameMode == GameMode.kPlayGame){
				useStrings[(int)InputMode.kWires] += 	 " (" + ElementFactory.singleton.GetStockRemaining("Wires")  +")";
				useStrings[(int)InputMode.kCells] += 	 " (" + ElementFactory.singleton.GetStockRemaining("Cells") +")";
				useStrings[(int)InputMode.kResistors] += " (" + ElementFactory.singleton.GetStockRemaining("Resistors")  +")";
				useStrings[(int)InputMode.kAmmeters] +=  " (" + ElementFactory.singleton.GetStockRemaining("Ammeters")  +")";
			}
			
			InputMode oldInputMode = inputMode;
		//	inputMode = (InputMode)GUI.Toolbar (toolbarRect, (int)inputMode, useStrings);
			if (inputMode == InputMode.kClearAll){
				Application.LoadLevel(Application.loadedLevel);
				inputMode = oldInputMode;
			}
			else if (inputMode == InputMode.kLoadLevel){
	
				LoadLevel(currentLevelIndex);
				inputMode = InputMode.kWires;
			}
			else if (inputMode == InputMode.kSaveLevel){
				LevelSerializer.singleton.SaveLevel(levelToSave + ".bytes");
				inputMode = oldInputMode;
			}
			else if (inputMode == InputMode.kBakeConnect){
				Circuit.singleton.BakeConnect();
				inputMode = oldInputMode;
			}	
			else if (inputMode == InputMode.kBakeAll){
				Circuit.singleton.BakeAll();
				inputMode = oldInputMode;
			}						
			else if (inputMode == InputMode.kUnbake){
				Circuit.singleton.Unbake();
				inputMode = oldInputMode;
			}				
			else if (inputMode == InputMode.kToggleEdit){
				enableEditor = !enableEditor;
				gameMode = enableEditor ? GameMode.kEditMode : GameMode.kPlayGame;
				inputMode = oldInputMode;
			}	
		}	
		


		
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
}
