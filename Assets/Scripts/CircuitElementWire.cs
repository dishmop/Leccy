using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementWire : CircuitElement {

	public GameObject wirePointPrefab;
	public GameObject wireStraightDownPrefab;
	public GameObject wireDownLeftCornerPrefab;
	public GameObject wireDeadEndDownPrefab;
	public GameObject wireCrossPrefab;
	public GameObject wireTJuncFromTopPrefab;
	
	const float	maxCurrent = 100f;	
		
	GameObject 	currentPrefab;
	
	const int			kLoadSaveVersion = 2;	
	bool				isInEmergency = false;
	
	
	public void Start(){
//		Debug.Log ("CircuitElementWire:Start()");
		
	}
	
	public void Awake(){
		connectionBehaviour[0] = ConnectionBehaviour.kReceptive;
		connectionBehaviour[1] = ConnectionBehaviour.kReceptive;
		connectionBehaviour[2] = ConnectionBehaviour.kReceptive;
		connectionBehaviour[3] = ConnectionBehaviour.kReceptive;
		
		RebuildMesh();
	}
	
	public override bool SuggestBehaviour(int dir, ConnectionBehaviour behaviour, bool honourAnchors){
		// If trying to modify anchored data, then do nothing
		if (thisPoint != null && honourAnchors){
			Circuit.AnchorData data = Circuit.singleton.GetAnchors(thisPoint);
			if (data.isAnchored[dir]) return false;
		}
		connectionBehaviour[dir]= behaviour;
		RebuildMesh();
		Circuit.singleton.OnCircutChange();
		return true;
	}
	
	// Called after an element has been placed on the circuit
	public override void OnPostPlace(){
		// Make any connections we have an invite (since wires are everyone's freind)
		for (int i = 0; i < 4; ++i){
			if (isConnected[i]){
				connectionBehaviour[i] = ConnectionBehaviour.kSociable;
			}
		}
		// If we are not connected and the connection is anchored, then set it to unreceptive
		Circuit.AnchorData data = Circuit.singleton.GetAnchors(thisPoint);
		for (int i = 0; i < 4; ++i){
			if (!isConnected[i] && data.isAnchored[i]){
				connectionBehaviour[i] = ConnectionBehaviour.kUnreceptive;
			}
		}
		
	}
		

	
	// Are we able to set these behaviours?
	public override bool IsAmenableToBehaviour(int dir, ConnectionBehaviour behaviour, bool honourAnchors){
		// If trying to modify anchored data, then do nothing
		if (thisPoint != null && honourAnchors){
			Circuit.AnchorData data = Circuit.singleton.GetAnchors(thisPoint);
			if (data.isAnchored[dir]) return false;
		}
		return true;
	}
	
	
	override public void Save(BinaryWriter bw){
		base.Save (bw);	
		bw.Write (kLoadSaveVersion);
		bw.Write(isInEmergency);
	}
	
	
	
	override public void Load(BinaryReader br){
		base.Load (br);	
		int version = br.ReadInt32();
		switch (version){
			case kLoadSaveVersion:{
					SerializationUtils.UpdateIfChanged(ref isInEmergency, br.ReadBoolean(), ref loadChangedSomething);
				break;
			}
			case 1:{
				// nothing to dp
				break;
			}
		}
	}
	public override void TriggerEmergency(){
		isInEmergency = true;
		Circuit.singleton.OnCircutChange();
	}	
		
	public override void RebuildMesh(){
		base.RebuildMesh();
		
		// Placeholder
		GameObject newPrefab;
		int newOrient = -1;
		
		// if we are in the UI
		if (thisPoint == null){
			newPrefab = wireStraightDownPrefab;
			newOrient = 0;
		}			
		// No connections
		else if (IsSociableOrConnected(false, false, false, false)){
			newPrefab = wirePointPrefab;
			newOrient = 0;
		}
		// 1 Connection
		else if (IsSociableOrConnected(true, false, false, false)){
			newPrefab = wireDeadEndDownPrefab;
			newOrient = 0;
		}
		else if (IsSociableOrConnected(false, true, false, false)){
			newPrefab = wireDeadEndDownPrefab;
			newOrient = 3;
		}
		else if (IsSociableOrConnected(false, false, true, false)){
			newPrefab = wireDeadEndDownPrefab;
			newOrient = 2;
		}
		else if (IsSociableOrConnected(false, false, false, true)){
			newPrefab = wireDeadEndDownPrefab;
			newOrient = 1;
		}
		// 2 connections
		else if (IsSociableOrConnected(true, true, false, false)){
			newPrefab = wireDownLeftCornerPrefab;
			newOrient = 3;
		}	
		else if (IsSociableOrConnected(false, true, true, false)){
			newPrefab = wireDownLeftCornerPrefab;
			newOrient = 2;
		}
		else if (IsSociableOrConnected(false, false, true, true)){
			newPrefab = wireDownLeftCornerPrefab;
			newOrient = 1;
		}	
		else if (IsSociableOrConnected(true, false, false, true)){
			newPrefab = wireDownLeftCornerPrefab;
			newOrient = 0;
		}									
		else if (IsSociableOrConnected(true, false, true, false)){
			newPrefab = wireStraightDownPrefab;
			newOrient = 0;
		}
		else if (IsSociableOrConnected(false, true, false, true)){
			newPrefab = wireStraightDownPrefab;
			newOrient = 1;
		}	
		// 3 connections
		else if (IsSociableOrConnected(true, true, true, false)){
			newPrefab = wireTJuncFromTopPrefab;
			newOrient = 3;
		}		
		else if (IsSociableOrConnected(false, true, true, true)){
			newPrefab = wireTJuncFromTopPrefab;
			newOrient = 2;
		}	
		else if (IsSociableOrConnected(true, false, true, true)){
			newPrefab = wireTJuncFromTopPrefab;
			newOrient = 1;
		}	
		else if (IsSociableOrConnected(true, true, false, true)){
			newPrefab = wireTJuncFromTopPrefab;
			newOrient = 0;
		}
		// 4 connections
		else if (IsSociableOrConnected(true, true, true, true)){
			newPrefab = wireCrossPrefab;
			newOrient = 0;
		}															
		else{
			Debug.LogError ("Unknown junction type");
			// But set up something to draw anyway
			newPrefab = wirePointPrefab;
			newOrient = 0;
		}		
		
		
		if (newPrefab != currentPrefab){
			GameObject dispMesh = GetDisplayMesh();
			GameObject.Destroy(dispMesh);
			currentPrefab = newPrefab;
			displayMesh = Instantiate(currentPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, newOrient * 90)) as GameObject;
			displayMesh.name = displayMeshName;
			displayMesh.transform.parent = transform;
			
			// Need to reset our capflags here too
			hasCapTop = false;
			hasCapRight = false;
			hasCapBottom = false;
			hasCapLeft = false;
			
		}
		if (newOrient != orient){
			orient = newOrient;
			GetDisplayMesh().transform.rotation = Quaternion.Euler(0, 0, newOrient * 90);
		}
		SetColor (isInErrorState ? errorColor : normalColor);
	}	
	
	
	
	public override string GetUIString(){
		return "Wire";
	}	
	
	// Called when mouse is lciked on this. Return true if we changed the object in some way
	public override bool OnClick(){
		return  false;
	}			
	
	
	void Update () {
		HandleColorChange();
		HandleDisplayMeshChlid();	
		

		VisualiseTemperature();
	}

	// Call this before erasing an element	
	public override void RemoveConnections(bool honourAnchors){
		// Suggest to any elements around us that they may want to cancel their invite to us
		// Really only applicable to other wires (we are basically just talking to other wires!)
		if (IsOnCircuit()){
			for (int dir = 0; dir < 4; ++dir){
				GridPoint otherPoint = thisPoint + Circuit.singleton.offsets[dir];
				CircuitElement otherElement = Circuit.singleton.GetElement(otherPoint);
				if (otherElement){
					otherElement.SuggestBehaviour(this, ConnectionBehaviour.kReceptive, honourAnchors);
				}
			}
		}
	}
	
	// called on each element once we have established which elements are connected to which other ones
	// Add Caps on the end if not connected
	public override void PostConnectionAdjstments(){
	
		float[] angles = new float[4];
		angles[0] = 0;
		angles[1] = -90;
		angles[2] = 180;
		angles[3] = 90;

		// Top Cap
		bool needCapTop = IsSociableOrConnected(Circuit.kUp, true) && !isConnected[Circuit.kUp];
		if (needCapTop && !hasCapTop){
			GameObject topCap = Instantiate(capPrefab) as GameObject;
			topCap.transform.parent = GetDisplayMesh().transform;
			topCap.transform.localPosition = Vector3.zero;
			topCap.transform.rotation = Quaternion.identity;
			topCap.name = "TopCap";
			hasCapTop = true;
		}
		else if (!needCapTop && hasCapTop){
			GameObject topCap = GetDisplayMesh().transform.FindChild("TopCap").gameObject;
			Destroy (topCap);
			hasCapTop = false;
		}
		
		// Right cap
		bool needCapRight = IsSociableOrConnected(Circuit.kRight, true) && !isConnected[Circuit.kRight];
		if (needCapRight && !hasCapRight){
			GameObject rightCap = Instantiate(capPrefab) as GameObject;
			rightCap.transform.parent = GetDisplayMesh().transform;
			rightCap.transform.localPosition = Vector3.zero;
			rightCap.transform.rotation = Quaternion.Euler(0, 0, -90);
			rightCap.name = "RightCap";
			hasCapRight = true;
		}
		else if (!needCapRight && hasCapRight){
			GameObject rightCap = GetDisplayMesh().transform.FindChild("RightCap").gameObject;
			Destroy (rightCap);
			hasCapRight = false;
		}		
		
		// Bottom Cap
		bool needCapBottom = IsSociableOrConnected(Circuit.kDown, true) && !isConnected[Circuit.kDown];
		if (needCapBottom && !hasCapBottom){
			GameObject bottomCap = Instantiate(capPrefab) as GameObject;
			bottomCap.transform.parent = GetDisplayMesh().transform;
			bottomCap.transform.localPosition = Vector3.zero;
			bottomCap.transform.rotation = Quaternion.Euler(0, 0, 180);
			bottomCap.name = "BottomCap";
			hasCapBottom = true;
		}
		else if (!needCapBottom && hasCapBottom){
			GameObject bottomCap = GetDisplayMesh().transform.FindChild("BottomCap").gameObject;
			Destroy (bottomCap);
			hasCapBottom = false;
		}
		
		// Left cap
		bool needCapLeft = IsSociableOrConnected(Circuit.kLeft, true) && !isConnected[Circuit.kLeft];
		if (needCapLeft && !hasCapLeft){
			GameObject leftCap = Instantiate(capPrefab) as GameObject;
			leftCap.transform.parent = GetDisplayMesh().transform;
			leftCap.transform.localPosition = Vector3.zero;
			leftCap.transform.rotation = Quaternion.Euler(0, 0, 90);
			leftCap.name = "LeftCap";
			hasCapLeft = true;
		}
		else if (!needCapLeft && hasCapLeft){
			GameObject leftCap = GetDisplayMesh().transform.FindChild("LeftCap").gameObject;
			Destroy (leftCap);
			hasCapLeft = false;
		}	

	}
	
	
	void OnDestroy() {
		// When the object is destoryed, we must make sure to dispose of any meshes we may have
//		Debug.Log ("OnDestroy");
		
		
	}	
	
	
	
	float GetAbsCurrentFlow(){
		if (!IsOnCircuit()) return 0f;
		return  Mathf.Abs (Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 0)) + 
				Mathf.Abs (Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 1)) + 
				Mathf.Abs (Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 2)) + 
				Mathf.Abs (Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 3));
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
			temperature += UnityEngine.Random.Range(0, 200) * 0.01f;
		}
		else{
			if (temperature > 0){
				temperature -= 1;
			}
			else{
				temperature = 0;
			}
		}
		// If we reach our maximum temperature then we should remove the component
		if (temperature > maxTemp){
			DestorySelf();
		}
		
		
	}	
	


}
