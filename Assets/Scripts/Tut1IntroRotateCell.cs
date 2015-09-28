using UnityEngine;
using System.Collections;

public class Tut1IntroRotateCell : TutTriggerBase {

	public enum Status{
		kUnset,
		kNotTwoCells,
		kCellNotSelected,
		kDoRotation,
	};
	
	Status status = Status.kUnset;
	Status lastStatus = Status.kUnset;
	
	int rotCount = 0;
	int lastOrient = 0;
	bool hasFoundTwo = false;
	
	
	// Use this for initialization
	protected override void OnEnable(){
		base.OnEnable();
		rotCount = 0;
		hasFoundTwo = false;
		transform.FindChild("ContinueButton").gameObject.SetActive(false);
	}	
	

	
	// Update is called once per frame
	void FixedUpdate () {

		
		int count = 0;
		foreach (GameObject go in Circuit.singleton.elements){
			if (go == null) continue;
			CircuitElementCell cell = go.GetComponent<CircuitElementCell>();
			if (cell == null) continue;
			
			count++;
		}
		
		status = Status.kDoRotation;
		if (UI.singleton.selectedPrefabId != "Cell"){
			status = Status.kCellNotSelected;
		}
		else if (count != 2){
			status = Status.kNotTwoCells;
		}
		
		if (IsActive()){
			status = Status.kDoRotation;
		}
		
		string text = "";
		switch (status){
			case Status.kCellNotSelected:{
				text = "You must select\nCell in the UI panel.";
				rotCount = 0;
				hasFoundTwo = false;
				break;
			}
			case Status.kNotTwoCells:{
				text = "Place a Cell\non the grid.";
				rotCount = 0;
				hasFoundTwo = false;
				break;
			}
			case Status.kDoRotation:{
				text = "Click on the cell\nyou placed to rotate it.\nNotice how the voltage changes.";
				// Find the cell we have placed
				
				GameObject voltMeter = null;
				foreach (GameObject go in Circuit.singleton.elements){
					if (go == null) continue;
					if (go.GetComponent<CircuitElementVoltmeter>() != null){
						voltMeter = go;
						break;
					}
				}
				if (MathUtils.FP.Feq(voltMeter.GetComponent<CircuitElementVoltmeter>().GetVoltageDiff(), 2)){
					hasFoundTwo = true;
				}
			    
				GameObject myCell = null;
				foreach (GameObject go in Circuit.singleton.elements){
					if (go == null) continue;
		
					
					CircuitElementCell cell = go.GetComponent<CircuitElementCell>();
					if (cell == null) continue;
					
					int x = Mathf.RoundToInt(go.transform.position.x);
					int y = Mathf.RoundToInt(go.transform.position.y);
					
					if (Circuit.singleton.elements[x,y] != go){
						Debug.LogError("ERROR");
					}
					
					Circuit.AnchorData anchorData = Circuit.singleton.anchors[x,y];
					if (anchorData.isAnchored[0]) continue;
					
					myCell = go;
					break;
					
				}
				int orient = myCell.GetComponent<CircuitElementCell>().orient;
				if (lastStatus == status){
					if (lastOrient != orient){
						rotCount++;
					}
				}
				lastOrient = orient;
				if (rotCount > 0){
					text += "\n↑";
				}
				if (rotCount > 1){
					text += "→";
				}
				if (rotCount > 2){
					text += "↓";
				}
				if (rotCount > 3){
					text += "←";
					if (!hasFoundTwo){
						text += "\nMake sure there is\nan unbroken wire from the cells\nto the Voltmeter";
						
					}
					else{
						text += "\nWhen the two cells are\naligned the votlages add.\nIf opposing then they cancel out.\n";
						transform.FindChild("ContinueButton").gameObject.SetActive(true);
					}
				}
				break;
			}
		}
		lastStatus = status;
		transform.FindChild("MainText").GetComponent<TextMesh>().text = text;
		
	//		Debug.Log("lastNumLoops: " + Simulator.singleton.lastNumLoops);
		
	
	}
}
