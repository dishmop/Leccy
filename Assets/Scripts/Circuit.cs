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

		
	// Useful consts
	public const int kErr = 	-1;
	public const int kUp = 		0;
	public const int kRight = 	1;
	public const int kDown = 	2;
	public const int kLeft = 	3;

	// Useful array of offsets
	public GridPoint[] offsets = new GridPoint[4];
	
	
	public GameObject[,] 	elements;
	int[,] 					elementHash;
	
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
	
	
	public 	void Save(BinaryWriter bw){
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
			bw.Write(dataList[i].y);
			bw.Write (dataList[i].id);
		}
		for (int i = 0; i < dataList.Count; ++i){
			GetElement (new GridPoint(dataList[i].x, dataList[i].y)).Save(bw);
		}
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
	
	
	
//	public void AddElement(GridPoint point, GameObject circuitElementGO){
//	}
//	
	public void RemoveElement(GridPoint point){
		GetElement(point).SetIsOnCircuit(false);
		elements[point.x, point.y] = null;
	}
	
	
	public void PlaceElement(GameObject newElement, GridPoint point){
		if (GetElement (point) != null){
			Debug.LogError ("Attempting to place an element where one already exists");
		}
		newElement.transform.parent = transform;
		elements[point.x, point.y] = newElement;
		GetElement(point).SetGridPoint(point);
		GetElement(point).SetIsOnCircuit(true);
		GetElement(point).RebuildMesh();
		
	}
	
	public 	void Load(BinaryReader br){
		CreateBlankCircuit();

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
		
		// Go through each entry adding a crcuit element to the circuit
		for (int i = 0; i < numElements; ++i){
			ElementSerializationData data = dataList[i];
			PlaceElement(ElementFactory.singleton.InstantiateElement(data.id), new GridPoint(data.x, data.y));
//			if (data.id == wireElementPrefab.GetComponent<SerializationID>().id){
//				RawAddElement(new GridPoint(data.x, data.y), wireElementPrefab);
//			}
//			else if (data.id == cellElementPrefab.GetComponent<SerializationID>().id){
//				RawAddElement(new GridPoint(data.x, data.y), cellElementPrefab);
//			}
//			else if (data.id == resistorElementPrefab.GetComponent<SerializationID>().id){
//				RawAddElement(new GridPoint(data.x, data.y), resistorElementPrefab);
//			}
//			else if (data.id == ameterElementPrefab.GetComponent<SerializationID>().id){
//				RawAddElement(new GridPoint(data.x, data.y), ameterElementPrefab);
//			}
			CircuitElement newElement = GetElement (new GridPoint(data.x, data.y));
			newElement.Load(br);
			newElement.PostLoad();
		}
	}
	
	public void BakeConnect(){
		if (elements != null){
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					GridPoint thisPoint = new GridPoint(x,y);
					if (ElementExists(thisPoint)){
						for (int i = 0; i < 4; ++i){
							GetElement(thisPoint).isBaked[i] = GetElement(thisPoint).IsConnected(i);
						}
					}
					
				}
			}
		}
	}
	
	public void BakeAll(){
		if (elements != null){
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					GridPoint thisPoint = new GridPoint(x,y);
					// If we have an element here, then bake all connections
					if (ElementExists(thisPoint)){
						for (int i = 0; i < 4; ++i){
							GetElement(thisPoint).isBaked[i] = true;
						}
					}
					
				}
			}
		}
	}	
	
	public void Unbake(){
		if (elements != null){
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					GridPoint thisPoint = new GridPoint(x,y);
					if (ElementExists(thisPoint)){
						for (int i = 0; i < 4; ++i){
							GetElement(thisPoint).isBaked[i] = false;
							
						}
					}
				}
			}
		}
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
	

	 void CreateBlankCircuit(){	
		// If there is a load of circuit elemnts here already, go through and destroy them all
		if (elements != null){
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					GameObject.Destroy(elements[x,y]);
				}
			}
		}
		elements = new GameObject[Grid.singleton.gridWidth, Grid.singleton.gridHeight];
		elementHash = new int[Grid.singleton.gridWidth, Grid.singleton.gridHeight];
		
	}

	
	public void CalcBounds(){
		bounds.xMin = 1000;
		bounds.yMin = 1000;
		bounds.xMax = -1000;
		bounds.yMax = -1000;
		
		for (int x = 0; x < elements.GetLength (0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				if (ElementExists(new GridPoint(x, y))){
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
		MakeConnections();
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
		
		// Rebiuld any meshes that need rebuilding
		
		// Create a hash key for each element so we know if it has changed
		for (int x = 0; x < elements.GetLength(0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				CircuitElement thisElement = GetElement(new GridPoint(x, y));
				if (thisElement && elementHash[x,y] != thisElement.CalcHash()){
					thisElement.RebuildMesh();
				}
			}
		}		
			
		
	}
}
