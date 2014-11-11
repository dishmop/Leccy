using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;

public class Circuit : MonoBehaviour {
	
	public static Circuit singleton = null;
	
	public GameObject wireElementPrefab;	
	public GameObject cellElementPrefab;	
	public GameObject resistorElementPrefab;	
	public GameObject ameterElementPrefab;	
	public GameObject gridGO;
	public GameObject particleExplosionPrefab;
	
	// Keep track of the number of each type of circuit element used
	public Dictionary<string, int> numElementsUsed;

	
	// Useful consts
	public const int kErr = 	-1;
	public const int kUp = 		0;
	public const int kRight = 	1;
	public const int kDown = 	2;
	public const int kLeft = 	3;
	
	// Useful array of offsets
	GridPoint[] offsets = new GridPoint[4];
	
	// Grid Script Of GridGO
	Grid 	grid;
	
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
			if (data.id == wireElementPrefab.GetComponent<SerializationID>().id){
				RawAddElement(new GridPoint(data.x, data.y), wireElementPrefab);
			}
			else if (data.id == cellElementPrefab.GetComponent<SerializationID>().id){
				RawAddElement(new GridPoint(data.x, data.y), cellElementPrefab);
			}
			else if (data.id == resistorElementPrefab.GetComponent<SerializationID>().id){
				RawAddElement(new GridPoint(data.x, data.y), resistorElementPrefab);
			}
			else if (data.id == ameterElementPrefab.GetComponent<SerializationID>().id){
				RawAddElement(new GridPoint(data.x, data.y), ameterElementPrefab);
			}			
			GetElement (new GridPoint(data.x, data.y)).Load(br);
			GetElement (new GridPoint(data.x, data.y)).SetupMesh();
		}
	}
	
	public void BakeConnect(){
		if (elements != null){
			for (int x = 0; x < elements.GetLength (0); ++x){
				for (int y = 0; y < elements.GetLength(1); ++y){
					GridPoint thisPoint = new GridPoint(x,y);
					if (ElementExists(thisPoint)){
						for (int i = 0; i < 4; ++i){
							GetElement(thisPoint).isBaked[i] = GetElement(thisPoint).isConnected[i];
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
									
		
	
	public void AddCell(GridPoint point){
		AddStraighComponent(point, cellElementPrefab);
	}
	
	public void AddCell(GridPoint prevPoint, GridPoint nextPoint){
		GridPoint[] path = CalcGridPath(prevPoint, nextPoint);
		for (int i = 0; i < path.GetLength(0)-1; ++i){
			AddStraighComponent( path[i], path[i+1], cellElementPrefab);
		}
		
	}	
	
	public void ClickCell(GridPoint point){
		ClickStraighComponent(point, cellElementPrefab);
	}
	
	
	
	
	public void AddAmeter(GridPoint point){
		AddStraighComponent(point, ameterElementPrefab);
	}
	
	public void AddAmeter(GridPoint prevPoint, GridPoint nextPoint){
		GridPoint[] path = CalcGridPath(prevPoint, nextPoint);
		for (int i = 0; i < path.GetLength(0)-1; ++i){
			AddStraighComponent( path[i], path[i+1], ameterElementPrefab);
		}
		
		
	}	
	
	
	
	
	public void AddResistor(GridPoint point){
		AddStraighComponent(point, resistorElementPrefab);
	}
	
	public void AddResistor(GridPoint prevPoint, GridPoint nextPoint){
		GridPoint[] path = CalcGridPath(prevPoint, nextPoint);
		for (int i = 0; i < path.GetLength(0)-1; ++i){
			AddStraighComponent( path[i], path[i+1], resistorElementPrefab);
		}

			
	}
	
	public bool Validate(){
		for (int x = 0; x < elements.GetLength(0); ++x){
			for (int y = 0; y < elements.GetLength(1); ++y){
				GridPoint thisPoint = new GridPoint(x, y);
				if (ElementExists (thisPoint)){
					CircuitElement thisElement = GetElement (thisPoint);
					for (int i = 0; i < 4; ++i){
						if (thisElement.isConnected[i]){
							GridPoint otherPoint = thisPoint + offsets[i];
							if (!IsPointInGrid(otherPoint)) return false;
							if (!ElementExists(otherPoint)) return false;
							if (GetElement (otherPoint).isConnected[CircuitElement.CalcInvDir(i)] == false) return false;
							
						}
					}
				}
			}
		}
		return true;
	}
	
	
	void ClickStraighComponent(GridPoint point, GameObject prefab){
		if (!IsPointInGrid(point)) return;
		
		// If there is a component here which is already baked then cannot do anything else
		if (ElementExists (point) && GetElement (point).IsComponentBaked()) return;
		
		// If there is already the same kind of straight component here, then just send it an on-click message and return
		if (ElementExists (point) && UnityEngine.Object.ReferenceEquals(GetElement (point).GetType (), prefab.GetComponent<CircuitElement>().GetType ())){
			GetElement (point).OnClick();
			return;
		}
	}
	
	void AddStraighComponent(GridPoint point, GameObject prefab){
	
		if (!IsPointInGrid(point)) return;
		
		// If there is a component here which is already baked then cannot do anything else
		if (ElementExists (point) && GetElement (point).IsComponentBaked()) return;
		
		// If there is already the same kind of straight component here, then just send it an on-click message and return
		if (ElementExists (point) && UnityEngine.Object.ReferenceEquals(GetElement (point).GetType (), prefab.GetComponent<CircuitElement>().GetType ())){
			GetElement (point).OnClick();
			return;
		}
		
		// Do a test to ensure that there are no baking restictions that stop us placing it here at all
		GridPoint upPoint = point + offsets[kUp];
		GridPoint downPoint = point + offsets[kDown];
		GridPoint leftPoint = point + offsets[kLeft];
		GridPoint rightPoint = point + offsets[kRight];
		
		
		bool upOK = 	(!ElementExists(upPoint)    || !GetElement (upPoint).IsComponentBaked()    || GetElement (upPoint).CanSetConnection(kDown, true));
		bool downOK = 	(!ElementExists(downPoint)  || !GetElement (downPoint).IsComponentBaked()  || GetElement (downPoint).CanSetConnection(kUp, true));
		bool leftOK = 	(!ElementExists(leftPoint)  || !GetElement (leftPoint).IsComponentBaked()  || GetElement (leftPoint).CanSetConnection(kRight, true));
		bool rightOK = 	(!ElementExists(rightPoint) || !GetElement (rightPoint).IsComponentBaked() || GetElement (rightPoint).CanSetConnection(kLeft, true));
		
		bool upDownOK = (upOK && downOK);
		bool leftRightOK = (leftOK && rightOK);
		
		// If we can't place one up and down or we can't place one left and right then we can't place once at all.
		if (!leftRightOK && !upDownOK) return;
			
		// Now create our new resistor
		GameObject newElement = Instantiate(
			prefab, 
			new Vector3(point.x, point.y, prefab.transform.position.z), 
			Quaternion.identity)
			as GameObject;
		newElement.transform.parent = transform;	
		newElement.GetComponent<CircuitElement>().SetGridPoint(point);
		numElementsUsed[prefab.GetComponent<SerializationID>().id]++;
		
	
		
		// Copy any connections already there to the new resistor component
		// We clean them up afterwards
		if (ElementExists (point)){
			newElement.GetComponent<CircuitElement>().CopyConnectionsFrom(GetElement (point));
			RawRemoveElement (point);
		}	
		elements[point.x, point.y] = newElement;
		
		CircuitElement  thisElement = GetElement(point);
		// If due to baking constraints, we are only allowed to go verically or horizontally, then simply place it
		// Otherwise do a whoel load of complex logic to decern where to put it
		if (!upDownOK){
			thisElement.isConnected[kLeft] = true;
			thisElement.isConnected[kRight] = true;
			if (ElementExists (leftPoint)) GetElement (leftPoint).isConnected[kRight] = true;
			if (ElementExists (rightPoint)) GetElement (rightPoint).isConnected[kLeft] = true;
			
			thisElement.SetupMesh();	
			return;
		}
		if (!leftRightOK){
			thisElement.isConnected[kUp] = true;
			thisElement.isConnected[kDown] = true;
			if (ElementExists (upPoint)) GetElement (upPoint).isConnected[kDown] = true;
			if (ElementExists (downPoint)) GetElement (downPoint).isConnected[kUp] = true;
			
			thisElement.SetupMesh();	
			return;
		}	

		// If we have not got any connections, then we should have a look at our neighbours
		// and connect to any which are close enough
		if (thisElement.CountNumConnections() == 0){
			for (int i = 0; i < 4; ++i){
				GridPoint neigbourPoint = point + offsets[i];
				if (IsPointInGrid(neigbourPoint) && ElementExists(neigbourPoint)){
					// Check that we can add ourselves
					if (GetElement (neigbourPoint).CanSetConnection(CircuitElement.CalcInvDir(i), true)){
						// Check the connection at the other side to see if it is a baked component who cannot make this connection
						GridPoint oppositPoint = point + offsets[CircuitElement.CalcInvDir(i)];
						if (IsPointInGrid(oppositPoint) && ElementExists (oppositPoint) && (!GetElement(oppositPoint).IsComponentBaked() || GetElement(oppositPoint).CanSetConnection(i, true))){
							thisElement.isConnected[i] = true;
						}
					}
				}
			}
		}
		// If we still don't have any, see if there are any places we CAN'T connect to
		// (this is a pretty horrible bit of code)
		if (thisElement.CountNumConnections() == 0){
			for (int i = 0; i < 4; ++i){
				GridPoint neigbourPoint = point + offsets[i];
				if (IsPointInGrid(neigbourPoint) && ElementExists(neigbourPoint)){
					// Check if we can connect to that
					if (!GetElement (neigbourPoint).CanSetConnection(CircuitElement.CalcInvDir(i), true)){
						// If we can't see if we could connect to the next (and its opposit)
						GridPoint newNeightbour1 = point + offsets[(i + 1) % 4];
						GridPoint newNeightbour2 = point + offsets[(i + 3) % 4];
						int ok = 0;
						if (IsPointInGrid(newNeightbour1) && IsPointInGrid (newNeightbour2)){
							if (!ElementExists (newNeightbour1) || GetElement (newNeightbour1).CanSetConnection(CircuitElement.CalcInvDir((i + 1) % 4), true)) ++ok;
							if (!ElementExists (newNeightbour2) || GetElement (newNeightbour2).CanSetConnection(CircuitElement.CalcInvDir((i + 3) % 4), true)) ++ok;
						}
						if (ok == 2) thisElement.isConnected[(i + 1) % 4] = true;
						break;
					}
				}
			}
		}
		
		// Clean up the connections to make them only valid for a resistor
		GetElement (point).ValidateConnections();
		
		ValidateNeighbourConnections(point);
		GetElement(point).SetupMesh();	
	}
	
	
	

	
	void AddStraighComponent(GridPoint prevPoint, GridPoint nextPoint, GameObject prefab){
		
		if (!IsPointInGrid(nextPoint)) return;
		
		// If there is a component here which is already baked then cannot do anything else
		if (ElementExists (nextPoint) && GetElement (nextPoint).IsComponentBaked()) return;
		
		
		int dir = CalcNeighbourDir(prevPoint, nextPoint);
		
		// Can't currently deal with putting resistor between two distant points
		if (dir == kErr) return;
		
		// Check if there is a baking restriction
		GridPoint furtherPoint = nextPoint + offsets[dir];
		if (!IsPointInGrid(furtherPoint)) return;
		if (ElementExists (furtherPoint) && GetElement(furtherPoint).IsComponentBaked() && !GetElement (furtherPoint).CanSetConnection(CircuitElement.CalcInvDir(dir), true)) return;
		if (ElementExists (prevPoint) && GetElement (prevPoint).IsComponentBaked() && !GetElement(prevPoint).CanSetConnection(dir, true)) return;
		
		// If there is no element here or the one that is here is not a resistor, then 
		// make a new element and add it into the map
		if (!ElementExists (nextPoint) ||  !UnityEngine.Object.ReferenceEquals(GetElement (nextPoint).GetType (), prefab.GetComponent<CircuitElement>().GetType ())){
			GameObject newElement = Instantiate(
				prefab, 
				new Vector3(nextPoint.x, nextPoint.y, prefab.transform.position.z), 
				Quaternion.identity)
				as GameObject;
			newElement.transform.parent = transform;	
			newElement.GetComponent<CircuitElement>().SetGridPoint(nextPoint);
			numElementsUsed[prefab.GetComponent<SerializationID>().id]++;
			
			
			// Copy any connections already there to the new resistor component
			// We clean them up aferwards
			if (ElementExists (nextPoint)){
				RawRemoveElement (nextPoint);
			}	
			elements[nextPoint.x, nextPoint.y] = newElement;
		}
		
		// Set up connections to point from pre to here (and out the other side)
		GetElement (nextPoint).ClearConnections();
		GetElement (nextPoint).isConnected[dir] = true;
		GetElement (nextPoint).isConnected[CircuitElement.CalcInvDir (dir)] = true;
		
		ValidateNeighbourConnections(nextPoint);
		
		GetElement(nextPoint).SetupMesh();
	}
	
	// Add the element without doing anything clever (you need to call SetupMesh yourself afterwards()
	void RawAddElement(GridPoint point, GameObject prefab){
		if (!IsPointInGrid(point)) return;
		
		// If there is already something there, then don't add it (and assert)
		if (ElementExists (point)){
			Debug.LogError("Attempting to RAW add a circuit element over one that is there already");
		}
		
		GameObject newElement = Instantiate(
			prefab, 
			new Vector3(point.x, point.y, prefab.transform.position.z), 
			Quaternion.identity)
			as GameObject;
		newElement.transform.parent = transform;
		newElement.GetComponent<CircuitElement>().SetGridPoint(point);
		numElementsUsed[prefab.GetComponent<SerializationID>().id]++;
		
		
		elements[point.x, point.y] = newElement;
		GetElement(point).SetupMesh();			
	}
	
	// Circuit modification interface
	public void AddWire(GridPoint point){
	
		if (!IsPointInGrid(point)) return;
		
		// If there is already a wire here, then nothing to do
		if (ElementExists (point) && GetElement (point) as CircuitElementWire) return;
		
		// Or if the element exists, it is not a wire, but some of its connections are baked, then nothing to do
		if (ElementExists (point) && GetElement(point).IsComponentBaked()) return;
		
		
		GameObject newElement = Instantiate(
			wireElementPrefab, 
			new Vector3(point.x, point.y, wireElementPrefab.transform.position.z), 
			Quaternion.identity)
			as GameObject;
		newElement.transform.parent = transform;
		newElement.GetComponent<CircuitElement>().SetGridPoint(point);
		numElementsUsed[wireElementPrefab.GetComponent<SerializationID>().id]++;
		
		
		// Copy any connections already there to the new component
		if (ElementExists (point)){
			newElement.GetComponent<CircuitElement>().CopyConnectionsFrom(GetElement (point));
			RawRemoveElement (point);
		}
		
		elements[point.x, point.y] = newElement;
		GetElement(point).SetupMesh();			
	}
	
	public void AddWire(GridPoint prevPoint, GridPoint nextPoint){
	
		GridPoint[] path = CalcGridPath(prevPoint, nextPoint);
		for (int i = 0; i < path.GetLength(0)-1; ++i){
			AddWireRaw( path[i], path[i+1]);
		}
		
	}
	
	void AddWireRaw(GridPoint prevPoint, GridPoint nextPoint){
	
		if (!IsPointInGrid(nextPoint)) return;
		
		int dir = CalcNeighbourDir(prevPoint, nextPoint);
		if (dir == kErr){
			Debug.LogError("Trying to connect distant points?");
		}
		// If the place we are trying to connect to has a baked element which will not let us connect
		if (ElementExists(nextPoint) && GetElement(nextPoint).IsComponentBaked() && !GetElement (nextPoint).CanSetConnection(CircuitElement.CalcInvDir(dir), true)) return;
		
	
		// First we just add the new point (the old oneshould already be there)
		AddWire(nextPoint);
		
		if (IsPointInGrid(prevPoint)){
			if (GetElement (prevPoint).CanSetConnection(dir, true))
			{
				GetElement (prevPoint).isConnected[dir] = true;
				GetElement (nextPoint).isConnected[CircuitElement.CalcInvDir (dir)] = true;
			}
		}
		GetElement(nextPoint).SetupMesh();		
	}
	
	
	public void Erase(GridPoint prevPoint, GridPoint nextPoint){
		GridPoint[] path = CalcGridPath(prevPoint, nextPoint);
		for (int i = 0; i < path.GetLength(0); ++i){
			Erase( path[i]);
		}
	}
	
	
	public void EraseConnection(GridPoint thisPoint, GridPoint otherPoint){
	
		// First check that we have elements in these positions
		if (!IsPointInGrid(thisPoint) || !ElementExists(thisPoint) || !IsPointInGrid(otherPoint) || !ElementExists(otherPoint)) return;
			
		// Find the directions of the two connections to remove
		CircuitElement thisElement = GetElement(thisPoint);
		CircuitElement otherElement = GetElement (otherPoint);
		
		int thisDir = CalcNeighbourDir(thisPoint, otherPoint);
		int otherDir = CircuitElement.CalcInvDir(thisDir);
		
		// If either of them are baked, then nothing to do
		if (thisElement.isBaked[thisDir] || otherElement.isBaked[otherDir]) return;
		
		// If we can't set these connection to removed, then replace this elment with a wire
		if (!thisElement.CanSetConnection(thisDir, false)){
			AddWire (thisPoint);
			thisElement = GetElement (thisPoint);	
		}
		
		if (!otherElement.CanSetConnection(otherDir, false)){
			AddWire (otherPoint);
			otherElement = GetElement (otherPoint);	
		}
		
		// NOw we know we can repalce them - so do it
		thisElement.isConnected[thisDir] = false;
		otherElement.isConnected[otherDir] = false;
		
		// Check if we have made a component with no connections and if so, remove it
		if (thisElement.CountNumConnections() == 0) RawRemoveElement(thisPoint);
		if (otherElement.CountNumConnections() == 0) RawRemoveElement(otherPoint);
			
		
	}
		
		
	public void Erase(GridPoint point){
		Erase (point, true);
	}
	
	// Set doCount to false if we don't want to replenish out "numebr of elements left" counter
	public void Erase(GridPoint point, bool doCount){
	
		if (!IsPointInGrid(point) || !ElementExists (point)) return;
		
		CircuitElement thisElement = GetElement (point);
		
		// First remove connections
		for (int i = 0; i < 4; ++i){
			// If the connection we are trying to remove is baked, then do not remove it
			if (thisElement.isBaked[i]) continue;
			
			GridPoint otherPoint = point + offsets[i];
			
			if (IsPointInGrid(otherPoint) && ElementExists(otherPoint))
			{
				CircuitElement otherElement = GetElement (otherPoint);
				
				// If we can't remove this connection, replace this neighbour with a wire 
				if (!otherElement.CanSetConnection(CircuitElement.CalcInvDir(i), false)){
					AddWire (otherPoint);
					otherElement = GetElement (otherPoint);
				}
						
				otherElement.isConnected[CircuitElement.CalcInvDir(i)] = false;
				if (otherElement.CountNumConnections() == 0) RawRemoveElement(otherPoint);
				
				// Remove the connect on this element to
				thisElement.isConnected[i] = false;
			}
			
		}
		
		// If we have managed to remove all out connections, then remove the elment
		
		// Now remove the element
		if (!thisElement.HasAnyConnections(true, true, true,true)){
			RawRemoveElement(point, doCount);
		}
		
	}
	
	// Given a circuit element (with a set of connections) - this ensures the neighbours
	// all conform to these connections. If 
	void ValidateNeighbourConnections(GridPoint point){
		CircuitElement thisElement = GetElement (point);
		// Cycle through the 4 neighbours
		for (int i = 0; i < 4; ++i){
			GridPoint neighbourPoint = point + offsets[i];
			if (IsPointInGrid(neighbourPoint)){
				// If the neighbour doesn't exist
				if (!ElementExists(neighbourPoint)){
					// and if we are indeed trying to make a connection
					if (thisElement.isConnected[i]){
						// then add the wire
						AddWire (point, neighbourPoint);
					}
					else{
						// Otehrwise, do nothing to the neighbour
					}
				}
				else{
					// If there is something there, ensure we can set the connection
					CircuitElement neighbourElement = GetElement (neighbourPoint);
					bool prevConnectionState = neighbourElement.isConnected[CircuitElement.CalcInvDir(i)];
					if (neighbourElement.CanSetConnection(CircuitElement.CalcInvDir(i), thisElement.isConnected[i])){
						// If we can, then do so
						neighbourElement.isConnected[CircuitElement.CalcInvDir(i)] = thisElement.isConnected[i];
					}
					else{
						// Otherwise...
						// if We are trying to make a connection,
						if (thisElement.isConnected[i])	{
							// make it a wire
							AddWire (point, neighbourPoint);
						}
						else{
							// Otherwise, ensure it is a wire. but remove the connection to this
							AddWire (neighbourPoint);
							GetElement (neighbourPoint).isConnected[CircuitElement.CalcInvDir(i)] = false;
						}
					}
					// The neighbouring element does exist and we have changed our conneciotn status to is
					// make sure we didn't leave any straglers on it
					// Do this by checking if any of ITs immediate neighbours are dead ends or if it has neigbours
					neighbourElement = GetElement (neighbourPoint);
					if (prevConnectionState != neighbourElement.isConnected[CircuitElement.CalcInvDir(i)])
					{
						if (neighbourElement.CountNumConnections() == 0){
							Erase (neighbourPoint);
						}
						for (int j = 0; j < 4; ++j){
							GridPoint newNeighbour = neighbourPoint + offsets[j];
							if (IsPointInGrid(newNeighbour) && ElementExists(newNeighbour) && GetElement (newNeighbour).CountNumConnections() == 1){
								// Check that we can actually remove this from our neighbour
								if (neighbourElement.CanSetConnection(i, false))
									Erase (newNeighbour);
							}
						}
					}
				}		
			}
		}
		
	}
	

	
	void RawRemoveElement(GridPoint point){
		if (ElementExists(point)){
			numElementsUsed[GetElement (point).GetComponent<SerializationID>().id]--;
		}
		GameObject.Destroy(elements[point.x, point.y]);
		elements[point.x, point.y] = null;	
	}
	
	void RawRemoveElement(GridPoint point, bool doCount){
		if (ElementExists(point) && doCount){
			numElementsUsed[GetElement (point).GetComponent<SerializationID>().id]--;
		}
		GameObject.Destroy(elements[point.x, point.y]);
		elements[point.x, point.y] = null;	
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
		if (!IsPointInGrid(point))
			Debug.LogError ("Element at point " + point.x + ", " + point.y + " is not in the grid");
		return (elements[point.x, point.y] != null);
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
		if (!ElementExists(point)) 
			Debug.LogError ("Element at poin t" + point.x + ", " + point.y + " does not exists");
		return elements[point.x, point.y].GetComponent<CircuitElement>();
	}
	
	public GameObject GetGameObject(GridPoint point){
		if (!ElementExists(point)) 
			Debug.LogError ("Element at poin t" + point.x + ", " + point.y + " does not exists");
		return elements[point.x, point.y];
	}
	
	void Awake(){
		singleton = this;
		offsets[kLeft] = 	new GridPoint(-1,  0);
		offsets[kRight] = 	new GridPoint( 1,  0);
		offsets[kUp] = 		new GridPoint( 0,  1);
		offsets[kDown] = 	new GridPoint( 0,  -1);	
		
		
		grid = gridGO.GetComponent<Grid>();
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
		
		numElementsUsed = new Dictionary<string, int>();
		numElementsUsed.Add (wireElementPrefab.GetComponent<SerializationID>().id, 0);
		numElementsUsed.Add (resistorElementPrefab.GetComponent<SerializationID>().id, 0);
		numElementsUsed.Add (cellElementPrefab.GetComponent<SerializationID>().id, 0);
		numElementsUsed.Add (ameterElementPrefab.GetComponent<SerializationID>().id, 0);
	}
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
