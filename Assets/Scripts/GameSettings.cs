using UnityEngine;
using System.Collections;

// These settings do not get saved as part of the level

public class GameSettings : MonoBehaviour {

	public static GameSettings singleton = null;


	// Use this for initialization
	void Start () {
		singleton = this;
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
