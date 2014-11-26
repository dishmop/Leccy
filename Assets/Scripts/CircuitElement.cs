using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;




public class CircuitElement : MonoBehaviour {
	
	public int			orient = 0;			// In 90 degree steps anti-clockwise
	public GameObject	anchorCentralPrefab;
	public GameObject	anchorBranchPrefab;
	public GameObject	capPrefab;
	public GameObject	emptyGO;
	
	public Color		normalColor;
	public Color		errorColor;
	
	protected GameObject	displayMesh;
	protected GameObject	anchorMesh;
	
	
	protected float			temperature = 0;
	
	bool			isOnCircuit = false;
	
	// For setting alpha and color values
	float 	alpha = 1f;
	bool 	dirtyAlpha = false;
	Color 	color = new Color(1f, 1f, 1f);
	bool 	dirtyColor = false;	
	
	// Used to put caps on sriaght components
	bool				hasCapTop = false;
	bool				hasCapBottom = false;	
	

		// What is the best UI scheme to use when placing these elents
	public enum UIType{
		kNone,
		kDraw,			// Lay them out in lines like drawing with a pen
		kPlace,			// Place sinlge elements down one at a time
		kModify,		// Change the elements which are there (e.g. remove them or add anchors)
		kNumTypes   	// Useufl for iterating over the,
	};
	
	public UIType uiType = UIType.kNone;
	
	protected string	displayMeshName = "DisplayMesh";


	public void SetAlpha(float a){
		dirtyAlpha = true;
		alpha = a;
	}
	
	public void SetColor(Color col){
		dirtyColor = true;
		color = col;
	}

	
			// These enums describe the connection situation in a given direction
	public enum ConnectionBehaviour{ 
		kUnreceptive,   // If invited to make a connection, I will say no
		kReceptive,		// If invited to make one, I wil say yes
		kSociable,		// I will invite anyone to connect here
	};
	
	
	protected void HandleColorChange(){
	
		if (dirtyAlpha){
			dirtyAlpha = false;
			ImplementAlpha(gameObject, alpha);
		}
		if (dirtyColor){
			dirtyColor = false;
			Color thisCol = color;
			thisCol.a = alpha;
			ImplementColor(gameObject, thisCol);
		}
	}	
	
	
	
	// 0 - up
	// 1 - right
	// 2 - down
	// 3  - left 
	public ConnectionBehaviour[] connectionBehaviour = new ConnectionBehaviour[4];
	public bool[] isAnchored = new bool[5];	// if true, then cannot be changed in the editor
	public bool[] isConnected = new bool[4];
	
	

	protected GridPoint		thisPoint;
	protected bool 			isInErrorState = false;
	
	protected float 		maxTemp = 45f;

	GameObject[]			anchors = new GameObject[5];
	
	
	
	public void SetErrorState(bool isError){
		isInErrorState = isError;

		RebuildMesh();
	}
	
	
//	// Generally is any of our connections are baked, then the component itself is also baked
//	public bool IsComponentBaked(){
//		for (int i = 0; i < 4; ++i){
//			if (isAnchored[i]) return true;
//		}
//		return false;
//	}
	
	public virtual void TriggerEmergency(){
		
		
	}
	
	// Return true of this element is one which is attached to wires (most are)
	public virtual bool IsWired(){
		return true;
	}
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	public virtual bool CanModify(GridPoint thisPt, GridPoint otherPt){
		return false;
	}
	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public virtual bool Modify(GridPoint thisPt, GridPoint otherPt){
		return false;
	}	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public virtual bool Modify(GridPoint thisPt){
		return false;
	}		
	
	// The prefab to use in the UI (each element may have several meshes - need to just show one in the UI)
	public  GameObject GetDisplayMesh(){
		return displayMesh;
//		Transform dispTrans = transform.FindChild(displayMeshName);
//		return dispTrans ? dispTrans.gameObject : null;
	}	
	
	public virtual string GetUIString(){
		return "None";
	}
	
