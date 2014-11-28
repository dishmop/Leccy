using UnityEngine;

public class SpringValue{

	float desValue = 0f;
	float currentValue = 0f;
	float speed = 0.1f;
	
	
	public SpringValue(float val){
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

		// Update is called once per frame
	public void Update () {
		currentValue = Mathf.Lerp(currentValue, desValue, speed);
	
	}
}
