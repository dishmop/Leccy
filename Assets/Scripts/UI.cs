using UnityEngine;
using System;
using System.Collections;
using System.IO;


public class UI : MonoBehaviour {

	public static UI singleton;
	
	public AudioSource placeElementSound;
	public AudioSource removeElementSound;
	public AudioSource buttonClickSound;
	public AudioSource failSound;
	public GameObject  elementSelectPanel;
	
	
	public bool	honourAnchors = false;

	string 		selectedPrefabId;
	GameObject	ghostElement;
	GridPoint	thisPoint;
	GridPoint	otherPoint;
	Vector3		worldPos;
	bool		buttonIsHeld;
	GridPoint	lastPoint;
	GridPoint	lastOtherPoint;
	
	// For transfering mouse events between UI and fixed update
	bool cacheMouseHeld = false;
	bool cacheMousePressed = false;
	bool hideMouse = false;
	
	const int ghostGridPoitDepth = -3;

	const int		kLoadSaveVersion = 1;	
	
	
	bool		isInUI;
	
	// Temporary debug thing
//	GUITextDisplay	guiTextDisplay;
	
	
	public void SetSelectedPrefabId(string id){
		selectedPrefabId = id; 
		GameObject.Destroy (ghostElement);
		
		// We can't get the factory to instantate it otherwise it will reduce the number of the we have left
		GameObject prefab = ElementFactory.singleton.GetPrefab(selectedPrefabId);
		ghostElement = Instantiate(prefab) as GameObject;
		ghostElement.transform.parent = transform;
		ghostElement.GetComponent<CircuitElement>().SetAlpha(0.5f);
		ghostElement.SetActive(true);	
		ghostElement.GetComponent<CircuitElement>().SetGridPoint(new GridPoint(0, 0));
		ghostElement.GetComponent<CircuitElement>().RebuildMesh();
	}
	
	public void OnEnterUI(){
		Debug.Log("OnEnterUI()");
		isInUI = true;
	}
	
	public void OnExitUI(){
		Debug.Log("OnExitUI()");
		isInUI = false;
	}
	
	public void HideMousePointer(){
		hideMouse = true;
	}
	
	public void PlayButtonClickSound(){
		buttonClickSound.Play();
	}
	
		
	// GameUpdate is called once per frame in a specific order
	public void LateGameUpdate () {
		// A bit wired that this just cals a function on Circuit - but we need to pass in whether 
		// anchors are being honored
		Circuit.singleton.TidyUpConnectionBehaviours(honourAnchors);
		hideMouse = false;
		
		
		// Not to mention the fact that these changes will now not get implemented until next go
	}	


	// Use this for initialization
	void Awake () {
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
		
	}
	
	
	void Start(){
//			guiTextDisplay = new GUITextDisplay(400f, 200f, 500f, 20f);
	}
	
	void OnDestroy () {
		singleton = null;
	
	}
	
	
	void CalcCurrentGridPoints(){
		// Get mouse pointer position in world and circuite space
		Vector3 mousePos = Input.mousePosition;
		mousePos.z = transform.position.z - Camera.main.transform.position.z;
		worldPos = Camera.main.ScreenToWorldPoint( mousePos);
		if (!Telemetry.singleton.enableTelemetry || Telemetry.singleton.mode == Telemetry.Mode.kRecord){
			thisPoint = new GridPoint((int)(worldPos.x + 0.5f), (int)(worldPos.y + 0.5f));
			if (!Grid.singleton.IsPointInGrid(thisPoint) || isInUI){
				thisPoint = null;
			}
		}
		else{
			thisPoint = ghostElement.GetComponent<CircuitElement>().GetGridPoint();
		}
		if (thisPoint == null) return;		
		
		// Test to see if we are nearly eleecting another point (i.e. we asre selecting a connection). 
		float distThreshold = 0.15f;
		
		otherPoint = null;
		float xDiff = thisPoint.x - worldPos.x;
		float yDiff = thisPoint.y - worldPos.y;
		if (Mathf.Abs (xDiff) < distThreshold && Mathf.Abs (yDiff) > distThreshold){
			if (yDiff > 0){
				otherPoint = new GridPoint(thisPoint.x, thisPoint.y - 1);
			}
			else{
				otherPoint = new GridPoint(thisPoint.x, thisPoint.y + 1);
			}
		}
		else if (Mathf.Abs (xDiff) > distThreshold && Mathf.Abs (yDiff) < distThreshold){
			if (xDiff > 0){
				otherPoint = new GridPoint(thisPoint.x - 1, thisPoint.y );
			}
			else{
				otherPoint = new GridPoint(thisPoint.x + 1, thisPoint.y);
			}
		}
				

		if (otherPoint == null ||!Grid.singleton.IsPointInGrid(otherPoint) || isInUI){
			otherPoint = null;
		}		
	}
	
