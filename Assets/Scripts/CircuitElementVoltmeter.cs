using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


public class CircuitElementVoltmeter : CircuitElement {
	public GameObject 	voltmeterPrefab;
	public GameObject	triggerEffect;
	public float		targetVolts = 1;
	public bool			hasTarget = true;
	public Color		textColorNorm;
	public Color		textColorTarget;
	public Color 		signColorNorm;
	public Color 		signColorTarget;
	
	

	bool				buttonActivated = false;
	bool 				hasTriggered = false;		
	
	// So we can see if it gets changed (esp via the inspector)
	bool 	prevHasTarget = false;
	
	const int		kLoadSaveVersion = 2;	
	
	public void Start(){
//		Debug.Log ("CircuitElementAmpMeter:Start()");
	}
	
	public void Awake(){
		CreateDisplayMesh();	
	}
	
	
	public override void Save(BinaryWriter bw){
		base.Save (bw);	
		bw.Write (kLoadSaveVersion);
		bw.Write (targetVolts);
		bw.Write (hasTarget);
		bw.Write (buttonActivated);
		
	}
	
	
	public override void Load(BinaryReader br){
		base.Load (br);	
		int version = br.ReadInt32();
		switch (version){
			case kLoadSaveVersion:{
				SerializationUtils.UpdateIfChanged(ref targetVolts, br.ReadSingle (), ref loadChangedSomething);
				SerializationUtils.UpdateIfChanged(ref hasTarget, br.ReadBoolean(), ref loadChangedSomething);
				SerializationUtils.UpdateIfChanged(ref buttonActivated, br.ReadBoolean(), ref loadChangedSomething);
				break;
			}			
			case 1:{
				SerializationUtils.UpdateIfChanged(ref targetVolts, br.ReadSingle (), ref loadChangedSomething);
				SerializationUtils.UpdateIfChanged(ref hasTarget, br.ReadBoolean(), ref loadChangedSomething);
				break;
			}
		}
	}	
	
	
	public override string GetUIString(){
		return "Voltmeter";
	}	
	
	// Only return true if this component cannot actualy conduct electiricty at all
	
	// Hmm this doesn't work because we need the voltage to leak over to the other connection if it is not connected to anything sink
	public override bool IsInsulator(){
		return true;
	}	
	
	public override float GetResistance(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		
		// Only return a resistance if we are being asked about the spoke that the resistance is on
		if (dir == ModelDir2WorldDir(Circuit.kUp)){
			return 100000f;
		}
		return 0f;
	}

	public override void RebuildMesh(){
		base.RebuildMesh ();
		GetDisplayMesh().transform.rotation = Quaternion.Euler(0, 0, orient * 90);
		SetupStraightConnectionBehaviour(true);
		SetColor (isInErrorState ? errorColor : normalColor);
		SetupColorsAndText();			
	}	
	
