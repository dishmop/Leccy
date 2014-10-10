using UnityEngine;
using System.Collections;

public class UI : MonoBehaviour {

	public GameObject 	gridGO;
	public GameObject	circuitGO;
	public GridPoint	newDrawPoint = new GridPoint();
	public GridPoint	oldDrawPoint = new GridPoint();
	
	public Rect			toolbarRect = new Rect(25, 25, 500, 30);


	Grid				grid;
	Circuit				circuit;
	
	public enum InputMode{
		kWires,
		kCells,
		kResistors,
		kErase
	};
	public InputMode inputMode;
	
	// Toolbar
	string[] toolbarStrings = {"Wires", "Cells", "Resistors", "Eraser"};

		
	// Use this for initialization
	void Start () {
		grid = gridGO.GetComponent<Grid>();	
		circuit = circuitGO.GetComponent<Circuit>();	
		
	}
	
	bool IsPosInUI(Vector3 pos){
		return toolbarRect.Contains (pos);
	}
	
	// Update is called once per frame
	void Update () {
			// If we are in erase mode, put grid highligher in that mode
		grid.EnableEraseHighlightMode((inputMode == InputMode.kErase));
	
		// Track the mouse pointer highlight
		Vector3 mousePos = Input.mousePosition;
		
		// If inside the UI, then not active and nothing else to do
		// as UI is hangled in OnGUI function
		if (IsPosInUI(new Vector3(mousePos.x, Screen.height - mousePos.y, 0f))){
			grid.SetSelected(new GridPoint(), new GridPoint());
			return;
		}
		mousePos.z = transform.position.z - Camera.main.transform.position.z;
		Vector3 worldPos = Camera.main.ScreenToWorldPoint( mousePos);
		
		GridPoint newPoint = new GridPoint((int)(worldPos.x + 0.5f), (int)(worldPos.y + 0.5f));
		
		// We also get a secondary point (used for selecting a connetion)
		float distThreshold = 0.15f;
		GridPoint otherPoint = new GridPoint();
		float xDiff = newPoint.x - worldPos.x;
		float yDiff = newPoint.y - worldPos.y;
		if (Mathf.Abs (xDiff) < distThreshold && Mathf.Abs (yDiff) > distThreshold){
			if (yDiff > 0){
				otherPoint = new GridPoint(newPoint.x, newPoint.y - 1);
			}
			else{
				otherPoint = new GridPoint(newPoint.x, newPoint.y + 1);
			}
		}
		else if (Mathf.Abs (xDiff) > distThreshold && Mathf.Abs (yDiff) < distThreshold){
			if (xDiff > 0){
				otherPoint = new GridPoint(newPoint.x - 1, newPoint.y );
			}
			else{
				otherPoint = new GridPoint(newPoint.x + 1, newPoint.y);
			}
		}
		
		
		grid.SetSelected(newPoint, otherPoint);
		
		// If the mouse button is down
		if (Input.GetMouseButton(0) && !Input.GetKey (KeyCode.LeftControl)){
			oldDrawPoint = newDrawPoint;
			newDrawPoint = new GridPoint(newPoint);
			
			// If the point we are drawing to has changed and the new one is valid
			if (!oldDrawPoint.IsEqual(newDrawPoint) && newDrawPoint.IsValid()){
			
				// If drawing wires
				if (inputMode == InputMode.kWires){
					if (oldDrawPoint.IsValid ()){
						circuit.AddWire(oldDrawPoint, newDrawPoint);
					}
					else{
						circuit.AddWire(newDrawPoint);
					}
				}
				// If drawing resistors
				if (inputMode == InputMode.kResistors){
					if (oldDrawPoint.IsValid ()){
						circuit.AddResistor(oldDrawPoint, newDrawPoint);
					}
					else{
						circuit.AddResistor(newDrawPoint);
					}
					
				}
				// If drawing cells
				if (inputMode == InputMode.kCells){
					if (oldDrawPoint.IsValid ()){
						circuit.AddCell(oldDrawPoint, newDrawPoint);
					}
					else{
						circuit.AddCell(newDrawPoint);
					}
					
				}				
				
				// If erasing
				if (inputMode == InputMode.kErase){
					if (oldDrawPoint.IsValid ()){
						circuit.Erase(oldDrawPoint, newDrawPoint);
					}
					else{
						// if we want to erase a connection
						if (otherPoint.IsValid ()){
							circuit.EraseConnection(newDrawPoint, otherPoint);
						}	
						else{
							circuit.Erase(newDrawPoint);
						}			
					}
				}
			}
		}
		else{
			// Set to invalid
			newDrawPoint = new GridPoint();
		}
	
	}
	
	void OnGUI () {
		inputMode = (InputMode)GUI.Toolbar (toolbarRect, (int)inputMode, toolbarStrings);
					
	}
	
}
