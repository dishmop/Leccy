using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;

public class Circuit : MonoBehaviour {
	
	public static Circuit singleton = null;
	
	public GameObject particleExplosionPrefab;
	
	public Rect bounds;
	
	public GameObject	anchorCentralPrefabDefault;
	public GameObject	anchorBranchPrefabDefault;	
	public GameObject	emptyGO;

		
	// Useful consts
	public const int kErr = 	-1;
	public const int kUp = 		0;
	public const int kRight = 	1;
	public const int kDown = 	2;
	public const int kLeft = 	3;
	public const int kCentre =  4;

	// Useful array of offsets
	public GridPoint[] offsets = new GridPoint[4];
	
	
	public GameObject[,] 	elements;

	public class AnchorData{
		public bool[] 		isAnchored = new bool[5];
		public GameObject 	anchorMesh = null;
		public bool			disableGrid = false;
		public bool			isDirty = false;
	}
	
	public AnchorData[,]	anchors;
		
		
	int[,] 					elementHash;
	
	// This can be used to identify a level that has been saved
	String					currentGuid = Guid.NewGuid().ToString();
	
	// Whether the circuit has changed since we last reset the dirt flag
	bool					isDirty = true;
	
	const int		kLoadSaveVersion = 3;	
		
	[Serializable]
	class ElementSerializationData{
		public int x;
		public int y;
		public string id;
		
		// Constructor
		public ElementSerializationData(){}
		
		public ElementSerializationData(int x, int y, string id){
			this.x = x;
			this.y = y;
			this.id = id;
		}
	};
	
	
	public void RefreshAll(){
		for (int x = 0; x < elements.GetLength(0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				GridPoint thisPoint = new GridPoint(x, y);
				CircuitElement element = GetElement(thisPoint);
				if (element){
					element.RebuildMesh();
				}
				GetAnchors(thisPoint).isDirty = true;
			}
		}
		OnCircutChange();
	}
	
	
	public bool IsDirty(){
		return isDirty;
	}
	
	
	public void ResetDirty(){
		isDirty = false;
	}
	
	public 	void Save(BinaryWriter bw){
		bw.Write (kLoadSaveVersion);
		bw.Write (currentGuid);
	
		List<ElementSerializationData> dataList = new List<ElementSerializationData>();
		for (int x = 0; x < elements.GetLength(0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				if (elements[x,y] != null){
					dataList.Add ( new ElementSerializationData(x, y, elements[x,y].GetComponent<SerializationID>().id));					 
				}
			}

		}	

		bw.Write (dataList.Count);
	
		for (int i = 0; i < dataList.Count; ++i){
			bw.Write (dataList[i].x);
			bw.Write (dataList[i].y);
			bw.Write (dataList[i].id);
		}
		for (int i = 0; i < dataList.Count; ++i){
			GetElement (new GridPoint(dataList[i].x, dataList[i].y)).Save(bw);
		}			
		for (int x = 0; x < elements.GetLength (0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				for (int i = 0 ; i < 5; ++i){
					bw.Write(anchors[x,y].isAnchored[i]);
				}
				bw.Write(anchors[x,y].disableGrid);
			}
		}		
	}

