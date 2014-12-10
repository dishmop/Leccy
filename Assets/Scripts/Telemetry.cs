using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;

public class Telemetry : MonoBehaviour {

	public static Telemetry singleton = null;
	
	public bool enable;
	public string gameName;
	
	public string playbackFilename;


	public string gameVersion = null;

	public enum Mode{
		kRecord,
		kPlayback
	};
	

	// Flags about what has happened in this frame. try not to delete these as they may be needed to 
	// parse older files (event enums are stored as strings)
	public enum TelemetryEvent{
		kCircuitChanged,
		kNewGameStarted,
		kGameFinished,
		kLevelStarted,
		kLevelCompleteWait,
		kLevelComplete,
		kGameComplete,
		kApplicationQuit,
		kMouseMove
		
	};
	
	
	List<TelemetryEvent> frameEvents = new List<TelemetryEvent>();
	Mode mode = Mode.kRecord;
	
	// Additional Header info
	int    thisLoadSaveVersion;
	string yyyymmdd;
	string hhmmss;
	string machineGUID;
	string fileGuid;
	
	const int		kLoadSaveVersion = 1;		
	
	
	Stream	ioStream = null;
	
	public void SetMode(Mode m){
		mode = m;
		
	}
	
	// Inform the telemetry system that a particular event has occured this frame
	public void RegisterEvent(TelemetryEvent e){
		frameEvents.Add (e);
	}
	
	// Update is called once per frame
	public void GameUpdate () {
	
		switch (mode){
			case Mode.kRecord:{
				RecordUpdate();
				break;
			}
			case Mode.kPlayback:{
				PlaybackUpdate();
				break;
			}
		}


	}

	void RecordUpdate(){
		if (!enable){
			frameEvents.Clear();
			return;
		}
		// If no events then do nothing
		if (frameEvents.Count == 0){
			return;
		}
		
		// If starting a new game - special case - create a file
		if (frameEvents[0] == TelemetryEvent.kNewGameStarted){
			OpenFileForWriting();
			frameEvents.RemoveAt(0);
		}
		
		BinaryWriter bw = new BinaryWriter(ioStream);
		// Always write the time of the event (from game start time).
		bw.Write (GameModeManager.singleton.GetGameTime());
		
		while (frameEvents.Count != 0){
			TelemetryEvent e = frameEvents[0];
			
			bw.Write (e.ToString());
			
			switch(e){
				case TelemetryEvent.kCircuitChanged:{
					LevelManager.singleton.SerializeLevel(ioStream);
					break;
				}
				case TelemetryEvent.kNewGameStarted:{
					RecordHeader (bw);

					break;
				}
				case TelemetryEvent.kGameFinished:{
					CloseFile();

					break;
				}
				case TelemetryEvent.kLevelStarted:{
					bw.Write (LevelManager.singleton.currentLevelIndex);
					bw.Write (LevelManager.singleton.GetCurrentLevelName());
					
					// Serialise out the level too (as it includes the all-important circuit GUID)
					LevelManager.singleton.SerializeLevel(ioStream);
				
					
					break;
				}
				case TelemetryEvent.kLevelCompleteWait:{
					break;
				}
				case TelemetryEvent.kLevelComplete:{
					break;
				}
				case TelemetryEvent.kGameComplete:{
					break;
				}
				case TelemetryEvent.kApplicationQuit:{
					break;
				}
				case TelemetryEvent.kMouseMove:{
					break;
				}			
			}

			
			
			
			if (e == TelemetryEvent.kCircuitChanged){
				
			}
			frameEvents.RemoveAt(0);
		}
	}
	
	void OpenFileForWriting(){
		// If the directory doesn't exist, make it exist
		if (!Directory.Exists(BuildPathName())){
			Directory.CreateDirectory(BuildPathName());
		}
		string fullFilename = BuildPathName() + BuildFileName();
		ioStream = File.Create(fullFilename);
	}
	
	void CloseFile(){
		ioStream.Close();
		ioStream = null;
	}
	
	void RecordHeader(BinaryWriter bw){
		bw.Write (kLoadSaveVersion);
		bw.Write (gameName);
		bw.Write (gameVersion);
		bw.Write (yyyymmdd);
		bw.Write (hhmmss);
		bw.Write (machineGUID);
		bw.Write (fileGuid);			
	}
	
	void ReadHeader(BinaryReader br){
		thisLoadSaveVersion = br.ReadInt32();
		switch (thisLoadSaveVersion){
			case kLoadSaveVersion:{
				gameName = 		br.ReadString ();
				gameVersion = 	br.ReadString ();
				yyyymmdd = 		br.ReadString ();
				hhmmss = 		br.ReadString ();
				machineGUID = 	br.ReadString ();
				fileGuid = 		br.ReadString ();
				break;
			}
		}
	}

	void PlaybackUpdate(){
	}
	
	string BuildPathName(){
		return Application.persistentDataPath + "/LeccyTelemetry/";
	}
	
	// format is GAMEVERSION_YYYYMMDD_HHMMSS_MACHINEGUID_FILEGUID
	string BuildFileName(){
		DateTime dt = DateTime.Now;
		
		yyyymmdd = dt.Year.ToString("0000.##") + dt.Month.ToString("00.##") + dt.Day.ToString("00.##");
		hhmmss = dt.Hour.ToString("00.##") + dt.Minute.ToString("00.##") + dt.Second.ToString("00.##");
		machineGUID = GetMachineGUID();
		fileGuid = Guid.NewGuid().ToString();
		return gameName + "_" + gameVersion + "_" + yyyymmdd + "_" + hhmmss + "_" + machineGUID + "_" + fileGuid + ".telemetry";
	}
	

	string GetMachineGUID(){
		string machineGUIDKey = "MachineGUID";
		if (!PlayerPrefs.HasKey(machineGUIDKey)){
			PlayerPrefs.SetString(machineGUIDKey, Guid.NewGuid().ToString());
			
		}
		return PlayerPrefs.GetString(machineGUIDKey);
	}	
	
	
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
	}
	
	void OnDestroy(){
		singleton = null;
	}
	
	
	
}


