
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
	
	public static string errorPathName = "err/";
	public static string uploadedPathName = "uploaded/";
	

	// Flags about what has happened in this frame. try not to delete these as they may be needed to 
	// parse older files (event enums are stored as strings)
	public enum Event{
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
		kUserComment,
		kUIStateNone,
		kUIStateSplash,
		kUIStateStart,
		kUIStateStartEditor,
		kUIStateTitleScreen,
		kUIStatePlayLevelInit,
		kUIStatePlayLevel,
		kUIStateLevelCompleteWait,
		kUIStateLevelComplete,
		kUIStateGameComplete,
		kUIStateQuitGame,
		kUIStateReallyQuitGame,	
		kNumEvents
		
	};
	
	public enum EventType{
		kUnknown,
		kUserComments,
		kGameState,
		kCursor,
		kCircuitChange,
		kUIState,
		kNumTypes
	
	};
	
	EventType[] eventTypes = new EventType[(int)Event.kNumEvents];
	
	
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
	
	bool[]	stepProcessEvent = new bool[(int)EventType.kNumTypes];
	List<TelemetryListener> listeners = new List<TelemetryListener>();
	
	ReadState readState = ReadState.kReadTime;
	
	PlaybackState playbackState = PlaybackState.kStopped;
	
	bool[,] playbackStateMatrix = new bool[(int)PlaybackState.kNumStates, (int)PlaybackState.kNumStates];
	float	playbackTime = 0;
	List<Event>    lastEventsRead = new List<Event>();
	
	
	List<Event> frameEvents = new List<Event>();
	List<string> frameStrings = new List<string>();
	Dictionary<string, Event> eventLookup = new Dictionary<string, Event> ();
	
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
	string playerName;
	
	const int		kLoadSaveVersion = 2;		
	
	string writeFilename;
	string writeFilenameFinal;
	
	
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
	
	public bool IsTextEvent(Event e){
		if (GetEventType(e) == EventType.kUserComments){
			return true;
		}
		return false;
	}
	
	// Inform the telemetry system that a particular event has occured this frame
	public void RegisterEvent(Event e){
	
		// If this is a text event, then we should register a string with it
		if (IsTextEvent(e)){
			RegisterEvent(e, "");
		}
		
		frameEvents.Add (e);
		
	}
	
	// Inform the telemetry system that a particular event has occured this frame
	public void RegisterEvent(Event e, string text){
		if (!IsTextEvent(e)){
			Debug.LogError ("Trying to write text to a non texct event");
			return;
		}
		frameEvents.Add (e);
		frameStrings.Add(text);
	}	
	
	// Update is called once per frame
	public void GameUpdate () {
		if (!enableTelemetry){
			frameEvents.Clear();
			frameStrings.Clear();
			return;
		}
		
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
	
	public void RegisterListener(TelemetryListener listener){
		listeners.Add(listener);
	}
	
	// Whether to process a particular event when stepping forwards and backwards
	public void EnableStepType(EventType type, bool enable){
		stepProcessEvent[(int)type] = enable;
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

	public bool HasFile(){
		return (useStream != null);
	}
	
	void RecordUpdate(){
		
		// If we don't have a file, go through the events looking for a newgame event
		if (useStream == null){
			while (frameEvents.Count != 0){
				if (frameEvents[0] == Event.kNewGameStarted){
					OpenFileForWriting();
					break;
				}
				if (IsTextEvent(frameEvents[0])){
					frameStrings.RemoveAt(0);
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
			Event e = frameEvents[0];
			
			WriteTelemetryEvent(bw, e);
		

			frameEvents.RemoveAt(0);
		}
		if (frameStrings.Count != 0){
			Debug.LogError ("Failed to get through all the strings in the events");
		}
	}
	
	void WriteTelemetryEvent(BinaryWriter bw, Event e){
		bw.Write (e.ToString());
		
		if (IsTextEvent(e)){
			bw.Write (frameStrings[0]);
			foreach (TelemetryListener listener in listeners){
				listener.OnEvent(e, frameStrings[0]);
			}
			frameStrings.RemoveAt(0);
		}
		else{
			foreach (TelemetryListener listener in listeners){
				listener.OnEvent(e);
			}
		}
//		Debug.Log ("Write event = " + e.ToString());
		
		
		switch(e){
			case Event.kCircuitChanged:{
				LevelManager.singleton.SerializeLevel(useStream);
				break;
			}
			case Event.kNewGameStarted:{
				WriteHeader (bw);
				foreach(TelemetryListener listener in listeners){
					listener.OnNewGame();
				}
			
				
				break;
			}
			case Event.kGameFinished:{
				FinaliseRecording();
				break;
			}
			case Event.kLevelStarted:{
				bw.Write (LevelManager.singleton.currentLevelIndex);
				bw.Write (LevelManager.singleton.GetCurrentLevelName());
				
				// Serialise out the level too (as it includes the all-important circuit GUID)
				LevelManager.singleton.SerializeLevel(useStream);
				
				
				break;
			}
			
			case Event.kGhostChange:{
				UI.singleton.SerializeGhostElement(useStream);
				break;
			}

		}
		// We do this in case the player quits the application in an unexpeted way.
		if (useStream != null)
			useStream.Flush();
	}	
	
	void OnDesroy(){
		Debug.Log ("Destroy Telmetry - Can we use this to tidy up the files?");
	}	
	
	void OpenFileForWriting(){
		// If the directory doesn't exist, make it exist
		if (!Directory.Exists(GetPathName())){
			Directory.CreateDirectory(GetPathName());
		}
			
		BuildFileNames();
		string fullFilename = GetPathName() + writeFilename;
		
		fileStream = File.Create(fullFilename);
		// The compressed stream
		gZipOutStream = new DCOutputStream(fileStream, 65536);
		// The one we use
		useStream = gZipOutStream;
		
		// Set up positions
		streamPos = -1;
		lastStreamPos = -2;		
		
	}
	
	void FinaliseRecording(){
		CloseFile();
		RenameFiletoFinal();
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
		string usePath = GetPathName() + playbackFilename;
		// If it's not where we left it, it might have been moved to to the uploaded folder
		if (!File.Exists(usePath)){
			usePath = GetPathName() + uploadedPathName + playbackFilename;
		}
		// If not there then it might be in the error folder
		if (!File.Exists(usePath)){
			usePath = GetPathName() + errorPathName + playbackFilename;
		}	
		if (!File.Exists(usePath)){
			Debug.LogError ("Failed to open telemtry file for reading");
			return;
		}		
		fileStream = File.Open(usePath, FileMode.Open);

		gZipInStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(fileStream, 65536);
		useStream = gZipInStream;
		
		playbackTime = 0;
		readState = ReadState.kReadTime;
		streamPos = -1;
		lastStreamPos = -2;	
		
		
	}
	
	void WriteHeader(BinaryWriter bw){
		bw.Write (kLoadSaveVersion);
		bw.Write (gameName);
		bw.Write (gameVersion);
		bw.Write (yyyymmdd);
		bw.Write (hhmmss);
		bw.Write (machineGUID);
		bw.Write (fileGuid);	
		bw.Write (playerName);

		
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
				playerName =	br.ReadString ();
				break;
			}
			case 1:{
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
				
				bool exists = lastEventsRead.Exists(e => ShouldProcessStepEvent(GetEventType(e)));
				
				// Assume we are going to steop
				playbackState = PlaybackState.kPaused;
				
				// however, if we are not on an event we should be processing, and we can step forward again,
				// do so.
				if (!exists && CanEnterPlaybackState(PlaybackState.kStepForward)){
					playbackState = PlaybackState.kStepForward;	
				}
				
				break;
			}
			case PlaybackState.kStepBackwards:{
				ReadSeek(lastStreamPos);
			    // Note that this is ok to do as it will set the read state to ensure that data is read next
				ReadEventTime(true);
				ReadAndProcessTelemetryEvent();	

				bool exists = lastEventsRead.Exists(e => ShouldProcessStepEvent(GetEventType(e)));
				
				// Assume we are going to steop
				playbackState = PlaybackState.kPaused;
				
				// however, if we are not on an event we should be processing, and we can step forward again,
				// do so.
				if (!exists && CanEnterPlaybackState(PlaybackState.kStepBackwards)){
					playbackState = PlaybackState.kStepBackwards;	
				}
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
	
	bool ShouldProcessStepEvent(EventType type){
		return stepProcessEvent[(int)type];
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
					lastEventsRead.Clear();
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
		
		Event e;
		bool ok = eventLookup.TryGetValue(eventString, out e);
		if (!ok){
			Debug.LogError("Failed to convert string to enum");
		}
		lastEventsRead.Add(e);
		if (IsTextEvent(e)){
			string text = br.ReadString();
			// Tell all our listeners about the event
			foreach (TelemetryListener listener in listeners){
				listener.OnEvent(e, text);
			}
		}
		else{
			foreach (TelemetryListener listener in listeners){
				listener.OnEvent(e);
			}
		}
		

		
		
		switch(e){
			case Event.kCircuitChanged:{
				LevelManager.singleton.DeserializeLevel(useStream);
				break;
			}
			case Event.kGameFinished:{
				playbackState = PlaybackState.kPaused;
				// Record the last posiiton in the file
				finalStreamPos = streamPos;
				finalLastStreamPos = lastStreamPos;
				return true;
			}
			case Event.kNewGameStarted:{
				GameModeManager.singleton.StartGame();
				Circuit.singleton.CreateBlankCircuit();
				foreach(TelemetryListener listener in listeners){
					listener.OnNewGame();
				}
				ReadHeader();
				break;
			}
			case Event.kLevelStarted:{
				LevelManager.singleton.currentLevelIndex = br.ReadInt32 ();
				string levelName = br.ReadString();
				if (levelName != LevelManager.singleton.GetCurrentLevelName()){
					Debug.LogError ("Level name does not match the name in the level manager");
				}
				
				// Serialise out the level too (as it includes the all-important circuit GUID)
				LevelManager.singleton.DeserializeLevel(useStream);
				break;
			}

			case Event.kGhostChange:{
				UI.singleton.DeserializeGhostElement(useStream);
				break;
			}
		}
		if (e > Event.kUIStateNone){
			// don't do this if it is a quit game message
			if (e != Event.kUIStateQuitGame || e != Event.kUIStateReallyQuitGame ){
				GameModeManager.singleton.SetUIState((int)e - (int)Event.kUIStateNone);
			}
		}
			
		return false;
			
	}	
	
	
	public static string GetPathName(){
		return Application.persistentDataPath + "/LeccyTelemetry/";
	}
	
	public static string BuildExtension(){
		return ".telem_";
	}
	
	public static string BuildFinalExtension(){
		return ".telemetry";
	}
	
	public EventType GetEventType(Event e){
		return eventTypes[(int)e];
	}
	
	// format is GAMEVERSION_YYYYMMDD_HHMMSS_MACHINEGUID_FILEGUID
	void BuildFileNames(){
		DateTime dt = DateTime.Now;
		
		yyyymmdd = dt.Year.ToString("0000.##") + dt.Month.ToString("00.##") + dt.Day.ToString("00.##");
		hhmmss = dt.Hour.ToString("00.##") + dt.Minute.ToString("00.##") + dt.Second.ToString("00.##");
		machineGUID = GetMachineGUID();
		fileGuid = Guid.NewGuid().ToString();
		playerName = PlayerPrefs.GetString(GameModeManager.playerNameKey);
		if (playerName == "") playerName = "NONE-GIVEN";
		writeFilename =  gameName + "_" + gameVersion + "_" + playerName + "_" + yyyymmdd + "_" + hhmmss + "_" + machineGUID + "_" + fileGuid + BuildExtension();
		writeFilenameFinal =  gameName + "_" + gameVersion + "_" + playerName + "_" + yyyymmdd + "_" + hhmmss + "_" + machineGUID + "_" + fileGuid + BuildFinalExtension();
	}
	
	void RenameFiletoFinal(){
		string oldFilename = GetPathName() + writeFilename;
		string newFilename = GetPathName() + writeFilenameFinal;
		
		try
		{
			// Move didn't seem to work - trying copy (and allowing overwrites) instead
			File.Copy(oldFilename, newFilename);
			File.Delete(oldFilename);
		}
		catch (IOException exception)
		{
			Debug.Log("Failed to rename file: " + exception.Message);
		}
		
		
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
		SetupEventTypeMatrix();
		SetupEventLookup();
	}
	
	
	void OnDestroy(){
		singleton = null;
	}
	
	
	// Set up a lookup table for loking up event enums from strings
	// We need to do this since we write events out as strings 
	void SetupEventLookup(){
		for (int i = 0; i < (int)Event.kNumEvents; ++i){
			Event e = (Event)i;
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
	
	
	void SetupEventTypeMatrix(){
	
		eventTypes[(int)Event.kCircuitChanged] = EventType.kCircuitChange;
		eventTypes[(int)Event.kNewGameStarted] = EventType.kGameState;
		eventTypes[(int)Event.kLevelStarted] = EventType.kGameState;
		eventTypes[(int)Event.kLevelCompleteWait] = EventType.kGameState;
		eventTypes[(int)Event.kLevelComplete] = EventType.kGameState;
		eventTypes[(int)Event.kGameComplete] = EventType.kGameState;
		eventTypes[(int)Event.kApplicationQuit] = EventType.kGameState;
		eventTypes[(int)Event.kFrameInc] = EventType.kUnknown;
		eventTypes[(int)Event.kMouseMove] = EventType.kCursor;
		eventTypes[(int)Event.kGhostChange] = EventType.kCursor;
		eventTypes[(int)Event.kUserComment] = EventType.kUserComments;
		eventTypes[(int)Event.kUIStateNone] = EventType.kUIState;
		eventTypes[(int)Event.kUIStateStart] = EventType.kUIState;
		eventTypes[(int)Event.kUIStateSplash] = EventType.kUIState;
		eventTypes[(int)Event.kUIStateStartEditor] = EventType.kUIState;
		eventTypes[(int)Event.kUIStateTitleScreen] = EventType.kUIState;
		eventTypes[(int)Event.kUIStatePlayLevelInit] = EventType.kUIState;
		eventTypes[(int)Event.kUIStatePlayLevel] = EventType.kUIState;
		eventTypes[(int)Event.kUIStateLevelCompleteWait] = EventType.kUIState;
		eventTypes[(int)Event.kUIStateLevelComplete] = EventType.kUIState;
		eventTypes[(int)Event.kUIStateGameComplete] = EventType.kUIState;
		eventTypes[(int)Event.kUIStateQuitGame] = EventType.kUIState;
		eventTypes[(int)Event.kUIStateReallyQuitGame] = EventType.kUIState;
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
				case PlaybackState.kStepForward:{
					Time.timeScale = 100;
					break;				
				}				
				case PlaybackState.kStepBackwards:{
					Time.timeScale = 100;
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


