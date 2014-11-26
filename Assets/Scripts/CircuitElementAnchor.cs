using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementAnchor : CircuitElement {
	
	GridPoint			otherPoint;
	GridPoint			lastOtherPoint;
	
	
	
	
	public void Start(){
		Debug.Log ("CircuitElementAnchor:Start()");
	}
	
	public void Awake(){
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
	
	

	
	public override void RebuildMesh(){
	
		// Set up the Anchors as they should be
		if (thisPoint == null){
			for (int i = 0; i < 5; ++i){
				isAnchored[i] = true;
			}
		}
		else{
			for (int i = 0; i < 5; ++i){
				isAnchored[i] = false;
			}	
			if (otherPoint == null){
				isAnchored[Circuit.kCentre] = true;
				
			}
			else{
				// Get which direction the other point is in
				int dir = Circuit.CalcNeighbourDir(thisPoint, otherPoint);
				// This is a hack - it should never be -1
				if (dir != -1){
					isAnchored[dir] = true;
				}
			}
		}
		base.RebuildMesh();
		
		// Copy the mesh t the diusplay mesh (so it can be alphed out)
		Destroy (displayMesh);
		displayMesh = Instantiate(anchorMesh, transform.position, transform.rotation) as GameObject;
		displayMesh.transform.parent = transform;
		displayMesh.name = displayMeshName;
		
		// Now remove the anchors again
		for (int i = 0; i < 5; ++i){
			isAnchored[i] = false;
		}	
		
		base.RebuildMesh();	
		
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
		int otherDir = CalcInvDir(thisDir);
		
		CircuitElement thisElement = null;
		CircuitElement otherElement = null;
		
		if (thisPoint != null) thisElement = Circuit.singleton.GetElement(thisPt);
		if (otherPoint != null) otherElement = Circuit.singleton.GetElement(otherPt);
		
		if (thisElement){
			thisElement.isAnchored[thisDir] = true;
			thisElement.RebuildMesh();
		}
		if (otherElement){
			otherElement.isAnchored[otherDir] = true;
			otherElement.RebuildMesh();
			
		}
		return true;
	}	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt){
		CircuitElement existingElement = Circuit.singleton.GetElement(thisPt);
		
		// If there is one there already
		if (existingElement != null){
			
			existingElement.isAnchored[Circuit.kCentre] = true;
			existingElement.RebuildMesh();
		}
		return true;
	}	
	
	
	
	// Update is called once per frame
	void Update () {
		HandleDisplayMeshChlid();
		HandleColorChange();
		
		
		
	}
}
