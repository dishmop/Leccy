using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;




public class CircuitElement : MonoBehaviour {
	
	public int			orient = 0;			// In 90 degree steps anti-clockwise
	public GameObject	anchorCentralPrefab;
	public GameObject	anchorBranchPrefab;
	// only set this on eif you want a different prefab used on branches which are not connected
	public GameObject	anchorEmptyBranchPrefab = null;
	public GameObject	capPrefab;
	
	public Color		normalColor;
	public Color		errorColor;
	
	protected GameObject	displayMesh;
	
	
	protected float			temperature = 0;
	
	bool					isOnCircuit = false;
	
	
	// For setting alpha and color values
	protected float alpha = 1f;
	protected bool 	dirtyAlpha = false;
	protected Color color = new Color(1f, 1f, 1f);
	protected bool 	dirtyColor = false;	
	
	// Used to put caps on sriaght components
	protected bool		hasCapTop = 	false;
	protected bool		hasCapBottom = 	false;	
	protected bool		hasCapLeft = 	false;
	protected bool		hasCapRight = 	false;		

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
	public bool[] isConnected = new bool[4];
	
	

	protected GridPoint		thisPoint;
	protected bool 			isInErrorState = false;
	
	protected float 		maxTemp = 45f;
	
	
	
	public void SetErrorState(bool isError){
		isInErrorState = isError;

		RebuildMesh();
	}
	
	
	
	public virtual void TriggerEmergency(){
		
		
	}
	
	// return true if this component is only available in the editor
	public virtual bool IsEditorOnly(){
		return false;
	}
	
	// Return true of this element is one which is attached to wires (most are)
	public virtual bool IsWired(){
		return true;
	}
	
	// Only return true if this component cannot actualy conduct electiricty at all
	public virtual bool IsInsulator(){
		return false;
	}
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	public virtual bool CanModify(GridPoint thisPt, GridPoint otherPt, bool honourAnchors){
		return false;
	}

		
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public virtual bool Modify(GridPoint thisPt, GridPoint otherPt, bool honourAnchors){
		return false;
	}	
	
