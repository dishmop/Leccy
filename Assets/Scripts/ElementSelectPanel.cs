using UnityEngine;
using System.Collections;

public class ElementSelectPanel : MonoBehaviour {

	public GameObject 				leccyButtonPrefab;
	public CircuitElement.UIType 	uiTypeFilter;
	public float					xCount;		// Number of buttons in a row
	public float					yCount;		// Number of buttons in a column
	
	
	public void ClearSelection(){
		foreach (Transform child in transform){
			child.GetComponent<LeccyUIButton>().isSelected = false;
		}
	}

	// Use this for initialization
	void Start () {
		ElementFactory factory = ElementFactory.singleton;
		int numElements = factory.GetNumElements(uiTypeFilter);
		int xPos = 0;
		int yPos = 0;
		for (int i = 0; i < numElements; ++i){
			GameObject newButton = Instantiate(leccyButtonPrefab) as GameObject;
			newButton.transform.SetParent(transform, false);
			
			// Set which circuie element we are referencing
			newButton.GetComponent<LeccyUIButton>().circuitElementPrefab = factory.GetPrefab(uiTypeFilter, i);
			
			// Set the transform
			RectTransform rectTransform = newButton.GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(xPos/xCount, (yCount - yPos - 1f)/yCount);
			rectTransform.anchorMax = new Vector2((1f + xPos)/xCount, (yCount - yPos)/yCount);
			xPos++;
			if (xPos >= xCount){
				xPos = 0;
				yPos++;
			}
			rectTransform.pivot = new Vector2(0, 0);
			rectTransform.offsetMax = new Vector2(0, 0);
			rectTransform.offsetMin = new Vector2(0, 0);
		}
	
	
	}
	
	
	

	
}
