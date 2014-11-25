using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LeccyUIButton : MonoBehaviour, PrefabListener {

	public GameObject circuitElementPrefab;
	public GameObject wireMeshPrefab;
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
			
			// make a copy so that we can modify it for display
			GameObject mainMeshPrefab = Instantiate(circuitElementPrefab.GetComponent<CircuitElement>().GetDisplayMesh()) as GameObject;
			GameObject wire1 = Instantiate (wireMeshPrefab) as GameObject;
			GameObject wire2 = Instantiate (wireMeshPrefab) as GameObject;
			wire1.transform.parent = mainMeshPrefab.transform;
			wire2.transform.parent = mainMeshPrefab.transform;
			
			// are we horizontal?
			bool isHorizontal = false;//circuitElementPrefab.GetComponent<CircuitElement>().connectionBehaviour[1] == CircuitElement.ConnectionBehaviour.kSociable;
			if (isHorizontal){
				wire1.transform.localPosition = new Vector3(1, 0, 0);
				wire1.transform.localRotation = Quaternion.Euler(0, 0, 90);
				wire2.transform.localPosition = new Vector3(-1, 0, 0);
				wire2.transform.localRotation = Quaternion.Euler(0, 0, -90);
			}
			else{
				wire1.transform.localPosition = new Vector3(0, 1, 0);
				wire1.transform.localRotation = Quaternion.Euler(0, 0, 180);
				wire2.transform.localPosition = new Vector3(0, -1, 0);
				wire2.transform.localRotation = Quaternion.Euler(0, 0, 0);
			}
			

			
			transform.FindChild("ButtonFrame").FindChild("Button").FindChild("UIMesh").GetComponent<UIMesh>().SetPrefabMesh(mainMeshPrefab);
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
		UI.singleton.SetSelectedPrefabId(circuitElementPrefab.GetComponent<SerializationID>().id);
	}	
		

	// Use this for initialization
	void Start () {
		ConfigureButton();
		if (isSelected) UI.singleton.SetSelectedPrefabId(circuitElementPrefab.GetComponent<SerializationID>().id);
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