	// Used to cache mouse results
	void Update(){
		if ((isInUI || hideMouse) && (!Telemetry.singleton.enableTelemetry || Telemetry.singleton.mode == Telemetry.Mode.kRecord)){
			ghostElement.SetActive(false);
		}
		else{
			ghostElement.SetActive(true);
		}
		// if there is no gohst element then we should not register mous einputs
		if (hideMouse){
			cacheMouseHeld = false;
			cacheMousePressed = false;
			
		}
		else{
			if (!Telemetry.singleton.enableTelemetry || Telemetry.singleton.mode == Telemetry.Mode.kRecord){
				cacheMouseHeld = (Input.GetMouseButton(0) && !Input.GetKey (KeyCode.LeftControl));
				if ((Input.GetMouseButtonDown(0) && !Input.GetKey (KeyCode.LeftControl))){
					cacheMousePressed = true;
				}
			}
		}	
	}

	public void OnLoadLevel(){
		elementSelectPanel.GetComponent<ElementSelectPanel>().OnLoadLevel();
	
	}
	
	public void GameUpdate(){
	
		if (ghostElement == null){
			Debug.Log("Ghost is null!?! - NOt initialised yet?");
			return;
		}

		CalcCurrentGridPoints();
		
		
		
		// Deal with the ghost element
		ghostElement.SetActive(thisPoint != null);
		CircuitElement ghostElementComp = ghostElement.GetComponent<CircuitElement>();
		if (!Telemetry.singleton.enableTelemetry || Telemetry.singleton.mode == Telemetry.Mode.kRecord) ghostElementComp.SetGridPoint(thisPoint, ghostGridPoitDepth);
		ghostElementComp.RebuildMesh();
		ghostElementComp.SetOtherGridPoint(otherPoint);		
		
		
		
		// Placed elements are dealt with differntly than drawn ones
		switch (ElementFactory.singleton.GetPrefab(selectedPrefabId).GetComponent<CircuitElement>().uiType){
			case CircuitElement.UIType.kPlace:
				HandlePlacedElementInput();
				break;
			case CircuitElement.UIType.kDraw:
				HandleDrawnElementInput();
				break;
			case CircuitElement.UIType.kModify:
				HandleModifyElementInput();
				break;
		}

		lastPoint = thisPoint;
		lastOtherPoint = otherPoint;
		
		cacheMousePressed = false;
		
		
		// As far as the telemetry goes, ust assume tha the ghost element is changing every frame
		Telemetry.singleton.RegisterEvent(Telemetry.Event.kGhostChange);
		
	}
	
	public void SerializeGhostElement(Stream stream){
		BinaryWriter bw = new BinaryWriter(stream);
		bw.Write(kLoadSaveVersion);
		
		// Write out whether we have a valid ghost element
		bool haveGhost = (ghostElement != null && ghostElement.GetComponent<CircuitElement>().GetGridPoint() != null && ghostElement.activeSelf);
		bw.Write (haveGhost);
		if (haveGhost){
			CircuitElement element = ghostElement.GetComponent<CircuitElement>();
			GridPoint point = element.GetGridPoint();
			bw.Write(point.x);
			bw.Write(point.y);
			bw.Write (ghostElement.GetComponent<SerializationID>().id);
			element.Save (bw);
		}
		
	
	}

