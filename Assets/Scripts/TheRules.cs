using UnityEngine;
using System.Collections;

public class TheRules : MonoBehaviour {

	public enum Rules{
		kAll,
		kConstantVoltage,
		kCurrentJunction,
		kOhmsLaw,
		kSeriesResistors,
		kParallelResistors,
		kNumRules
	};
	
	string[] ruleNames = new string[(int)Rules.kNumRules];
	
	void Start(){
		ruleNames[(int)Rules.kAll] = "";
		ruleNames[(int)Rules.kConstantVoltage] = 	"Constant Voltage";
		ruleNames[(int)Rules.kCurrentJunction] = 	"Current Junction";
		ruleNames[(int)Rules.kOhmsLaw] = 			"Ohms Law";
		ruleNames[(int)Rules.kSeriesResistors] =	"Series Resistors";
		ruleNames[(int)Rules.kParallelResistors] = 	"Parallel Resistors";
	}
	
	public void EnableRule (Rules rule) {
		EnableRule(rule, true);
	}
	
	public void DisableRule (Rules rule) {
		EnableRule(rule, false);
	}
	
	public void EnableRule (int rule) {
		EnableRule((Rules)rule, true);
	}
	
	public void DisableRule (int rule) {
		EnableRule((Rules)rule, false);
	}

		// Use this for initialization
	void EnableRule (Rules rule, bool enable) {
	
		if (rule == Rules.kAll){
			foreach (Transform child in transform){
				child.gameObject.SetActive(enable);
			}
		}
		else{
			transform.FindChild (ruleNames[(int)rule]).gameObject.SetActive(enable);
		}
		// If only the title is visible, make it invisible
		bool isSomethingVisible = false;
		foreach (Transform child in transform){
			if (child.gameObject.activeSelf && child.name != "Title"){
				isSomethingVisible = true;
			}
		}
		if (isSomethingVisible){
			bool isSomethingOnRightVisible = transform.FindChild(ruleNames[(int)Rules.kSeriesResistors]).gameObject.activeSelf || transform.FindChild(ruleNames[(int)Rules.kParallelResistors]).gameObject.activeSelf;
			if (isSomethingOnRightVisible){
				transform.FindChild("Title_Centre").gameObject.SetActive(true);
				transform.FindChild("Title_Left").gameObject.SetActive(false);
			}
			else{
				transform.FindChild("Title_Centre").gameObject.SetActive(false);
				transform.FindChild("Title_Left").gameObject.SetActive(true);
			}
			
		}
		else{
			transform.FindChild("Title_Centre").gameObject.SetActive(false);
			transform.FindChild("Title_Left").gameObject.SetActive(false);
		}
	}
	
	// Update is called once per frame
	void FixedUpdate () {
	
	}
}
