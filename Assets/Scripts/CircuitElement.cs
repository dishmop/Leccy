﻿using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;




public class CircuitElement : MonoBehaviour {
	
	public int			orient = 0;			// In 90 degree steps anti-clockwise
	public GameObject	anchorPrefab;
	public float		temperature = 0;
	
	
	// What is the best UI scheme to use when placing these elents
	public enum UIType{
		kNone,
		kDraw,			// Lay them out in lines like drawing with a pen
		kPlace,			// Place sinlge elements down one at a time
		kNumTypes		// Useufl for iterating over the,
	};
	
	public UIType uiType = UIType.kNone;
	
	
	// These enums describe the connection situation in a given direction
	public enum ConnectionStatus{ 
		kUnreceptive,   // there is no connection, and if invited to make one, I will say no
		kReceptive,		// There is no connection but if invited to make one, I wil say yes
		kSocialble,		// There is no connection, but I will invite anyone to connect here
		kConnected,		// There is a connection
	};
	
	// 0 - up
	// 1 - right
	// 2 - down
	// 3  - left 
	public ConnectionStatus[] connectionStatus = new ConnectionStatus[4];
	public bool[] isBaked = new bool[4];	// if true, then cannot be changed in the editor

	
	protected float 	maxTemp = 45f;
	protected GridPoint	thisPoint;

	GameObject[]	anchors = new GameObject[4];
	
	
	// Generally is any of our connections are baked, then the component itself is also baked
	public bool IsComponentBaked(){
		for (int i = 0; i < 4; ++i){
			if (isBaked[i]) return true;
		}
		return false;
	}
	
	public virtual void TriggerEmergency(){
		
		
	}
	
	
	// The prefab to use in the UI (each element may have several meshes - need to just show one in the UI)
	public virtual GameObject   GetDisplayMesh(){
		return null;
	}
	
	public virtual string GetUIString(){
		return "None";
	}
	
	
	public void SetGridPoint(GridPoint thisPoint){
		this.thisPoint = thisPoint;
	}
	
	public virtual void Save(BinaryWriter bw){
		// This is calculated from connections and is assumed ot match up with orientation of mesh - so don't store
		////		bw.Write (orient);
		for (int i = 0; i < 4; ++i){
			bw.Write ((int)connectionStatus[i]);
			bw.Write(isBaked[i]);
		}
		bw.Write (temperature);
	}
	
	public 	virtual void Load(BinaryReader br){
		// This is calculated from connections and is assumed ot match up with orientation of mesh - so don't store
		//		orient = br.ReadInt32();
		for (int i = 0; i < 4; ++i){
			connectionStatus[i] = (ConnectionStatus)br.ReadInt32();
			isBaked[i] = br.ReadBoolean();
		}
		temperature = br.ReadSingle();
	}
	
	public virtual void PostLoad(){
		RebuildMesh ();
	}
	
// 	int CalcBakedHash(){
//		return (isBaked[0] ? 1 : 0) + (isBaked[1]  ? 2 : 0) + (isBaked[2] ? 4 : 0) + (isBaked[3] ? 8 : 0);
//	}

	public bool IsConnected(int dir){
		return connectionStatus[dir] == ConnectionStatus.kConnected;
	}
	
	void RebuildAnchorMeshes(){
		// Destory all the existing Anchor meshes
		for (int i = 0; i < 4; ++i){
			GameObject.Destroy(anchors[i]);
		}
		// Create new ones as needed
		Vector3[] positions = new Vector3[4];
		positions[0] = IsConnected(0) ? new Vector3(-0.5f, 0.5f, 0f) : new Vector3(0f, 0.5f, 0f);
		positions[1] = IsConnected(1) ? new Vector3(0.5f, 0.5f, 0f) : new Vector3(0.5f, 0f, 0f);
		positions[2] = IsConnected(2) ? new Vector3(0.5f, -0.5f, 0f) : new Vector3(0f, -0.5f, 0f);
		positions[3] = IsConnected(3) ? new Vector3(-0.5f, -0.5f, 0f) : new Vector3(-0.5f, 0f, 0f);
		
		Quaternion[] orientations = new Quaternion[4];
		orientations[0] = IsConnected(0)  ? Quaternion.Euler(0, 0, 270) : Quaternion.Euler(0, 0, 180);
		orientations[1] = IsConnected(1)  ? Quaternion.Euler(0, 0, 180) : Quaternion.Euler(0, 0, 90);
		orientations[2] = IsConnected(2)  ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(0, 0, 0);
		orientations[3] = IsConnected(3)  ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 0, 270);
		
