using UnityEngine;
using System;

public class SpringValue{

	public enum Mode{
		kAsymptotic,
		kLinear
	};
	
	float desValue = 0f;
	float currentValue = 0f;
	float linSpeed = 500f;
	float asSpeed = 10f;
	
	
	
	Mode mode =  Mode.kAsymptotic;
	
	
	public SpringValue(float val){
		desValue = val;
		currentValue = val;
	}

	public SpringValue(float val, Mode setMode ){
		mode = setMode;
		desValue = val;
		currentValue = val;
	}
	
	public void Set(float newVal){
		desValue = newVal;
	}
	
	public void Force(float newVal){
		desValue = newVal;
		currentValue = newVal;
	}
	
	public float GetValue(){
		return currentValue;
	}

	public float GetDesValue(){
		return desValue;
	}
	
	public bool IsAtTarget(){
		return MathUtils.FP.Feq(GetValue(), GetDesValue());
	}
	
	// Update is called once per frame
	public void Update () {
		float deltatTime = Time.deltaTime;
		switch (mode){
			case Mode.kAsymptotic:
				currentValue = Mathf.Lerp(currentValue, desValue, asSpeed * deltatTime);
				break;
			case Mode.kLinear:
				if (!MathUtils.FP.Feq (currentValue, desValue, linSpeed * Time.fixedDeltaTime)){
					if (currentValue > desValue)
						currentValue -= linSpeed * deltatTime;
					else
						currentValue += linSpeed * deltatTime;
				}
				else{
					currentValue = desValue; 
				}
				break;
		}
	
	}
}
