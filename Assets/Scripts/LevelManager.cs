using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class LevelManager : MonoBehaviour {
	public static LevelManager singleton = null;
	
	public string 		levelToSave = "DefaultLevel";
	public TextAsset[]	levelsToLoad = new TextAsset[10];
	public int			currentLevelIndex = 0;
	
	
	// Manage which level to load and then pass the file name to the loading function that does the work
	public void LoadLevel(){
		if (currentLevelIndex < levelsToLoad.Length && levelsToLoad[currentLevelIndex] != null){
			LoadLevel(levelsToLoad[currentLevelIndex].name);
		}
	}
	
	public void SaveLevel(){
		SaveLevel(levelToSave);	
	}	

	public void ClearLevel(){
		Circuit.singleton.CreateBlankCircuit();
		Circuit.singleton.RefreshAll();
		
	}
	
	
	
	// Does the actual serialising
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
						
			Grid.singleton.Load(br);
			Circuit.singleton.Load(br);
			ElementFactory.singleton.Load(br);
			
			// Ensure the meshes and all rebuilt to reflect the new level state
			Simulator.singleton.ClearSimulation();
			Camera.main.GetComponent<CamControl>().CentreCamera();
			UI.singleton.OnLoadLevel();
			
			Resources.UnloadAsset(asset);
		}	
		else{
			Debug.Log ("Failed to load asset");
		}
	}
	
	// Does the actual serialising
	public void SaveLevel(string filename){
#if UNITY_EDITOR		
		FileStream file = File.Create(BuildFullPath(filename));
		
		BinaryWriter bw = new BinaryWriter(file);
		
		Grid.singleton.Save(bw);
		Circuit.singleton.Save(bw);
		ElementFactory.singleton.Save(bw);
		
		file.Close();
		
		// Ensure the assets are all realoaded and the cache cleared.
		UnityEditor.AssetDatabase.Refresh();
#endif
	}	
	
	public bool IsOnLastLevel(){
		return currentLevelIndex == levelsToLoad.GetLength(0) - 1;
	}
	
	// We saving using the standard file system
	string BuildFullPath(string filename){
		return Application.dataPath + "/Resources/Levels/" + filename + ".bytes";
		//return Application.persistentDataPath + "/" + filename;
		
	}
	
	// We load using the resources
	string BuildResourcePath(string filename){
		return "Levels/" + filename;
		
	}	
	
	
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
	}
	
	void OnDestroy(){
		singleton = null;
	}				

}
