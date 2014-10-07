using UnityEngine;
using System.Collections;

public class CircuitElementCell : CircuitElement {

	public GameObject 	cellPrefab;
	public GameObject	displayMesh;
	public int			orient = 0;			// In 90 degree steps anti-clockwise
	public bool			invertOrient;		// Turn through 180 degrees?
	public float		voltage = 1;

	public void Start(){
		Debug.Log ("CircuitElementCell:Start()");
	}

	public override void OnClick(){
		invertOrient = !invertOrient;
	}
	
	// Analyse current connections and ensure they are valid for this object
	// If not, then change them
	// It is then up to the caller to bring them into line with the neighbouring connections
	public override void ValidateConnections(){
		
		// No connections
		int numConnections = CountNumConnections();
		switch (numConnections)
		{
		case 0:
			isConnected[0] = true;
			isConnected[2] = true;
			break;
		case 1:
			// enabled the opposing one
			for (int i = 0; i < 4; ++i){
				if (isConnected[i]) isConnected[CalcInvDir(i)] = true;
			}
			break;
		case 2: 
			// Find the first one and set the opposing one
			for (int i = 0; i < 4; ++i){
				if (isConnected[i]){
					ClearConnections();
					isConnected[i] = true;
					isConnected[CalcInvDir(i)] = true;
					break;
				}
			}
			break;
		case 3:
			// Fine the one without the opposing side and remove it
			for (int i = 0; i < 4; ++i){
				if (!isConnected[i]){
					isConnected[CalcInvDir(i)] = false;
					break;
				}
			}	
			break;
		case 4:
			ClearConnections();
			isConnected[0] = true;
			isConnected[2] = true;
			break;					
		}
	}
	
	// Return true if it is ok to set this connection on this element
	// For cells, it is only ok if this is what has been set already
	public override bool CanSetConnection(int dir, bool value){
		return isConnected[dir] == value;
	}	
	
	public override float GetResistance(int dir){
		if (!isConnected[dir]) Debug.LogError("Being asked about a nonexistanct connection");
		return 0f;
	}
	
	public override float GetVoltageDrop(int dir){
		if (!isConnected[dir]) Debug.LogError("Being asked about a nonexistanct connection");
		
		if ((dir == Circuit.kLeft || dir == Circuit.kDown) && invertOrient)
			return voltage;
		if ((dir == Circuit.kRight || dir == Circuit.kUp) && !invertOrient)
			return voltage;			
		else 
			return -voltage;
	}	
	
	public override void SetupMesh(){
		int newOrient = 0;
		
		// Just need to check one value to see if we are connected horizontally rather than virtically		
		if (isConnected[0]) newOrient = 1;
		
		if (invertOrient) newOrient += 2;
		
		if (newOrient != orient){
			orient = newOrient;
			displayMesh.transform.rotation = Quaternion.Euler(0, 0, newOrient * 90);
		}
		
	}	
	
	// Use this for initialization
	void Awake () {
		displayMesh = Instantiate(cellPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
		displayMesh.transform.parent = transform;
		
	}
	
	void OnDestroy(){
		GameObject.Destroy (displayMesh);
	}
	
	// Update is called once per frame
	void Update () {
		SetupMesh ();
		
		
	}
}
