using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


public class CircuitElementResistor : CircuitElement {
	public GameObject 	resistorPrefab;
	public float		resistance = 1;
		

	public void Start(){
		Debug.Log ("CircuitElementResistor:Start()");
	}
	
	public void Awake(){
		CreateDisplayMesh();	
	}

	override public void Save(BinaryWriter bw){
		base.Save (bw);	
		bw.Write(resistance);
	}
	
	
	override public void Load(BinaryReader br){
		base.Load (br);	
		
		resistance = br.ReadSingle();
	}	
	

	public override void RebuildMesh(){
		base.RebuildMesh ();
		GetDisplayMesh ().transform.rotation = Quaternion.Euler(0, 0, orient * 90);
		
		SetupStraightConnectionBehaviour(true);
	}	
	
	public override float GetResistance(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		return resistance;
	}	
	
	
	public override string GetUIString(){
		return "Resistor";
	}
		
	

	
	
	void CreateDisplayMesh(){
		Destroy(GetDisplayMesh ());
		displayMesh = Instantiate(resistorPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
		displayMesh.name = displayMeshName;
		displayMesh.transform.parent = transform;
		
		RebuildMesh ();
	}	
	
	void OnDestroy(){
	}
	
	
	float GetAbsCurrentFlow(){
		if (!IsOnCircuit()) return 0f;
		return  Mathf.Abs (Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 0) + Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 1));
	}
	
	
	// Update is called once per frame
	void Update () {
		HandleDisplayMeshChlid();	
		HandleAlpha();
		

		GetDisplayMesh().transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().value = resistance;
		
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
