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
	GameObject	displayMesh;
	
	
	// Se this up so that the UI mesh is reasonable
	void Awake(){
		connectionStatus[0] = ConnectionStatus.kConnected;
		connectionStatus[2] = ConnectionStatus.kConnected;
	}
	
	public void Start(){
		Debug.Log ("CircuitElementWire:Start()");
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
		
		// No connections
		if (HasConnections(false, false, false, false)){
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
			GameObject.Destroy(displayMesh);
			currentPrefab = newPrefab;
			displayMesh = Instantiate(currentPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, newOrient * 90)) as GameObject;
			displayMesh.transform.parent = transform;
		}
		else if (newOrient != orient){
			orient = newOrient;
			displayMesh.transform.rotation = Quaternion.Euler(0, 0, newOrient * 90);
		}
	}	
	
	// The prefab to use in the UI (each element may have several meshes - need to just show one in the UI)
	public  override GameObject GetDisplayMesh(){
		return displayMesh;
	}		
	
	public override string GetUIString(){
		return "Wire";
	}		
	
	
	void Update(){
		VisualiseTemperature();
	}
	
	void OnDestroy() {
		// When the object is destoryed, we must make sure to dispose of any meshes we may have
		Debug.Log ("OnDestroy");
		
	}	
	


}