	public void DeserializeGhostElement(Stream stream){
		BinaryReader br = new BinaryReader(stream);
		int version = br.ReadInt32 ();
		
		switch(version){
		 	case kLoadSaveVersion:{
				// Write out whether we have a valid ghost element
				bool haveGhost = br.ReadBoolean();
				if (haveGhost){
					GridPoint point = new GridPoint();
					point.x = br.ReadInt32();
					point.y = br.ReadInt32 ();
					string id = br.ReadString ();
					
					// If our current one is inactive in any way, just set a new one
					if (ghostElement == null || !ghostElement.activeSelf || ghostElement.GetComponent<SerializationID>().id != id || ghostElement.GetComponent<CircuitElement>().GetGridPoint() == null){
						SetSelectedPrefabId(id);
					}
					// If our gridPoint is not correct, then set that too
					CircuitElement element = ghostElement.GetComponent<CircuitElement>();
					GridPoint currentGrid = element.GetGridPoint();
					if (currentGrid == null || !currentGrid.IsEqual(point)){
						ghostElement.GetComponent<CircuitElement>().SetGridPoint(point, ghostGridPoitDepth);
						
					}
					element.Load (br);
					ghostElement.SetActive(true);
				}
				else{
					if (ghostElement){
						ghostElement.SetActive(false);
					} 
					
					
				}
			}
			break;
		}
		
	}
	

		
	CircuitElement.ConnectionBehaviour[] SaveOldConnections(CircuitElement existingElement){
		CircuitElement.ConnectionBehaviour[] oldConnections = new CircuitElement.ConnectionBehaviour[4];
		Array.Copy(existingElement.connectionBehaviour, oldConnections, 4);
		return oldConnections;
	}
	
	void AttemptToReestablishConnections(CircuitElement thisElement, CircuitElement.ConnectionBehaviour[] oldConnections){
		// If there were connections before, try and set them up again
		if (oldConnections != null){
			for (int i = 0; i < 4; ++i){
				// We do this regardless of the anchor situation (seeing as we are just trying to restor what was there a second ago)
				thisElement.SuggestBehaviour(i, oldConnections[i], false);
			}
		}
	}

