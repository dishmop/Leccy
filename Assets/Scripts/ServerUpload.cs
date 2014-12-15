using UnityEngine;
using System.Collections;
using System.IO;




public class ServerUpload : MonoBehaviour {
	public static ServerUpload singleton = null;

	public float waitDuration = 1f;

	FileInfo[] fileList;
	
	
	enum State{
		kStartup,
		kWaiting,
		kUploadFile
	};
	
	State state = State.kStartup;
	float waitStartTime;
	
	string uploadURL = "http://toby.eng.cam.ac.uk/leccy/upload.php";
	


	// Use this for initialization
	public void GameUpdate () {
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
					// Whatever happens we have dealt with the file - so remove it from the list
					// NOte this is a bespoke function we are calling
					fileList = fileList.RemoveAt(0);
				}
				else{
					state = State.kWaiting;
				}
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
			MoveToSubFolder(fileList[0].Name, Telemetry.errorPathName);
			return;
		}
		// Try and read the data into a byte array
		byte[] data = new byte[fileLength];
		int numBytesRead = file.Read(data, 0, (int)fileLength);
		if (numBytesRead < (int)fileLength){
			Debug.Log ("UploadError: Failed to read all the bytes in the file");
			MoveToSubFolder(fileList[0].Name, Telemetry.errorPathName);
			return;
		}
		
		// Create the form
		// Create a Web Form
		WWWForm form = new WWWForm();
		form.AddBinaryData("fileToUpload", data, fileList[0].Name, "");

				
		// Try to upload the file
		WWW uploadWWW = new WWW(uploadURL, form);
		
		// Block and wait for data transfer completion - there was an error (hmm script doesn't seem ot raise an error)
//		if (!string.IsNullOrEmpty(uploadWWW.error)){
//			Debug.Log ("UploadError: Failed to upload data to server - " + uploadWWW.error);
//			MoveToSubFolder(fileList[0].Name, errorPathName);	
//			return;	
//		}
		
		// Wait until we are done
		while (!uploadWWW.isDone){}
		
		// Test  if we got an errro (the uploaded file will be 
		if (uploadWWW.text.Substring(0, 5) == "Sorry"){
			Debug.Log("Failed to upload file: " + fileList[0].Name + " Error message: " + uploadWWW.text);
			MoveToSubFolder(fileList[0].Name, Telemetry.errorPathName);
		}
		else{
			Debug.Log("Telemetry file upolad successful: " + uploadWWW.text);
			MoveToSubFolder(fileList[0].Name, Telemetry.uploadedPathName);
		}	
	}
	
	void MoveToSubFolder(string filename, string subFolder){
		string fullDestPath = Telemetry.BuildPathName() + subFolder;
		// If the directory doesn't existTelemetry.BuildPathName() + errorPathName, make it exist
		if (!Directory.Exists(fullDestPath)){
			Directory.CreateDirectory(fullDestPath);
		}		
		
		string srcPath = Telemetry.BuildPathName() + filename;
		string desPath = fullDestPath + filename;
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
		DirectoryInfo dir = new DirectoryInfo(Telemetry.BuildPathName());
		fileList = dir.GetFiles("*" + Telemetry.BuildExtension());
		
	}
	
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
	}
	
	void OnDestroy(){
		singleton = null;
	}	

	
	

}
