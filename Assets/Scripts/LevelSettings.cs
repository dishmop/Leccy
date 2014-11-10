using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class LevelSettings : MonoBehaviour {

	public static LevelSettings singleton = null;

	public Circuit circuit;
	
	public int numWires = 10;
	public int numCells = 10;
	public int numResistors = 10;
	public int numAmeters = 1;
	
	public int numWiresOnStartup = 0;
	public int numCellsOnStartup = 0;
	public int numResistorsOnStartup = 0;
	public int numAmetersOnStartup = 0;
	

	public 	void Save(BinaryWriter bw){
		bw.Write (numWires);
		bw.Write (numCells);
		bw.Write (numResistors);
		bw.Write (numAmeters);
	}
	
	public 	void Load(BinaryReader br){
		numWires = br.ReadInt32();
		numCells = br.ReadInt32();
		numResistors = br.ReadInt32();
		numAmeters = br.ReadInt32();
		
		// The circuit must have been loaded first for this to work
		numWiresOnStartup = circuit.numElementsUsed["Wire"];
		numCellsOnStartup = circuit.numElementsUsed["Cell"];
		numResistorsOnStartup = circuit.numElementsUsed["Resistor"];
		numAmetersOnStartup = circuit.numElementsUsed["Ameter"];
	}

	// Use this for initialization
	void Start () {
		singleton = this;
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
