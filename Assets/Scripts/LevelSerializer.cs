using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class LevelSerializer : MonoBehaviour {
	public GameObject gridGO;
	public GameObject circuitGO;
	
	
	Grid	grid;
	Circuit	circuit;
	
	void Start(){
		grid = gridGO.GetComponent<Grid>();	
		circuit = circuitGO.GetComponent<Circuit>();	
	}
	

	public void SaveLevel(string filename){
		FileStream file = File.Create(Application.persistentDataPath + "/" + filename);
		BinaryWriter bw = new BinaryWriter(file);
		
		grid.Save(bw);
		circuit.Save(bw);

		file.Close();
	}


	
	public void LoadLevel(string filename){
		if (File.Exists(Application.persistentDataPath + "/" + filename)){
			FileStream file = File.OpenRead(Application.persistentDataPath + "/" + filename);
			BinaryReader br = new BinaryReader(file);
			
			grid.Load(br);
			circuit.Load(br);
			file.Close();
		}	
	}
}