	void HandleDrawnElementInput(){
	
		// Check if it is held down
		buttonIsHeld = cacheMouseHeld;
		
		// If not on the grid, then nothing to do
		if (thisPoint == null) return;

		// Check if (in addition) we have only just pressed it down
		bool buttonIsClicked = cacheMousePressed;
		
		
		// If we don't have any elements left to place, then we are in an error state
		bool error = (ElementFactory.singleton.GetStockRemaining(selectedPrefabId) == 0);	
		
		// In this case there is very little else to do
		ghostElement.GetComponent<CircuitElement>().SetErrorState(error);
		if (error){
			if (buttonIsClicked) failSound.Play ();
			return;
		}	
		
		// We are also in an error state if we are over a component that is anchored
		if (honourAnchors){
			if (Circuit.singleton.GetAnchors(thisPoint).isAnchored[Circuit.kCentre]){
				error = true;
			}
		}
			
		ghostElement.GetComponent<CircuitElement>().SetErrorState(error);
		
		// Howeer, in this case, we only leave if we are clicked here (if we are dragging here then we stil have stuff to do)
		if (error && buttonIsClicked){
			failSound.Play ();
			return;
		}

		
		// If the buttons is not down, there is nothing to do
		if (!buttonIsHeld){
			return;
		}
		
		
		// Get the path
		GridPoint[] gridPath = null;
		 if (buttonIsClicked){
			gridPath = new GridPoint[1];
			gridPath[0] = thisPoint;
		 }
		else if (lastPoint != null && !thisPoint.IsEqual(lastPoint)){
			gridPath = CalcGridPath(lastPoint, thisPoint);
		 }
		

		if (gridPath != null)
		{
			for (int i = 0; i < gridPath.GetLength(0); ++i){
				GameObject existingElement = Circuit.singleton.GetGameObject(gridPath[i]);
				
				CircuitElement.ConnectionBehaviour[] oldConnections = null;
				
				// If there is one there already
				if (existingElement != null){
					oldConnections = SaveOldConnections(existingElement.GetComponent<CircuitElement>());
					
					// If this is not the same kind of element - then replace it
					if (existingElement.GetComponent<SerializationID>().id != selectedPrefabId){
						// But only if it is not anchored
						if (!honourAnchors || !Circuit.singleton.GetAnchors(gridPath[i]).isAnchored[Circuit.kCentre]){
							RemoveElement(existingElement);
							GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
							PlaceElement(newElement, gridPath[i]);
						}
					}
				}
				else{
					// Check for anchors
					if (!honourAnchors || !Circuit.singleton.GetAnchors(gridPath[i]).isAnchored[Circuit.kCentre]){
						GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
						PlaceElement(newElement, gridPath[i]);
					}
				}
				CircuitElement thisElement = Circuit.singleton.GetElement(gridPath[i]);
				AttemptToReestablishConnections(thisElement, oldConnections);
				
				// If we have a previous one, make a connection
				if (i > 0){
					CircuitElement lastElement =  Circuit.singleton.GetElement(gridPath[i-1]);
					int lastDir = Circuit.CalcNeighbourDir(gridPath[i-1], gridPath[i]);
					int thisDir = Circuit.CalcNeighbourDir(gridPath[i], gridPath[i-1]);
					
					// But only if anchors allow
					// Also note that the elemtents may not be there (if we could not draw them due to machors (Really?)
					
					if (!honourAnchors || !Circuit.singleton.GetAnchors(gridPath[i-1]).isAnchored[lastDir]){
						if (lastElement != null && thisElement != null){
							lastElement.SuggestBehaviour(thisElement, CircuitElement.ConnectionBehaviour.kSociable, honourAnchors);
						}
					    
					}
					if (!honourAnchors || !Circuit.singleton.GetAnchors(gridPath[i]).isAnchored[thisDir]){
						if (thisElement != null && lastElement != null) thisElement.SuggestBehaviour(lastElement, CircuitElement.ConnectionBehaviour.kSociable, honourAnchors);
					}
					placeElementSound.Play ();

				}
			}
			
		}
		

	}
	
		
	void HandlePlacedElementInput(){
		// If not on the grid, then nothing to do
		if (thisPoint == null) return;
		
		GameObject existingElement = Circuit.singleton.GetGameObject(thisPoint);
		CircuitElement existingCirEl = (existingElement != null) ? existingElement.GetComponent<CircuitElement>() : null;
		GameObject prefab = ElementFactory.singleton.GetPrefab(selectedPrefabId);		
		
		
		// If we don't have any elements left to place, then perhaps we should be in an error state?
		// if we are over an element like this, then we can "place" this one as it will replace
		// (or change) the one that is already there
		bool error = false;
		if (ElementFactory.singleton.GetStockRemaining(selectedPrefabId) == 0){
			if (existingElement == null || (existingElement.GetComponent<SerializationID>().id != selectedPrefabId)){
				error = true;
			}
		}
		if (honourAnchors){
			// It is also an error if we are trying to place an element when the node is anchored
			Circuit.AnchorData anchorData = Circuit.singleton.GetAnchors(thisPoint);
			if (anchorData.isAnchored[Circuit.kCentre]){
				error = true;
			}
			// It is also an error if trying to place an element that is not amenable to setting up connections in the same
			// way as the previous one
			CircuitElement.ConnectionBehaviour[] connectionBehaviour = new CircuitElement.ConnectionBehaviour[4];			
			if (existingCirEl != null){
				connectionBehaviour = existingCirEl.connectionBehaviour;
			}
			for (int i = 0; i < 4; ++i){
				if (anchorData.isAnchored[i]){
					if (!ghostElement.GetComponent<CircuitElement>().IsAmenableToBehaviour(i, connectionBehaviour[i], honourAnchors)) error = true;
					
				}
				
			}
			// IF we would be clicking the element, but are unable to, then error
			if (existingCirEl != null && existingCirEl.ShouldClick(prefab) && !existingCirEl.CanClick()){
				error = true;
			}			
			
		}
		


		ghostElement.GetComponent<CircuitElement>().SetErrorState(error);
		
		
		if (cacheMousePressed){
			// If in an error state, do nothing other than thud if we press the mouse button
			if (error){
				failSound.Play ();
				return;
			}
			
			// If there is one there already
			if (existingElement){
				CircuitElement.ConnectionBehaviour[] oldConnections = null;

				// Check if we should simply OnClick() the element which is there
				if (existingCirEl.ShouldClick(prefab)){
					existingCirEl.OnClick();
					
					// Change the prefab too
					prefab.GetComponent<CircuitElement>().OnClick();
					PrefabManager.OnChangePrefab(prefab);
					SetSelectedPrefabId(selectedPrefabId);
					placeElementSound.Play();
					
				}
				// Othewise, remove it and add a new one
				else{
					oldConnections = SaveOldConnections(existingElement.GetComponent<CircuitElement>());
					RemoveElement(existingElement);
					GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
					PlaceElement(newElement, thisPoint);
					AttemptToReestablishConnections(newElement.GetComponent<CircuitElement>(), oldConnections);					
				}
			}
			else{
				GameObject newElement = ElementFactory.singleton.InstantiateElement(selectedPrefabId);
				PlaceElement(newElement, thisPoint);
			}
			
		}
	}
	
