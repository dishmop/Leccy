using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementEraser : CircuitElement {

	public GameObject 	eraserPrefab;

	
	GridPoint			otherPoint;
	GridPoint			lastOtherPoint;
	
	
	

	public void Start(){
		Debug.Log ("CircuitElementEraser:Start()");
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
	public override bool CanModify(GridPoint thisPt, GridPoint otherPt){
		// Test if we are able to erasing the thing if we were t press the button
		CircuitElement thisElement = null;
		CircuitElement otherElement = null;
		
		if (thisPoint != null) thisElement = Circuit.singleton.GetElement(thisPt);
		if (otherPoint != null) otherElement = Circuit.singleton.GetElement(otherPt);
		
		if (thisElement != null && otherElement != null){
			// First make then unresponsive - if we were not able to do that, try making the unreceptive
			bool ok1 = thisElement.IsAmenableToBehaviour(otherElement, ConnectionBehaviour.kReceptive);
			bool ok2 = otherElement.IsAmenableToBehaviour(thisElement, ConnectionBehaviour.kReceptive);
			if (!ok1) ok1 = thisElement.IsAmenableToBehaviour(otherElement, ConnectionBehaviour.kUnreceptive);
			if (!ok2) ok2 = otherElement.IsAmenableToBehaviour(thisElement, ConnectionBehaviour.kUnreceptive);
			return (ok1 && ok2);
		}
		return true;
	}
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt, GridPoint otherPt){
		CircuitElement thisElement = null;
		CircuitElement otherElement = null;
		
		if (thisPoint != null) thisElement = Circuit.singleton.GetElement(thisPt);
		if (otherPoint != null) otherElement = Circuit.singleton.GetElement(otherPt);

		if (thisElement != null && otherElement != null){
			// First make then unresponsive - if we were not able to do that, try making the unreceptive
			bool ok1 = thisElement.SuggestBehaviour(otherElement, ConnectionBehaviour.kReceptive);
			bool ok2 = otherElement.SuggestBehaviour(thisElement, ConnectionBehaviour.kReceptive);
			if (!ok1) ok1 = thisElement.SuggestBehaviour(otherElement, ConnectionBehaviour.kUnreceptive);
			if (!ok2) ok2 = otherElement.SuggestBehaviour(thisElement, ConnectionBehaviour.kUnreceptive);
			return (ok1 && ok2);
		}
		return true;
	}	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public override bool Modify(GridPoint thisPt){
		GameObject existingElement = Circuit.singleton.GetGameObject(thisPt);
		
		// If there is one there already
		if (existingElement != null){
			
			UI.singleton.RemoveElement(existingElement);
		}
		return true;
	}
	
	
	// Update is called once per frame
	void Update () {
		HandleDisplayMeshChlid();
		HandleColorChange();
		

		
	}
}
