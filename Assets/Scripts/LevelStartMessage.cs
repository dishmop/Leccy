using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LevelStartMessage : MonoBehaviour {

	const float lifeTime = 3;
	const float fadeTime = 1;
	float birthTime;
		
	// Use this for initialization
	void OnEnable () {
		transform.FindChild("Text").GetComponent<Text>().text = LevelManager.singleton.GetCurrentLevelName();
		birthTime = Time.fixedTime;
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (Time.fixedTime > birthTime + lifeTime) gameObject.SetActive(false);
		
		Color col = transform.FindChild("Text").GetComponent<Text>().color;
		float age = (Time.fixedTime - birthTime - (lifeTime - fadeTime)) / fadeTime;
		col.a = 1- age;
		transform.FindChild("Text").GetComponent<Text>().color = col;
	
	}
}
