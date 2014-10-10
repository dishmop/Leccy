using UnityEngine;
using System.Collections;

public class CircuitElement : MonoBehaviour {
	
	public int			orient = 0;			// In 90 degree steps anti-clockwise
	
	// 0 - up
	// 1 - right
	// 2 - down
	// 3  - left 
	public bool[] isConnected = new bool[4];
	
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
		return true;
	}
	
	public virtual void OnClick(){
	}
	
	
	public bool HasConnections(bool up, bool right, bool down, bool left){
		return 	isConnected[0] == up &&
				isConnected[1] == right &&
				isConnected[2] == down &&
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
