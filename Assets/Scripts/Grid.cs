using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class Grid : MonoBehaviour {

	public static Grid singleton = null;

	public int gridWidth;
	public int gridHeight;
	public float squareWidth = 1f;
	public float squareHeight = 1f;	
	public GameObject gridSquarePrefab;
	
	


	public 	void Save(BinaryWriter bw){
		bw.Write (gridWidth);
		bw.Write (gridHeight);
	}
	
	public 	void Load(BinaryReader br){
		gridWidth = br.ReadInt32();	
		gridHeight = br.ReadInt32();	
		
		CreateGrid();
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
	
		for (int x = 0; x < gridWidth; ++x){
			for (int y = 0; y < gridHeight; ++y){
				GameObject gridSquare = Instantiate(
					gridSquarePrefab, 
					new Vector3(x * squareWidth, y * squareHeight, 0), 
					Quaternion.identity)
					as GameObject;
				gridSquare.transform.parent = transform;
			}
			
		}
		
	}
	

	// Use this for initialization
	void Start () {
		CreateGrid ();		
	
	}
	

}