	public void PlaceElement(GameObject newElement, GridPoint thisPoint){
		Circuit.singleton.PlaceElement(newElement, thisPoint);
		placeElementSound.Play ();
		if (!GameModeManager.singleton.enableEditor) ElementFactory.singleton.DecrementStock(newElement);
	
	}
	
	public void RemoveElement(GameObject existingElement){
		if (!GameModeManager.singleton.enableEditor) ElementFactory.singleton.IncrementStock(existingElement);
		GridPoint point = existingElement.GetComponent<CircuitElement>().GetGridPoint();
		existingElement.GetComponent<CircuitElement>().RemoveConnections(honourAnchors);
		Destroy (Circuit.singleton.GetGameObject(point));
		Circuit.singleton.RemoveElement(point);
	}
	
	void HandleModifyElementInput(){
	
		CircuitElement ghostCircEl = ghostElement.GetComponent<CircuitElement>();
		bool error = !ghostCircEl.CanModify(thisPoint, otherPoint, honourAnchors);
		ghostCircEl.SetErrorState(error);
		
		buttonIsHeld = cacheMouseHeld;
		
		// If the buttons is not down, there is nothing more to do
		if (thisPoint == null || !buttonIsHeld){
			return;
		}
	
		
		// Check if we have only just pressed it down
		bool buttonIsClicked = cacheMousePressed;
		
		
		GridPoint[] gridPath = null;
		bool ignoreFirst = false;
		bool ignoreLast = false;
		
		// If clicking a connection - then simple request that the connection be removed
		if (buttonIsClicked && otherPoint != null){
			bool ok = ghostCircEl.Modify(thisPoint, otherPoint, honourAnchors);
			if (ok)
				removeElementSound.Play ();
			else
				failSound.Play ();
			
		}
		// Otherwise, remove the component itself
		else if (buttonIsClicked){
			gridPath = new GridPoint[1];
			gridPath[0] = thisPoint;
		}
		// If we weren't on a connection, but are now, and have not moved base point then modify the connection only
		else if(lastOtherPoint == null && otherPoint != null && lastPoint != null && lastPoint.IsEqual(thisPoint)){
			bool ok = ghostCircEl.Modify(thisPoint, otherPoint, honourAnchors);
			if (ok)
				removeElementSound.Play ();
			else
				failSound.Play ();
		}
		// If we were on a connection, but are not now, then modify the connection and the node
		else if(lastOtherPoint != null && otherPoint == null && lastPoint != null && lastPoint.IsEqual(thisPoint)){
			bool ok1 = ghostCircEl.Modify(thisPoint, lastOtherPoint, honourAnchors);
			bool ok2 = ghostCircEl.Modify(thisPoint, honourAnchors);
			if (ok1 || ok2)
				removeElementSound.Play ();
			else
				failSound.Play ();
		}		// If dragging from one point to the next
		else if (lastPoint != null && !thisPoint.IsEqual(lastPoint)){
			gridPath = CalcGridPath(lastPoint, thisPoint);
			
			// If our last thing was to modify a connection, then check if the first two
			// elements of this list are that connection - if they are then don;t modify the first one node
			if (lastOtherPoint != null && lastOtherPoint.IsEqual(gridPath[1]) && lastPoint.IsEqual(gridPath[0])){
				ignoreFirst = true;
				
			}
			// If this thing we do is modify a connection, and the last link in the list is this connection
			// then don't modify the last node itself
			int pathLength = gridPath.GetLength(0);
			if (otherPoint != null && otherPoint.IsEqual (gridPath[pathLength -2]) && thisPoint.IsEqual(gridPath[pathLength-1])){
				ignoreLast = true;
			}
			
		}
		// If we were modifying a connection and how now moved to the point itself, then mdify the point
		else if (lastOtherPoint != null && otherPoint == null && thisPoint.IsEqual(lastPoint)){
			gridPath = new GridPoint[1];
			gridPath[0] = thisPoint;
		}
		
		
		if (gridPath != null)
		{
			int pathLength = gridPath.GetLength(0);
			for (int i = 0; i < pathLength; ++i){
				bool ignoreThis = (i == 0 && ignoreFirst) || (i == pathLength-1 && ignoreLast);
				if (!ignoreThis){
					ghostCircEl.Modify (gridPath[i], honourAnchors);
				}
				// Show do the connections in the path too 
				if (i < pathLength-1){
					ghostCircEl.Modify (gridPath[i], gridPath[i+1], honourAnchors);
					ghostCircEl.Modify (gridPath[i+1], gridPath[i], honourAnchors);
				}

			}
			removeElementSound.Play ();
			
		}
		
		
	}	
	
