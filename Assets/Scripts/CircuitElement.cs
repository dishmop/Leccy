using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;




public class CircuitElement : MonoBehaviour {
	
	public int			orient = 0;			// In 90 degree steps anti-clockwise
	public GameObject	anchorPrefab;
	
	// 0 - up
	// 1 - right
	// 2 - down
	// 3  - left 
	public bool[] isConnected = new bool[4];
	public bool[] isBaked = new bool[4];	// if true, then cannot be changed in the editor
	
	protected GridPoint	thisPoint;
	
	GameObject[]	anchors = new GameObject[4];
	int				lastBakedHash = -1;
	
	
	// Generally is any of our connections are baked, then the component itself is also baked
	public bool IsComponentBaked(){
		for (int i = 0; i < 4; ++i){
			if (isBaked[i]) return true;
		}
		return false;
	}
	
	public void SetGridPoint(GridPoint thisPoint){
		this.thisPoint = thisPoint;
	}
	
	public virtual void Save(BinaryWriter bw){
		// This is calculated from connections and is assumed ot match up with orientation of mesh - so don't store
		////		bw.Write (orient);
		for (int i = 0; i < 4; ++i){
			bw.Write (isConnected[i]);
			bw.Write(isBaked[i]);
		}
	}
	
	protected int CalcBakedHash(){
		return (isBaked[0] ? 1 : 0) + (isBaked[1]  ? 2 : 0) + (isBaked[2] ? 4 : 0) + (isBaked[3] ? 8 : 0);
	}
	
	protected void HandleAnchorMeshes(){
		// Check if the baking has changed
		int newBakedHash = CalcBakedHash();
		if (newBakedHash != lastBakedHash){
			lastBakedHash = newBakedHash;
			
			// Destory all the existing Anchor meshes
			for (int i = 0; i < 4; ++i){
				GameObject.Destroy(anchors[i]);
			}
			// Create new ones as needed
			Vector3[] positions = new Vector3[4];
			positions[0] = isConnected[0] ? new Vector3(-0.5f, 0.5f, 0f) : new Vector3(0f, 0.5f, 0f);
			positions[1] = isConnected[1] ? new Vector3(0.5f, 0.5f, 0f) : new Vector3(0.5f, 0f, 0f);
			positions[2] = isConnected[2] ? new Vector3(0.5f, -0.5f, 0f) : new Vector3(0f, -0.5f, 0f);
			positions[3] = isConnected[3] ? new Vector3(-0.5f, -0.5f, 0f) : new Vector3(-0.5f, 0f, 0f);
			
			Quaternion[] orientations = new Quaternion[4];
			orientations[0] = isConnected[0] ? Quaternion.Euler(0, 0, 270) : Quaternion.Euler(0, 0, 180);
			orientations[1] = isConnected[1] ? Quaternion.Euler(0, 0, 180) : Quaternion.Euler(0, 0, 90);
			orientations[2] = isConnected[2] ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(0, 0, 0);
			orientations[3] = isConnected[3] ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 0, 270);
			
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
	}
		
		
	public 	virtual void Load(BinaryReader br){
	// This is calculated from connections and is assumed ot match up with orientation of mesh - so don't store
//		orient = br.ReadInt32();
		for (int i = 0; i < 4; ++i){
			isConnected[i] = br.ReadBoolean();
			isBaked[i] = br.ReadBoolean();
		}
	}

	public void CopyConnectionsFrom(CircuitElement other){
		if (!other) return;	
		for (int i = 0; i < 4; ++i){
			isConnected[i] = other.isConnected[i];
		}
	}
	
	// Analyse current connections and ensure they are valid for this object
	// If not, then change them
	// It is then up to the caller to bring them into line with the neighbouring connections
	public virtual void ValidateConnections(){
	}
	
		
	// Return true if it is ok to set this connection on this element
	public virtual bool CanSetConnection(int dir, bool value){
		return !isBaked[dir] || isConnected[dir] == value;
	}
	
	public virtual void OnClick(){
	}
	
	
	public bool HasConnections(bool up, bool right, bool down, bool left){
		return 	isConnected[0] == up &&
				isConnected[1] == right &&
				isConnected[2] == down &&
				isConnected[3] == left;
	}
	
	public bool HasAnyConnections(bool up, bool right, bool down, bool left){
		return 	isConnected[0] == up ||
				isConnected[1] == right ||
				isConnected[2] == down ||
				isConnected[3] == left;
	}	
	
	public int CountNumConnections(){
		int count = 0;
		for (int i = 0; i < 4; ++i){
			if (isConnected[i]) ++count;
		}
		return count; 
	}
	
	public virtual float GetResistance(int dir){
		if (!isConnected[dir]) Debug.LogError("Being asked about a nonexistanct connection");
		return 0f;
	}
	
	public virtual float GetVoltageDrop(int dir){
		if (!isConnected[dir]) Debug.LogError("Being asked about a nonexistanct connection");
		return 0f;
	}
	
 	public static int CalcInvDir(int dir){
		return (dir + 2) % 4;
	}
	
	public void ClearConnections(){
		isConnected[0]  = false;
		isConnected[1]  = false;
		isConnected[2]  = false;
		isConnected[3]  = false;
	}
	
	public virtual void SetupMesh(){
	}
	

	
}