	public 	void Load(BinaryReader br){

		int version = br.ReadInt32();
		switch (version){
		case kLoadSaveVersion:{
			currentGuid = br.ReadString();
			
			// Get the list of objects
			List<ElementSerializationData> dataList = new List<ElementSerializationData>();
			int numElements = br.ReadInt32();
			
			
			for (int i = 0; i < numElements; ++i){
				ElementSerializationData data = new ElementSerializationData();
				data.x = br.ReadInt32 ();
				data.y = br.ReadInt32 ();
				data.id = br.ReadString ();
				dataList.Add (data);
			}
			// Go through each position on the grid in the same oreder which which we wrote them
			int index = 0;
			ElementSerializationData nextData = dataList[index];
			for (int x = 0; x < elements.GetLength(0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					GridPoint thisPoint = new GridPoint(x, y);
					// If this element position is the next one in our list, then compare it
					if (nextData != null && nextData.x == x && nextData.y == y){
						// If it is a different ID, then we destroy it and make an element of the correct type
						GameObject thisObj = GetGameObject(thisPoint);
						string thisId = thisObj != null ? thisObj.GetComponent<SerializationID>().id : "NULL";
						if (thisObj == null || nextData.id != thisId){
							Destroy(thisObj);
							RemoveElement(thisPoint);
							PlaceElement(ElementFactory.singleton.InstantiateElement(nextData.id), thisPoint);
						}
						
						// Get a pointer to the CircuitElement bit of the component which is there
						CircuitElement newElement = GetElement (thisPoint);
						newElement.Load(br);
						newElement.PostLoad();
						
						// Advance our list element
						index++;
						if (index < dataList.Count){
							nextData = dataList[index];
						}
						else{
							nextData = null;
						}
					}
					// otherwise, this should be null
					else if (elements[x,y] != null){
						Destroy(elements[x,y]);
						RemoveElement(thisPoint);
					}
					// otherwise it should be null and it is null 
					else{
						//- so do nothing
					}
				}
				
			}					
			
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					AnchorData data = anchors[x, y];
					int hash = CalcAnchorHash(data);
					for (int i = 0 ; i < 5; ++i){
						data.isAnchored[i] = br.ReadBoolean();
					}
					data.disableGrid = br.ReadBoolean();
					int newHash = CalcAnchorHash(data);
					if (newHash != hash){
						data.isDirty = true;
					}
				}
			}			
			CalcBounds();
			break;
		}
		case 2:{
			currentGuid = br.ReadString();
			
			// Get the list of objects
			List<ElementSerializationData> dataList = new List<ElementSerializationData>();
			int numElements = br.ReadInt32();
			
			
			for (int i = 0; i < numElements; ++i){
				ElementSerializationData data = new ElementSerializationData();
				data.x = br.ReadInt32 ();
				data.y = br.ReadInt32 ();
				data.id = br.ReadString ();
				dataList.Add (data);
			}
			// Go through each position on the grid in the same oreder which which we wrote them
			int index = 0;
			ElementSerializationData nextData = dataList[index];
			for (int x = 0; x < elements.GetLength(0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					GridPoint thisPoint = new GridPoint(x, y);
					// If this element position is the next one in our list, then compare it
					if (nextData != null && nextData.x == x && nextData.y == y){
						// If it is a different ID, then we destroy it and make an element of the correct type
						GameObject thisObj = GetGameObject(thisPoint);
						string thisId = thisObj != null ? thisObj.GetComponent<SerializationID>().id : "NULL";
						if (thisObj == null || nextData.id != thisId){
							Destroy(thisObj);
							RemoveElement(thisPoint);
							PlaceElement(ElementFactory.singleton.InstantiateElement(nextData.id), thisPoint);
						}
						
						// Get a pointer to the CircuitElement bit of the component which is there
						CircuitElement newElement = GetElement (thisPoint);
						newElement.Load(br);
						newElement.PostLoad();
						
						// Advance our list element
						index++;
						if (index < dataList.Count){
							nextData = dataList[index];
						}
						else{
							nextData = null;
						}
					}
					// otherwise, this should be null
					else if (elements[x,y] != null){
						Destroy(elements[x,y]);
						RemoveElement(thisPoint);
					}
					// otherwise it should be null and it is null 
					else{
						//- so do nothing
					}
				}
				
			}					
			
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					AnchorData data = anchors[x, y];
					int hash = CalcAnchorHash(data);
					for (int i = 0 ; i < 5; ++i){
						data.isAnchored[i] = br.ReadBoolean();
						data.disableGrid = br.ReadBoolean();
					}
					int newHash = CalcAnchorHash(data);
					if (newHash != hash){
						data.isDirty = true;
					}
				}
			}			
			CalcBounds();
			break;
		}	
		case 1:{
			
			// Get the list of objects
			List<ElementSerializationData> dataList = new List<ElementSerializationData>();
			int numElements = br.ReadInt32();
			
			
			for (int i = 0; i < numElements; ++i){
				ElementSerializationData data = new ElementSerializationData();
				data.x = br.ReadInt32 ();
				data.y = br.ReadInt32 ();
				data.id = br.ReadString ();
				dataList.Add (data);
			}
			// Go through each position on the grid in the same oreder which which we wrote them
			int index = 0;
			ElementSerializationData nextData = dataList[index];
			for (int x = 0; x < elements.GetLength(0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					GridPoint thisPoint = new GridPoint(x, y);
					// If this element position is the next one in our list, then compare it
					if (nextData != null && nextData.x == x && nextData.y == y){
						// If it is a different ID, then we destroy it and make an element of the correct type
						GameObject thisObj = GetGameObject(thisPoint);
						string thisId = thisObj != null ? thisObj.GetComponent<SerializationID>().id : "NULL";
						if (thisObj == null || nextData.id != thisId){
							Destroy(thisObj);
							RemoveElement(thisPoint);
							PlaceElement(ElementFactory.singleton.InstantiateElement(nextData.id), thisPoint);
						}
						
						// Get a pointer to the CircuitElement bit of the component which is there
						CircuitElement newElement = GetElement (thisPoint);
						newElement.Load(br);
						newElement.PostLoad();
						
						// Advance our list element
						index++;
						if (index < dataList.Count){
							nextData = dataList[index];
						}
						else{
							nextData = null;
						}
					}
					// otherwise, this should be null
					else if (elements[x,y] != null){
						Destroy(elements[x,y]);
						RemoveElement(thisPoint);
					}
					// otherwise it should be null and it is null 
					else{
						//- so do nothing
					}
				}
				
			}					
			
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					AnchorData data = anchors[x, y];
					int hash = CalcAnchorHash(data);
					for (int i = 0 ; i < 5; ++i){
						data.isAnchored[i] = br.ReadBoolean();
						data.disableGrid = br.ReadBoolean();
					}
					int newHash = CalcAnchorHash(data);
					if (newHash != hash){
						data.isDirty = true;
					}
				}
			}			
			CalcBounds();
			break;
		}		
		}
		
	}
	
	int CalcAnchorHash(AnchorData data){
		
		return  (data.isAnchored[0] ?  1 << 0 : 0) + 
				(data.isAnchored[1] ?  1 << 1 : 0) + 
				(data.isAnchored[2] ?  1 << 2 : 0) + 
				(data.isAnchored[3] ?  1 << 3 : 0) + 
				(data.disableGrid 	?  1 << 4 : 0);
	}
	
	
	public bool Validate(){
		for (int x = 0; x < elements.GetLength (0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				GridPoint thisPoint = new GridPoint(x,y);
				CircuitElement thisElement = GetElement(thisPoint);
				if (thisElement){
					for (int dir = 0; dir < 4; ++dir){
						if (thisElement.isConnected[dir]){
							GridPoint otherPoint = thisPoint + offsets[dir];
							CircuitElement otherElement = GetElement (otherPoint);
							// If we are connected, then there must be another element there
							if (otherElement == null){
								return false;
							}
							
							// Futhermore, that element should be connected back to us
							int otherDir = CircuitElement.CalcInvDir(dir);
							if (!otherElement.isConnected[otherDir]){
								return false;
							}
							
						}
						
					}
				}
				
			}
		}
		// Otherwise, if we got through all that - then we are ok
		return true; 
	}
	
	
	
	public void RemoveElement(GridPoint point){
		elements[point.x, point.y] = null;
		GetAnchors(point).isDirty = true;
		OnCircutChange();
		
	}
	
	
	public void PlaceElement(GameObject newElement, GridPoint point){
		if (GetElement (point) != null){
				Debug.LogError ("Attempting to place an element where one already exists");
		}
		newElement.transform.parent = transform;
		elements[point.x, point.y] = newElement;
		CircuitElement element = GetElement(point);
		element.SetGridPoint(point);
		element.SetIsOnCircuit(true);
		element.OnPostPlace();
		element.RebuildMesh();
		GetAnchors(point).isDirty = true;
		OnCircutChange();
		
	}
	
	public void OnCircutChange(){
		isDirty = true;
		currentGuid = Guid.NewGuid().ToString();
	}
	
	
	public void TriggerExplosion(GridPoint point){
		GameObject newElement = Instantiate(
			particleExplosionPrefab, 
			new Vector3(point.x, point.y, particleExplosionPrefab.transform.position.z), 
			Quaternion.identity)
			as GameObject;
		newElement.transform.parent = transform;
		newElement.GetComponent<ParticleEmitter>().Emit();
		
	}
	
	
	
		
	
	// Returns the connection direction from thisPoint to the other point
	public static int CalcNeighbourDir(GridPoint thisPoint, GridPoint otherPoint){
		if (otherPoint.x == thisPoint.x + 1 && otherPoint.y == thisPoint.y)
			return 	kRight;
		if (otherPoint.x == thisPoint.x - 1 && otherPoint.y == thisPoint.y)
			return 	kLeft;
		if (otherPoint.x == thisPoint.x  && otherPoint.y == thisPoint.y + 1)
			return 	kUp;
		if (otherPoint.x == thisPoint.x  && otherPoint.y == thisPoint.y - 1)
			return 	kDown;			
		return kErr;
	}

	
	// Assumed to be in the grid
	public bool ElementExists(GridPoint point){
		return (Grid.singleton.IsPointInGrid(point) &&  elements[point.x, point.y] != null);
	}
	

	
			
	// Assumed that the element exists
	public CircuitElement GetElement(GridPoint point){
		if (!ElementExists(point)) return null;
		return elements[point.x, point.y].GetComponent<CircuitElement>();
	}
	
	public AnchorData GetAnchors(GridPoint point){
		return anchors[point.x, point.y];
	}
	
	public GameObject GetGameObject(GridPoint point){
		if (!ElementExists(point)) return null;
		return elements[point.x, point.y];
	}
	
	
	void Start(){
		CreateBlankCircuit();
	}
		
		
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
		
		offsets[kLeft] = 	new GridPoint(-1,  0);
		offsets[kRight] = 	new GridPoint( 1,  0);
		offsets[kUp] = 		new GridPoint( 0,  1);
		offsets[kDown] = 	new GridPoint( 0,  -1);	
		
			

	}
	
	void OnDestroy(){
		
		singleton = null;
	}
	

	 public void CreateBlankCircuit(){	
		// If there is a load of circuit elemnts here already, go through and destroy them all
		if (elements != null){
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					Destroy(elements[x,y]);
				}
			}
		}
		elements = new GameObject[Grid.singleton.gridWidth, Grid.singleton.gridHeight];
		elementHash = new int[Grid.singleton.gridWidth, Grid.singleton.gridHeight];
		// If we have anchors already then delete them all
		if (anchors != null){
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					Destroy(anchors[x,y].anchorMesh);
				}
			}
		}
		// Now rebuidl the anchors
		anchors = new AnchorData[Grid.singleton.gridWidth, Grid.singleton.gridHeight];
		for (int x = 0; x < elements.GetLength (0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				anchors[x,y] = new AnchorData();
			}
		}
		
	}

	
	public void CalcBounds(){
		bounds.xMin = 1000;
		bounds.yMin = 1000;
		bounds.xMax = -1000;
		bounds.yMax = -1000;
		
		for (int x = 0; x < elements.GetLength (0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				GridPoint thisPoint = new GridPoint(x, y);
				bool isAnchored = false;
				AnchorData data = GetAnchors(thisPoint);
				if (!data.disableGrid){
					for (int i = 0; i < 5; ++i){
						if (data.isAnchored[i]) isAnchored = true;
					}
				}
				if (ElementExists(thisPoint) || isAnchored){
					bounds.xMin = Mathf.Min (bounds.xMin, x);
					bounds.yMin = Mathf.Min (bounds.yMin, y);
					bounds.xMax = Mathf.Max (bounds.xMax, x);
					bounds.yMax = Mathf.Max (bounds.yMax, y);
				}
				

			}
		}
		
	}
	
	// GameUpdate is called once per frame in a specific order
	public void GameUpdate () {
		CalcBounds();
		UpdateGrid();
		CircuitElementGameUpdate();
		MakeConnections();
		PostConnectionUpdate();
		UpdateAnchorMeshes();	
		
		
	}
	
	void UpdateGrid(){
//		int width = Grid.singleton.gridWidth;
//		int height = Grid.singleton.gridHeight;
//		for (int x = 0; x < width; ++x){
//			for (int y = 0; y < height; ++y){
//				AnchorData data = GetAnchors(new GridPoint(x,y));
//				Grid.singleton.gridObjects[x,y].SetActive(!data.disableGrid);
//				data.anchorMesh.SetActive(!data.disableGrid);
//				
//				
//			}
//		}
		
	}
	
	void CircuitElementGameUpdate(){
		for (int x = 0; x < elements.GetLength (0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				CircuitElement element = GetElement(new GridPoint(x, y));
				if (element){
					element.GameUpdate();
				}
				
			}
		}
	}
	
	// If we have (usually wires) that are being sociable, but have no actual connection
	// but there is no anchor holding them there, then suggest that they go back to being receptive only
	public void TidyUpConnectionBehaviours(bool honourAnchors){
		for (int x = 0; x < elements.GetLength (0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				GridPoint thisPoint = new GridPoint(x, y);
				CircuitElement thisElement = GetElement(thisPoint);
				if (thisElement != null){
					AnchorData data = GetAnchors(thisPoint);
					for (int i = 0; i < 4; ++i){
						if (thisElement.connectionBehaviour[i] == CircuitElement.ConnectionBehaviour.kSociable && thisElement.isConnected[i] == false){
							// Note that this will not do anything if the anchors prevent it
							if (data.isAnchored[i]){
								thisElement.SuggestBehaviour(i, CircuitElement.ConnectionBehaviour.kUnreceptive, honourAnchors);
							}
							else{
								thisElement.SuggestBehaviour(i, CircuitElement.ConnectionBehaviour.kReceptive, honourAnchors);
							}
						}
						// If connections are unreceptive - generally try making them more receptive (anchors allowing)
						if (thisElement.connectionBehaviour[i] == CircuitElement.ConnectionBehaviour.kUnreceptive){
							// Note that this will not do anything if the anchors prevent it
							if (!data.isAnchored[i]){
								thisElement.SuggestBehaviour(i, CircuitElement.ConnectionBehaviour.kReceptive, honourAnchors);
							}
						}
						
					}
				}
			}
		}
	}
	
	
	void PostConnectionUpdate(){
		for (int x = 0; x < elements.GetLength (0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				CircuitElement element = GetElement(new GridPoint(x, y));
				if (element != null){
					element.PostConnectionAdjstments();
					
				}
				
			}
		}
	}
	
	void MakeConnections(){
		// Create a hash key for each element so we know if it has changed
		for (int x = 0; x < elements.GetLength(0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				CircuitElement thisElement = GetElement(new GridPoint(x, y));
				if (thisElement){
					elementHash[x,y] = thisElement.CalcHash();
				}
			}
		}
			
				
		// Clear all connections in the circuit
		for (int x = 0; x < elements.GetLength(0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				GridPoint thisPoint = new GridPoint(x, y);
				CircuitElement thisElement = GetElement(thisPoint);
				for (int dir = 0; dir < 4; ++dir){
					if (thisElement) thisElement.isConnected[dir] = false;
				}
			}
		}
				     
		// Go through making connections where invites have been made and accepted
		for (int x = 0; x < elements.GetLength(0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				GridPoint thisPoint = new GridPoint(x, y);
				CircuitElement thisElement = GetElement(thisPoint);
				if (thisElement){
					for (int dir = 0; dir < 4; ++dir){
						GridPoint otherPoint = thisPoint + offsets[dir];
						int otherDir = CircuitElement.CalcInvDir(dir);
						CircuitElement otherElement = GetElement (otherPoint);
						if (otherElement){
							bool makeConnection = false;
		
							// If both are inviting					
							if (thisElement.connectionBehaviour[dir] == CircuitElement.ConnectionBehaviour.kSociable && 
							    otherElement.connectionBehaviour[otherDir] == CircuitElement.ConnectionBehaviour.kSociable){
								makeConnection = true;
							 }
							if (thisElement.connectionBehaviour[dir] == CircuitElement.ConnectionBehaviour.kSociable && 
							    otherElement.connectionBehaviour[otherDir] == CircuitElement.ConnectionBehaviour.kReceptive){
								makeConnection = true;
							}
							if (otherElement.connectionBehaviour[otherDir] == CircuitElement.ConnectionBehaviour.kSociable && 
							    thisElement.connectionBehaviour[dir] == CircuitElement.ConnectionBehaviour.kReceptive){
								makeConnection = true;
							}
							if (makeConnection){
								thisElement.isConnected[dir] = true;
								otherElement.isConnected[otherDir] =true;
							}
						}
					}
				}
			}
		}
		
		// Rebiuld any meshes that need rebuilding and indorm the anchors that they need rebuilding too
		
		// Create a hash key for each element so we know if it has changed
		for (int x = 0; x < elements.GetLength(0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				GridPoint thisPoint = new GridPoint(x, y);
				CircuitElement thisElement = GetElement(thisPoint);
				if (thisElement && elementHash[x,y] != thisElement.CalcHash()){
					thisElement.RebuildMesh();
					GetAnchors(thisPoint).isDirty = true;
				}
			}
		}		
	}
	
	void UpdateAnchorMeshes(){
		List<GridPoint> changedGridPoints = new List<GridPoint>();
		for (int x = 0; x < elements.GetLength(0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				GridPoint thisPoint = new GridPoint(x, y);
				AnchorData data = GetAnchors(thisPoint);
				// If something has changed
				if (data.isDirty){
					GameObject centrePrefab = anchorCentralPrefabDefault;
					GameObject branchPrefab = anchorBranchPrefabDefault;
					GameObject emptyBranchPrefab = null;
					bool[]	   isConnected = null;
					int		   orient = 0;
					
					// If there is an element here, ask it what prefabs to use
					// otherwise, use the defaults
					CircuitElement element = GetElement(thisPoint);
					if (element){
						centrePrefab = element.anchorCentralPrefab;
						branchPrefab = element.anchorBranchPrefab;
						emptyBranchPrefab = element.anchorEmptyBranchPrefab;
						orient = element.orient;
						isConnected = new bool[4];
						// for the connected array, we actually set it to whether they are connection of sociable - this is a bit of a hack
						for (int i = 0; i < 4; ++i){
							isConnected[i] = element.IsSociableOrConnected(i, true);	
						}
					}
					RebuildAnchorMesh(data, isConnected, orient, centrePrefab, branchPrefab, emptyBranchPrefab, emptyGO);
					data.isDirty = false;
					if (data.anchorMesh){
						// want it positioned behind everyhing (note that the ghost versio of this should be in between)
						data.anchorMesh.transform.position = new Vector3(thisPoint.x, thisPoint.y, 1);
						data.anchorMesh.transform.parent = transform;
						data.anchorMesh.SetActive(!data.disableGrid);
						
					}
					changedGridPoints.Add(thisPoint);
					
				}
			}
		}	
		Grid.singleton.RebuildGridPoints(changedGridPoints);
	}
	

	public static void RebuildAnchorMesh(AnchorData data, bool[] isConnected, int orient, GameObject centrePrefab, GameObject branchPrefab, GameObject emptyBranchPrefab, GameObject emptyGO){
		// Destory the previous one
		Destroy (data.anchorMesh);
		data.anchorMesh = null;
		
		// If there are no anchors, then do not build a mesh
		int count = 0;
		for (int i = 0; i < 5; ++i){
			if (data.isAnchored[i]) ++count;
			
		}
		if (count == 0) return;
		
		data.anchorMesh = GameObject.Instantiate(emptyGO) as GameObject;
		
		float[] angles = new float[4];
		angles[0] = 0;
		angles[1] = -90;
		angles[2] = 180;
		angles[3] = 90;
		
		
		// Do the central anchor
		if (data.isAnchored[Circuit.kCentre]){
			GameObject centreAnchor =  Instantiate(
				centrePrefab, 
				new Vector3(data.anchorMesh.transform.position.x, data.anchorMesh.transform.position.y, centrePrefab.transform.position.z),
				Quaternion.Euler(0, 0, angles[orient])) as GameObject;
			centreAnchor.transform.parent = data.anchorMesh.transform;
			
		}
		
		// Do the branch anchors
		for (int i = 0; i < 4; ++i){
			GameObject useBranchPrefab = (emptyBranchPrefab == null) ? branchPrefab : (isConnected[i] ? branchPrefab : emptyBranchPrefab);
			if (data.isAnchored[i]){
				GameObject branchAnchor = Instantiate(
					useBranchPrefab, 
					new Vector3(data.anchorMesh.transform.position.x, data.anchorMesh.transform.position.y, useBranchPrefab.transform.position.z),
					Quaternion.Euler(0, 0, angles[i])) as GameObject;
				branchAnchor.transform.parent = data.anchorMesh.transform;
				
			}
		}
		
	}
	
}
