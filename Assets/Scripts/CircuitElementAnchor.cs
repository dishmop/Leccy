using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementAnchor : CircuitElement {
	
	// The ones to use for the UI button
	public GameObject	emptyGO;
	public Color		buildColor;
	public Color 		eraseColor;
	
	GridPoint			otherPoint;
	GridPoint			lastOtherPoint;
	Circuit.AnchorData	anchorData;
	GameObject			anchorCentralPrefabUI;
	GameObject			anchorBranchPrefabUI;
	
	// Need this so it gets copied appropriatly
	[SerializeField]
	// Whether we are plaving anchors or removing them
	bool				erase = false;

		
	
	public void Start(){
		Debug.Log ("CircuitElementAnchor:Start()");
	}
	
	public void Awake(){
		anchorCentralPrefabUI = anchorCentralPrefab;
		anchorBranchPrefabUI = anchorBranchPrefab;
		anchorData = new Circuit.AnchorData();
		
		RebuildMesh();
		

	}
	
	
	public override string GetUIString(){
		return "Anchor";
	}	
	
	
	
	
	public override bool IsWired(){
		return false;
	}
	
	
	// Some elements have the notion of another grid point which they use for UI purposes
	public override void SetOtherGridPoint(GridPoint otherPoint){
		if (lastOtherPoint == null || !lastOtherPoint.IsEqual(otherPoint)){
			this.lastOtherPoint = this.otherPoint;
			if (otherPoint != null){
				this.otherPoint = new GridPoint(otherPoint);
			}
			else{
				this.otherPoint = null;
			}
			
			RebuildMesh();
		}
	}	
	
	
	public override void SetGridPoint(GridPoint thisPoint){
		base.SetGridPoint(thisPoint);
		RebuildMesh();
	}
	
	// return true if this component is only available in the editor
	public override bool IsEditorOnly(){
		return true;
	}	
	
	public override void RebuildMesh(){
	
		// Is in the UI
		if (thisPoint == null){
			for (int i = 0; i < 5; ++i){
				anchorData.isAnchored[i] = true;
			}
		}
		// Otherwise
		else{
			for (int i = 0; i < 5; ++i){
				anchorData.isAnchored[i] = false;
			}
			// Is cirectly on a node	
			if (otherPoint == null){
				anchorData.isAnchored[Circuit.kCentre] = true;
				
			}
			// If on a branch
			else{
				// Get which direction the other point is in
				int dir = Circuit.CalcNeighbourDir(thisPoint, otherPoint);
				// This is a hack - it should never be -1
				if (dir != -1){
					anchorData.isAnchored[dir] = true;
				}
			}
		}
		
		// Figure out what prefabs to use
		GameObject 	centrePrefab = anchorCentralPrefabUI;
		GameObject 	branchPrefab = anchorBranchPrefabUI;
		GameObject 	emptyBranchPrefab = null;
		bool[]		isConnected = null;
		int 		orient = 0;
		
		// Get the correct anchore pregfabs
		CircuitElement existingElement = null;
		
		if (thisPoint != null){
			existingElement = Circuit.singleton.GetElement(thisPoint);
			if (existingElement){
				centrePrefab = existingElement.anchorCentralPrefab;
				branchPrefab = existingElement.anchorBranchPrefab;
				emptyBranchPrefab = existingElement.anchorEmptyBranchPrefab;
				isConnected = existingElement.isConnected;
				orient = existingElement.orient;
			}
			else{
				centrePrefab = Circuit.singleton.anchorCentralPrefabDefault;
				branchPrefab = Circuit.singleton.anchorBranchPrefabDefault;
			}
		}
		
		Destroy(anchorData.anchorMesh);
		Destroy(displayMesh);
		
		
		Circuit.RebuildAnchorMesh(anchorData, isConnected, orient, centrePrefab, branchPrefab, emptyBranchPrefab, emptyGO);
		anchorData.isDirty = false;
		if (anchorData.anchorMesh){
			anchorData.anchorMesh.transform.parent = transform;
			anchorData.anchorMesh.transform.localPosition = new Vector3(0f, 0f, 3);
			displayMesh = anchorData.anchorMesh;
			displayMesh.name = displayMeshName;
		}
		

		SetupColor();		
		dirtyAlpha = true;
	}
	
	
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	public override bool CanModify(GridPoint thisPt, GridPoint otherPt, bool honourAnchors){
		return true;
	}
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt, GridPoint otherPt, bool honourAnchors){
	
		// Work out which direction the other point is in
		int thisDir = Circuit.CalcNeighbourDir(thisPt, otherPt);
		
		// If there is no connection here, we should suggest that this
		// element adopts an unreceptive attitude from now on
		CircuitElement existingElement = Circuit.singleton.GetElement(thisPt);
		if (existingElement != null && !existingElement.isConnected[thisDir]){
			existingElement.SuggestBehaviour(thisDir, ConnectionBehaviour.kUnreceptive, honourAnchors);
		}

		
		Circuit.AnchorData thisData = Circuit.singleton.GetAnchors(thisPt);
		
		thisData.isAnchored[thisDir] = !erase;
		thisData.isDirty = true;
		
		
		
		return true;
	}	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt, bool honourAnchors){
	
		Circuit.AnchorData thisData = Circuit.singleton.GetAnchors(thisPt);
		
		thisData.isAnchored[Circuit.kCentre] = !erase;
		thisData.isDirty = true;
		
		return true;
	}	
	
	// Called when mouse is lciked on this. Return true if we changed the object in some way
	public override bool OnClick(){
		erase = !erase;
		SetupColor();
		// need to handle the color change straigth away as we will be rebuilding the UI element asAP
		HandleColorChange();
		return  true;
	}
	
	// Decide if we should call OnClick() if we are clicked on with the selectd prefab
	// For us this will only happy in the UI
	public override bool ShouldClick(GameObject selectionPrefab){
		// IF a different kind of prefab - then don;t click
		if (GetComponent<SerializationID>().id != selectionPrefab.GetComponent<SerializationID>().id){
			return false;
		}

		return true;
		
	}
	
	void SetupColor(){
		SetColor(erase ? eraseColor : buildColor);
	}
	
	
	
	// Update is called once per frame
	void Update () {
		HandleDisplayMeshChlid();
		HandleColorChange();
		
		
		
	}
}
