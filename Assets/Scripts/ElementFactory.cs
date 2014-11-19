using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class ElementFactory : MonoBehaviour {
	public static ElementFactory singleton = null;
	
	public int defaultStockRemaining = 0;

	[Serializable]
	public class FactoryData{
		public GameObject prefab;
		public int		  stockRemaining;
	}

	public FactoryData[] stock;
	

	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;

		foreach (FactoryData data in stock){
			data.stockRemaining = defaultStockRemaining;
		}
	}
	

	
	void OnDestroy(){
		singleton = null;
	}
	
	// Create a new circuit element
	public GameObject InstantiateElement(string id){
		return Instantiate(FindStockData(id).prefab) as GameObject;
	}	
	
	// Get a pointer to the prefab we use to instantiate new elements
	// This can be modified if we want (e.g. change its orientation)
	public GameObject GetPrefab(string id){
		return FindStockData(id).prefab;
	}
	
	public int GetStockRemaining(string id){
		return FindStockData(id).stockRemaining;
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
	

}
