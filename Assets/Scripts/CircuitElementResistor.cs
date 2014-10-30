using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


public class CircuitElementResistor : CircuitElement {
	public GameObject 	resistorPrefab;
	public GameObject	displayMesh;
	public float		resistance = 1;
	


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
	// For resistors, it is only ok if this is what has been set already
	public override bool CanSetConnection(int dir, bool value){
		return isConnected[dir] == value;
	}	
	
	public override void SetupMesh(){
		int newOrient = 0;
		
		// Just need to check one value to see if we are connected horizontally rather than virtically		
		if (isConnected[1]) newOrient = 1;
		
		if (newOrient != orient){
			orient = newOrient;
			displayMesh.transform.rotation = Quaternion.Euler(0, 0, newOrient * 90);
		}
	
	}	
	
	public override float GetResistance(int dir){
		if (!isConnected[dir]) Debug.LogError("Being asked about a nonexistanct connection");
		return resistance;
	}	
	
	// Use this for initialization
	void Awake () {
		displayMesh = Instantiate(resistorPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
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
