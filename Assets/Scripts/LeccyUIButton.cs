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
	
	public AudioSource okPress;
	public AudioSource notOkPress;
	
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
	
	public void IncStock(){
		ElementFactory.singleton.IncrementStock(circuitElementPrefab);
		PrefabManager.OnChangePrefab(circuitElementPrefab);
		
	}
	
	public void DecStock(){
		ElementFactory.singleton.DecrementStock(circuitElementPrefab);
		PrefabManager.OnChangePrefab(circuitElementPrefab);
	}
	
	void ConfigureButton(){
		if (circuitElementPrefab){
			CircuitElement element = circuitElementPrefab.GetComponent<CircuitElement>();
			transform.FindChild ("TextFrame").FindChild("Text").GetComponent<Text>().text = element.GetUIString();
			
			// make a copy so that we can modify it for display
			GameObject mainMeshPrefab = Instantiate(element.GetDisplayMesh()) as GameObject;
			
			if (element.IsWired()){
				GameObject wire1 = Instantiate (wireMeshPrefab) as GameObject;
				GameObject wire2 = Instantiate (wireMeshPrefab) as GameObject;
				wire1.transform.parent = mainMeshPrefab.transform;
				wire2.transform.parent = mainMeshPrefab.transform;
				
				wire1.transform.localPosition = new Vector3(0, 1, 0);
				wire1.transform.localRotation = Quaternion.Euler(0, 0, 180);
				wire2.transform.localPosition = new Vector3(0, -1, 0);
				wire2.transform.localRotation = Quaternion.Euler(0, 0, 0);
			}
			mainMeshPrefab.transform.parent = transform;

			
			transform.FindChild("ButtonFrame").FindChild("Button").FindChild("UIMesh").GetComponent<UIMesh>().SetPrefabMesh(mainMeshPrefab);
			int stockNum = ElementFactory.singleton.GetStockRemaining(circuitElementPrefab.GetComponent<SerializationID>().id);
			GameObject stockTextBox = transform.FindChild("ButtonFrame").FindChild("Button").FindChild("StockLeft").gameObject;
			if (element.uiType != CircuitElement.UIType.kModify){
				stockTextBox.GetComponent<Text>().text = stockNum.ToString();
			}
			else{
				stockTextBox.GetComponent<Text>().text = "";
			}
			
			// we only enable the stock inc and doc buttons in editor mode
			transform.FindChild("ButtonFrame").FindChild("StockAdjust").gameObject.SetActive(GameModeManager.singleton.enableEditor && (element.uiType != CircuitElement.UIType.kModify));
			
		}
	}
	
	public void OnClick(){
		okPress.Play();
		
		if (isSelected){
			bool changed = circuitElementPrefab.GetComponent<CircuitElement>().OnClick();
			if (changed) PrefabManager.OnChangePrefab(circuitElementPrefab);
			UI.singleton.SetSelectedPrefabId(circuitElementPrefab.GetComponent<SerializationID>().id);
		}
		else{
			SetSelected();
		}
	}	
	
	public void SetSelected(){
		transform.parent.GetComponent<ElementSelectPanel>().ClearSelection();
		isSelected = true;
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
