using UnityEngine;
using System.Collections;
using System;

public class UI : MonoBehaviour {

	public static UI singleton;

	public GameObject 	gridGO;
	public GameObject	circuitGO;
	public GameObject 	levelSerialiserGO;
	public GridPoint	newDrawPoint = new GridPoint();
	public GridPoint	oldDrawPoint = new GridPoint();
	public string 		levelToSave = "DefaultLevel";
	public TextAsset	levelToLoad;
	public bool			enableEditMode = true;
	public bool			loadLevelOnStartup = false;
	public TextAsset 	nextLevel;
	
	
	bool 				startupLevelLoaded = false;
	int 				triggersTriggered = 0;
	int					numLevelTriggers = 0;
	bool				levelComplete = false;
	
	public Rect			toolbarRect = new Rect(25, 25, 1000, 30);


	Grid				grid;
	Circuit				circuit;
	LevelSerializer		levelSerializer;
	
	public enum InputMode{
		kWires,
		kCells,
		kResistors,
		kAmeters,
		kErase,
		kToggleEdit,
		kLoadLevel,
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
	string[] toolbarStrings = {"Wires", "Cells", "Resistors", "Ameter", "Eraser", "Toggle edit", "Load Level", "Save Level", "Clear all", "Bake connect", "Bake All", "Unbake"};

		
	// Use this for initialization
	void Start () {
		singleton = this;
		grid = gridGO.GetComponent<Grid>();	
		circuit = circuitGO.GetComponent<Circuit>();	
		levelSerializer = levelSerialiserGO.GetComponent<LevelSerializer>();
		

	}
	
	bool IsPosInUI(Vector3 pos){
		return toolbarRect.Contains (pos);
	}
	