	// Some elements have the notion of another grid point which they use for UI purposes
	public virtual void SetOtherGridPoint(GridPoint otherPoint){
	}
	
	public void SetGridPoint(GridPoint thisPoint){
		this.thisPoint = thisPoint;
		if (thisPoint != null){
			transform.position = new Vector3(thisPoint.x, thisPoint.y, transform.position.z);
		}
	}
	
	// can override the z position
	public void SetGridPoint(GridPoint thisPoint, float z){
		this.thisPoint = thisPoint;
		if (thisPoint != null){
			transform.position = new Vector3(thisPoint.x, thisPoint.y, z);
		}
	}
	
	public GridPoint GetGridPoint(){
		return thisPoint;
	}
	
	public virtual void Save(BinaryWriter bw){
		// This is calculated from connections and is assumed ot match up with orientation of mesh - so don't store
		////		bw.Write (orient);
		for (int i = 0; i < 4; ++i){
			bw.Write ((int)connectionBehaviour[i]);
		}
		for (int i = 0; i < 5; ++i){
			bw.Write(isAnchored[i]);
		}
		bw.Write (temperature);
	}
	
	public 	virtual void Load(BinaryReader br){
		// This is calculated from connections and is assumed ot match up with orientation of mesh - so don't store
		//		orient = br.ReadInt32();
		for (int i = 0; i < 4; ++i){
			connectionBehaviour[i] = (ConnectionBehaviour)br.ReadInt32();
		}
		for (int i = 0; i < 5; ++i){
			isAnchored[i] = br.ReadBoolean();
		}
		temperature = br.ReadSingle();
	}
	
	public virtual void PostLoad(){
		RebuildMesh ();
	}
	
	// called on each element once we have established which elements are connected to which other ones
	public virtual void PostConnectionAdjstments(){
	}
	
	protected void HandleDisplayMeshChlid()
	{
	
		foreach (Transform child in transform){
			if (child.gameObject.name == displayMeshName && child.gameObject != displayMesh){
				Destroy (child.gameObject);
			}
			
		}
	}
	
	protected int ModelDir2WorldDir(int modelDir){
		return (modelDir + 6-orient) % 4;
	}
	
	public virtual bool SuggestInvite(CircuitElement otherElement){
		return false;
	}
	
	
	public virtual bool SuggestUninvite(CircuitElement otherElement){
		return false;
	}
	
	
	// Whether the previous two functions would return true if called
	public virtual bool IsAmenableToSuggestion(CircuitElement otherElement){
		return false;
	}
		
	
	public virtual int CalcHash(){
		return  (isAnchored[0] ? 1 << 0 : 0) + 
			   	(isAnchored[1] ? 1 << 1 : 0) + 
				(isAnchored[2] ? 1 << 2 : 0) + 
				(isAnchored[3] ? 1 << 3 : 0) + 
				(isAnchored[4] ? 1 << 4 : 0) + 
				(isConnected[0] ?  1 << 5 : 0) + 
				(isConnected[1] ?  1 << 6 : 0) + 
				(isConnected[2] ?  1 << 7 : 0) + 
				(isConnected[3] ?  1 << 8 : 0) + 
				orient * 1 << 9;
	}
	
	
// 	int CalcBakedHash(){
//		return (isBaked[0] ? 1 : 0) + (isBaked[1]  ? 2 : 0) + (isBaked[2] ? 4 : 0) + (isBaked[3] ? 8 : 0);
//	}

	public bool IsConnected(int dir){
		return isConnected[dir];
	}
	
		
	
