using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementWire : CircuitElement {

	public GameObject wirePointPrefab;
	public GameObject wireStraightDownPrefab;
	public GameObject wireDownLeftCornerPrefab;
	public GameObject wireDeadEndDownPrefab;
	public GameObject wireCrossPrefab;
	public GameObject wireTJuncFromTopPrefab;
		
	GameObject 	currentPrefab;
	//GameObject	displayMesh;
	


	
	public void Start(){
		Debug.Log ("CircuitElementWire:Start()");
		
	}
	
	public void Awake(){
		RebuildMesh();
		connectionBehaviour[0] = ConnectionBehaviour.kReceptive;
		connectionBehaviour[1] = ConnectionBehaviour.kReceptive;
		connectionBehaviour[2] = ConnectionBehaviour.kReceptive;
		connectionBehaviour[3] = ConnectionBehaviour.kReceptive;
		
	}
	
	public override bool SuggestInvite(CircuitElement otherElement){
		int dir = Circuit.CalcNeighbourDir(GetGridPoint(), otherElement.GetGridPoint());
		
		connectionBehaviour[dir]= ConnectionBehaviour.kSociable;
		return true;
	}
	
	public override bool SuggestUninvite(CircuitElement otherElement){
		int dir = Circuit.CalcNeighbourDir(GetGridPoint(), otherElement.GetGridPoint());
		
		connectionBehaviour[dir]= ConnectionBehaviour.kReceptive;
		return true;
	}	
	
	
	
	override public void Save(BinaryWriter bw){
		base.Save (bw);	
	}
	
	
	override public void Load(BinaryReader br){
		base.Load (br);	
	}
		
	public override void RebuildMesh(){
		base.RebuildMesh();
		
		// Placeholder
		GameObject newPrefab;
		int newOrient = -1;
		
		// if we are in the UI
		if (!IsOnCircuit()){
			newPrefab = wireStraightDownPrefab;
			newOrient = 0;
		}			
		// No connections
		else if (HasConnections(false, false, false, false)){
			newPrefab = wirePointPrefab;
			newOrient = 0;
		}
		// 1 Connection
		else if (HasConnections(true, false, false, false)){
			newPrefab = wireDeadEndDownPrefab;
			newOrient = 0;
		}
		else if (HasConnections(false, true, false, false)){
			newPrefab = wireDeadEndDownPrefab;
			newOrient = 3;
		}
		else if (HasConnections(false, false, true, false)){
			newPrefab = wireDeadEndDownPrefab;
			newOrient = 2;
		}
		else if (HasConnections(false, false, false, true)){
			newPrefab = wireDeadEndDownPrefab;
			newOrient = 1;
		}
		// 2 connections
		else if (HasConnections(true, true, false, false)){
			newPrefab = wireDownLeftCornerPrefab;
			newOrient = 3;
		}	
		else if (HasConnections(false, true, true, false)){
			newPrefab = wireDownLeftCornerPrefab;
			newOrient = 2;
		}
		else if (HasConnections(false, false, true, true)){
			newPrefab = wireDownLeftCornerPrefab;
			newOrient = 1;
		}	
		else if (HasConnections(true, false, false, true)){
			newPrefab = wireDownLeftCornerPrefab;
			newOrient = 0;
		}									
		else if (HasConnections(true, false, true, false)){
			newPrefab = wireStraightDownPrefab;
			newOrient = 0;
		}
		else if (HasConnections(false, true, false, true)){
			newPrefab = wireStraightDownPrefab;
			newOrient = 1;
		}	
		// 3 connections
		else if (HasConnections(true, true, true, false)){
			newPrefab = wireTJuncFromTopPrefab;
			newOrient = 3;
		}		
		else if (HasConnections(false, true, true, true)){
			newPrefab = wireTJuncFromTopPrefab;
			newOrient = 2;
		}	
		else if (HasConnections(true, false, true, true)){
			newPrefab = wireTJuncFromTopPrefab;
			newOrient = 1;
		}	
		else if (HasConnections(true, true, false, true)){
			newPrefab = wireTJuncFromTopPrefab;
			newOrient = 0;
		}
		// 4 connections
		else if (HasConnections(true, true, true, true)){
			newPrefab = wireCrossPrefab;
			newOrient = 0;
		}															
		else{
			Debug.LogError ("Unknown junction type");
			// But set up something to draw anyway
			newPrefab = wirePointPrefab;
			newOrient = 0;
		}		
		
		
		if (newPrefab != currentPrefab){
			GameObject dispMesh = GetDisplayMesh();
			GameObject.Destroy(dispMesh);
			currentPrefab = newPrefab;
			displayMesh = Instantiate(currentPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, newOrient * 90)) as GameObject;
			displayMesh.name = displayMeshName;
			displayMesh.transform.parent = transform;
			
		}
		if (newOrient != orient){
			orient = newOrient;
			GetDisplayMesh().transform.rotation = Quaternion.Euler(0, 0, newOrient * 90);
		}

	}	
	
	
	
	public override string GetUIString(){
		return "Wire";
	}	
	
	// Called when mouse is lciked on this. Return true if we changed the object in some way
	public override bool OnClick(){
		return  false;
	}			
	
	
	void Update () {
		HandleAlpha();
		HandleDisplayMeshChlid();	
		

		VisualiseTemperature();
	}

	// Call this before erasing an element	
	public override void RemoveConnections(){
		// Suggest to any elements around us that they may want to cancel their invite to us
		// Really only applicable to other wires
		if (IsOnCircuit()){
			for (int dir = 0; dir < 4; ++dir){
				GridPoint otherPoint = thisPoint + Circuit.singleton.offsets[dir];
				CircuitElement otherElement = Circuit.singleton.GetElement(otherPoint);
				if (otherElement){
					otherElement.SuggestUninvite(this);
				}
			}
		}
	}
	
	void OnDestroy() {
		// When the object is destoryed, we must make sure to dispose of any meshes we may have
		Debug.Log ("OnDestroy");
		
		
	}	
	


}
