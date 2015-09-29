using UnityEngine;
using System.Collections;

public class FractionCalc : MonoBehaviour {

	public float value = 0f;
	public Color color;
	
	static public float epsilon  = 0.001f;
	float 	lastValue = -1f;
	int		integer = 0;
	int		numerator = 0;
	int		denominator = 0;
	bool 	isNeg = false;


	// Use this for initialization
	void Start () {
		Transform recentreT = transform.FindChild ("Recentre").transform;
		recentreT.gameObject.SetActive(false);
		
	
	}
	
	// Update is called once per frame
	void Update () {
	
		Transform recentreT = transform.FindChild ("Recentre").transform;
		
		recentreT.gameObject.SetActive(true);
		
		// Ensure the text always points upwards
		recentreT.rotation = Quaternion.identity;
		
		recentreT.FindChild("Integer").GetComponent<TextMesh>().color = color;
		recentreT.FindChild("Numerator").GetComponent<TextMesh>().color = color;
		recentreT.FindChild("Denominator").GetComponent<TextMesh>().color = color;
		recentreT.FindChild("Seperator").GetComponent<TextMesh>().color = color;
		if (!MathUtils.FP.Feq(lastValue, value, epsilon)){
			lastValue = value;
			RecalcFraction();
			int intToDisplay = integer * (isNeg ? -1 : 1);
			recentreT.FindChild("Integer").GetComponent<TextMesh>().text = intToDisplay.ToString();
			if (!MathUtils.FP.Feq(numerator, 0, epsilon)){
				recentreT.FindChild("Numerator").GetComponent<TextMesh>().text = numerator.ToString();
				recentreT.FindChild("Denominator").GetComponent<TextMesh>().text = denominator.ToString();
				recentreT.FindChild("Seperator").GetComponent<TextMesh>().text = "_";
				
				// If the integer is zero then don't show the integer
				if (MathUtils.FP.Feq(integer, 0, epsilon)){
					if (isNeg){
						recentreT.FindChild("Integer").GetComponent<TextMesh>().text = "-";
					}
					else{
						recentreT.FindChild("Integer").GetComponent<TextMesh>().text = "";
					}
				}
					
			}
			else{
				recentreT.FindChild("Numerator").GetComponent<TextMesh>().text = "";
				recentreT.FindChild("Denominator").GetComponent<TextMesh>().text = "";
				recentreT.FindChild("Seperator").GetComponent<TextMesh>().text = "";
			}
		}
		if (Mathf.Abs(value) > 100){
			recentreT.FindChild("Integer").GetComponent<TextMesh>().text = "∞";
			recentreT.FindChild("Numerator").GetComponent<TextMesh>().text = "";
			recentreT.FindChild("Denominator").GetComponent<TextMesh>().text = "";
			recentreT.FindChild("Seperator").GetComponent<TextMesh>().text = "";
			
		}
		// Recentre
		Bounds integerBounds = recentreT.FindChild("Integer").GetComponent<TextMesh>().GetComponent<Renderer>().bounds;
		Bounds numeratorBounds = recentreT.FindChild("Numerator").GetComponent<TextMesh>().GetComponent<Renderer>().bounds;
		Bounds denominatorBounds = recentreT.FindChild("Denominator").GetComponent<TextMesh>().GetComponent<Renderer>().bounds;
		
		float minX = Mathf.Min (Mathf.Min (integerBounds.min.x, numeratorBounds.min.x), denominatorBounds.min.x);
		float maxX= Mathf.Max (Mathf.Max (integerBounds.max.x, numeratorBounds.max.x), denominatorBounds.max.x);
		float midX = 0.5f * (minX + maxX);
		
		Vector3 offset = new Vector3(transform.position.x - midX, 0, 0);
		
		// Need to do this to cope with Amater effect scaking the number
		offset.x *= 1f/recentreT.lossyScale.x;
		offset.y *= 1f/recentreT.lossyScale.y;
		
		Vector3 newPos = recentreT.position + offset;
		recentreT.position  = newPos;
		
		
		
	}
	
	void RecalcFraction(){
		float x = value;
		isNeg = (x < 0);
		x = Mathf.Abs(x);
		integer = Mathf.FloorToInt(x);
		x -= integer;
		
		// Check if we are exactly an integer
		if (MathUtils.FP.Feq(x, 0, epsilon)){
			numerator = 0;
			denominator = 1;
			return;
		}
		if (MathUtils.FP.Feq(x, 1, epsilon)){
			integer += 1;
			numerator = 0;
			denominator = 1;
			return;
		}
				
		
		// The lower fraction is 0/1
		int lower_n = 0;
		int lower_d = 1;
		// The upper fraction is 1/1
		int upper_n = 1;
		int upper_d = 1;
		
		while (true){
			// The middle fraction is (lower_n + upper_n) / (lower_d + upper_d)
			int middle_n = lower_n + upper_n;
			int middle_d = lower_d + upper_d;
			
			// If x + error < middle
			float middleVal = (float)middle_n / (float)middle_d;
			if (MathUtils.FP.Feq (middleVal, x, epsilon)){
				numerator = middle_n;
				denominator = middle_d;
				return;
			}
			if (x < middleVal){
				// middle is our new upper
				upper_n = middle_n;
				upper_d = middle_d;
			}
			if (x > middleVal){
				// middle is our new lower
				lower_n = middle_n;
				lower_d = middle_d;
			}
	
		}
	}
}
