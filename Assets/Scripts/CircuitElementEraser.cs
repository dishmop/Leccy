using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CircuitElementEraser : CircuitElement {

	public GameObject 	eraserPrefab;
	
	
	GridPoint			otherPoint;
	GridPoint			lastOtherPoint;
	

	public void Start(){
		Debug.Log ("CircuitElementEraser:Start()");
	}
	
	public void Awake(){
		CreateDisplayMesh();	
	}


	public override string GetUIString(){
		return "Eraser";
	}	

	
	

	public override bool IsWired(){
		return false;
	}
	

	// Some elements have the notion of another grid point which they use for UI purposes
	public override void SetOtherGridPoint(GridPoint otherPoint){
		if (lastOtherPoint == null || !lastOtherPoint.IsEqual(otherPoint)){
			this.lastOtherPoint = this.otherPoint;
			if (otherPoint != null){
				this.otherPoint = new GridPoint(otherPoint);
			}
			else{
				this.otherPoint = null;
			}
				
			RebuildMesh();
		}
	}	

	
	void CreateDisplayMesh(){
		Destroy(GetDisplayMesh ());
		displayMesh = Instantiate(eraserPrefab, gameObject.transform.position, Quaternion.Euler(0, 0, orient * 90)) as GameObject;
		displayMesh.name = displayMeshName;
		displayMesh.transform.parent = transform;

		
		RebuildMesh();	
	}
	
	public override void RebuildMesh(){
		base.RebuildMesh();
		
		
		if (thisPoint != null && otherPoint != null){
			Vector3 newLocalPos = new Vector3((otherPoint.x - thisPoint.x) * 0.5f, (otherPoint.y - thisPoint.y) * 0.5f, 0f);
			displayMesh.transform.localPosition = newLocalPos;
		}		
		else{
			displayMesh.transform.localPosition = new Vector3(0, 0, 0);
		}
		
	}
	
	
	
	
	
	
	
	// Update is called once per frame
	void Update () {
		HandleDisplayMeshChlid();
		HandleAlpha();
		

		
	}
}
