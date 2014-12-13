using UnityEngine;
using System.Collections;

public class AmmeterEffect : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
		Transform fractionTransform = transform.FindChild ("FractionTextBox");
		// Rise up
		Vector3 pos = fractionTransform.position;
		pos.y += 0.01f * 50 * Time.deltaTime;
		fractionTransform.position = pos;
		
		// Get larger
		Vector3 scale = fractionTransform.localScale;
		scale.x *= Mathf.Pow (1.01f, 50 * Time.deltaTime);
		scale.y *= Mathf.Pow (1.01f, 50 * Time.deltaTime);
		fractionTransform.localScale = scale;
		
		// Alpha out
		Color thisCol = fractionTransform.gameObject.GetComponent<FractionCalc>().color;
		thisCol.a -= 0.004f * 50 * Time.deltaTime;
		
		fractionTransform.gameObject.GetComponent<FractionCalc>().color = thisCol;
		
		if (thisCol.a <= 0){
			GameObject.Destroy(this.gameObject);
		}
		
		
	
	}
}
