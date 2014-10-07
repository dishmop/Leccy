using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Simluator : MonoBehaviour {

	public GameObject	circuitGO;
	public GameObject 	textMeshGO;
	public enum VisMode{
		kNone,
		kLoops,
		kGroups,
		kCurrents
	};
	public VisMode visMode = VisMode.kNone;
	
	
	GameObject[,]		debugTextBoxes;
	GameObject[,]       debugCurrentTextBoxesRight;
	GameObject[,]       debugCurrentTextBoxesUp;
	
	Circuit						circuit;
	int							width;
	int							height;
	
	
	SimData[,,]					simData;
	
	// List lof loops (the first loop is always the outer one which we can ignore
	// The remainder will form an indepednent set of loops
	List<List<BranchAddress>>	loops;	
	int							numGroups;
	float[]						loopCurrents;
	
	// GUI layout
	GUITextDisplay				guiTextDisplay;
	
	class SimData{
		
		public bool traversed;
		public int loopId;
		public int groupId;
	}	
	
	// Corresponds to the branch exiting that node in that dir
	struct BranchAddress{
	
		public BranchAddress(int x, int y, int dir){
			this.x = x;
			this.y = y;
			this.dir = dir;
		}

		public int x;
		public int y;
		public int dir; 		// 0 - kUp, 1 - kRight, 2 - kDown, 3 - kLeft
	};

	// Use this for initialization
	void Start () {
		circuit = circuitGO.GetComponent<Circuit>();
		width = circuit.elements.GetLength(0);
		height = circuit.elements.GetLength(1);
		
		DebugCreateLoopCurrentVis();
		DebugCreateCurrentVis();
		ClearSimulation ();

		// Sert up the text gui
		guiTextDisplay = new GUITextDisplay(10f, 70f, 500f, 20f);
	}
	
	SimData GetBranchData(BranchAddress address){
		return simData[address.x, address.y, address.dir];
	}
	
	// Return the address of the branch travelling in the oposite direction
	BranchAddress GetOppositeAddress(BranchAddress address){
		switch (address.dir){
			case Circuit.kUp: return new BranchAddress(address.x, address.y + 1, Circuit.kDown);
			case Circuit.kRight: return new BranchAddress(address.x + 1, address.y, Circuit.kLeft);
			case Circuit.kDown: return new BranchAddress(address.x, address.y - 1, Circuit.kUp);
			case Circuit.kLeft: return new BranchAddress(address.x - 1, address.y, Circuit.kRight);
		}
		return new BranchAddress();
	}
	
	
	BranchAddress CalcNextDir(GridPoint point, int[] choices){
		CircuitElement nextElement = circuit.GetElement(point);
		for (int i = 0;  i < 4; ++i){
			if (nextElement.isConnected[choices[i]]) return new BranchAddress(point.x, point.y, choices[i]);
		}
		Debug.LogError ("Error in CalcNextDir");
		return new BranchAddress();
	}
	
	// go from our current branch to the next one turning right if we can
	BranchAddress MoveLeft(BranchAddress address){
		BranchAddress oppositeAddress = GetOppositeAddress(address);
		GridPoint nextNode = new GridPoint(oppositeAddress.x, oppositeAddress.y);
		switch (address.dir){
			case Circuit.kUp: return CalcNextDir(nextNode, new int[4] {Circuit.kLeft, Circuit.kUp, Circuit.kRight, Circuit.kDown});
			case Circuit.kRight: return CalcNextDir(nextNode, new int[4] {Circuit.kUp, Circuit.kRight, Circuit.kDown, Circuit.kLeft});
			case Circuit.kDown: return CalcNextDir(nextNode, new int[4] {Circuit.kRight, Circuit.kDown, Circuit.kLeft, Circuit.kUp});
			case Circuit.kLeft: return CalcNextDir(nextNode, new int[4] {Circuit.kDown, Circuit.kLeft, Circuit.kUp, Circuit.kRight});
			default:{
				Debug.Log ("Error in switch statement");
				return new BranchAddress();
			}
		}

	}
	
	void AddLoopElement(BranchAddress addr, int loopId, int groupId){
		SimData data = GetBranchData (addr);
		data.traversed = true;
		data.loopId = loopId;
		data.groupId = groupId;
		loops[loops.Count-1].Add(addr);
	}
	
	void FollowLoopLeft(BranchAddress startAddress, int loopId, int groupId){
	
		// Add the starting position to the loop and then move
		AddLoopElement(startAddress, loopId, groupId);
		
		//bool uTurn = false;
		
		for (BranchAddress addr = MoveLeft(startAddress);!addr.Equals(startAddress); addr = MoveLeft (addr)){
			AddLoopElement(addr, loopId, groupId);
		}
	}
	
	int CreateLoop(){
		loops.Add (new List<BranchAddress>());
		return loops.Count-1;
	}
	
	
	// This method will only work if there are no wires crossing over each other
	// It does it in a slightly odd way because it needs to also group the loops by disjoint circuits
	void FindLoops(){
		int groupId = 0;
		
		// Can the grid until we find a connector which has not yet  been traversed
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				// If no element at this node, then no connections to it
				if (circuit.elements[x,y]){
					// Go through each of the branches from this node and only consider data on them if we have
					// a connection along that branch
					CircuitElement thisElement = circuit.GetElement(new GridPoint(x, y));
					for (int i = 0; i < 4; ++i){
						if (thisElement.isConnected[i]){
							BranchAddress thisAddress = new BranchAddress(x, y, i);
							if (!GetBranchData(thisAddress).traversed){
								// Yes! we have found one
								
								
								// Create the outer (zeroID) loop
								int loopId = CreateLoop();								
								FollowLoopLeft(thisAddress, loopId, groupId);
								
								// Now start at the begginning of our outer loop and traverse it 
								// Looking for conections that have not yet been traversed. If we find one, follow that loop
								int searchloop = loopId;
								while (searchloop <= loopId){
									for  (int searchIndex = 0; searchIndex < loops[searchloop].Count; ++searchIndex){
										GridPoint thisPoint2 = new GridPoint(loops[searchloop][searchIndex].x, loops[searchloop][searchIndex].y);
										CircuitElement thisElement2 = circuit.GetElement(thisPoint2);
										for (int j = 0; j < 4; ++j){
											if (thisElement2.isConnected[j]){
												BranchAddress thisAddress2 = new BranchAddress(thisPoint2.x, thisPoint2.y, j);
												if (!GetBranchData(thisAddress2).traversed){
													loopId = CreateLoop ();
													FollowLoopLeft (thisAddress2, loopId, groupId);
												}
											}
										}
									}
									searchloop++;
								}
								groupId++;
							}
						}
					}
				}
			}
		}
		numGroups = groupId;
	}
	
	// ALl loops that simply tracing the outline of a disjoint circuit 
	// are not needed and so are makred with the loopId 0
	void FlagOuterLoops(){
		int lastGroupId = -1;
		for (int i = 0; i < loops.Count; ++i)
		{
			int groupId = GetBranchData(loops[i][0]).groupId;
			if (groupId != lastGroupId){
				lastGroupId = groupId;
				for (int j = 0; j < loops[i].Count; ++j){
					GetBranchData(loops[i][j]).loopId = -1;
				}
			}
		}
	}
	
	void TrimSpokes(){
		for (int i = 0; i < loops.Count; ++i)
		{
			bool mayHaveDeadEnds = true;
			while (mayHaveDeadEnds){
				// Locate a dead end
				int 	thisIndex = 0;
				int  	nextIndex = 0;
				mayHaveDeadEnds = false;
				bool haveDeadEnd = false;
				while (thisIndex < loops[i].Count){
					nextIndex = (thisIndex + 1) % loops[i].Count;
					
					// It is a dead end if two adjacent elements are opposits
					if(loops[i][thisIndex].Equals (GetOppositeAddress(loops[i][nextIndex]))){
						mayHaveDeadEnds = true;
						haveDeadEnd = true;
						break;
					} 
					++thisIndex;
				}
				while (haveDeadEnd){
					// First null them in the simdData array
					GetBranchData (loops[i][thisIndex]).loopId = -2;
					GetBranchData (loops[i][nextIndex]).loopId = -2;
					// Remove this dead end
					if (thisIndex < nextIndex){
						loops[i].RemoveAt (nextIndex);
						loops[i].RemoveAt (thisIndex);
						thisIndex -= 1;
					}
					else{
						loops[i].RemoveAt (thisIndex);
						loops[i].RemoveAt (nextIndex);
						thisIndex -= 2;
					}
					// If we are less than zero, hen loop round
					if (thisIndex < 0) thisIndex += loops[i].Count;
					
					// If still less than zero, then we have emptied our loop
					if (thisIndex > 0) {
						nextIndex = (thisIndex + 1) % loops[i].Count;
					
						// It is a dead end if two adjacent elements are opposits
						haveDeadEnd = (loops[i][thisIndex].Equals (GetOppositeAddress(loops[i][nextIndex])));
					}
					else{
						haveDeadEnd = false;
					}

				}					
			}
		}
		
	}
	
	void SolveForCurrents(){
	/*
		// Test the equation solver (using example from : http://www.electronics-tutorials.ws/dccircuits/dcp_5.html)
		double [,] A = new double[2,2];
		double [,] B = new double[2,1];
		A[0,0] = 50;
		A[0,1] = -40;
		A[1,0] = -40;
		A[1,1] = 60;
		B[0,0] = 10;
		B[1,0] = -20;
		
		double[,] X = MathUtils.Matrix.SolveLinear(A, B);
		*/
		
		// Go through the loops and remove any which are outer loops or of zero length o
		for (int i = loops.Count-1; i >=0 ; --i){
			if (loops[i].Count == 0 || GetBranchData(loops[i][0]).loopId < 0) loops.RemoveAt (i);

		}
		
		// Now renumber our loops
		for (int i = 0; i < loops.Count; ++i){
			for (int j= 0; j < loops[i].Count; ++j){
				GetBranchData(loops[i][j]).loopId = i;
			}
		}

		// Create arrays need to solve equation coeffs
		double [,] R = new double[loops.Count, loops.Count];
		double [,] V = new double[loops.Count, 1];
		
		// For through each loop in tune (for each row in the matrices)
		for (int i = 0; i < loops.Count; ++i){
			// For each connection in the loop, check the resistance and any voltage drop
			// We do this by considering the properties of the node we are leaving
			for (int j = 0; j < loops[i].Count; ++j){
				// This connection
				BranchAddress thisAddress = loops[i][j];
				CircuitElement thisElement = circuit.GetElement (new GridPoint(thisAddress.x, thisAddress.y));
				SimData thisData = GetBranchData (thisAddress);

								// Our data loopID shoudl be the same as i
				if (thisData.loopId != i) Debug.LogError ("LoopId/i missmatch!@");
				
				// Get current ID for current travelling in the opposite direction
				BranchAddress oppAddress = GetOppositeAddress(thisAddress);
				SimData oppData	= GetBranchData (oppAddress);
					
				
				R[i, thisData.loopId] += thisElement.GetResistance(thisAddress.dir);
				if (oppData.loopId >= 0) R[i, oppData.loopId] -= thisElement.GetResistance(thisAddress.dir);
				V[i, 0] += thisElement.GetVoltageDrop(thisAddress.dir);
			}
		}  

		// Currents
		double[,] I = new double[0,0];
		
		// for some reason the solver doens't work with one equation
		if (loops.Count == 1){
			I = new double[1,1];
			I[0,0] = (V[0,0] / R[0,0]);
		}
		else if (loops.Count > 0){
			if (MathUtils.Matrix.Rank(R) == loops.Count){
				I = MathUtils.Matrix.SolveLinear(R, V);
			}
			else{
				I = new double[loops.Count,1];
				for (int i = 0; i < loops.Count; ++i){
					I[i,0] = float.NaN;
				}
			}
		}
		
		loopCurrents = new float[loops.Count];
		if (I.GetLength(0) != 0){
			for (int i = 0; i < loops.Count; ++i){
				loopCurrents[i] = (float)I[i,0];
			}
		}
		
	}
		
	void Simulate(){
		// Find all the loops
		FindLoops();
		// Flag the ones going round the outside of disjoint circuits as 0 (so we can ignore them)
		FlagOuterLoops();
		// Go through each loop and remove any elements which are from a "Spoke" - i.e., not a loop
		TrimSpokes();
		// Set up equations and solve them
		SolveForCurrents();
		
	}
	
	void ClearSimulation(){
		loops = new List<List<BranchAddress>>();
		simData = new SimData[width, height, 4];
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				for (int i = 0; i < 4; ++i){
					simData[x,y,i] = new SimData();
				}
			}
		}
		loopCurrents = new float[0];
		ClearLoopCurrentVis();		
		ClearCurrentVis();
	}
	
	// Not really unique, but does an OK job
	Color GetUniqueColor(int index){
		// don't want to use black
		index++;
		int red = (index * 184) % 256;
		int green = (index * 123) % 256;
		int blue = (index * 22) % 256;
		return new Color(red/256f, green/256f, blue/256f);
	}
	
	void DebugDrawArrow(Vector3 from, Vector3 to, Color col){
		float arrowSize	 = 0.2f;
		Vector3 perpVec = 0.5f * Vector3.Cross(to - from, new Vector3(0f, 0f, 1));
		Vector3 headEnd0 = to + (from - to + perpVec )* arrowSize;
		Vector3 headEnd1 = to + (from - to - perpVec) * arrowSize;

		// Main shaft of arrow		
		Debug.DrawLine(from, to, col, 0f, false);
		
		// Head
		Debug.DrawLine(to, headEnd0, col, 0f, false);
		Debug.DrawLine(to, headEnd1, col, 0f, false);
		
	} 
	
	void DebugRenderLoops(){
		float offsetSize = 0.2f;
		Vector3[] offsets = new Vector3[4];
		offsets[Circuit.kUp] = new Vector3(-offsetSize, 0f, 0f);
		offsets[Circuit.kRight] = new Vector3(0f, offsetSize, 0f);
		offsets[Circuit.kDown] = new Vector3(offsetSize, 0f, 0f);
		offsets[Circuit.kLeft] = new Vector3(0f, -offsetSize, 0f);
		
		float lengthScale = 0.5f;
		
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				for (int i = 0; i < 4; ++i){
					BranchAddress thisAddress = new BranchAddress(x, y, i);
					BranchAddress nextAddress = GetOppositeAddress(thisAddress);
					
					SimData data = GetBranchData (thisAddress);
					if (data.traversed && data.loopId >= 0){
						Vector3 from = new Vector3(x, y, 0f) + offsets[i];
						Vector3 to = new Vector3(nextAddress.x, nextAddress.y, 0f) + offsets[i];
						Vector3 avPos = (from + to) * 0.5f;
						from = avPos + (from - avPos) * lengthScale;
						to = avPos + (to - avPos) * lengthScale;
						Color col = GetUniqueColor ((visMode == VisMode.kGroups) ? data.groupId : data.loopId);
						
						// If our currents are negative, draw the arrrows the other way round
						if (loopCurrents[data.loopId] > 0f){
							DebugDrawArrow(from, to, col);
						}
						else{
							DebugDrawArrow(to, from, col);
						}
					}
				}
			}
		}
	}
	
	void DebugRenderCurrents(){
		float offsetSize = 0.0f;
		Vector3[] offsets = new Vector3[4];
		offsets[Circuit.kUp] = new Vector3(-offsetSize, 0f, 0f);
		offsets[Circuit.kRight] = new Vector3(0f, offsetSize, 0f);
		offsets[Circuit.kDown] = new Vector3(offsetSize, 0f, 0f);
		offsets[Circuit.kLeft] = new Vector3(0f, -offsetSize, 0f);
		
		float lengthScale = 0.5f;
		
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				for (int i = 0; i < 4; ++i){
					BranchAddress thisAddress = new BranchAddress(x, y, i);
					BranchAddress oppAddress = GetOppositeAddress(thisAddress);
					
					SimData thisData = GetBranchData (thisAddress);
					if (thisData.traversed && thisData.loopId >= 0){
						SimData oppData = GetBranchData (oppAddress);
						Vector3 from = new Vector3(x, y, 0f) + offsets[i];
						Vector3 to = new Vector3(oppAddress.x, oppAddress.y, 0f) + offsets[i];
						Vector3 avPos = (from + to) * 0.5f;
						from = avPos + (from - avPos) * lengthScale;
						to = avPos + (to - avPos) * lengthScale;
						
						// If our currents are negative, draw the arrrows the other way round
						float current = loopCurrents[thisData.loopId];
						if (oppData.loopId >=0) current -= loopCurrents[oppData.loopId];
						if (current > 0f){
							DebugDrawArrow(from, to, new Color(0.75f, 0.75f, 1f));
						}
						else{
							DebugDrawArrow(to, from,  new Color(0.75f, 0.75f, 1f));
						}
					}
				}
			}
		}
	}	
	
	bool IsPointInGrid(int x, int y){
		return 	
			x > 0 &&
			y > 0 &&
			x < width &&
			y < height;		
	}	

	
	// Go through each of the squares and find edges which have a loop current in them
	// NOte that in our current setup, a single square only descrbes a single loop current
	void DebugRenderLoopCurrentVis(){
		BranchAddress[] squareAddresses = new BranchAddress[4];
		// The coodinates of a square are the coordinates of the bottom right corner
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
			
				// Work out the address of the edges of our square
				squareAddresses[Circuit.kRight] = new BranchAddress(x, y, Circuit.kRight);
				squareAddresses[Circuit.kUp] = new BranchAddress(x + 1, y, Circuit.kUp);
				squareAddresses[Circuit.kLeft] = new BranchAddress(x + 1, y + 1, Circuit.kLeft);
				squareAddresses[Circuit.kDown] = new BranchAddress(x, y + 1, Circuit.kDown);
				
				// For each brnach address, see if it is valid, tranvered and what loopID it is
				int loopId = -1;
				int groupId = -1;;
				for (int i = 0; i < 4; ++i){
					if (IsPointInGrid(squareAddresses[i].x, squareAddresses[i].y)){
						SimData data = GetBranchData(squareAddresses[i]);
						if (data.traversed && data.loopId >= 0){
							if (loopId != -1 && loopId != data.loopId) Debug.LogError("Inconsistnet loop Ids");
							loopId = data.loopId;
							groupId = data.groupId;
						}
						
					}
				}
				if (loopId != -1){
					debugTextBoxes[x,y].SetActive(true);
					TextMesh textMesh = debugTextBoxes[x,y].GetComponent<TextMesh>();
					textMesh.color = GetUniqueColor((visMode == VisMode.kLoops) ? loopId : groupId);
					textMesh.text = Mathf.Abs (loopCurrents[loopId])	.ToString("0.00");
				}
				else{
					debugTextBoxes[x,y].SetActive(false);
				}
				
			}
		}
	}
	
	// Go through each of the squares and find edges which have a loop current in them
	// NOte that in our current setup, a single square only descrbes a single loop current
	void DebugRenderCurrentVis(){
		// The coodinates of a square are the coordinates of the bottom right corner
		for (int x = 0; x < width-1; ++x){
			for (int y = 0; y < height-1; ++y){
			
				/*
				if (GetBranchData (new BranchAddress(x, y, 0)).traversed){
					Debug.DrawLine(new Vector3(0f, 0f, 0f), new Vector3(x, y, 0f), Color.red);
				}
				if (GetBranchData (new BranchAddress(x, y, 1)).traversed){
					Debug.DrawLine(new Vector3(1f, 0f, 0f), new Vector3(x, y, 0f), Color.green);
				}
				if (GetBranchData (new BranchAddress(x, y, 2)).traversed){
					Debug.DrawLine(new Vector3(2f, 0f, 0f), new Vector3(x, y, 0f), Color.blue);
				}
				if (GetBranchData (new BranchAddress(x, y, 3)).traversed){
					Debug.DrawLine(new Vector3(3f, 0f, 0f), new Vector3(x, y, 0f), Color.yellow);
				}
				*/
				
				
				// Do branch going right
				float rightCurrent = 0f;
				BranchAddress rightAddr = new BranchAddress(x, y, Circuit.kRight);
				BranchAddress rightAddrInv = GetOppositeAddress(rightAddr);
				SimData rightData = GetBranchData (rightAddr);
				SimData rightInvData = GetBranchData (rightAddrInv);
				
				if (rightData.traversed && rightData.loopId >= 0){
					rightCurrent += loopCurrents[GetBranchData (rightAddr).loopId];
				}
				if (rightInvData.traversed && rightInvData.loopId >= 0){
					rightCurrent -= loopCurrents[GetBranchData (rightAddrInv).loopId];
				}
				
				if (!MathUtils.FP.Feq(rightCurrent, 0f)){
					debugCurrentTextBoxesRight[x,y].SetActive(true);
					TextMesh textMesh = debugCurrentTextBoxesRight[x,y].GetComponent<TextMesh>();
					textMesh.color = Color.white;
					textMesh.text = Mathf.Abs (rightCurrent).ToString("0.00");
				}
				else{
					debugCurrentTextBoxesUp[x,y].SetActive(false);
				}

				
				// Do branch going up
					
				float upCurrent = 0f;
				BranchAddress upAddr = new BranchAddress(x, y, Circuit.kUp);
				BranchAddress upAddrInv = GetOppositeAddress(upAddr);
				SimData upData = GetBranchData (upAddr);
				SimData upInvData = GetBranchData (upAddrInv);
				
				
				if (upData.traversed && upData.loopId >= 0){
					upCurrent += loopCurrents[GetBranchData (upAddr).loopId];
				}
				if (upInvData.traversed && upInvData.loopId >= 0){
					upCurrent -= loopCurrents[GetBranchData (upAddrInv).loopId];
				}
				
				if (!MathUtils.FP.Feq(upCurrent, 0f)){
					debugCurrentTextBoxesUp[x,y].SetActive(true);
					TextMesh textMesh = debugCurrentTextBoxesUp[x,y].GetComponent<TextMesh>();
					textMesh.color = Color.white;
					textMesh.text = Mathf.Abs (upCurrent).ToString("0.00");
				}
				else{
					debugCurrentTextBoxesUp[x,y].SetActive(false);
				}
				

				
			}
		}
	}	
	
	void DebugCreateCurrentVis(){
		debugCurrentTextBoxesRight = new GameObject[width, height];
		debugCurrentTextBoxesUp = new GameObject[width, height];
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				debugCurrentTextBoxesRight[x,y] = Instantiate (textMeshGO, new Vector3(x + 0.5f, y, 0f), Quaternion.identity) as GameObject;
				debugCurrentTextBoxesRight[x,y].transform.parent = transform;
				debugCurrentTextBoxesRight[x,y].SetActive(false);

				debugCurrentTextBoxesUp[x,y] = Instantiate (textMeshGO, new Vector3(x, y + 0.5f, 0f), Quaternion.identity) as GameObject;
				debugCurrentTextBoxesUp[x,y].transform.parent = transform;
				debugCurrentTextBoxesUp[x,y].SetActive(false);
				
			}		
		}
	}
	
	void DebugCreateLoopCurrentVis(){
		debugTextBoxes = new GameObject[width, height];
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				debugTextBoxes[x,y] = Instantiate (textMeshGO, new Vector3(x + 0.5f, y + 0.5f, 0f), Quaternion.identity) as GameObject;
				debugTextBoxes[x,y].transform.parent = transform;
				debugTextBoxes[x,y].SetActive(false);
			}		
		}
	}
	
	void ClearLoopCurrentVis(){
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				debugTextBoxes[x,y].SetActive(false);
			}		
		}
	}	
	
	void ClearCurrentVis(){
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				debugCurrentTextBoxesRight[x,y].SetActive(false);
				debugCurrentTextBoxesUp[x,y].SetActive(false);
			}		
		}
		/* - test
		debugCurrentTextBoxesRight[10, 10].SetActive(true);
		TextMesh textMeshRight = debugCurrentTextBoxesRight[10, 10].GetComponent<TextMesh>();
		textMeshRight.color = Color.white;
		textMeshRight.text = "Test-Right";

		debugCurrentTextBoxesUp[10, 10].SetActive(true);
		TextMesh textMeshUp = debugCurrentTextBoxesUp[10, 10].GetComponent<TextMesh>();
		textMeshUp.color = Color.white;
		textMeshUp.text = "Test-Up";
		*/ 
	}	

		// Update is called once per frame
	void FixedUpdate () {
	

		ClearSimulation();
		Simulate();
		
		if (visMode == VisMode.kGroups || visMode == VisMode.kLoops){
			DebugRenderLoops();	
			DebugRenderLoopCurrentVis();
		}
		else if (visMode == VisMode.kCurrents){
			DebugRenderCurrents();	
			DebugRenderCurrentVis();
		}

	
	}
	
	
	void OnGUI () {
		guiTextDisplay.GUIResetTextLayout();
		guiTextDisplay.GUIPrintText( "Number of disjoint circuits: " + numGroups, Color.white);
		guiTextDisplay.GUIPrintText( "Number of loops: " + loops.Count, Color.white);
		
	}	
}
