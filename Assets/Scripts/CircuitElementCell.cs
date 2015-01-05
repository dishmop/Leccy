using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementCell : CircuitElement {

	public GameObject 	cellPrefab;
	public float		voltage = 1f;

	bool				isInEmergency = false;
	const float	normalResistance = 0f;
	const float	emergencyResistance = 0.001f;
	const float	maxCurrent = 100f;

	const int		kLoadSaveVersion = 1;		
	
	
	
	public void Start(){
//		Debug.Log ("CircuitElementCell:Start()");
	}
	
	public void Awake(){
		CreateDisplayMesh();	
	}

	public override void Save(BinaryWriter bw){
		base.Save (bw);	
		
		bw.Write (kLoadSaveVersion);
		bw.Write (voltage);
		bw.Write(isInEmergency);
	}
	
	
	public override void Load(BinaryReader br){
		base.Load (br);	
		
		int version = br.ReadInt32();
		switch (version){
			case kLoadSaveVersion:{
				SerializationUtils.UpdateIfChanged(ref voltage, br.ReadSingle(), ref loadChangedSomething);
				SerializationUtils.UpdateIfChanged(ref isInEmergency, br.ReadBoolean(), ref loadChangedSomething);
				break;
			}
		}
	}
	
	public override void TriggerEmergency(){
		isInEmergency = true;
		Circuit.singleton.OnCircutChange();
	}	
	
	
	public override string GetUIString(){
		return "Cell";
	}	

	
	public override float GetResistance(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		return isInEmergency ? emergencyResistance : normalResistance;
	}
	
	public override float GetVoltageDrop(int dir, bool fwd){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		
		// Ony return a value if we are being asked about the spoke that the voltage drop is on
		if (dir == ModelDir2WorldDir(Circuit.kUp)){
			return fwd ? voltage : -voltage;
		}
		return 0f;
	}	
	

	public override float GetUnconnectedVoltage(int dir){
		// Ony return a value if we are being asked about the spoke that the voltage drop is on
		if (dir == ModelDir2WorldDir(Circuit.kUp)){
			return voltage;
		}
		return 0f;
	}
	
	
	

	public override void RebuildMesh(){
		base.RebuildMesh();
		GetDisplayMesh().transform.rotation = Quaternion.Euler(0, 0, orient * 90);

		SetupStraightConnectionBehaviour(true);
		SetColor (isInErrorState ? errorColor : normalColor);		
	}	
	


	// called on each element once we have established which elements are connected to which other ones
	// Add Caps on the end if not connected
	public override void PostConnectionAdjstments(){
		DoStraightConnectionAdjustments();
	}
	


	
	void CreateDisplayMesh(){
		Destroy(GetDisplayMesh ());
		displayMesh = Instantiate(cellPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
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
		HandleDisplayMeshChlid();
		HandleColorChange();
		
		GetDisplayMesh().transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().value = voltage;
		
		VisualiseTemperature();
		
		// Set up the audio
		float currentFlow = GetAbsCurrentFlow();
		
		AudioSource source = gameObject.GetComponent<AudioSource>();

		source.pitch = currentFlow * 0.1f * Time.timeScale;
		// Mute if the pitch is zero
		source.mute = MathUtils.FP.Feq (source.pitch , 0);
		
		
	}
	
	
	public override void GameUpdate(){
		
		// If our current is not huge then we are probably in a zero resistance loop
		// and we should stay in out state of emergency. However, if we are not, then the player 
		// has got in quickly enough so can reset our emegency flag
		float currentFlow = GetAbsCurrentFlow();
		if (isInEmergency && currentFlow < maxCurrent){
			Debug.Log ("Resetting emergency state");
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
		}

		
	}
	
	
}
	