	// Update is called once per frame
	void Update () {
	
		if (loadLevelOnStartup && !startupLevelLoaded){
			levelSerializer.LoadLevel(levelToLoad.name + ".bytes");
			startupLevelLoaded = true;
		}
			
		// If we are in erase mode, put grid highligher in that mode
		grid.EnableEraseHighlightMode((inputMode == InputMode.kErase));
	
		// Track the mouse pointer highlight
		Vector3 mousePos = Input.mousePosition;
		
		// If inside the UI, then not active and nothing else to do
		// as UI is hangled in OnGUI function
		if (IsPosInUI(new Vector3(mousePos.x, Screen.height - mousePos.y, 0f))){
			grid.SetSelected(new GridPoint(), new GridPoint());
			return;
		}
		mousePos.z = transform.position.z - Camera.main.transform.position.z;
		Vector3 worldPos = Camera.main.ScreenToWorldPoint( mousePos);
		
		GridPoint newPoint = new GridPoint((int)(worldPos.x + 0.5f), (int)(worldPos.y + 0.5f));
		
		// We also get a secondary point (used for selecting a connetion)
		float distThreshold = 0.15f;
		GridPoint otherPoint = new GridPoint();
		float xDiff = newPoint.x - worldPos.x;
		float yDiff = newPoint.y - worldPos.y;
		if (Mathf.Abs (xDiff) < distThreshold && Mathf.Abs (yDiff) > distThreshold){
			if (yDiff > 0){
				otherPoint = new GridPoint(newPoint.x, newPoint.y - 1);
			}
			else{
				otherPoint = new GridPoint(newPoint.x, newPoint.y + 1);
			}
		}
		else if (Mathf.Abs (xDiff) > distThreshold && Mathf.Abs (yDiff) < distThreshold){
			if (xDiff > 0){
				otherPoint = new GridPoint(newPoint.x - 1, newPoint.y );
			}
			else{
				otherPoint = new GridPoint(newPoint.x + 1, newPoint.y);
			}
		}
		
		
		grid.SetSelected(newPoint, otherPoint);
		
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
				if (inputMode == InputMode.kWires && (GameSettings.singleton.enableEdit || GetNumWiresRemaining() > 0)){
					if (oldDrawPoint.IsValid ()){
						circuit.AddWire(oldDrawPoint, newDrawPoint);
					}
					else{
						circuit.AddWire(newDrawPoint);
					}
				}

				// If drawing cells
				if (inputMode == InputMode.kCells && (GameSettings.singleton.enableEdit || GetNumCellsRemaining() > 0)){
					if (oldDrawPoint.IsValid ()){
						circuit.AddCell(oldDrawPoint, newDrawPoint);
					}
					else{
						circuit.AddCell(newDrawPoint);
					}
					
				}				
				// If drawing resistors
				if (inputMode == InputMode.kResistors && (GameSettings.singleton.enableEdit || GetNumResistorsRemaining() > 0)){
					if (oldDrawPoint.IsValid ()){
						circuit.AddResistor(oldDrawPoint, newDrawPoint);
					}
					else{
						circuit.AddResistor(newDrawPoint);
					}
					
				}
				// If drawing Ameter
				if (inputMode == InputMode.kAmeters && (GameSettings.singleton.enableEdit || GetNumAmetersRemaining() > 0)){
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
		

	
	}

	
	
	void LateUpdate(){
		levelComplete =  (numLevelTriggers != 0 && triggersTriggered == numLevelTriggers);
		// Reset this as it must be reevaualted every frame
		triggersTriggered = 0;
		
	}	
	
	int GetNumWiresRemaining(){
		return (LevelSettings.singleton.numWires + LevelSettings.singleton.numWiresOnStartup - circuit.numElementsUsed["Wire"]);
	}
	
	int GetNumCellsRemaining(){
		return (LevelSettings.singleton.numCells + LevelSettings.singleton.numCellsOnStartup - circuit.numElementsUsed["Cell"]) ;
	}

	int GetNumResistorsRemaining(){
		return (LevelSettings.singleton.numResistors + LevelSettings.singleton.numResistorsOnStartup - circuit.numElementsUsed["Resistor"]);
	}

	int GetNumAmetersRemaining(){
		return (LevelSettings.singleton.numAmeters + LevelSettings.singleton.numAmetersOnStartup - circuit.numElementsUsed["Ameter"]);
	}

		
	void OnGUI () {
		// If not in editor mode we only want to show a subset of the buttons
		int numButtons = GameSettings.singleton.enableEdit ? (int)InputMode.kNumButtons : numNonEditButtons;
		string[] useStrings = new string[numButtons];
		Array.Copy(toolbarStrings, useStrings, numButtons);
		
		// If not in edit mode, append the number of elements let to use
		if (!GameSettings.singleton.enableEdit){
			useStrings[(int)InputMode.kWires] += 	 " (" + GetNumWiresRemaining()  +")";
			useStrings[(int)InputMode.kCells] += 	 " (" + GetNumCellsRemaining() +")";
			useStrings[(int)InputMode.kResistors] += " (" + GetNumResistorsRemaining()  +")";
			useStrings[(int)InputMode.kAmeters] += 	 " (" + GetNumAmetersRemaining()  +")";
		}
		
		InputMode oldInputMode = inputMode;
		inputMode = (InputMode)GUI.Toolbar (toolbarRect, (int)inputMode, useStrings);
		if (inputMode == InputMode.kClearAll){
			Application.LoadLevel(Application.loadedLevel);
			inputMode = oldInputMode;
		}
		else if (inputMode == InputMode.kLoadLevel){

			levelSerializer.LoadLevel(levelToLoad.name + ".bytes");
			inputMode = oldInputMode;
		}
		else if (inputMode == InputMode.kSaveLevel){
			levelSerializer.SaveLevel(levelToSave + ".bytes");
			inputMode = oldInputMode;
		}
		else if (inputMode == InputMode.kBakeConnect){
			circuit.BakeConnect();
			inputMode = oldInputMode;
		}	
		else if (inputMode == InputMode.kBakeAll){
			circuit.BakeAll();
			inputMode = oldInputMode;
		}						
		else if (inputMode == InputMode.kUnbake){
			circuit.Unbake();
			inputMode = oldInputMode;
		}				
		else if (inputMode == InputMode.kToggleEdit){
			GameSettings.singleton.enableEdit = !GameSettings.singleton.enableEdit;
			inputMode = oldInputMode;
		}		
		
		if (levelComplete ){
			float labelWidth = 400f;
			float labelHeight = 50f;
			float borderWidth = 0.5f * (Screen.width - labelWidth);
			float borderHeight = 100f;
			
			GUIStyle labelStyle = new GUIStyle();
			
			labelStyle.alignment = TextAnchor.UpperCenter;
			labelStyle.fontSize = (int)labelHeight;
			labelStyle.normal.textColor = Color.yellow;
					
			GUI.Label(new Rect(borderWidth, Screen.height - borderHeight - labelHeight, labelWidth, labelHeight), "Level Complete!", labelStyle);
//			if (GUI.Button(new Rect
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
