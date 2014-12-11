using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;


public class Telemetry : MonoBehaviour {

	public static Telemetry singleton = null;
	
	public bool enableTelemetry;
	public string gameName;
	
	public string playbackFilename;
	public float playbackSpeed;


	public string gameVersion = null;

	public enum Mode{
		kRecord,
		kPlayback
	};
	public Mode mode = Mode.kRecord;
	
	

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
		kFrameInc,
		kMouseMove,
		kNumEvents
		
	};
	
	
	
	public enum PlaybackState{
		kStopped,
		kLoadFile,
		kPlaying,
		kPaused,
		kStepForward,
		kStepBackwards,
		kRewind,
		kWindForwards,
		kCloseFile,
		kNumStates
	};
	
	// This seems a bit complicated, but it caused by the fact the we need to read the game time and then potentially wait for a while
	// before we can read the rest of the data
	enum ReadState{
		kReadTime,
		kReadData,
	};
	
	ReadState readState = ReadState.kReadTime;
	
	PlaybackState playbackState = PlaybackState.kStopped;
	
	bool[,] playbackStateMatrix = new bool[(int)PlaybackState.kNumStates, (int)PlaybackState.kNumStates];
	float	playbackTime = 0;
	
	List<TelemetryEvent> frameEvents = new List<TelemetryEvent>();
	Dictionary<string, TelemetryEvent> eventLookup = new Dictionary<string, TelemetryEvent> ();
	
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
	
	public string GetPlaybackTime(){
		TimeSpan timeSpan = TimeSpan.FromSeconds(playbackTime);
		string timeText = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
		return timeText;
		
	}
	
	// Playback operations
	public void SetPlayabckState(PlaybackState desState){
		if (CanEnterPlaybackState(desState)){
			playbackState = desState;
		}
	}
	
	
	public bool CanEnterPlaybackState(PlaybackState desState){
		return (mode == Mode.kPlayback && playbackStateMatrix[(int)playbackState, (int)desState]);
	}
	
	
	public void StartPlayback(){
		playbackState = PlaybackState.kPlaying;
	}
	
	public void PausePlayback(){
		playbackState = PlaybackState.kPaused;
	}

	void RecordUpdate(){
		if (!enableTelemetry){
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
		}
		
		// If we don't have a file, then just ignore any events we may be getting because we cannot be recording them
		if (ioStream == null){
			frameEvents.Clear();
			return;
		}
		
		BinaryWriter bw = new BinaryWriter(ioStream);
		// Always write the time of the event (from game start time).
		float gameTime = GameModeManager.singleton.GetGameTime();
		bw.Write (gameTime);
		Debug.Log ("Write game time = " + gameTime);
		bw.Write ((int)frameEvents.Count);
		Debug.Log ("Number of frameevents = " + (int)frameEvents.Count);
		
		while (frameEvents.Count != 0){
			TelemetryEvent e = frameEvents[0];
			
			WriteTelemetryEvent(bw, e);
		

			frameEvents.RemoveAt(0);
		}
	}
	
	void WriteTelemetryEvent(BinaryWriter bw, TelemetryEvent e){
		bw.Write (e.ToString());
		Debug.Log ("Write event = " + e.ToString());
		
		
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
			case TelemetryEvent.kFrameInc:{
				break;
			}				
			case TelemetryEvent.kMouseMove:{
				break;
			}			
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
	
	void OpenFileForReading(){
		ioStream = File.Open(BuildPathName() + playbackFilename, FileMode.Open);
		if (ioStream == null){
			Debug.LogError ("Failed to open telemtry file for reading");
		}
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
		
		if (playbackState == PlaybackState.kLoadFile){
			// open the File for reading
			OpenFileForReading();
			GameModeManager.singleton.ResetGameTime();
			playbackTime = 0;
			playbackState = PlaybackState.kPlaying;
			Time.timeScale = playbackSpeed;
			readState = ReadState.kReadTime;
		}
		// Is telemetr is disabled - then do nothing		
		if (!enableTelemetry){
			return;
		}
		// If we are simply in Stopped mode, then nothing to do
		if (playbackState == PlaybackState.kStopped){
			Time.timeScale = 1;
			return;
		}
		
		// If we are paused then do nothing also
		if (playbackState == PlaybackState.kPaused){
			return;
		}		
	
		
	
		switch (playbackState){
			case PlaybackState.kStopped:{
				break;
			}
			case PlaybackState.kPlaying:{
				ReadAndProcessTelemetryEvent();			
				break;
			}			
			case PlaybackState.kPaused:{
				break;
			}
			case PlaybackState.kStepForward:{
				// Step forward
				playbackState = PlaybackState.kPaused;
				break;
			}
			case PlaybackState.kStepBackwards:{
				// Step backward
				playbackState = PlaybackState.kPaused;
				break;
			}
			case PlaybackState.kRewind:{
				// Rewind
				playbackState = PlaybackState.kPaused;
				break;
			}
			case PlaybackState.kWindForwards:{
				// Wind forward to the last frame
				playbackState = PlaybackState.kPaused;
				break;
			}
			case PlaybackState.kCloseFile:{
				// Close the file then we are stopped
				CloseFile ();
				playbackState = PlaybackState.kStopped;
				break;
			}

		}
	}
	
	void ReadAndProcessTelemetryEvent(){
		BinaryReader br = new BinaryReader(ioStream);
		bool finish = false;
		while (!finish && playbackTime < GameModeManager.singleton.GetGameTime()){
			switch (readState){
				case ReadState.kReadTime:{
					playbackTime = br.ReadSingle();
					Debug.Log ("Read game time = " + playbackTime);
					readState = ReadState.kReadData;
					break;
				}
				case ReadState.kReadData:{
					int numEvents = br.ReadInt32 ();
					for (int i = 0; i < numEvents; ++i){
						// Hopefully any "finishing" events should be the last event that frame!
						finish = ReadData(br);
					}
					readState = ReadState.kReadTime;
					break;
				}

			}
		}
	}
	
	bool ReadData(BinaryReader br){
		string eventString = br.ReadString();
		Debug.Log ("Read event = " + eventString);
		
		TelemetryEvent e;
		bool ok = eventLookup.TryGetValue(eventString, out e);
		if (!ok){
			Debug.LogError("Failed to convert string to enum");
		}
		
		switch(e){
			case TelemetryEvent.kCircuitChanged:{
				LevelManager.singleton.DeserializeLevel(ioStream);
				break;
			}
			case TelemetryEvent.kGameFinished:{
				playbackState = PlaybackState.kPaused;
				// We need to finish now as we are not expecting any more events
				return true;
			}
			case TelemetryEvent.kNewGameStarted:{
				ReadHeader(br);
				break;
			}
			case TelemetryEvent.kLevelStarted:{
				LevelManager.singleton.currentLevelIndex = br.ReadInt32 ();
				string levelName = br.ReadString();
				if (levelName != LevelManager.singleton.GetCurrentLevelName()){
					Debug.LogError ("Level name does not match the name in the level manager");
				}
				
				// Serialise out the level too (as it includes the all-important circuit GUID)
				LevelManager.singleton.DeserializeLevel(ioStream);
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
			case TelemetryEvent.kFrameInc:{
				break;
			}				
			case TelemetryEvent.kMouseMove:{
				break;
			}			
		}
		// Don't/t finish
		return false;
			
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
		
		SetupPlaybackStateMatrix();
		SetupEventLookup();
	}
	
	
	void OnDestroy(){
		singleton = null;
	}
	
	
	// Set up a lookup table for loking up event enums from strings
	// We need to do this since we write events out as strings 
	void SetupEventLookup(){
		for (int i = 0; i < (int)TelemetryEvent.kNumEvents; ++i){
			TelemetryEvent e = (TelemetryEvent)i;
			eventLookup.Add (e.ToString(), e);
		}
	}
	
	
	// Sets up which actions are valid given the current state we are in
	// We assume everything is false and just set the ones we want to true
	void SetupPlaybackStateMatrix(){
		// When stopped, all we can do is load a file
		playbackStateMatrix[(int)PlaybackState.kStopped, (int)PlaybackState.kLoadFile] = true;
		
		
		// When loading a file, we can't do anything (it will leave this state automatically when done
		// and enter a paused state
		
		
		// When playing a file we can pause or stop or rewind
		playbackStateMatrix[(int)PlaybackState.kPlaying, (int)PlaybackState.kPaused] = true;
		playbackStateMatrix[(int)PlaybackState.kPlaying, (int)PlaybackState.kCloseFile] = true;
		playbackStateMatrix[(int)PlaybackState.kPlaying, (int)PlaybackState.kRewind] = true;
		playbackStateMatrix[(int)PlaybackState.kPlaying, (int)PlaybackState.kWindForwards] = true;
		
		// When paused we can start playing again, stop or step in either direction
		playbackStateMatrix[(int)PlaybackState.kPaused, (int)PlaybackState.kPlaying] = true;
		playbackStateMatrix[(int)PlaybackState.kPaused, (int)PlaybackState.kCloseFile] = true;
		playbackStateMatrix[(int)PlaybackState.kPaused, (int)PlaybackState.kStepForward] = true;
		playbackStateMatrix[(int)PlaybackState.kPaused, (int)PlaybackState.kStepBackwards] = true;
		playbackStateMatrix[(int)PlaybackState.kPaused, (int)PlaybackState.kRewind] = true;
		playbackStateMatrix[(int)PlaybackState.kPaused, (int)PlaybackState.kWindForwards] = true;
		
		// When stepping forward or backwards or rewinding or forwardWinding, we can't do anyhing (it will leave automatically when done
		// and enter a paused state)
		
		// When in a closed file state we will automatically leave it and enter a stopped state
	}
	
	
	
}