		for (int i = 0; i < 4; ++i){
			if (isBaked[i]){
				
				GameObject newElement = Instantiate(
					anchorPrefab, 
					new Vector3(transform.position.x + positions[i].x, transform.position.y + positions[i].y, anchorPrefab.transform.position.z), 
					orientations[i])
					as GameObject;
				newElement.transform.parent = transform;	
				anchors[i] = newElement;
			}
		}
			
	}
		
		


	public void CopyConnectionsFrom(CircuitElement other){
		if (!other) return;	
		for (int i = 0; i < 4; ++i){
			connectionStatus[i] = other.connectionStatus[i];
		}
	}
	
		
//	// Return true if it is ok to set this connection on this element
//	public virtual bool CanSetConnection(int dir, bool value){
//		return !isBaked[dir] || isConnected[dir] == value;
//	}
	
//	
	public bool HasConnections(bool up, bool right, bool down, bool left){
		return 	IsConnected(0) == up &&
		        IsConnected(1) == right &&
				IsConnected(2) == down &&
				IsConnected(3) == left;
	}
//	
//	public bool HasAnyConnections(bool up, bool right, bool down, bool left){
//		return 	isConnected[0] == up ||
//				isConnected[1] == right ||
//				isConnected[2] == down ||
//				isConnected[3] == left;
//	}	
//	
//	public int CountNumConnections(){
//		int count = 0;
//		for (int i = 0; i < 4; ++i){
//			if (isConnected[i]) ++count;
//		}
//		return count; 
//	}

	public void Rotate(int stepsCW){
		orient = (4 + orient + stepsCW) % 4;
		RebuildMesh();
		
	}
	
	public virtual float GetResistance(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		return 0f;
	}
	
	// Called when mouse is lciked on this. Return true if we changed the object in some way
	public virtual bool OnClick(){
		Rotate (1);
		return  true;
	}
	
	public virtual float GetVoltageDrop(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		return 0f;
	}
	
 	public static int CalcInvDir(int dir){
		return (dir + 2) % 4;
	}
//	
//	public void ClearConnections(){
//		isConnected[0]  = false;
//		isConnected[1]  = false;
//		isConnected[2]  = false;
//		isConnected[3]  = false;
//	}
//	
	public virtual void RebuildMesh(){
		RebuildAnchorMeshes();
	}
	
	protected void VisualiseTemperature(){
//		foreach (Transform child in transform.GetChild(0)){
//			MeshRenderer mesh = child.gameObject.transform.GetComponent<MeshRenderer>();
//			if (mesh != null) mesh.materials[0].SetFloat ("_Temperature", temperature / maxTemp );
//		}		
		
	}	
	
	protected void DestorySelf(){
		isBaked[0] = false;
		isBaked[1] = false;
		isBaked[2] = false;
		isBaked[3] = false;
		Circuit.singleton.RemoveElement(thisPoint);
		Circuit.singleton.TriggerExplosion(thisPoint);
	
	}
	
	// Return the maximum voltage difference accross all connections
	public float GetMaxVoltage(){
		if (thisPoint == null) return 0f;
		return Mathf.Max(
			Mathf.Max(
				Mathf.Abs(Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, 0)), 
				Mathf.Abs(Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, 1))),
			Mathf.Max(
				Mathf.Abs(Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, 2)), 
				Mathf.Abs(Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, 3))));
	}
	
	
	public float GetMaxCurrent(){
		if (thisPoint == null) return 0f;
		return Mathf.Max(
			Mathf.Max(
				Mathf.Abs(Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 0)), 
				Mathf.Abs(Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 1))),
			Mathf.Max(
				Mathf.Abs(Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 2)), 
				Mathf.Abs(Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 3))));	
	}	

	
}
