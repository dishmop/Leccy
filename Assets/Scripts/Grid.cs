using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;

public class Grid : MonoBehaviour {

	public static Grid singleton = null;

	public int gridWidth;
	public int gridHeight;
	public GameObject gridSquarePrefab;
	public GameObject gridCentrePrefab;
	public GameObject gridSpokePrefab;
	public GameObject emptyGO;
	public GameObject[,] gridObjects = null;
	
	const int		kLoadSaveVersion = 1;		


	public 	void Save(BinaryWriter bw){
		bw.Write (kLoadSaveVersion);
		bw.Write (gridWidth);
		bw.Write (gridHeight);
	}
	
	public 	void Load(BinaryReader br){
		int oldGridWidth = gridWidth;
		int oldGridHeight = gridHeight;
		
		int version = br.ReadInt32();
		switch (version){
			case kLoadSaveVersion:{
				gridWidth = br.ReadInt32();	
				gridHeight = br.ReadInt32();	
				break;
			}
		}
		// If things have chnaged since we last made the grid, remake it
		if (oldGridWidth != gridWidth || oldGridHeight != gridHeight){
			CreateGrid();
		}
	}
	

	public void RebuildGridPoints(List<GridPoint> changedGridPoints){
		foreach (GridPoint point in changedGridPoints){
			Circuit.AnchorData data = Circuit.singleton.GetAnchors(point);
			
			// If we have one here already then remove it and rebuild it
			Destroy (gridObjects[point.x, point.y]);
			gridObjects[point.x,point.y] = ConstructBespokeGridObject(point);
			
			
			// Do modifications to the grid
			gridObjects[point.x,point.y].SetActive(!data.disableGrid);
			
			// See if there are any around it that are not hidden. If there are, we hide the spokes pointing this way
			for (int i = 0; i < 4; ++i){
				GridPoint newPoint = point + Circuit.singleton.offsets[i];
				
				// If this new point is not on the grid, then don't do anyhting
				if (!IsPointInGrid(newPoint)) continue;
				
				
				// If this new grid points is als disabled - then do nothing
				if (Circuit.singleton.GetAnchors(newPoint).disableGrid) continue;
				
				// Remove the existing one
				Destroy (gridObjects[newPoint.x, newPoint.y]);
				gridObjects[newPoint.x, newPoint.y] = null;
				gridObjects[newPoint.x, newPoint.y] = ConstructBespokeGridObject(newPoint);
				
				                     
			}
			
		}
	}
	
	GameObject ConstructBespokeGridObject(GridPoint point){
	
		// First check if all the connections are fine and if they are, we just make our standard grid object
		int numSpokes = 0;
		for (int i = 0; i < 4; ++i){
			GridPoint otherPoint = point + Circuit.singleton.offsets[i];
			if (!IsPointInGrid(otherPoint)) continue;
			Circuit.AnchorData data = Circuit.singleton.GetAnchors(otherPoint);
			if (!data.disableGrid){
				numSpokes++;
			}
		}
		if (numSpokes == 4){
			GameObject newObj = Instantiate(
				gridSquarePrefab, 
				new Vector3(point.x , point.y , 0), 
				Quaternion.identity)
				as GameObject;
			newObj.transform.parent = transform;
			return newObj;
			
		}
	
		// Make an empty object to act as our parent
		GameObject bespokeGridObj = Instantiate(emptyGO, new Vector3((float)point.x, (float) point.y, 0f), Quaternion.identity) as GameObject;
		bespokeGridObj.transform.parent = transform;
		
		
		float[] angles = new float[4];
		angles[0] = 0;
		angles[1] = -90;
		angles[2] = 180;
		angles[3] = 90;
		
		for (int i = 0; i < 4; ++i){
			GridPoint otherPoint = point + Circuit.singleton.offsets[i];
			if (!IsPointInGrid(otherPoint)) continue;
			Circuit.AnchorData data = Circuit.singleton.GetAnchors(otherPoint);
			if (!data.disableGrid){
				GameObject newSpoke = Instantiate(gridSpokePrefab, new Vector3((float)point.x, (float) point.y, 0f), Quaternion.Euler(0f, 0f, angles[i])) as GameObject;
				newSpoke.transform.parent = bespokeGridObj.transform;
			}
		}
		
		// We we had anything to render, then make a centre
		if (numSpokes > 0){
			GameObject newCentre = Instantiate(gridCentrePrefab, new Vector3((float)point.x, (float) point.y, 0f), Quaternion.identity) as GameObject;
			newCentre.transform.parent = bespokeGridObj.transform;
		}
		bespokeGridObj.name = "BespokeGridObj";
		return bespokeGridObj;
	}
	
	
	public bool IsPointInGrid(GridPoint point){
		return 	point.x >= 0 &&
				point.y >= 0 &&
				point.x < gridWidth &&
				point.y < gridHeight;		
	}

	public bool IsPointInGrid(int x, int y){
		return 	x >= 0 &&
				y >= 0 &&
				x < gridWidth &&
				y < gridHeight;		
	}			
	
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
	}
	
	void OnDestroy(){
		singleton = null;
	}
	
	
	
	void CreateGrid(){
		// if we already have a load of grig obejcts, destroy them all
		if (gridObjects != null){
			for (int x = 0; x < gridWidth; ++x){
				for (int y = 0; y < gridHeight; ++y){
					Destroy(gridObjects[x,y]);
				}
				
			}
			
		}
		
		gridObjects = new GameObject[gridWidth, gridHeight];
	
		for (int x = 0; x < gridWidth; ++x){
			for (int y = 0; y < gridHeight; ++y){
				gridObjects[x,y] = ConstructBespokeGridObject(new GridPoint(x,y));
			}
			
		}
		
	}
	

	// Use this for initialization
	void Start () {
		CreateGrid ();		
	
	}
	

}
