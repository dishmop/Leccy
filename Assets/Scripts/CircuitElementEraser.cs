using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementEraser : CircuitElement {

	public GameObject 	eraserPrefab;

	
	GridPoint			otherPoint;
	GridPoint			lastOtherPoint;

	const int kLoadSaveVersion = 1;

	public void Start(){
//		Debug.Log ("CircuitElementEraser:Start()");
	}
	
	public void Awake(){
		CreateDisplayMesh();	
	}


	public override string GetUIString(){
		return "Eraser";
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
	
	// Only need these for serialising the ghost for telemetry
	override public void Save(BinaryWriter bw){
		base.Save (bw);	
		
		bw.Write (kLoadSaveVersion);
		bool hasOtherPoint = (otherPoint != null);
		bw.Write (hasOtherPoint);
		if (hasOtherPoint){
			bw.Write(otherPoint.x);
			bw.Write(otherPoint.y);
		}
	}
	
	
	override public void Load(BinaryReader br){
		base.Load (br);	
		bool rebuild = false;
		int version = br.ReadInt32();
		switch (version){
			case kLoadSaveVersion:{
				bool hasOtherPoint = br.ReadBoolean();
				if (hasOtherPoint){
					if (otherPoint == null){
						otherPoint = new GridPoint();
						rebuild = true;
					}
					otherPoint.x = br.ReadInt32 ();
					otherPoint.y = br.ReadInt32 ();
				}
				else{
					if (otherPoint!= null){
						otherPoint = null;
						rebuild = true;
					}
				}
				break;
			}
		}
		if (rebuild) RebuildMesh();
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
		
		
		if (thisPoint != null && otherPoint != null){
			Vector3 newLocalPos = new Vector3((otherPoint.x - thisPoint.x) * 0.5f, (otherPoint.y - thisPoint.y) * 0.5f, 0f);
			displayMesh.transform.localPosition = newLocalPos;
			
			// Squish it a bit
			if (MathUtils.FP.Feq(thisPoint.x, otherPoint.x)){
				displayMesh.transform.localScale = new Vector3(0.5f, 1f, 1f);
			}
			else{
				displayMesh.transform.localScale = new Vector3(1f, 0.5f, 1f);
			}
		}		
		else{
			displayMesh.transform.localPosition = new Vector3(0, 0, 0);
			displayMesh.transform.localScale = new Vector3(1f, 1f, 1f);
		}
		SetColor (isInErrorState ? errorColor : normalColor);
	
	}
	
	
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify the proposed points
	public override bool CanModify(GridPoint thisPt, GridPoint otherPt, bool honourAnchors){
		CircuitElement thisElement = null;
		CircuitElement otherElement = null;
		
		if (thisPt != null) thisElement = Circuit.singleton.GetElement(thisPt);
		if (otherPt != null) otherElement = Circuit.singleton.GetElement(otherPt);
		
		// Sometimes we cannot remove a connection because it is a necessary part of an element
		if (thisElement != null && otherElement != null){
			// First make then unresponsive - if we were not able to do that, try making the unreceptive
			bool ok1 = thisElement.IsAmenableToBehaviour(otherElement, ConnectionBehaviour.kReceptive, honourAnchors);
			bool ok2 = otherElement.IsAmenableToBehaviour(thisElement, ConnectionBehaviour.kReceptive, honourAnchors);
			if (!ok1) ok1 = thisElement.IsAmenableToBehaviour(otherElement, ConnectionBehaviour.kUnreceptive, honourAnchors);
			if (!ok2) ok2 = otherElement.IsAmenableToBehaviour(thisElement, ConnectionBehaviour.kUnreceptive, honourAnchors);
			return (ok1 && ok2);
		}
		
		// Other than that, the only reason is because of achors
		if (!honourAnchors) return true;	
		
		// First check for anchors which stop the node being deleted
		if (thisElement != null && otherElement == null){
			Circuit.AnchorData anchorData = Circuit.singleton.GetAnchors(thisPt);
			if (anchorData.isAnchored[Circuit.kCentre]) return false;
			for (int i = 0; i < 4; ++i){
				if (anchorData.isAnchored[i] && thisElement.IsSociableOrConnected(i, true)){
					return false;
				}
			}
		}
		
		// Then check for conection anchors
		if (thisPt != null && otherPt != null){ 
			int dir = Circuit.CalcNeighbourDir(thisPt, otherPt);
			if (Circuit.singleton.GetAnchors(thisPt).isAnchored[dir]) return false;
		}
		


		return true;
	}
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt, GridPoint otherPt, bool honourAnchors){
		if (!CanModify(thisPt, otherPt, honourAnchors)) return false;
		
		CircuitElement thisElement = null;
		CircuitElement otherElement = null;
		
		if (thisPt != null) thisElement = Circuit.singleton.GetElement(thisPt);
		if (otherPt != null) otherElement = Circuit.singleton.GetElement(otherPt);

		if (thisElement != null && otherElement != null){
			// First make then unresponsive - if we were not able to do that, try making the unreceptive
			bool ok1 = thisElement.SuggestBehaviour(otherElement, ConnectionBehaviour.kReceptive, honourAnchors);
			bool ok2 = otherElement.SuggestBehaviour(thisElement, ConnectionBehaviour.kReceptive, honourAnchors);
			if (!ok1) ok1 = thisElement.SuggestBehaviour(otherElement, ConnectionBehaviour.kUnreceptive, honourAnchors);
			if (!ok2) ok2 = otherElement.SuggestBehaviour(thisElement, ConnectionBehaviour.kUnreceptive, honourAnchors);
			Circuit.singleton.OnCircutChange();
			return (ok1 && ok2);
		}
		return true;
	}
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt, bool honourAnchors){
		if (!CanModify(thisPt, null, honourAnchors)) return false;
		
		
		GameObject existingElement = Circuit.singleton.GetGameObject(thisPt);
		
		// If there is one there already
		if (existingElement != null){
			
			UI.singleton.RemoveElement(existingElement);
			Circuit.singleton.OnCircutChange();
		}
		return true;
	}
	
	
	// Update is called once per frame
	void Update () {
		HandleDisplayMeshChlid();
		HandleColorChange();
		

		
	}
}
