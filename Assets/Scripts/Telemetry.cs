
using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;


// Stream includes
using System.Threading;

public class DCOutputStream : ICSharpCode.SharpZipLib.GZip.GZipOutputStream{

	public long writeCount = 0;

	public DCOutputStream(Stream other, int size): base(other, size){
	
	}
	
	public override void Write(
		byte[] buffer,
		int offset,
		int count){
		base.Write (buffer, offset, count);
		writeCount += count;
		
	}


}

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
		kGhostChange,
		kUIStateNone,
		kUIStateStart,
		kUIStateStartEditor,
		kUIStateTitleScreen,
		kUIStatePlayLevelInit,
		kUIStatePlayLevel,
		kUIStateLevelCompleteWait,
		kUIStateLevelComplete,
		kUIStateGameComplete,
		kUIStateQuitGame,
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
	
	long streamPos;
	long lastStreamPos;
	// This is only valid after we have reached the end of the file
	long finalStreamPos;
	long finalLastStreamPos;
	
	// Additional Header info
	int    thisLoadSaveVersion;
	string yyyymmdd;
	string hhmmss;
	string machineGUID;
	string fileGuid;
	
	const int		kLoadSaveVersion = 1;		
	
	
	// The file we open
	FileStream	fileStream = null;
	// The compressed stream
	ICSharpCode.SharpZipLib.GZip.GZipInputStream  gZipInStream = null;
	DCOutputStream  gZipOutStream = null;
	// The one we use
	Stream		useStream = null;
	
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
	
	public float GetPlaybackTime(){
		return playbackTime;
		
	}
	
	// Playback operations
	public void SetPlayabckState(PlaybackState desState){
		if (CanEnterPlaybackState(desState)){
			playbackState = desState;
		}
	}
	
	
	public bool CanEnterPlaybackState(PlaybackState desState){
	
		// there are some special cases that override the default behaviour
		switch (desState){
			case PlaybackState.kStepBackwards:{
				if (lastStreamPos < 0) return false;
				break;
			}
			case PlaybackState.kRewind:{
				if (lastStreamPos < 0) return false;
				break;
			}
			case PlaybackState.kWindForwards:{
				if (finalStreamPos < 0) return false;
				if (streamPos == finalStreamPos) return false;
				break;
			}
			case PlaybackState.kStepForward:{
				if (streamPos == finalStreamPos) return false;
				break;
			}
			case PlaybackState.kPlaying:{
				if (streamPos == finalStreamPos) return false;
				break;
			}
		}
		// There is some specialised logic for certainstates
		return RawCanEnterPlaybackState(desState);
		
	}
	
	 bool RawCanEnterPlaybackState(PlaybackState desState){
		// There is some specialised logic for certainstates
		
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
		
		
		// If we don't have a file, go through the events looking for a newgame event
		if (useStream == null){
			while (frameEvents.Count != 0){
				if (frameEvents[0] == TelemetryEvent.kNewGameStarted){
					OpenFileForWriting();
					break;
				}
				frameEvents.RemoveAt(0);
			}
		}
		// If no events then do nothing
		if (frameEvents.Count == 0){
			return;
		}
		
		BinaryWriter bw = new BinaryWriter(useStream);
		// Always write the time of the event (from game start time).
		float gameTime = GameModeManager.singleton.GetGameTime();
		
		// In the file, we record the position of the last event - since this is where
		// we want to jump back to when we say step backwards
		lastStreamPos = streamPos;
		streamPos = gZipOutStream.writeCount;
		
		bw.Write (gameTime);
//		Debug.Log ("Write game time = " + gameTime);
		bw.Write (lastStreamPos);
//		Debug.Log ("Write lastSteamPos = " + lastStreamPos);
		bw.Write (streamPos);
//		Debug.Log ("Write streamPos = " + streamPos);
		bw.Write ((int)frameEvents.Count);
//		Debug.Log ("Number of frameevents = " + (int)frameEvents.Count);
		
		while (frameEvents.Count != 0){
			TelemetryEvent e = frameEvents[0];
			
			WriteTelemetryEvent(bw, e);
		

			frameEvents.RemoveAt(0);
		}
	}
	
	void WriteTelemetryEvent(BinaryWriter bw, TelemetryEvent e){
		bw.Write (e.ToString());
//		Debug.Log ("Write event = " + e.ToString());
		
		
		switch(e){
			case TelemetryEvent.kCircuitChanged:{
				LevelManager.singleton.SerializeLevel(useStream);
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
				LevelManager.singleton.SerializeLevel(useStream);
				
				
				break;
			}
			
			case TelemetryEvent.kGhostChange:{
				UI.singleton.SerializeGhostElement(useStream);
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
		
		fileStream = File.Create(fullFilename);
		// The compressed stream
		gZipOutStream = new DCOutputStream(fileStream, 65536);
		// The one we use
		useStream = gZipOutStream;
		
		// Set up positions
		streamPos = -1;
		lastStreamPos = -2;		
		
	}
	
	void CloseFile(){
		if (gZipOutStream != null) gZipOutStream.Close ();
		if (gZipInStream != null) gZipInStream.Close ();
		if (fileStream != null) fileStream.Close();
		fileStream  = null;
		gZipOutStream  = null;
		gZipInStream  = null;
		useStream = null;
	}
	

	void OpenFileForReading(){
		fileStream = File.Open(BuildPathName() + playbackFilename, FileMode.Open);
		if (fileStream == null){
			Debug.LogError ("Failed to open telemtry file for reading");
		}
		gZipInStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(fileStream, 65536);
		useStream = gZipInStream;
		
		playbackTime = 0;
		readState = ReadState.kReadTime;
		streamPos = -1;
		lastStreamPos = -2;	
		
		
	}
	
	void RecordHeader(BinaryWriter bw){
		bw.Write (kLoadSaveVersion);
		bw.Write (gameName);
		bw.Write (gameVersion);
		bw.Write (yyyymmdd);
		bw.Write (hhmmss);
		bw.Write (machineGUID);
		bw.Write (fileGuid);	

		
		Debug.Log ("Write Header");
		
	}
	
	void ReadHeader(){
		BinaryReader br = new BinaryReader(useStream);
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
				
		Debug.Log ("Read Header");
		


	}
	
	// If we are reading we can use this to seek to a point in the file
	// This is a slow operation as it means reading from the beginning of the file till we read the point we want
	void ReadSeek(long pos){
	
		// Close and then reopen the file
		CloseFile();
		OpenFileForReading();


		// NOw seek forward a byte at a time until we reach the posiiton we are after
		BinaryReader br = new BinaryReader(useStream);
		
//		br.ReadSingle();		// 0 + 4 = 4
//		br.ReadInt64();			// 4 + 8 = 12
//		br.ReadInt32();			// 12 + 4 = 16
//		br.ReadString ();		// 16 + 16 = 32
		
		
		br.ReadBytes((int)pos);
		
	}

	void PlaybackUpdate(){
		
		if (playbackState == PlaybackState.kLoadFile){
			// open the File for reading
			OpenFileForReading();
			finalStreamPos = -3;
			
			GameModeManager.singleton.ResetGameTime();
			playbackState = PlaybackState.kStepForward;
		}
		// Is telemetr is disabled - then do nothing		
		if (!enableTelemetry){
			return;
		}
		// If we are simply in Stopped mode, then nothing to do
		if (playbackState == PlaybackState.kStopped){
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

			case PlaybackState.kStepForward:{
				GameModeManager.singleton.ForceSetGameTime(playbackTime);
				ReadAndProcessTelemetryEvent();		
				
				playbackState = PlaybackState.kPaused;
				break;
			}
			case PlaybackState.kStepBackwards:{
				ReadSeek(lastStreamPos);
			    // Note that this is ok to do as it will set the read state to ensure that data is read next
				ReadEventTime(true);
				ReadAndProcessTelemetryEvent();	
				playbackState = PlaybackState.kPaused;
				break;
			}
			case PlaybackState.kRewind:{
				// Rewind
				ReadSeek(0);
				// Note that this is ok to do as it will set the read state to ensure that data is read next
				ReadEventTime(true);
				ReadAndProcessTelemetryEvent();	
				playbackState = PlaybackState.kPaused;
				break;
			}
			case PlaybackState.kWindForwards:{
				if (finalStreamPos < 0){	
					Debug.LogError("Do not have value of finalStreamPos");
				}
				// Wind forward to one before the last frame (which is when the circuit would have last been changed)
				// And then stop forward one to get us to the end
				ReadSeek(finalLastStreamPos);
				ReadEventTime(true);
				ReadAndProcessTelemetryEvent();	
				playbackState = PlaybackState.kStepForward;
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
	
	// use forceGameToMatch=true if we want the game to set itself to this new time
	void ReadEventTime(bool forceGameToMatch){
		BinaryReader br = new BinaryReader(useStream);

				// Read the time of this event and set the gameitme to be that
		playbackTime = br.ReadSingle();
//		Debug.Log ("Read game time = " + playbackTime);
		readState = ReadState.kReadData;
		
		
		if (forceGameToMatch)
			GameModeManager.singleton.ForceSetGameTime(playbackTime);
	}
	
	void ReadAndProcessTelemetryEvent(){
		bool finish = false;
		while (!finish && MathUtils.FP.Fleq (playbackTime, GameModeManager.singleton.GetGameTime())){
			switch (readState){
				case ReadState.kReadTime:{
					ReadEventTime(false);
					break;
				}
				case ReadState.kReadData:{
					BinaryReader br = new BinaryReader(useStream);
					// Read the position of the event before this one (so we can jump back to it if necessary
					lastStreamPos = br.ReadInt64();
//					Debug.Log("Read lastSteamPos = " + lastStreamPos);
					streamPos = br.ReadInt64();
//					Debug.Log("Read streamPos = " + streamPos);
					
					int numEvents = br.ReadInt32 ();
					for (int i = 0; i < numEvents; ++i){
						// Hopefully any "finishing" events should be the last event that frame!
						finish = ReadData();
					}
					readState = ReadState.kReadTime;
					break;
				}

			}
		}
	}
	
	bool ReadData(){
		BinaryReader br = new BinaryReader(useStream);
		
		string eventString = br.ReadString();
//		Debug.Log ("Read event = " + eventString);
		
		TelemetryEvent e;
		bool ok = eventLookup.TryGetValue(eventString, out e);
		if (!ok){
			Debug.LogError("Failed to convert string to enum");
		}
		
		switch(e){
			case TelemetryEvent.kCircuitChanged:{
				LevelManager.singleton.DeserializeLevel(useStream);
				break;
			}
			case TelemetryEvent.kGameFinished:{
				playbackState = PlaybackState.kPaused;
				// Record the last posiiton in the file
				finalStreamPos = streamPos;
				finalLastStreamPos = lastStreamPos;
				return true;
			}
			case TelemetryEvent.kNewGameStarted:{
				GameModeManager.singleton.StartGame();
				Circuit.singleton.CreateBlankCircuit();
				ReadHeader();
				break;
			}
			case TelemetryEvent.kLevelStarted:{
				LevelManager.singleton.currentLevelIndex = br.ReadInt32 ();
				string levelName = br.ReadString();
				if (levelName != LevelManager.singleton.GetCurrentLevelName()){
					Debug.LogError ("Level name does not match the name in the level manager");
				}
				
				// Serialise out the level too (as it includes the all-important circuit GUID)
				LevelManager.singleton.DeserializeLevel(useStream);
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
			case TelemetryEvent.kGhostChange:{
				UI.singleton.DeserializeGhostElement(useStream);
				break;
			}
		}
		if (e > TelemetryEvent.kUIStateNone){
			// don't do this if it is a quit game message
			if (e != TelemetryEvent.kUIStateQuitGame){
				GameModeManager.singleton.SetUIState((int)e - (int)TelemetryEvent.kUIStateNone);
			}
		}
			
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
	
	
	void Update(){
		// Sort out our time scale (need to do it in the render update because when paused, the fixed update doesn't get called
		if (mode == Mode.kPlayback)
		{
			switch (playbackState){
				case PlaybackState.kPlaying:{
					Time.timeScale = playbackSpeed;
					break;				
				}
				case PlaybackState.kPaused:{
					Time.timeScale = 0;
					break;				
				}
				default:{
					Time.timeScale = 1;
					break;				
				}
			}
		}
	}
	
	
	
}


