using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementAnchor : CircuitElement {
	
	// The ones to use for the UI button
	public GameObject	emptyGO;
	
	GridPoint			otherPoint;
	GridPoint			lastOtherPoint;
	Circuit.AnchorData	anchorData;
	GameObject			anchorCentralPrefabUI;
	GameObject			anchorBranchPrefabUI;
	
	
	
	
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
		anchorData.anchorMesh.transform.parent = transform;
		anchorData.anchorMesh.transform.localPosition = new Vector3(0f, 0f, 3);
		
		displayMesh = anchorData.anchorMesh;
		displayMesh.name = displayMeshName;
		
		dirtyAlpha = true;
		dirtyColor = true;
	}
	
	
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	public override bool CanModify(GridPoint thisPt, GridPoint otherPt){
		return true;
	}
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt, GridPoint otherPt){
	
		// Work out which direction the other point is in
		int thisDir = Circuit.CalcNeighbourDir(thisPt, otherPt);
		
		// If there is no connection here, we should suggest that this
		// element adopts an unreceptive attitude from now on
		CircuitElement existingElement = Circuit.singleton.GetElement(thisPt);
		if (existingElement != null && !existingElement.isConnected[thisDir]){
			existingElement.SuggestBehaviour(thisDir, ConnectionBehaviour.kUnreceptive);
		}

		
		Circuit.AnchorData thisData = Circuit.singleton.GetAnchors(thisPt);
		
		thisData.isAnchored[thisDir] = true;
		thisData.isDirty = true;
		
		
		
		return true;
	}	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt){
	
		Circuit.AnchorData thisData = Circuit.singleton.GetAnchors(thisPt);
		
		thisData.isAnchored[Circuit.kCentre] = true;
		thisData.isDirty = true;
		
		return true;
	}	
	
	
	
	// Update is called once per frame
	void Update () {
		HandleDisplayMeshChlid();
		HandleColorChange();
		
		
		
	}
}
