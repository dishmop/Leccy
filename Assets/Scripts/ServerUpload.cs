using UnityEngine;
using System.Collections;
using System.IO;




public class ServerUpload : MonoBehaviour {
	public static ServerUpload singleton = null;

	public float waitDuration = 1f;
	
	public bool enableUpload;
	

	FileInfo[] fileList;
	WWW uploadWWW;
	
	
	enum State{
		kStartup,
		kWaiting,
		kUploadFile,
		kWaitForUpload,
		kDealWithOutcome
	};
	
	State state = State.kStartup;
	float waitStartTime;
	
	string uploadURL = "http://toby.eng.cam.ac.uk/leccy/upload.php";
	

	// Call to check if it is OK to quit the application
	public bool CanQuit(){
		return 	state == State.kWaiting;
	}

	// Call if we want to esnure that everything we can gets uploaded
	public void ForceUpload(){
		if (state == State.kWaiting){
			FillFileList();
			state = State.kUploadFile;
		}
	}
	// Use this for initialization
	public void GameUpdate () {
		if (!enableUpload) return;
		
		switch (state){
			case State.kStartup:{
				waitStartTime = Time.realtimeSinceStartup;
				state = State.kWaiting;
				break;
			}
			case State.kWaiting:{
				float nowTime =Time.realtimeSinceStartup;
				if (nowTime > waitStartTime + waitDuration){
					FillFileList();
					state = State.kUploadFile;
					waitStartTime = nowTime;
				}
				break;
			}
			case State.kUploadFile:{
				if (fileList.Length > 0) {
					UploadFile ();
					state = State.kWaitForUpload;
				}
				else{
					state = State.kWaiting;
				}				
				break;
			}
			case State.kWaitForUpload:{
				if (WaitForUploadToComplete()){
					state = State.kDealWithOutcome;
				}
				break;
			} 
			case State.kDealWithOutcome:{
				DealWithUploadOutcome();
				fileList = fileList.RemoveAt(0);
				state = State.kUploadFile;
				break;
			} 		
		}
	
	}
	
	// Update is called once per frame
	void UploadFile () {
		
		// Load the file as a binary block
		FileStream file = File.OpenRead(fileList[0].FullName);
		long fileLength = file.Length;
		if (fileLength > int.MaxValue){
			Debug.Log ("UploadError: File to long for Int length");
		//	MoveFile(fileList[0].FullName, Telemetry.GetPathName() + Telemetry.errorPathName + fileList[0].Name);
			return;
		}
		// Try and read the data into a byte array
		byte[] data = new byte[fileLength];
		int numBytesRead = file.Read(data, 0, (int)fileLength);
		if (numBytesRead < (int)fileLength){
			Debug.Log ("UploadError: Failed to read all the bytes in the file");
		//	MoveFile(fileList[0].FullName, Telemetry.GetPathName() + Telemetry.errorPathName + fileList[0].Name);
			return;
		}
		
		// Create the form
		// Create a Web Form
		WWWForm form = new WWWForm();
		form.AddBinaryData("fileToUpload", data, fileList[0].Name, "");

				
		// Try to upload the file
		uploadWWW = new WWW(uploadURL, form);
	}
	
	bool WaitForUploadToComplete(){
		return uploadWWW.isDone;
	}
	
	void DealWithUploadOutcome(){
	
		// Block and wait for data transfer completion - there was an error (hmm script doesn't seem ot raise an error)
		if (!string.IsNullOrEmpty(uploadWWW.error)){
			Debug.Log ("UploadError: Failed to upload data to server - " + uploadWWW.error);
			// Add more seconds onto the wait time
			waitStartTime += 5;
			// Don't move to to the error folder - since we are probably just not connected to the internet	
//			MoveToSubFolder(fileList[0].Name, Telemetry.errorPathName);	
			return;	
		}
		
		
		// Test  if we got an errro (the uploaded file will be 
		if (uploadWWW.text.Substring(0, 5) == "Sorry"){
			Debug.Log("Failed to upload file: " + fileList[0].Name + " Error message: " + uploadWWW.text);
//			MoveFile(fileList[0].FullName, Telemetry.GetPathName() + Telemetry.errorPathName + fileList[0].Name);
		}
		else{
			Debug.Log("Telemetry file upolad successful: " + uploadWWW.text);
			MoveFile(fileList[0].FullName, Telemetry.GetPathName() + Telemetry.uploadedPathName + fileList[0].Name);
		}	
	}
	
	void MoveFile(string srcPath, string desPath){
	
		string destDir = Path.GetDirectoryName(desPath);
		// If the directory doesn't existTelemetry.BuildPathName() + errorPathName, make it exist
		if (!Directory.Exists(destDir)){
			Directory.CreateDirectory(destDir);
		}		
		
		try
		{
		// Move didn't seem to work - trying copy (and allowing overwrites) instead
			File.Copy(srcPath, desPath, true);
			File.Delete(srcPath);
		}
		catch (IOException exception)
		{
			Debug.Log("File IO Exepction thrown" + exception.Message);
		}		
		
		// Move the file
		
		
	}
	
	void FillFileList(){
		// Fil the file list
		DirectoryInfo dir = new DirectoryInfo(Telemetry.GetPathName());
		FileInfo[] fileListFinal = dir.GetFiles("*" + Telemetry.BuildExtension());	
		FileInfo[] fileListInt = dir.GetFiles("*" + Telemetry.BuildFinalExtension());
		
		DirectoryInfo errDir = new DirectoryInfo(Telemetry.GetPathName() + Telemetry.errorPathName);
		FileInfo[] fileListFinalErr = errDir.GetFiles("*" + Telemetry.BuildExtension());	
		FileInfo[] fileListIntErr = errDir.GetFiles("*" + Telemetry.BuildFinalExtension());
		
		fileList = new FileInfo[fileListFinal.Length + fileListInt.Length + fileListFinalErr.Length + fileListIntErr.Length];
		
		// Normal list
		for (int i = 0; i < fileListFinal.Length; ++i){
			fileList[i] = fileListFinal[i];
		}
		for (int i = 0; i < fileListInt.Length; ++i){
			fileList[fileListFinal.Length + i] = fileListInt[i];
		}		
		
		for (int i = 0; i < fileListFinalErr.Length; ++i){
			fileList[fileListInt.Length + fileListFinal.Length + i] = fileListFinalErr[i];
		}
		for (int i = 0; i < fileListIntErr.Length; ++i){
			fileList[fileListFinalErr.Length + fileListInt.Length + fileListFinal.Length + i] = fileListIntErr[i];
		}			
		
	}
	
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
	}
	
	void OnDestroy(){
		singleton = null;
	}	

	
	

}
