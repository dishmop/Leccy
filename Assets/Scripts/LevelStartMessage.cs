using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LevelStartMessage : MonoBehaviour {

	const float lifeTime = 1;
	float birthTime;
		
	// Use this for initialization
	void OnEnable () {
		int currentIndex = 	 LevelManager.singleton.currentLevelIndex;
		transform.FindChild("Text").GetComponent<Text>().text = "Level: " + currentIndex  + " - " + LevelManager.singleton.levelsToLoad[currentIndex].name;
		birthTime = Time.fixedTime;
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (Time.fixedTime > birthTime + lifeTime) gameObject.SetActive(false);
		Color col = transform.FindChild("Text").GetComponent<Text>().color;
		float age = (Time.fixedTime - birthTime) / lifeTime;
		col.a = 1- age;
		transform.FindChild("Text").GetComponent<Text>().color = col;
	
	}
}
