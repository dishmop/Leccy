using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class LevelSerializer : MonoBehaviour {
	public GameObject gridGO;
	public GameObject circuitGO;
	public GameObject GlobalGO;
	
	
	Grid			grid;
	Circuit			circuit;
	LevelSettings	levelSettings;
	
	void Start(){
		grid = gridGO.GetComponent<Grid>();	
		circuit = circuitGO.GetComponent<Circuit>();	
		levelSettings = GlobalGO.GetComponent<LevelSettings>();
	}
	

	public void SaveLevel(string filename){
		FileStream file = File.Create(BuildFullPath(filename));
		BinaryWriter bw = new BinaryWriter(file);
		
		grid.Save(bw);
		circuit.Save(bw);
		levelSettings.Save(bw);

		file.Close();
	}
	
	
	string BuildFullPath(string filename){
		return Application.dataPath + "/Levels/" + filename;
		//return Application.persistentDataPath + "/" + filename;
		
	}


	
	public void LoadLevel(string filename){
		if (File.Exists(BuildFullPath(filename))){
			FileStream file = File.OpenRead(BuildFullPath(filename));
			BinaryReader br = new BinaryReader(file);
			
			grid.Load(br);
			circuit.Load(br);
			levelSettings.Load(br);
			
			file.Close();
		}	
	}
}
