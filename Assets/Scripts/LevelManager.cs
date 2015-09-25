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
	
	const int		kLoadSaveVersion = 1;	
		
	// Manage which level to load and then pass the file name to the loading function that does the work
	public void LoadLevel(){
		LoadLevel (currentLevelIndex);
	}
	
	public bool LoadLevel(int index){
		if (index >=0 && index < levelsToLoad.Length && levelsToLoad[index] != null){
			return LoadLevel(levelsToLoad[index].name);
		}
		return false;
	}
	

	
	public void SaveLevel(){
		SaveLevel(levelToSave);	
	}	
	
	public void SaveLevel(int index){
		if (index >=0 && index < levelsToLoad.Length && levelsToLoad[index] != null){
			SaveLevel(levelsToLoad[index].name);
		}
	}	
	
	public void ResaveAllLevels(){
		for (int i = 0; i < levelsToLoad.GetLength(0); ++i){
			bool ok = LoadLevel (i);
			if (ok){
				SaveLevel (i);
				Debug.Log("Level " + i + ": " + levelsToLoad[i].name + " resaved");
			}
		}
	}

	public void ClearLevel(){
		Circuit.singleton.CreateBlankCircuit();
		Circuit.singleton.RefreshAll();
		
	}
	
	public string GetCurrentLevelName(){
		return	"Level: " + currentLevelIndex  + " - " + LevelManager.singleton.levelsToLoad[currentLevelIndex].name.Substring(2);
	}
	
	public string GetRawLevelName(){
		return LevelManager.singleton.levelsToLoad[currentLevelIndex].name;
	
	}
	
	
	public void PreviousLevel(){
		currentLevelIndex--;
		LoadLevel();
		
	}
	
	
	// Does the actual serialising
	public bool LoadLevel(string filename){
		if (filename == null){
			Debug.Log ("Attempting to load null level");
			return false;
		}
		String path = BuildResourcePath(filename);
		Debug.Log("LoadLevel: " + path);
		TextAsset asset = Resources.Load(path) as TextAsset;
		if (asset != null){
			Debug.Log ("Loading asset");
			Stream s = new MemoryStream(asset.bytes);
			DeserializeLevel(s);
			Resources.UnloadAsset(asset);
			
			// After loading a level, call an update to ensure we don't get a frame rendered befroe the simulation has calculated
			GameModeManager.singleton.BulkGameUpdate();
		}	
		else{
			Debug.Log ("Failed to load asset");
			return false;
		}
		return true;
	}
	
	// Does the actual serialising
	public void SaveLevel(string filename){
#if UNITY_EDITOR		
		FileStream file = File.Create(BuildFullPath(filename));
		
		SerializeLevel(file);

		
		file.Close();
		
		// Ensure the assets are all realoaded and the cache cleared.
		UnityEditor.AssetDatabase.Refresh();
#endif
	}	
	
	public void SerializeLevel(Stream stream){
		BinaryWriter bw = new BinaryWriter(stream);
		
		bw.Write (kLoadSaveVersion);					
		Grid.singleton.Save(bw);
		Circuit.singleton.Save(bw);
		ElementFactory.singleton.Save(bw);
	}

	public void DeserializeLevel(Stream stream){
		BinaryReader br = new BinaryReader(stream);
		
		int version = br.ReadInt32();
		switch (version){
		case kLoadSaveVersion:{
			Grid.singleton.Load(br);
			Circuit.singleton.Load(br);
			ElementFactory.singleton.Load(br);
			
			// Ensure the meshes and all rebuilt to reflect the new level state
			Camera.main.GetComponent<CamControl>().CentreCamera();
			UI.singleton.OnLoadLevel();
			break;
		}
	}
		
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
