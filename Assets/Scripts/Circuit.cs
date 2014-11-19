using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;

public class Circuit : MonoBehaviour {
	
	public static Circuit singleton = null;
	

	public GameObject gridGO;
	public GameObject factoryGO;
	public GameObject particleExplosionPrefab;
	
	public Rect bounds;

		
	// Useful consts
	public const int kErr = 	-1;
	public const int kUp = 		0;
	public const int kRight = 	1;
	public const int kDown = 	2;
	public const int kLeft = 	3;

	// Useful array of offsets
	GridPoint[] offsets = new GridPoint[4];
	
	// Scripts associatd with the game objects
	Grid 			grid;
	ElementFactory	factory;
	
	public GameObject[,] 	elements;
	
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
		return true; 
	}
	
	public void AddElement(GridPoint point, GameObject circuitElementGO){
	}
	
	public void RemoveElement(GridPoint point){
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
			AddElement (new GridPoint(data.x, data.y), factory.InstantiateElement(data.id));
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
	int CalcNeighbourDir(GridPoint thisPoint, GridPoint otherPoint){
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
	
	
	public bool IsPointInGrid(GridPoint point){
		return 	
			point.x >= 0 &&
			point.y >= 0 &&
			point.x < grid.gridWidth &&
			point.y < grid.gridHeight;		
	}	
	
	
	// Assumed to be in the grid
	public bool ElementExists(GridPoint point){
		return (IsPointInGrid(point) &&  elements[point.x, point.y] != null);
	}
	
	GridPoint[] CalcGridPath(GridPoint prevPoint, GridPoint nextPoint){
		
		if (prevPoint.IsEqual(nextPoint)){
			Debug.LogError("Trying to dsraw from and to the same point!");
			return null;
		}
	
		int xDiff = nextPoint.x - prevPoint.x;
		int yDiff = nextPoint.y - prevPoint.y;
		
		// Work out how many elements the array should have
		int size = Mathf.Abs(xDiff) + Mathf.Abs(yDiff) + 1;
		GridPoint[] result = new GridPoint[size];
		
		// If we are not moving in x at all...
		int i = 0;
		if (Mathf.Abs(xDiff) > Mathf.Abs(yDiff)){
			int xInc = xDiff / Mathf.Abs (xDiff);
			int lastY = prevPoint.y;
			float grad = (float)yDiff/(float)xDiff;
			for (int x = prevPoint.x; x != nextPoint.x + xInc; x += xInc){
				int thisY = prevPoint.y + Mathf.RoundToInt((x-prevPoint.x) * grad);
				if (thisY != lastY){
					result[i++] = new GridPoint(x, lastY);
					lastY = thisY;
				}
				result[i++] = new GridPoint(x, thisY);	
			}
				                            
		}
		else{
			int yInc = yDiff / Mathf.Abs (yDiff);
			int lastX = prevPoint.x;
			float grad = (float)xDiff/(float)yDiff;
			for (int y = prevPoint.y; y != nextPoint.y + yInc; y += yInc){
				int thisX = prevPoint.x + Mathf.RoundToInt((y-prevPoint.y) * grad);
				if (thisX != lastX){
					result[i++] = new GridPoint(lastX, y);
					lastX = thisX;
				}					
				result[i++] = new GridPoint(thisX, y);
			}
		}
		
		return result;
		
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
	
	
	
	void Awake(){
		offsets[kLeft] = 	new GridPoint(-1,  0);
		offsets[kRight] = 	new GridPoint( 1,  0);
		offsets[kUp] = 		new GridPoint( 0,  1);
		offsets[kDown] = 	new GridPoint( 0,  -1);	
		
		
		grid = gridGO.GetComponent<Grid>();
		factory = factoryGO.GetComponent<ElementFactory>();
		
		CreateBlankCircuit();
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
		elements = new GameObject[grid.gridWidth, grid.gridHeight];
		
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
	
	// Update is called once per frame
	void Update () {
	
		CalcBounds();
		
	}
}
