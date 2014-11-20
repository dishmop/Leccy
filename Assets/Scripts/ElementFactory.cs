using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class ElementFactory : MonoBehaviour {
	public static ElementFactory singleton = null;
	
	public int defaultStockRemaining = 0;
	
	int[]	typeCount;

	//----------------------------------------------
	[Serializable]
	public class FactoryData{
		public GameObject	prefab;
		public int		  	stockRemaining;
		public int		  	typeIndex;
		public int			index;
	}

	public FactoryData[] stock;
	

	//----------------------------------------------
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
		
		
		// Will get initialised to zero
		typeCount = new int[(int)CircuitElement.UIType.kNumTypes];
		
		for (int i = 0; i < stock.Length; ++i){
			FactoryData data = stock[i];
			int uiType = (int)(data.prefab.GetComponent<CircuitElement>().uiType);
			
			data.stockRemaining = defaultStockRemaining;
			data.typeIndex = typeCount[uiType]++;
			data.typeIndex = i;
			
		}
		
	}
	

	
	void OnDestroy(){
		singleton = null;
	}
	
	//----------------------------------------------
	public int GetNumElements(CircuitElement.UIType uiType){
		if (uiType != CircuitElement.UIType.kNone)
			return typeCount[(int)uiType];
		else
			return stock.Length;
	}

	public int GetNumElements(){
		return stock.Length;
	}
	
	public int GetIndex(string id){
		return FindStockData(id).index;
	}
	
	public int GetIndex(CircuitElement.UIType uiType, int typeIndex){
		if (uiType != CircuitElement.UIType.kNone)
			return FindStockData(uiType, typeIndex).index;
		else
			return typeIndex;
	}	
	
	

	//----------------------------------------------
	// Create a new circuit element
	public GameObject InstantiateElement(int index){
		return Instantiate(stock[index].prefab) as GameObject;
	}	

	// Get a pointer to the prefab we use to instantiate new elements
	// This can be modified if we want (e.g. change its orientation)
	public GameObject GetPrefab(int index){
		return stock[index].prefab;
	}
	
	
	public int GetStockRemaining(int index){
		return stock[index].stockRemaining;
	}
	
	
	//----------------------------------------------
	// Create a new circuit element
	public GameObject InstantiateElement(string id){
		return InstantiateElement(GetIndex (id));
	}	
	
	// Get a pointer to the prefab we use to instantiate new elements
	// This can be modified if we want (e.g. change its orientation)
	public GameObject GetPrefab(string id){
		return GetPrefab(GetIndex (id));
	}
	
	
	public int GetStockRemaining(string id){
		return GetStockRemaining(GetIndex (id));
	}	
	
	//----------------------------------------------
	// Create a new circuit element

	public GameObject InstantiateElement(CircuitElement.UIType uiType, int typeIndex){
		return InstantiateElement(GetIndex (uiType, typeIndex));
	}	
	
	// Get a pointer to the prefab we use to instantiate new elements
	// This can be modified if we want (e.g. change its orientation)
	public GameObject GetPrefab(CircuitElement.UIType uiType, int typeIndex){
		return GetPrefab(GetIndex (uiType, typeIndex));
	}
	
	
	public int GetStockRemaining(CircuitElement.UIType uiType, int typeIndex){
		return GetStockRemaining(GetIndex (uiType, typeIndex));
	}	

	

	public 	void Save(BinaryWriter bw){
		int numStock = stock.Length;
		bw.Write(numStock);
		for (int i = 0; i < numStock; ++i){
			bw.Write (stock[i].prefab.GetComponent<SerializationID>().id);
			bw.Write (stock[i].stockRemaining);
		}
	}
	
	public 	void Load(BinaryReader br){
		foreach (FactoryData data in stock){
			data.stockRemaining = defaultStockRemaining;
		}

				// We don't attmept to remake the stock list
		// we ust assign the stock remaining values to the appropriate prefab
		int numStock = br.ReadInt32 ();
		for (int i = 0; i < numStock; ++i){
			string id = br.ReadString();
			FindStockData(id).stockRemaining = br.ReadInt32 ();
		}
	}
	
	// for the moment, do an exhaustive search (should use a dictionary)
	FactoryData FindStockData(string id){
		return Array.Find(stock, element => element.prefab.GetComponent<SerializationID>().id == id);
		
	}
	
	// for the moment, do an exhaustive search (should use a dictionary)
	FactoryData FindStockData(CircuitElement.UIType uiType, int typeIndex){
		return Array.Find(stock, element => (element.prefab.GetComponent<CircuitElement>().uiType == uiType && element.typeIndex == typeIndex));
		
	}	
	

}