	void RebuildAnchorMeshes(){
		// Destory all the existing Anchor meshes
		for (int i = 0; i < 5; ++i){
			GameObject.Destroy(anchors[i]);
		}
		GameObject.Destroy(anchorMesh);
		
		anchorMesh = GameObject.Instantiate(emptyGO, transform.position, transform.rotation) as GameObject;
		anchorMesh.transform.parent = transform;
		
		if (isAnchored[Circuit.kCentre]){
			anchors[Circuit.kCentre] = Instantiate(
			anchorCentralPrefab, 
			new Vector3(transform.position.x, transform.position.y, anchorCentralPrefab.transform.position.z),
			Quaternion.identity) as GameObject;
			anchors[Circuit.kCentre].transform.parent = anchorMesh.transform;
			
		}
		float[] angles = new float[4];
		angles[0] = 0;
		angles[1] = -90;
		angles[2] = 180;
		angles[3] = 90;
		for (int i = 0; i < 4; ++i){
			if (isAnchored[i]){
				anchors[i] = Instantiate(
					anchorBranchPrefab, 
					new Vector3(transform.position.x, transform.position.y, anchorCentralPrefab.transform.position.z),
					Quaternion.Euler(0, 0, angles[i])) as GameObject;
				anchors[i].transform.parent = anchorMesh.transform;
				
			}
		}
			
		return;
	
	}
		
		



		
//	// Return true if it is ok to set this connection on this element
//	public virtual bool CanSetConnection(int dir, bool value){
//		return !isBaked[dir] || isConnected[dir] == value;
//	}
	
//	

	protected void DoStraightConnectionAdjustments(){
		bool needCapTop = !isConnected[ModelDir2WorldDir(Circuit.kDown)];
		if (needCapTop && !hasCapTop){
			GameObject topCap = Instantiate(capPrefab) as GameObject;
			topCap.transform.parent = GetDisplayMesh().transform;
			topCap.transform.localPosition = Vector3.zero;
			topCap.transform.localRotation = Quaternion.identity;
			topCap.name = "TopCap";
			hasCapTop = true;
		}
		else if (!needCapTop && hasCapTop){
			GameObject topCap = GetDisplayMesh().transform.FindChild("TopCap").gameObject;
			Destroy (topCap);
			hasCapTop = false;
		}
		
		
		bool needCapBottom = !isConnected[ModelDir2WorldDir(Circuit.kUp)];
		if (needCapBottom && !hasCapBottom){
			GameObject bottomCap = Instantiate(capPrefab) as GameObject;
			bottomCap.transform.parent = GetDisplayMesh().transform;
			bottomCap.transform.localPosition = Vector3.zero;
			bottomCap.transform.localRotation = Quaternion.Euler(0, 0, 180);
			bottomCap.name = "BottomCap";
			hasCapBottom = true;
		}
		else if (!needCapBottom && hasCapBottom){
			GameObject bottomCap = GetDisplayMesh().transform.FindChild("BottomCap").gameObject;
			Destroy (bottomCap);
			hasCapBottom = false;
		}
		
	}	
	public bool HasConnections(bool up, bool right, bool down, bool left){
		return 	IsConnected(0) == up &&
		        IsConnected(1) == right &&
				IsConnected(2) == down &&
				IsConnected(3) == left;
	}


	public void Rotate(int stepsCW){
		orient = (4 + orient + stepsCW) % 4;
		RebuildMesh();
		
	}
	
	public virtual float GetResistance(int dir){
		if (!IsConnected(dir)) Debug.LogError("Being asked about a nonexistanct connection");
		return 0f;
	}
	
	// Called when mouse is lciked on this. Return true if we changed the object in some way
	public virtual bool OnClick(){
		Rotate (1);
		return  true;
	}
	
	// Decide if we should call OnClick*I( if we are clicked on with the selectd prefab
	public virtual bool ShouldClick(GameObject selectionPrefab){
		// IF a different kind of prefab - then don;t click
		if (GetComponent<SerializationID>().id != selectionPrefab.GetComponent<SerializationID>().id){
			return false;
		}
		// if it is the same kind of prefab, but a different oreientation then don't click
		if (selectionPrefab.GetComponent<CircuitElement>().orient != orient){
			return false;
		}
		return true;
		
	}
	
	// Voltage drop when solving for currents (Terrible hack!)
	public virtual float GetVoltageDrop(int dir, bool fw){
		if (!IsConnected(dir)){
			Debug.LogError("Being asked about a nonexistanct connection");
		}
		return 0f;
	}
	
