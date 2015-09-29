﻿using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


public class CircuitElementAmmeter : CircuitElement {
	public GameObject 	ammeterPrefab;
	public GameObject	triggerEffect;
	public float		targetAmp = 1;
	public bool			hasTarget = true;
	public Color		textColorNorm;
	public Color		textColorTarget;
	public Color 		signColorNorm;
	public Color 		signColorTarget;
	
	
	public bool			buttonActivated = false;
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
		bw.Write (targetAmp);
		bw.Write (hasTarget);
		bw.Write (buttonActivated);		
		
	}
	
	
	public override void Load(BinaryReader br){
		base.Load (br);	
		int version = br.ReadInt32();
		switch (version){
			case kLoadSaveVersion:{
				SerializationUtils.UpdateIfChanged(ref targetAmp, br.ReadSingle (), ref loadChangedSomething);
				SerializationUtils.UpdateIfChanged(ref hasTarget, br.ReadBoolean (), ref loadChangedSomething);
				SerializationUtils.UpdateIfChanged(ref buttonActivated, br.ReadBoolean(), ref loadChangedSomething);
				break;
			}
			case 1:{
				SerializationUtils.UpdateIfChanged(ref targetAmp, br.ReadSingle (), ref loadChangedSomething);
				SerializationUtils.UpdateIfChanged(ref hasTarget, br.ReadBoolean (), ref loadChangedSomething);
				break;
			}	
			
		}
	}	
	
	
	public override string GetUIString(){
		return "Ammeter";
	}	
	
	
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
	
//	// Return true if it is ok to set this connection on this element
//	// For resistors, it is only ok if this is what has been set already
//	public override bool CanSetConnection(int dir, bool value){
//		return isConnected[dir] == value;
//	}	
	
	public override void RebuildMesh(){
		base.RebuildMesh ();
		GetDisplayMesh().transform.rotation = Quaternion.Euler(0, 0, orient * 90);
		SetupStraightConnectionBehaviour(true);
		SetColor (isInErrorState ? errorColor : normalColor);
		SetupColorsAndText();		
	}	
	
	public override float GetResistance(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		return 0.00001f;
	}	
	
	void CreateDisplayMesh(){
		Destroy(GetDisplayMesh ());
		displayMesh = Instantiate(ammeterPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
		displayMesh.name = displayMeshName;
		displayMesh.transform.parent = transform;
		RebuildMesh();
	}
	
	void OnDestroy(){
		if (hasTarget && GameModeManager.singleton != null && IsOnCircuit()) GameModeManager.singleton.UnregisterLevelTrigger();
	}
	
	
	public bool IsOnTarget(){
		return hasTarget && MathUtils.FP.Feq(GetMaxCurrent(), targetAmp, FractionCalc.epsilon);
	}
	
	
	void TriggerTargetEffect(){
		Transform targetText = GetDisplayMesh().transform.FindChild ("TargetText");
		GameObject effect = GameObject.Instantiate(triggerEffect, targetText.position, targetText.rotation) as GameObject;
		effect.transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().value = GetMaxCurrent();
		effect.transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().color = targetText.gameObject.GetComponent<FractionCalc>().color;
		effect.transform.parent = transform;
	
	}
	
	// called on each element once we have established which elements are connected to which other ones
	// Add Caps on the end if not connected
	public override void PostConnectionAdjstments(){
		DoStraightConnectionAdjustments();
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
				float time = Time.fixedTime;
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
			if (hasTarget){
				GameModeManager.singleton.RegisterLevelTrigger();
			}
			else{
				GameModeManager.singleton.UnregisterLevelTrigger();
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
		float pulse = 0.5f + 0.5f *  Mathf.Cos (2 * 3.14159265f * (Time.fixedTime - 0.2f));
		
		foreach (Transform child in GetDisplayMesh().transform){
			if (child.name == "TargetText"){
				child.gameObject.GetComponent<FractionCalc>().value = targetAmp;
				child.gameObject.GetComponent<FractionCalc>().color = textColorTarget;
				child.gameObject.SetActive(hasTarget);
				
				
			}
			
			if (child.name == "ActualText"){
				child.gameObject.GetComponent<FractionCalc>().value = GetMaxCurrent ();
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
