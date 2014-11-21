using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementCell : CircuitElement {

	public GameObject 	cellPrefab;
	public float		voltage = 12f;
	public float		resistance = 0.06f;
	float				maxCurrent = 100f;
	bool				isInEmergency = false;
	

	public void Start(){
		Debug.Log ("CircuitElementCell:Start()");
		CreateDisplayMesh();	
	}
	
	public override void Save(BinaryWriter bw){
		base.Save (bw);	
		
		bw.Write (voltage);
		bw.Write(resistance);
	}
	
	
	public override void Load(BinaryReader br){
		base.Load (br);	
		
		voltage = br.ReadSingle();
		resistance = br.ReadSingle();
	}
	
	public override void TriggerEmergency(){
		isInEmergency = true;
	}	
	
	
	public override string GetUIString(){
		return "Cell";
	}	

//	public override void OnClick(){
//		invertOrient = !invertOrient;
//	}
//	
//	// Analyse current connections and ensure they are valid for this object
//	// If not, then change them
//	// It is then up to the caller to bring them into line with the neighbouring connections
//	public override void ValidateConnections(){
//		
//		// No connections
//		int numConnections = CountNumConnections();
//		switch (numConnections)
//		{
//		case 0:
//			isConnected[0] = true;
//			isConnected[2] = true;
//			break;
//		case 1:
//			// enabled the opposing one
//			for (int i = 0; i < 4; ++i){
//				if (isConnected[i]) isConnected[CalcInvDir(i)] = true;
//			}
//			break;
//		case 2: 
//			// Find the first one and set the opposing one
//			for (int i = 0; i < 4; ++i){
//				if (isConnected[i]){
//					ClearConnections();
//					isConnected[i] = true;
//					isConnected[CalcInvDir(i)] = true;
//					break;
//				}
//			}
//			break;
//		case 3:
//			// Fine the one without the opposing side and remove it
//			for (int i = 0; i < 4; ++i){
//				if (!isConnected[i]){
//					isConnected[CalcInvDir(i)] = false;
//					break;
//				}
//			}	
//			break;
//		case 4:
//			ClearConnections();
//			isConnected[0] = true;
//			isConnected[2] = true;
//			break;					
//		}
//	}
	
//	// Return true if it is ok to set this connection on this element
//	// For cells, it is only ok if this is what has been set already
//	public override bool CanSetConnection(int dir, bool value){
//		return isConnected[dir] == value;
//	}	
	
	public override float GetResistance(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		return isInEmergency ? 0.001f : resistance;
	}
	
	public override float GetVoltageDrop(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		
//		if ((dir == Circuit.kLeft || dir == Circuit.kDown) && invertOrient)
//			return voltage;
//		if ((dir == Circuit.kRight || dir == Circuit.kUp) && !invertOrient)
//			return voltage;			
//		else 
//			return -voltage;
		return voltage;
	}	
	
	public override void RebuildMesh(){
		base.RebuildMesh();
		GetDisplayMesh().transform.rotation = Quaternion.Euler(0, 0, orient * 90);
	}	
	


	
	// Call this if instantiating an inactive version
	public override void InactveStart(){
		CreateDisplayMesh();	
	}
	
	
	void CreateDisplayMesh(){
		Destroy(GetDisplayMesh ());
		GameObject displayMesh = Instantiate(cellPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
		displayMesh.name = displayMeshName;
		displayMesh.transform.parent = transform;
		RebuildMesh();	
	}
	
	
	
	float GetAbsCurrentFlow(){
		if (!IsOnCircuit()) return 0f;
		return  Mathf.Abs (Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 0) + Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 1));
	}
	
	// Update is called once per frame
	void Update () {
		HandleAlpha();
		
		GetDisplayMesh().transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().value = voltage;
		
		
		// If our current is not huge then we are probably in a zero resistance loop
		// and we should stay in out state of emergency. However, if we are not, then the player 
		// has got in quickly enough so can reset our emegency flag
		float currentFlow = GetAbsCurrentFlow();
		if (isInEmergency && currentFlow < maxCurrent){
			Debug.Log ("Resetting emergency staste");
			isInEmergency = false;
		}
		if (GetAbsCurrentFlow() > maxCurrent && temperature < maxTemp){
			temperature += 1f;
		}
		else{
			if (temperature > 0) temperature -= 1f;
		}
		// If we reach our maximum temperature then we should remove the component
		if (temperature == maxTemp){
			DestorySelf();
		}
		VisualiseTemperature();
		
		// Set up the audio
		AudioSource source = gameObject.GetComponent<AudioSource>();
		source.pitch = currentFlow * 0.1f;
		

		
	}
}