	// Return true if we are a modifying circuit element (like anchors or eraser)
	// and we are able t modidify in our current state
	// Ths function actually does the modifying though
	public virtual bool Modify(GridPoint thisPt, bool honourAnchors){
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
	
	public virtual void SetGridPoint(GridPoint thisPoint){
		this.thisPoint = thisPoint;
		if (thisPoint != null){
			transform.position = new Vector3(thisPoint.x, thisPoint.y, transform.position.z);
		}
	}
	
	// can override the z position
	public virtual void SetGridPoint(GridPoint thisPoint, float z){
		this.thisPoint = thisPoint;
		if (thisPoint != null){
			transform.position = new Vector3(thisPoint.x, thisPoint.y, z);
		}
	}
	
	// Called after an element has been placed on the circuit
	public virtual void OnPostPlace(){
	}
	
	public GridPoint GetGridPoint(){
		return thisPoint;
	}
	
	public virtual void Save(BinaryWriter bw){
			
		bw.Write (orient);
		bw.Write (isOnCircuit);
		bw.Write (alpha);
		bw.Write (color.r);
		bw.Write (color.g);
		bw.Write (color.b);
		bw.Write (color.a);
		bw.Write (temperature);
		
		for (int i = 0; i < 4; ++i){
			bw.Write ((int)connectionBehaviour[i]);
		}
	}
	
	public 	virtual void Load(BinaryReader br){
		orient = br.ReadInt32();
		isOnCircuit = br.ReadBoolean();
		alpha = br.ReadSingle();
		color.r = br.ReadSingle();
		color.g = br.ReadSingle();
		color.b = br.ReadSingle();
		color.a = br.ReadSingle();
		temperature = br.ReadSingle();
		for (int i = 0; i < 4; ++i){
			connectionBehaviour[i] = (ConnectionBehaviour)br.ReadInt32();
		}
	}
	
	public virtual void PostLoad(){
		RebuildMesh ();
	}
	
	// called on each element once we have established which elements are connected to which other ones
	public virtual void PostConnectionAdjstments(){
	}
	
	protected void HandleDisplayMeshChlid()
	{
		// WHen we copy this object, the diusplay mesh is not seralized and so will be null.
		// However, the actial displaymesh will still be copied as a child - we need to rectify this.
		// Make DisplayMesh serializable doesn't seem to help as it crates two versions of the mesh!
		// Though this might be due to some other bug of mine (something to investigate)
		foreach (Transform child in transform){
			if (child.gameObject.name == displayMeshName && child.gameObject != displayMesh){
				Destroy (child.gameObject);
			}
			
		}
	}
	
	protected int ModelDir2WorldDir(int modelDir){
		return (modelDir + 6-orient) % 4;
	}
	
	public bool SuggestBehaviour(CircuitElement otherElement, ConnectionBehaviour behaviour, bool honourAnchors){
		int dir = Circuit.CalcNeighbourDir(GetGridPoint(), otherElement.GetGridPoint());
		return SuggestBehaviour(dir, behaviour, honourAnchors);
	}
	

	
	// Whether the   functions would return true if called
	public bool IsAmenableToBehaviour(CircuitElement otherElement, ConnectionBehaviour behaviour, bool honourAnchors){
		int dir = Circuit.CalcNeighbourDir(GetGridPoint(), otherElement.GetGridPoint());
		return IsAmenableToBehaviour(dir, behaviour, honourAnchors);
	}
	
	// By default we are only amenable to things which we are already
	public virtual bool IsAmenableToBehaviour(int dir, ConnectionBehaviour behaviour, bool honourAnchors){
		return connectionBehaviour[dir] == behaviour;
	}
	
	// If a behaviour is suggested we simply do nothing and return true if this is compatible with the reques
	public virtual bool SuggestBehaviour(int dir, ConnectionBehaviour behaviour, bool honourAnchors){
		return IsAmenableToBehaviour(dir, behaviour, honourAnchors);
	}	
	
	public virtual int CalcHash(){
		return  (isConnected[0] ?  1 << 0 : 0) + 
				(isConnected[1] ?  1 << 1 : 0) + 
				(isConnected[2] ?  1 << 2 : 0) + 
				(isConnected[3] ?  1 << 3 : 0) + 
				orient * 1 << 4;
	}
	
	
// 	int CalcBakedHash(){
//		return (isBaked[0] ? 1 : 0) + (isBaked[1]  ? 2 : 0) + (isBaked[2] ? 4 : 0) + (isBaked[3] ? 8 : 0);
//	}

	public bool IsConnected(int dir){
		return isConnected[dir];
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
	

	public  bool IsSociableOrConnected(int dir, bool isConnectedOrSociable){
		return 	((connectionBehaviour[dir] == ConnectionBehaviour.kSociable || isConnected[dir]) == isConnectedOrSociable);
	}

	
	public  bool IsSociableOrConnected(bool up, bool right, bool down, bool left){
		return IsSociableOrConnected(Circuit.kUp, up) &&
				IsSociableOrConnected(Circuit.kRight, right) &&
				IsSociableOrConnected(Circuit.kDown, down) &&
				IsSociableOrConnected(Circuit.kLeft, left);
		
	}
	
	
	
//	public bool IsConnected(bool up, bool right, bool down, bool left){
//		return 	(isConnected[Circuit.kUp] == up) &&
//				(isConnected[Circuit.kRight] == right) &&
//				(isConnected[Circuit.kDown] == down) &&
//				(isConnected[Circuit.kLeft] == left);
//	}	
//
//	public bool IsSociable(bool up, bool right, bool down, bool left){
//		return 	((connectionBehaviour[Circuit.kUp] == ConnectionBehaviour.kSociable) == up) &&
//				((connectionBehaviour[Circuit.kRight] == ConnectionBehaviour.kSociable) == right) &&
//				((connectionBehaviour[Circuit.kDown] == ConnectionBehaviour.kSociable) == down) &&
//				((connectionBehaviour[Circuit.kLeft] == ConnectionBehaviour.kSociable) == left);
//	}
	
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
		if (thisPoint != null){
			Circuit.singleton.GetAnchors(thisPoint).isDirty = true;
		}
		return  true;
	}
	
	// Decide if we should call OnClick() if we are clicked on with the selectd prefab
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
	
	// Given that we should click - are we actually able to (regarding machors etc.)
	// Be default, if any of our connections, or the node itself are anchored, then we cannot
	public virtual bool CanClick(){
		Circuit.AnchorData data = Circuit.singleton.GetAnchors(thisPoint);
		for (int i = 0; i < 5; ++i){
			if (data.isAnchored[i]) return false;
		}
		return true;
	}
	
	
	// Voltage drop when solving for currents (Terrible hack!)
	public virtual float GetVoltageDrop(int dir, bool fw){
		if (!IsConnected(dir)){
			Debug.LogError("Being asked about a nonexistant connection");
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
		
		// Ensure any colours get applied correctly afterwards
		dirtyAlpha = true;
		dirtyColor = true;
		if (thisPoint != null) Circuit.singleton.GetAnchors(thisPoint).isDirty = true;
		
	}
	

		protected void VisualiseTemperature(){
		foreach (Transform child in transform.GetChild(0)){
			MeshRenderer mesh = child.gameObject.transform.GetComponent<MeshRenderer>();
			if (mesh != null) mesh.materials[0].SetFloat ("_Temperature", temperature / maxTemp );
		}		
		
	}	
	
	protected void DestorySelf(){
		Debug.Log("Destroy self : " + GetComponent<SerializationID>().id);
		RemoveConnections(false);
		Circuit.singleton.RemoveElement(thisPoint);
		Circuit.singleton.TriggerExplosion(thisPoint);
		Destroy (gameObject);
		
		// Also remove any anchors this object may have had and any anchors from its neighbours to it
		// this may not be the correction thing to do, but it is better than keeping them there
		// as it looks odd
		Circuit.AnchorData data = Circuit.singleton.GetAnchors(thisPoint);
		for (int i = 0; i < 5; ++i){
			data.isAnchored[i] = false;
		}
		data.isDirty = true;
		// each of the neighbouring points
		for (int i = 0; i < 4; ++i){
			GridPoint otherPoint = thisPoint + Circuit.singleton.offsets[i];
			int otherDir = CalcInvDir(i);
			Circuit.AnchorData otherData = Circuit.singleton.GetAnchors(otherPoint);
			otherData.isAnchored[otherDir] = false;
			otherData.isDirty = true;
		}
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
	
	public virtual void RemoveConnections(bool honourAnchors){
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
	
	
	public virtual  void OnMouseDown() {

		
	}	
	
	public virtual   void OnMouseOver() {

		
	}	

	// Do all game logic in here	
	public virtual void GameUpdate(){
	}	
	

	
}
