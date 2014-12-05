using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FlashingButton : MonoBehaviour {

	public bool isPointerInside;


	public void OnPointeEnter(){
		isPointerInside = true;
	}
	
	public void OnPointerExit(){
		isPointerInside = false;
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		Color col = GetComponent<Text>().color;
		if (isPointerInside)
			col.a = 1f;
		else
			col.a = 0.75f + 0.25f * Mathf.Sin (10 * Time.realtimeSinceStartup);
		
		GetComponent<Text>().color = col;
		
	
	}
	
	void OnEnable(){
		isPointerInside = false;
	
	}
	
}
