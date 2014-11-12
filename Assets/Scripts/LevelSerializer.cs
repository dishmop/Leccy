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
#if UNITY_EDITOR		
		FileStream file = File.Create(BuildFullPath(filename));
		
		BinaryWriter bw = new BinaryWriter(file);
		
		grid.Save(bw);
		circuit.Save(bw);
		levelSettings.Save(bw);

		file.Close();
		
		// Ensure the assets are all realoaded and the cache cleared.

		UnityEditor.AssetDatabase.Refresh();
#endif
	}
	
	
	string BuildFullPath(string filename){
		return Application.dataPath + "/Resources/Levels/" + filename;
		//return Application.persistentDataPath + "/" + filename;
		
	}
	
	string BuildResourcePath(string filename){
		String filenameWIthoutExtentions = filename.Substring(0, filename.Length - 6);
		return "Levels/" + filenameWIthoutExtentions;
		//return Application.persistentDataPath + "/" + filename;
		
	}	


	
	public void LoadLevel(string filename){
		if (filename == null){
			Debug.Log ("Attempting to load null level");
			return;
		}
		String path = BuildResourcePath(filename);
		Debug.Log("LoadLevel: " + path);
		TextAsset asset = Resources.Load(path) as TextAsset;
		if (asset != null){
			Debug.Log ("Loading asset");
			Stream s = new MemoryStream(asset.bytes);
			BinaryReader br = new BinaryReader(s);
			
//			FileStream file = File.OpenRead(BuildFullPath(filename));
//			BinaryReader br = new BinaryReader(file);
			
			grid.Load(br);
			circuit.Load(br);
			levelSettings.Load(br);
			
			Resources.UnloadAsset(asset);
		}	
		else{
			Debug.Log ("Failed to load asset");
		}
	}
}
