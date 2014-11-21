using UnityEngine;
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
	
	

	bool				wasOnTarget = false;
	
	float pulse = 0;
	
	// So we can see if it gets changed (esp via the inspector)
	bool 	prevHasTarget = false;
	

	public void Start(){
		Debug.Log ("CircuitElementAmpMeter:Start()");
		CreateDisplayMesh();	
	}
	
	// Call this if instantiating an inactive version
	public override void InactveStart(){
		CreateDisplayMesh();	
	}	
	
	public override void Save(BinaryWriter bw){
		base.Save (bw);	
		bw.Write (targetAmp);
		bw.Write (hasTarget);
		
	}
	
	
	public override void Load(BinaryReader br){
		base.Load (br);	
		targetAmp = br.ReadSingle ();
		hasTarget = br.ReadBoolean();
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
	}	
	
	public override float GetResistance(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		return 0.00001f;
	}	
	
	void CreateDisplayMesh(){
		Destroy(GetDisplayMesh ());
		GameObject displayMesh = Instantiate(ammeterPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
		displayMesh.name = displayMeshName;
		displayMesh.transform.parent = transform;
		RebuildMesh();
	}
	
	void OnDestroy(){
		if (hasTarget) OldUI.singleton.UnregisterLevelTrigger();
	}
	
	bool IsOnTarget(){
		return hasTarget && MathUtils.FP.Feq(GetMaxCurrent(), targetAmp);
	}
	
	
	void TriggerTargetEffect(){
		Transform actualText = GetDisplayMesh().transform.FindChild ("ActualText");
		GameObject effect = GameObject.Instantiate(triggerEffect, actualText.position, actualText.rotation) as GameObject;
		effect.transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().value = GetMaxCurrent();
		effect.transform.FindChild("FractionTextBox").GetComponent<FractionCalc>().color = actualText.gameObject.GetComponent<FractionCalc>().color;
		effect.transform.parent = transform;
	
	}
	

	
	// Update is called once per frame
	void Update () {
		HandleAlpha();
		pulse = 0.5f + 0.5f *  Mathf.Sin (10 * Time.realtimeSinceStartup);
		
		if (wasOnTarget != IsOnTarget() && IsOnTarget()){
			TriggerTargetEffect();
						
		}
		wasOnTarget = IsOnTarget();
				
		if (hasTarget != prevHasTarget){
			prevHasTarget = hasTarget;
			if (hasTarget){
				OldUI.singleton.RegisterLevelTrigger();
			}
			else{
				OldUI.singleton.UnregisterLevelTrigger();
			}
		}
		
		
		foreach (Transform child in GetDisplayMesh().transform){
			if (child.name == "TargetText"){
				child.gameObject.GetComponent<FractionCalc>().value = targetAmp;
				child.gameObject.GetComponent<FractionCalc>().color = textColorTarget;
				child.gameObject.SetActive(hasTarget);
							
//				TextMesh textMesh = child.gameObject.GetComponent<TextMesh>();
//				textMesh.text = targetAmp.ToString("0.00") + "A";
//				textMesh.color = textColorTarget;
//				child.gameObject.SetActive(hasTarget);
			}
			if (child.name == "Bomb"){
				// Don;t draw the bomb for the moment
				child.gameObject.SetActive(false);
			}
			if (child.name == "ActualText"){
				child.gameObject.GetComponent<FractionCalc>().value = GetMaxCurrent ();
				child.gameObject.GetComponent<FractionCalc>().color = IsOnTarget() ? textColorTarget : textColorNorm;
				
				/*
				TextMesh textMesh = child.gameObject.GetComponent<TextMesh>();
				if (GetMaxCurrent() < 100){
					textMesh.text = GetMaxCurrent().ToString("0.00");
					textMesh.fontSize = 24;
				}
				else{
					textMesh.text = "∞";
					textMesh.fontSize = 48;
				}
				textMesh.color = IsOnTarget() ? textColorTarget : textColorNorm;
				*/
				
			}		
			if (child.name == "SignPanel"){
				MeshRenderer mesh = child.gameObject.GetComponent<MeshRenderer>();
				Color useCol = signColorNorm;
				if (IsOnTarget()){
					useCol = Color.Lerp(signColorNorm, signColorTarget, pulse);
				}
				
				mesh.materials[0].SetColor ("_Color",  useCol);
				
			}						
			
//				
//				float current = Mathf.Abs (Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 0) + Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 1));
//				Color useCol = offCol;
//				if (current > 0.1f) useCol = halfOnCol;
//				if (current >= thresholdCurrent) useCol = onCol;
//				mesh.materials[0].SetColor ("_Color", useCol);

		}
	
		
		VisualiseTemperature();
		
		// Let the UI know if we have been succesfully acitavted
		if (IsOnTarget()){
			OldUI.singleton.TriggerComplete();
		}
		
		

		
	}

}
