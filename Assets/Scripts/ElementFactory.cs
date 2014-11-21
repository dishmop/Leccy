﻿using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class ElementFactory : MonoBehaviour {
	public static ElementFactory singleton = null;
	
	int[]	typeCount;

	//----------------------------------------------
	[Serializable]
	public class FactoryPrefabData{
		public GameObject	prefab;
		public int		  	initialStockCount;

	}
	

	//----------------------------------------------
	public FactoryPrefabData[] 	initialStock;
	

	class FactoryData{
		public GameObject	factoryPrefab;
		public int		  	stockRemaining;
		public int		  	typeIndex;
		public int			index;
	}
	
	
	FactoryData[] 		stock;
	
	
	// Call if you make any changes to the prefab
	public void OnChangePrefab(){
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
		GameObject obj =   Instantiate(stock[index].factoryPrefab) as GameObject;
		obj.SetActive(true);
		return obj;
	}	

	// Get a pointer to the prefab we use to instantiate new elements
	// This can be modified if we want (e.g. change its orientation)
	public GameObject GetPrefab(int index){
		return stock[index].factoryPrefab;
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
	
	/*
		int numStock = stock.Length;
		bw.Write(numStock);
		for (int i = 0; i < numStock; ++i){
			bw.Write (stock[i].factoryPrefab.GetComponent<SerializationID>().id);
			bw.Write (stock[i].stockRemaining);
		}
		*/
	}
	
	public 	void Load(BinaryReader br){
	/*
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
		*/
	}
	
	
	//----------------------------------------------
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
		
		// Create our stock list fromthe initial lis
		stock = new FactoryData[initialStock.Length];		
		
		// Will get initialised to zero
		typeCount = new int[(int)CircuitElement.UIType.kNumTypes];
		
		for (int i = 0; i < stock.Length; ++i){
			int uiType = (int)(initialStock[i].prefab.GetComponent<CircuitElement>().uiType);
			stock[i] = new FactoryData();
			
			stock[i].stockRemaining = initialStock[i].initialStockCount;
			stock[i].typeIndex = typeCount[uiType]++;
			stock[i].index = i;
			Vector3 pos = new Vector3(transform.position.x, transform.position.y, initialStock[i].prefab.transform.position.z);
			stock[i].factoryPrefab = Instantiate(initialStock[i].prefab, pos, transform.rotation) as GameObject;
			stock[i].factoryPrefab.transform.parent = transform;
			stock[i].factoryPrefab.SetActive(false);
			
			// Since this is not active, Start will not get called - this means we must call this
			stock[i].factoryPrefab.GetComponent<CircuitElement>().InactveStart();
			
		}
		
	}
	
	
	
	void OnDestroy(){
		singleton = null;
	}	
	// for the moment, do an exhaustive search (should use a dictionary)
	FactoryData FindStockData(string id){
		return Array.Find(stock, element => element.factoryPrefab.GetComponent<SerializationID>().id == id);
		
	}
	
	//----------------------------------------------
	// for the moment, do an exhaustive search (should use a dictionary)
	FactoryData FindStockData(CircuitElement.UIType uiType, int typeIndex){
		return Array.Find(stock, element => (element.factoryPrefab.GetComponent<CircuitElement>().uiType == uiType && element.typeIndex == typeIndex));
		
	}	
	

}