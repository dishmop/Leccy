using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


public class CircuitElementResistor : CircuitElement {
	public GameObject 	resistorPrefab;
	public float		resistance = 1;
	
	GameObject	displayMesh;
	

	public void Start(){
		Debug.Log ("CircuitElementResistor:Start()");
	}
	
	override public void Save(BinaryWriter bw){
		base.Save (bw);	
		bw.Write(resistance);
	}
	
	
	override public void Load(BinaryReader br){
		base.Load (br);	
		
		resistance = br.ReadSingle();
	}	
	
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
//			case 0:
//				isConnected[0] = true;
//				isConnected[2] = true;
//				break;
//			case 1:
//				// enabled the opposing one
//				for (int i = 0; i < 4; ++i){
//					if (isConnected[i]) isConnected[CalcInvDir(i)] = true;
//				}
//				break;
//			case 2: 
//				// Find the first one and set the opposing one
//				for (int i = 0; i < 4; ++i){
//					if (isConnected[i]){
//						ClearConnections();
//						isConnected[i] = true;
//						isConnected[CalcInvDir(i)] = true;
//						break;
//					}
//				}
//				break;
//			case 3:
//				// Fine the one without the opposing side and remove it
//				for (int i = 0; i < 4; ++i){
//					if (!isConnected[i]){
//						isConnected[CalcInvDir(i)] = false;
//						break;
//					}
//				}	
//				break;
//			case 4:
//				ClearConnections();
//				isConnected[0] = true;
//				isConnected[2] = true;
//				break;					
//		}
//	}
//	
//	// Return true if it is ok to set this connection on this element
//	// For resistors, it is only ok if this is what has been set already
//	public override bool CanSetConnection(int dir, bool value){
//		return isConnected[dir] == value;
//	}	
	
	public override void RebuildMesh(){
		base.RebuildMesh ();
		displayMesh.transform.rotation = Quaternion.Euler(0, 0, orient * 90);
	}	
	
	public override float GetResistance(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		return resistance;
	}	
	
	// The prefab to use in the UI (each element may have several meshes - need to just show one in the UI)
	public  override GameObject GetDisplayMesh(){
		return displayMesh;
	}		
	
	public override string GetUIString(){
		return "Resistor";
	}
		
	
	// Use this for initialization
	void Awake () {
		displayMesh = Instantiate(resistorPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
		displayMesh.transform.parent = transform;
		RebuildMesh ();
		
	}
	
	void OnDestroy(){
		GameObject.Destroy (displayMesh);
	}
	
	
	float GetAbsCurrentFlow(){
		if (thisPoint == null) return 0f;
		return  Mathf.Abs (Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 0) + Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 1));
	}
	
	
	// Update is called once per frame
	void Update () {
		displayMesh.transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().value = resistance;
		
		VisualiseTemperature();
		
		// If our current is not huge then we are probably in a zero resistance loop
		// and we should stay in out state of emergency. However, if we are not, then the player 
		// has got in quickly enough so can reset our emegency flag
		float currentFlow = GetAbsCurrentFlow();

		
		// Set up the audio
		AudioSource source = gameObject.GetComponent<AudioSource>();
		source.pitch = currentFlow * 0.1f;		
		
		
	}
}
