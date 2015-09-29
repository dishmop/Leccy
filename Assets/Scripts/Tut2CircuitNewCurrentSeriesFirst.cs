using UnityEngine;
using System.Collections;

public class Tut2CircuitNewCurrentSeriesFirst : TutTriggerBase {


	// Use this for initialization
	protected override void OnEnable () {
		base.OnEnable();
		
		
		
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (!IsActive()) return;
	

		bool haveFound = false;
		Vector2 rectMin = Vector2.zero;
		Vector2 rectMax = Vector2.zero;
		int resistorCount = 0;
		
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			CircuitElementResistor resistor = go.GetComponent<CircuitElementResistor>();
			if (resistor != null) resistorCount++;
			
			CircuitElement element = resistor;
			if (element != null){
				if (!haveFound){
					rectMin = new Vector2(element.GetGridPoint().x - 0.5f, element.GetGridPoint().y - 0.5f);
					rectMax = new Vector2(element.GetGridPoint().x + 0.5f, element.GetGridPoint().y + 0.5f);
					haveFound = true;
					
				}
				else{
					rectMin = new Vector2(
						Mathf.Min(rectMin.x, element.GetGridPoint().x - 0.5f), 
						Mathf.Min(rectMin.y, element.GetGridPoint().y - 0.5f));
					rectMax = new Vector2(
						Mathf.Max(rectMax.x, element.GetGridPoint().x + 0.5f), 
						Mathf.Max(rectMax.y, element.GetGridPoint().y + 0.5f));
				}
				
			}
		}
		
		if (resistorCount == 2){
			
			Rect rect = new Rect(rectMin, rectMax - rectMin);
			
			Vector3 pos = 0.5f * (rectMin + rectMax);
			pos.z = -4;
			transform.FindChild("White square").gameObject.SetActive(true);
			transform.FindChild("White square").position = pos;
			transform.FindChild("White square").localScale = new Vector3(rect.width, rect.height, 0);
			Color col = transform.FindChild("White square").GetComponent<SpriteRenderer>().color;
			col.a = 64f/256f;
			transform.FindChild("White square").GetComponent<SpriteRenderer>().color = col;
			
		}
		else{
			transform.FindChild("White square").gameObject.SetActive(false);
		}
		
		
		
		
		
		
		
	}
}
