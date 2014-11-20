using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LeccyUIButton : MonoBehaviour, PrefabListener {

	public GameObject circuitElementPrefab;
	public bool	isSelected = false;
	public Color selectColor;
	public Color normalColor;
	public Color selectHighlightColor;
	public Color normalHighlightColor;	
	public Color pressColor;	
	
	void Awake(){
		PrefabManager.AddListener(this);
	}
	
	void OnDestroy(){
		PrefabManager.RemoveListener(this);
	}
	
	public void OnChangePrefab(GameObject preFab){
		if (preFab == circuitElementPrefab){
			ConfigureButton();
		}
	}
	
	
	void ConfigureButton(){
		if (circuitElementPrefab){
			transform.FindChild ("TextFrame").FindChild("Text").GetComponent<Text>().text = circuitElementPrefab.GetComponent<CircuitElement>().GetUIString();
			transform.FindChild("ButtonFrame").FindChild("Button").FindChild("UIMesh").GetComponent<UIMesh>().SetPrefabMesh(circuitElementPrefab.GetComponent<CircuitElement>().GetDisplayMesh());
		}
	}
	
	public void OnClick(){
		if (isSelected){
			bool changed = circuitElementPrefab.GetComponent<CircuitElement>().OnClick();
			if (changed) PrefabManager.OnChangePrefab(circuitElementPrefab);
		}
		else{
			transform.parent.GetComponent<ElementSelectPanel>().ClearSelection();
			isSelected = true;
		}
	}	
		

	// Use this for initialization
	void Start () {
		ConfigureButton();
		
	}
	
	void Update(){
		Transform buttonT = transform.FindChild ("ButtonFrame").FindChild("Button");
		ColorBlock cols = buttonT.GetComponent<Button>().colors;
		cols.normalColor = isSelected ? selectColor : normalColor;
		cols.highlightedColor = isSelected ? selectHighlightColor : normalHighlightColor;	
		cols.pressedColor = pressColor;
		buttonT.GetComponent<Button>().colors = cols;
	}
	

}
