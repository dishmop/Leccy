using UnityEngine;
using System.Collections;

public class ElementSelectButton : MonoBehaviour {

	public void OnClick(){
		GameObject prefab = transform.parent.parent.GetComponent<LeccyUIButton>().circuitElementPrefab;
		prefab.GetComponent<CircuitElement>().Rotate(1);
		PrefabManager.OnChangePrefab(prefab);
	}
}