	GridPoint[] CalcGridPath(GridPoint prevPoint, GridPoint nextPoint){
		
		if (prevPoint.IsEqual(nextPoint)){
			Debug.LogError("Trying to draw from and to the same point!");
			return null;
		}
		if (prevPoint == null || nextPoint == null){
			Debug.LogError("Trying to draw from or to a null point!");
			return null;
		}		
		
		int xDiff = nextPoint.x - prevPoint.x;
		int yDiff = nextPoint.y - prevPoint.y;
		
		// Work out how many elements the array should have
		int size = Mathf.Abs(xDiff) + Mathf.Abs(yDiff) + 1;
		GridPoint[] result = new GridPoint[size];
		
		// If we are not moving in x at all...
		int i = 0;
		if (Mathf.Abs(xDiff) > Mathf.Abs(yDiff)){
			int xInc = xDiff / Mathf.Abs (xDiff);
			int lastY = prevPoint.y;
			float grad = (float)yDiff/(float)xDiff;
			for (int x = prevPoint.x; x != nextPoint.x + xInc; x += xInc){
				int thisY = prevPoint.y + Mathf.RoundToInt((x-prevPoint.x) * grad);
				if (thisY != lastY){
					result[i++] = new GridPoint(x, lastY);
					lastY = thisY;
				}
				result[i++] = new GridPoint(x, thisY);	
			}
			
		}
		else{
			int yInc = yDiff / Mathf.Abs (yDiff);
			int lastX = prevPoint.x;
			float grad = (float)xDiff/(float)yDiff;
			for (int y = prevPoint.y; y != nextPoint.y + yInc; y += yInc){
				int thisX = prevPoint.x + Mathf.RoundToInt((y-prevPoint.y) * grad);
				if (thisX != lastX){
					result[i++] = new GridPoint(lastX, y);
					lastX = thisX;
				}					
				result[i++] = new GridPoint(thisX, y);
			}
		}
		
		return result;
	}	
	
	
	// Temprary debug stuff
	void OnGUI(){
//		guiTextDisplay.GUIResetTextLayout();
//		if (thisPoint != null){
//			guiTextDisplay.GUIPrintText( "Selected Grid Position: " + thisPoint.x + ", " + thisPoint.y, Color.yellow);
//			float selVolt = 0f;
//			float selAmp = 0f;
//			CircuitElement thisElement = Circuit.singleton.GetElement(thisPoint);
//			if (thisElement){
//				selVolt = thisElement.GetMaxVoltage();
//				selAmp = thisElement.GetMaxCurrent();
//					
//			}
//			guiTextDisplay.GUIPrintText( "Selection max stats: " + selVolt.ToString("0.000") + "V, " + selAmp.ToString("0.000") + "A", Color.yellow);
//		}
	}
}
