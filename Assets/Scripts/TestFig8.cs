using UnityEngine;
using System.Collections;

public class TestFig8 : MonoBehaviour {

	public	float speed = 0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		
		Vector3 pos = transform.position;
		
		float time = Time.time;
		
	
		float pi = 3.14159f;
		
		// Go round twice so we can join in the middle (actually, we do a figure of 8)
		float scaledY = time * speed % 1f;
		bool isUpper = (scaledY - 0.5) > 0;
		//float multiplier = 1.0f;
		
		scaledY = scaledY * 4 * pi;
		
		pos.x = Mathf.Sin(scaledY);
		if (isUpper){	
			pos.y = 2.0f - Mathf.Cos(scaledY);
		}
		else{
			pos.y = Mathf.Cos(scaledY);
		}
		
		transform.position = pos;
		
		
	
	}
}