	void CreateDisplayMesh(){
		Destroy(GetDisplayMesh ());
		displayMesh = Instantiate(voltmeterPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
		displayMesh.name = displayMeshName;
		displayMesh.transform.parent = transform;
		RebuildMesh();
	}
	
	void OnDestroy(){
		if (hasTarget && GameModeManager.singleton != null && IsOnCircuit()) GameModeManager.singleton.UnregisterLevelTrigger();
	}
	
	bool IsOnTarget(){
		// our epsilon should be the same as the display epsilon
		return hasTarget && MathUtils.FP.Feq(GetVoltageDiff(), targetVolts, FractionCalc.epsilon);
	}
	
	
	void TriggerTargetEffect(){
		Transform actualText = GetDisplayMesh().transform.FindChild ("TargetText");
		GameObject effect = GameObject.Instantiate(triggerEffect, actualText.position, actualText.rotation) as GameObject;
		effect.transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().value = GetVoltageDiff();
		effect.transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().color = actualText.gameObject.GetComponent<FractionCalc>().color;
		effect.transform.parent = transform;
	
	}
	
	// called on each element once we have established which elements are connected to which other ones
	// Add Caps on the end if not connected
	public override void PostConnectionAdjstments(){
		DoStraightConnectionAdjustments();
	}
	
	
	public float GetVoltageDiff(){
		if (thisPoint != null && IsOnCircuit()){
			int upDir =  ModelDir2WorldDir(Circuit.kUp);
			int downDir =  ModelDir2WorldDir(Circuit.kDown);
			
			// only return a difference if both branches hav bee traversed
			if (Simulator.singleton.IsTraversed(thisPoint.x, thisPoint.y, upDir) && Simulator.singleton.IsTraversed(thisPoint.x, thisPoint.y, downDir)){
			
				return Mathf.Abs(Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, upDir) - 
				                 Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, downDir));
			}
		}
		return 0f;
	
	}
	

	
	// Update is called once per frame
	void Update () {
		HandleDisplayMeshChlid();	
		HandleColorChange();
		
		HandleAudio();
		SetupColorsAndText();
	}
	
	
	
	void HandleAudio(){
		bool shouldPlay = (IsOnTarget() && !buttonActivated);
		// The audio should be playing all the time and we should start it on a whole number of seconds
		if (shouldPlay != GetComponent<AudioSource>().isPlaying){
			if (shouldPlay){
				float time = Time.realtimeSinceStartup;
				int lastWhole = Mathf.FloorToInt(time);
				float timeToNextWhole = lastWhole + 1 - time;
				GetComponent<AudioSource>().PlayDelayed(timeToNextWhole);
			}
			else{
				GetComponent<AudioSource>().Stop();
			}
		}
	}
		
	public override void GameUpdate(){
		if (hasTarget != prevHasTarget){
			prevHasTarget = hasTarget;
			if (IsOnCircuit()){
				if (hasTarget){
					GameModeManager.singleton.RegisterLevelTrigger();
				}
				else{
					GameModeManager.singleton.UnregisterLevelTrigger();
				}
			}
		}
		
		
		
		
		// Let the UI know if we have been succesfully acitavted and fire the effect
		if (!hasTriggered && IsOnTarget() && buttonActivated){
			TriggerTargetEffect();
			hasTriggered = true;
		}
		
		if (IsOnTarget() && buttonActivated){
			GameModeManager.singleton.TriggerComplete();
		}

		
		if (!IsOnTarget()){
			hasTriggered = false;
			buttonActivated = false;
		}
	}
	
	void SetupColorsAndText(){
		float pulse = 0.5f + 0.5f *  Mathf.Cos (2 * 3.14159265f * (Time.realtimeSinceStartup - 0.2f));
		
		foreach (Transform child in GetDisplayMesh().transform){
			if (child.name == "TargetText"){
				child.gameObject.GetComponent<FractionCalc>().value = targetVolts;
				child.gameObject.GetComponent<FractionCalc>().color = textColorTarget;
				child.gameObject.SetActive(hasTarget);
				
				
			}
			
			if (child.name == "ActualText"){
				child.gameObject.GetComponent<FractionCalc>().value = GetVoltageDiff ();
				child.gameObject.GetComponent<FractionCalc>().color = IsOnTarget() ? textColorTarget : textColorNorm;
			}		
			if (child.name == "SignPanel"){
				MeshRenderer mesh = child.gameObject.GetComponent<MeshRenderer>();
				Color useCol = signColorNorm;
				if (IsOnTarget() && !buttonActivated){
					useCol = Color.Lerp(signColorNorm, signColorTarget, pulse);
				}
				else if (IsOnTarget() && buttonActivated || !hasTarget){
					useCol = signColorTarget;
				}				
				
				mesh.materials[0].SetColor ("_Color",  useCol);
				
			}						
			
		}
		
		
		VisualiseTemperature();
	}
	
	// return true if this component is only available in the editor
	public override bool IsEditorOnly(){
		return true;
	}	
	
	
	public override   void OnMouseOver() {
		
		if (IsOnTarget()){
			UI.singleton.HideMousePointer();
		}
		
	}	
	
	public override void OnMouseDown() {
		if (IsOnTarget() && !hasTriggered){
			buttonActivated = true;
			Circuit.singleton.OnCircutChange();
			
		}
		
	}		
	
}