	public virtual float GetUnconnectedVoltage(int dir){
		return 0f;
	}

	
	public static int CalcInvDir(int dir){
		return (dir + 2) % 4;
	}


	public virtual void RebuildMesh(){
		HandleDisplayMeshChlid();
		RebuildAnchorMeshes();
		
		// Ensure any colours get applied correctly afterwards
		dirtyAlpha = true;
		dirtyColor = true;
	}
	

		protected void VisualiseTemperature(){
		foreach (Transform child in transform.GetChild(0)){
			MeshRenderer mesh = child.gameObject.transform.GetComponent<MeshRenderer>();
			if (mesh != null) mesh.materials[0].SetFloat ("_Temperature", temperature / maxTemp );
		}		
		
	}	
	
	protected void DestorySelf(){
		Debug.Log("Destroy self : " + GetComponent<SerializationID>().id);
		isAnchored[0] = false;
		isAnchored[1] = false;
		isAnchored[2] = false;
		isAnchored[3] = false;
		RemoveConnections();
		Circuit.singleton.RemoveElement(thisPoint);
		Circuit.singleton.TriggerExplosion(thisPoint);
		Destroy (gameObject);
	}
	
	
	public void SetIsOnCircuit(bool isOn){
		isOnCircuit = isOn;
	}
	
	protected bool IsOnCircuit(){
		return isOnCircuit;
	}
	
	
	// Return the maximum voltage difference accross all connections
	public float GetMaxVoltage(){
		if (!IsOnCircuit()) return 0f;
		return Mathf.Max(
			Mathf.Max(
				Mathf.Abs(Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, 0)), 
				Mathf.Abs(Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, 1))),
			Mathf.Max(
				Mathf.Abs(Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, 2)), 
				Mathf.Abs(Simulator.singleton.GetVoltage(thisPoint.x, thisPoint.y, 3))));
	}
	
	
	public float GetMaxCurrent(){
		if (!IsOnCircuit()) return 0f;
		return Mathf.Max(
			Mathf.Max(
				Mathf.Abs(Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 0)), 
				Mathf.Abs(Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 1))),
			Mathf.Max(
				Mathf.Abs(Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 2)), 
				Mathf.Abs(Simulator.singleton.GetCurrent(thisPoint.x, thisPoint.y, 3))));	
	}	
	
	protected void ImplementAlpha(GameObject obj, float alpha){
		Renderer rend = obj.renderer;
		if (rend){
			Color col = rend.materials[0].GetColor("_Color");
			col.a = alpha;
			rend.materials[0].SetColor("_Color", col);
		}
		
		// Now do the same to all the children
		foreach (Transform child in obj.transform){
			ImplementAlpha(child.gameObject, alpha);
		}
	}
	
	protected void ImplementColor(GameObject obj, Color col){
		Renderer rend = obj.renderer;
		if (rend){
			rend.materials[0].SetColor("_Color", col);
		}
		
		// Now do the same to all the children
		foreach (Transform child in obj.transform){
			ImplementColor(child.gameObject, col);
		}
	}	
	
	public virtual void RemoveConnections(){
	}
	
	// Call to set up the connection behaviours for striaght component that can be connected at either end
	// This is a utility function as there are quite a few components like this - best to call this from the
	// RebuildMesh function
	protected void SetupStraightConnectionBehaviour(bool vertical){
		int addOrient = vertical ? 0 : 1;
		connectionBehaviour[(0 + orient + addOrient) % 4] = ConnectionBehaviour.kSociable;
		connectionBehaviour[(1 + orient + addOrient) % 4] = ConnectionBehaviour.kUnreceptive;
		connectionBehaviour[(2 + orient + addOrient) % 4] = ConnectionBehaviour.kSociable;
		connectionBehaviour[(3 + orient + addOrient) % 4] = ConnectionBehaviour.kUnreceptive;		
		
	}
	

	
}
