using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
//using System.Collections.Generic;
//using UnityEngine.Analytics;

public class LevelManager : MonoBehaviour {
	public static LevelManager singleton = null;
	
	public string 		levelToSave = "DefaultLevel";
	public TextAsset[]	levelsToLoad = new TextAsset[10];
	public int			currentLevelIndex = 1;
	public int			tutorialIndex;
	
	byte[]				cachedLevel = new byte[100 * 1024];	// 100K
	
	
	
	 public enum SaveMode{
		kSaveAll,
		kSaveFactory,
		kSaveNothing,
		kSaveAnchors,
		kSaveAnchorsAndFactory
	};
	
	public SaveMode saveMode = SaveMode.kSaveAll;
	
	const int		kLoadSaveVersion = 2;	
		
	// Manage which level to load and then pass the file name to the loading function that does the work
	public void LoadLevel(){
		LoadLevel (currentLevelIndex);
	}
	
	public bool IsTutorial(){
		return currentLevelIndex < tutorialIndex;
	}
	
	public bool LoadLevel(int index){
		if (index >=0 && index < levelsToLoad.Length && levelsToLoad[index] != null){
//			Debug.Log ("LoadLevel (): levelsToLoad[" + index + "].name = " + levelsToLoad[index].name);
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
		if (currentLevelIndex >= LevelManager.singleton.levelsToLoad.Count()) return "Out of range";
		if (IsTutorial()) return "Tutorial";
		return	LevelManager.singleton.levelsToLoad[currentLevelIndex].name.Substring(3);
	}
	
	public string GetRawLevelName(){
//		Debug.Log("GetRawLevelName. Count() = " + levelsToLoad.Count () + " currentLevelIndex = " + currentLevelIndex);
//		Debug.Log ("GetRawLevelName (): levelsToLoad[" + currentLevelIndex + "].name = " + levelsToLoad[currentLevelIndex].name);
		
		return levelsToLoad[currentLevelIndex].name;
	
	}
	
	public int GetIndexOfLevel(string name){
		
		for (int i = 0; i < LevelManager.singleton.levelsToLoad.Count(); ++i){
			if (LevelManager.singleton.levelsToLoad[i].name == name){
				return i;
			}
		}
		return -1;
	}
	
	
	public void PreviousLevel(){
		
		saveMode = SaveMode.kSaveNothing;
		while (saveMode != SaveMode.kSaveAll && currentLevelIndex > 0){
			currentLevelIndex--;
			LoadLevel();
		}
		GameModeManager.singleton.ResetSidePanel();
		
		// See if there is some tutorial text associated with this
		Tutorial.singleton.Deactivate();
		GameModeManager.singleton.ActivateTutorialText();
		
	}
	
	
	// Does the actual serialising
	public bool LoadLevel(string filename){
		if (filename == null){
			Debug.Log ("Attempting to load null level");
			return false;
		}
//		Debug.Log("loadLevel - levelName: " + filename + ", gameTime: " + GameModeManager.singleton.GetGameTime());
		GoogleAnalytics.Client.SendTimedEventHit("gamePlay", "loadLevel", filename, GameModeManager.singleton.GetGameTime());
//		Analytics.CustomEvent("loadLevel", new Dictionary<string, object>
//		{
//			{ "levelName", filename },
//			{ "gameTime", GameModeManager.singleton.GetGameTime()},
//		});
				
		String path = BuildResourcePath(filename);
		// Debug.Log("LoadLevel: " + path);
		TextAsset asset = Resources.Load(path) as TextAsset;
		if (asset != null){
//			Debug.Log ("Loading asset: " + filename);
			Stream s = new MemoryStream(asset.bytes);
			DeserializeLevel(s);
//			Resources.UnloadAsset(asset);
			
			// After loading a level, call an update to ensure we don't get a frame rendered befroe the simulation has calculated
			GameModeManager.singleton.BulkGameUpdate();
			
			// We have succesfully loaded in a new level - so save this one to the cache
			CacheLevel();
		}	
		else{
			Debug.Log ("Failed to load asset");
			return false;
		}
		return true;
	}
	
	// Stores the current level to memory 
	public void CacheLevel(){
		CacheLevel(cachedLevel);
		
		
	}	
	
	// Stores the current level to memory 
	public void CacheLevel(byte[] storage){
		SaveMode oldMode = saveMode;
		saveMode = SaveMode.kSaveAll;
		Stream s = new MemoryStream(storage);
		
		SerializeLevel(s);
		saveMode = oldMode;
		
	}	
	
	
	// Does the actual serialising
	public void ReloadFromCache(){
		ReloadFromCache(cachedLevel);
	}	
	
	// Does the actual serialising
	public void ReloadFromCache(byte[] storage){
		Debug.Log ("Loading from cache");
		Stream s = new MemoryStream(storage);
		DeserializeLevel(s);
		
		// After loading a level, call an update to ensure we don't get a frame rendered befroe the simulation has calculated
		GameModeManager.singleton.BulkGameUpdate();
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
		bw.Write ((int)saveMode);				
		if (saveMode == SaveMode.kSaveAll || saveMode == SaveMode.kSaveAnchors || saveMode == SaveMode.kSaveAnchorsAndFactory){
			Grid.singleton.Save(bw);
			if (saveMode == SaveMode.kSaveAnchors || saveMode == SaveMode.kSaveAnchorsAndFactory){
				Circuit.singleton.saveMode = Circuit.SaveMode.kSaveAnchors;
			}
			else{
				Circuit.singleton.saveMode = Circuit.SaveMode.kSaveAll;
			}
			Circuit.singleton.Save(bw);
		}
		if (saveMode == SaveMode.kSaveAll || saveMode == SaveMode.kSaveFactory || saveMode == SaveMode.kSaveAnchorsAndFactory){
			ElementFactory.singleton.Save(bw);
		}
	}

	public void DeserializeLevel(Stream stream){
		BinaryReader br = new BinaryReader(stream);
		
		int version = br.ReadInt32();
		switch (version){
			case kLoadSaveVersion:{
				saveMode = (SaveMode)br.ReadInt32 ();
				if(saveMode == SaveMode.kSaveAll || saveMode == SaveMode.kSaveAnchors || saveMode == SaveMode.kSaveAnchorsAndFactory){
				    Grid.singleton.Load(br);
					Circuit.singleton.Load(br);
				}
				if (saveMode == SaveMode.kSaveAll || saveMode == SaveMode.kSaveFactory || saveMode == SaveMode.kSaveAnchorsAndFactory){
					ElementFactory.singleton.Load(br);
				}
				
				// Ensure the meshes and all rebuilt to reflect the new level state
				Camera.main.GetComponent<CamControl>().CentreCamera();
				UI.singleton.OnLoadLevel();
				break;
			}		
			case 1:{
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
