using UnityEngine;
using System.Collections.Generic;

public class PrefabManager {


	static List<PrefabListener>	listeners = new List<PrefabListener>();
	
	public static void AddListener(PrefabListener listener){
		listeners.Add (listener);
	}
	
	public static void RemoveListener(PrefabListener listener){
		listeners.Remove(listener);
	} 


	public static void OnChangePrefab(GameObject prefab){
		foreach (PrefabListener listener in listeners){
			listener.OnChangePrefab(prefab);
		}
	}
}
 