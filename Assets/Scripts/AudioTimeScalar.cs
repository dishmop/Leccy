using UnityEngine;
using System.Collections;

// This only works for audio source that don't adjust their own pitch already
public class AudioTimeScalar : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		AudioSource[] sources = GetComponents<AudioSource>();
		foreach (AudioSource src in sources){
			src.pitch = Time.timeScale;
		}
	
	}
}
