using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementGridHider : CircuitElement {
	
	public GameObject 	eraserPrefab;
	public Color        construcColor;
	
	
	GridPoint			otherPoint;
	GridPoint			lastOtherPoint;
	
	// Whether we are removing gridpoints or adding them back in again
	// Need this so it gets copied appropriatly
	[SerializeField]
	public bool				erase = true;	
	
	
	
	
	public void Start(){
//		Debug.Log ("CircuitElementEraser:Start()");
	}
	
	public void Awake(){
		CreateDisplayMesh();	
	}
	
	
	public override string GetUIString(){
		return "GridHider";
	}	
	
	
	
	
	public override bool IsWired(){
		return false;
	}
	
	// return true if this component is only available in the editor
	public override bool IsEditorOnly(){
		return true;
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
	
	
	void CreateDisplayMesh(){
		Destroy(GetDisplayMesh ());
		displayMesh = Instantiate(eraserPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
		displayMesh.name = displayMeshName;
		displayMesh.transform.parent = transform;
		
		
		RebuildMesh();	
	}
	
	public override void RebuildMesh(){
		base.RebuildMesh();
		
		

		SetColor (isInErrorState ? errorColor : erase ? normalColor : construcColor);
		dirtyAlpha = true;
		
		
	}
	
	
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify the proposed points
	public override bool CanModify(GridPoint thisPt, GridPoint otherPt, bool honourAnchors){
		CircuitElement thisElement = null;
		
		if (thisPt == null) return false;
		
		if (thisPt != null) thisElement = Circuit.singleton.GetElement(thisPt);
		
		if (erase){
			// If we have an element here, then we cannot change the grid
			if (thisElement != null) return false;
			
			// If we have anchors here, then we cannot erase it
			for (int i = 0; i < 4; ++i){
				if (Circuit.singleton.GetAnchors(thisPt).isAnchored[i]) return false;
			}
		}
		else{
			// We can only unerase if it was erased to start with
			if (!Circuit.singleton.GetAnchors(thisPt).disableGrid) return false;
		}
		
		return true;
		
	}
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt, GridPoint otherPt, bool honourAnchors){
		if (!CanModify(thisPt, otherPt, honourAnchors)) return false;
		
		
		// Otherwise we can - and we set all brnahces and the node to have anchors
		Circuit.AnchorData data = Circuit.singleton.GetAnchors(thisPt);
		for (int i = 0; i < 5; ++i){
			data.isAnchored[i] = erase;
		}
		data.disableGrid = erase;
		data.isDirty = true;
		return true;
	}
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt, bool honourAnchors){
		if (!CanModify(thisPt, null, honourAnchors)) return false;
		
		// Otherwise we can - and we set all brnahces and the node to have anchors
		Circuit.AnchorData data = Circuit.singleton.GetAnchors(thisPt);
		for (int i = 0; i < 5; ++i){
			data.isAnchored[i] = erase;
		}
		data.disableGrid = erase;
		data.isDirty = true;
		return true;
	}
	
	// Called when mouse is lciked on this. Return true if we changed the object in some way
	public override bool OnClick(){
		erase = !erase;
		RebuildMesh();
		// need to handle the color change straigth away as we will be rebuilding the UI element asAP
		HandleColorChange();
		return  true;
	}
	
	
	
	// Update is called once per frame
	void Update () {
		HandleDisplayMeshChlid();
		HandleColorChange();
		
		
		
	}
}
