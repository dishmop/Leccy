using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class Simulator : MonoBehaviour {

	public static Simulator singleton = null;
	
	public GameObject 	textMeshGO;
	
	public enum VisMode{
		kNone,
		kLoops,
		kGroups,
		kCurrents
	};
	public VisMode 	visMode = 		VisMode.kNone;
	public enum MinMode{
		kLoops,
		kBranches,
		kElements,
		kElementsOpt
	};
	public MinMode minMode = MinMode.kLoops;
	
	public Color[]	voltageColors = new Color[7];
	public float 	maxVoltVis = 		50f;
	public float 	currentMulVis = 	0.1f;
	public bool 	enableVoltsgeAsHeight = true;

	public bool solveVoltges = false;
	

	
	double epsilon = 0.0001;
	
	
	
	GameObject[,]		debugTextBoxes;
	GameObject[,]       debugCurrentTextBoxesRight;
	GameObject[,]       debugCurrentTextBoxesUp;
	GameObject[,]		debugVoltageTextBoxes;
	
	int					width;
	int					height;
	
	
	BranchData[,,]				branchData;

	
	// List lof loops (the first loop is always the outer one which we can ignore
	// The remainder will form an indepednent set of loops
	List<List<BranchAddress>>	loops;	
	int							numValidLoops;
//	int							numGroups;
	float[]						loopCurrents;
	
	// The order in which we should traverse the loops in order to calculate the voltages
	int[]						voltageLoopOrder;
	
	// GUI layout
//	GUITextDisplay				guiTextDisplay;     
	
	// Used for calculating currents. There are two of these for each connection between noses
	// Each object stores info about the current LEAVING the node (so there is always an equivilent
	// (opposite) branch which is the one entering the node
	class BranchData{
		// Current calculations
		public bool traversed;
		public int loopId;
		public bool isOuterLoop;
		public bool isSpoke;
		public int groupId;
		public float totalCurrent;
		
		// Voltage calculations
		public bool initialVolt;
		public float totalVoltage;
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
	
	void Awake(){
		if (singleton != null) Debug.LogError ("Error assigning singleton");
		singleton = this;
	}
	
	void OnDestroy(){
		singleton = null;
	}

	// Use this for initialization
	void Start () {
		BuildArrays ();		
		DebugCreateLoopCurrentVis();
		DebugCreateCurrentVis();
		DebugCreateVoltageVis ();
		ClearSimulation ();
		
		// Sert up the text gui
//		guiTextDisplay = new GUITextDisplay(400f, 70f, 500f, 20f);
	}
	
	public float GetVoltage(int x, int y, int dir){
		return GetBranchData(new BranchAddress(x, y, dir)).totalVoltage;
	}

	public float GetCurrent(int x, int y, int dir){
		return GetBranchData(new BranchAddress(x, y, dir)).totalCurrent;
	}
	
	public bool IsTraversed(int x, int y, int dir){
		return GetBranchData(new BranchAddress(x, y, dir)).traversed;
	}
	
	BranchData GetBranchData(BranchAddress address){
		if (address.x < 0 || address.x > branchData.GetLength(0) ||
		    address.y < 0 || address.y > branchData.GetLength(1) ||
		    address.dir < 0 || address.dir > branchData.GetLength(2)){
			Debug.LogError ("Requesting out of range branch data");    
		}
		return branchData[address.x, address.y, address.dir];
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
		CircuitElement nextElement = Circuit.singleton.GetElement(point);
		
		// If we are an insulator, then we always just double back on ourselves (which is always the last option we pass in)
		if (nextElement.IsInsulator()){
			return new BranchAddress(point.x, point.y, choices[3]);
		}
		for (int i = 0;  i < 4; ++i){
			if (nextElement.IsConnected(choices[i])) return new BranchAddress(point.x, point.y, choices[i]);
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

		BranchData data = GetBranchData (addr);
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
	// It does it in a slightly odd way because it needs to also group the loops by disjoint circuit
	bool FindLoops(){
		if (!Circuit.singleton.Validate()){
			Debug.Log("Circuit is invalid and cannot be simulated");
			return false;
		}
		int groupId = 0;		
		// Can the grid until we find a connector which has not yet  been traversed
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				// If no element at this node, then no connections to it
				if (Circuit.singleton.elements[x,y]){
					// Go through each of the branches from this node and only consider data on them if we have
					// a connection along that branch
					CircuitElement thisElement = Circuit.singleton.GetElement(new GridPoint(x, y));
					for (int i = 0; i < 4; ++i){
						if (thisElement.IsConnected(i)){
							BranchAddress thisAddress = new BranchAddress(x, y, i);
							if (!GetBranchData(thisAddress).traversed){
								// Yes! we have found one
								
								
								// Create the outer loop
								int loopId = CreateLoop();								
								FollowLoopLeft(thisAddress, loopId, groupId);
								
								// Now start at the begginning of our outer loop and traverse it 
								// Looking for conections that have not yet been traversed. If we find one, follow that loop
								int searchloop = loopId;
								while (searchloop <= loopId){
									for  (int searchIndex = 0; searchIndex < loops[searchloop].Count; ++searchIndex){
										GridPoint thisPoint2 = new GridPoint(loops[searchloop][searchIndex].x, loops[searchloop][searchIndex].y);
										CircuitElement thisElement2 = Circuit.singleton.GetElement(thisPoint2);
										for (int j = 0; j < 4; ++j){
											if (thisElement2.IsConnected(j)){
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
//		numGroups = groupId;
		
		
		return true;
	}
	
	// ALl loops that simply tracing the outline of a disjoint Circuit.singleton 
	// are not needed for current calulations so are iflagged ot be ignored
	void FlagOuterLoops(){
		int lastGroupId = -1;
		for (int i = 0; i < loops.Count; ++i)
		{
			int groupId = GetBranchData(loops[i][0]).groupId;
			if (groupId != lastGroupId){
				lastGroupId = groupId;
				for (int j = 0; j < loops[i].Count; ++j){
					GetBranchData(loops[i][j]).isOuterLoop = true;
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
					GetBranchData (loops[i][thisIndex]).isSpoke = true;
					GetBranchData (loops[i][nextIndex]).isSpoke = true;

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
	
	double[,] 	CalcPseudoInverse(double[,] M){
		// Method taken from here: http://en.wikipedia.org/wiki/Moore%E2%80%93Penrose_pseudoinverse#The_iterative_method_of_Ben-Israel_and_Cohen
		// search for "A computationally simple and accurate way to compute the pseudo inverse " ont his page
		
		
		// First we need to calc the SVD of the matrix
		double[] W = null;	// S matrix as a vector (leading diagonal)
		double[,] U = null;
		double[,] Vt = null;
		alglib.svd.rmatrixsvd(M, M.GetLength (0), M.GetLength (1), 2, 2, 2, ref W, ref U, ref Vt);
		
		double[,] S = new double[M.GetLength (0), M.GetLength (1)];
		
		for (int i = 0; i < W.GetLength (0); ++i){
			S[i,i] = W[i];
		}
		//				MathUtils.Matrix.SVD(R, out S, out U, out Vt);
		// Log the results
		
		//		double[,] testR0 = MathUtils.Matrix.Multiply(U, S);
		//		double[,] testR1 = MathUtils.Matrix.Multiply(testR0, Vt);
		
		double[,] Ut = MathUtils.Matrix.Transpose(U);
		double[,] Vtt = MathUtils.Matrix.Transpose (Vt);
		
		// Get the psuedo inverse of the U matrix (which is diagonal)
		// The way we do this is by taking the recipricol of each diagonal element, leaving the (close to) zero's in place
		// and transpose (actually we don't need to transpose because we always have square matricies)
		
		// I assume this gets initialised with zeros (note we are making the transpose)
		double[,] SInv = new double[S.GetLength(1), S.GetLength(0)];
		
		double epsilon = 0.000001;
		for (int i = 0; i < W.GetLength(0); ++i){
			if (Math.Abs (S[i,i]) > epsilon){
				SInv[i,i] = 1.0 / S[i,i];
				
			}					
		}
		
		//Rinv = Vtt Uinv St
		double[,] RInvTemp = MathUtils.Matrix.Multiply (Vtt, SInv);
		return MathUtils.Matrix.Multiply (RInvTemp, Ut);
		
	}
	
	bool SolveForCurrentsOptElements(){
		
		int numInvalidLoops = 0;
		// Go through the loops and remove any which are outer loops or of zero length or an invlaid loop
		// e.g. an outer loop and place them at the end
		for (int i = loops.Count-1; i >=0 ; --i){
			if (loops[i].Count == 0 || GetBranchData(loops[i][0]).isOuterLoop){
				int newIndex = CreateLoop ();
				loops[newIndex] = loops[i];
				loops.RemoveAt (i);
				
				++numInvalidLoops;
			}
		}
		
		// It is useful rearrange the loops (see above), for the calculation of currents.
		// However, for the purposes of voltage analysis, it is better to have then in their original aorder
		// (as we can guarantee that the starting point of a loop starts connected to a loop we have already traverse
		// unless we are the first loop in a group.
		// This array is a way to store that original order  - we can calcukate it based on the fact that the loop Ids
		// have not yet neen recalcualted
		
		voltageLoopOrder = new int[loops.Count];
		for (int i = 0; i < loops.Count; ++i){
			for (int j = 0; j < loops.Count; ++j){
				if (GetBranchData(loops[j][0]).loopId == i){
					voltageLoopOrder[i] = j;
				}
			}
			
		}		
		
		
		
		
		numValidLoops = loops.Count - numInvalidLoops;
		
		// Now renumber our loops (up to the valid ones  the invalid ones should retain an ID of -1 or whatever)
		for (int i = 0; i < loops.Count; ++i){
			for (int j= 0; j < loops[i].Count; ++j){
				GetBranchData(loops[i][j]).loopId = i;
			}
		}
		
		
		// Create arrays need to solve equation coeffs
		double [,] R = new double[numValidLoops, numValidLoops];
		double [,] V = new double[numValidLoops, 1];
		
		//Dictionary<Eppy.Tuple<int, int>, bool> loopCombinations = new Dictionary<Eppy.Tuple<int, int>, bool>();
		List<Eppy.Tuple<int, int>> loopCombinations = new List<Eppy.Tuple<int, int>>();
		
		// run through each loop in ruen (for each row in the matrices)
		for (int i = 0; i < numValidLoops; ++i){
			// For each connection in the loop, check the resistance and any voltage drop
			// We do this by considering the properties of the node we are leaving and each one that we arrive at
			for (int j = 0; j < loops[i].Count; ++j){
				// This connection
				BranchAddress thisAddress = loops[i][j];
				CircuitElement thisElement = Circuit.singleton.GetElement (new GridPoint(thisAddress.x, thisAddress.y));
				BranchData thisData = GetBranchData (thisAddress);
				
				// Our data loopID should be the same as i
				if (thisData.loopId != i) Debug.LogError ("LoopId/i missmatch!@");
				
				// Get current ID for current travelling in the opposite direction
				BranchAddress oppAddress = GetOppositeAddress(thisAddress);
				BranchData oppData	= GetBranchData (oppAddress);
				CircuitElement oppElement = Circuit.singleton.GetElement(new GridPoint(oppAddress.x, oppAddress.y));
				
				// Work out the resistance in this branch
				float resistance = thisElement.GetResistance(thisAddress.dir) + oppElement.GetResistance(oppAddress.dir);
				
				
				// We build up the resistance matrix 
				R[i, thisData.loopId] += resistance;
				if (!oppData.isOuterLoop){
					R[i, oppData.loopId] -= resistance;
				}
				V[i, 0] += thisElement.GetVoltageDrop(thisAddress.dir, true);
				V[i, 0] += oppElement.GetVoltageDrop(oppAddress.dir, false);

			}
		}  
		
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				GridPoint thisPoint = new GridPoint(x,y);
				if (Circuit.singleton.ElementExists(thisPoint)){
					// Only check two of the directions (so we only check each connection once
					for (int dir = 0; dir < 2; ++dir){
						if (Circuit.singleton.GetElement(thisPoint).IsConnected(dir)){
							BranchAddress thisAddress = new BranchAddress(x, y, dir);
							BranchAddress oppAddress = GetOppositeAddress(thisAddress);
							BranchData thisData = GetBranchData(thisAddress);
							BranchData oppData = GetBranchData(oppAddress);
							
							int val0 = thisData.isOuterLoop ? -1 : thisData.loopId;
							int val1 = oppData.isOuterLoop ? -1 : oppData.loopId;
							
							if (val0 != -1 || val1 != -1){
								loopCombinations.Add (new Eppy.Tuple<int, int>(val0, val1));
							}
						}
						
					}
				}
			}
		}
		
		// Create "Loop Matrix" which maps loop currents onto the currents on the branches of our graph
		double [,] L = new double[loopCombinations.Count, numValidLoops];
		
		
		// Dictionary version
		//		int k = 0;
		//		foreach (KeyValuePair<Eppy.Tuple<int, int>, bool> entry in loopCombinations){
		//			if (entry.Key.Item1 != -1) L[k, entry.Key.Item1] = 1;
		//			if (entry.Key.Item2 != -1) L[k, entry.Key.Item2] = -1;
		//			++k;
		//		}
		
		int k = 0;
		foreach (Eppy.Tuple<int, int> entry in loopCombinations){
			if (entry.Item1 != -1) L[k, entry.Item1] = 1;
			if (entry.Item2 != -1) L[k, entry.Item2] = -1;
			++k;
		}
		
		// If we just wanted to calculate A solution for the loops, we would write:
		// Since we may not have full rank,, we find a solution using the Moore-Pensrose Pseudo-inverse
		//		double[,] RInv = CalcPseudoInverse(R);
		//		I = MathUtils.Matrix.Multiply(RInv, V); 
		
		// However, this doesn't distribute the current well over the different branches of the Circuit.singleton if there is
		// no reisstance in it
		// Instead, we calculate the crrents for each brnach (using a pseudo inverse) and then reverse calculate
		// what the loops should be (in the final version we should just lose the whole Loop business)
		
		// Find the pseudo inverse of this matrix
		double[,] LInv = CalcPseudoInverse (L);
		
		// Suppose C are the individual currents in our mesh, then
		// V = R x LInv x C
		
		// So, find R X LInv, and then find the Pseudo inverse of thast
		double[,] comb = MathUtils.Matrix.Multiply(R, LInv);
		double[,] combInv = CalcPseudoInverse(comb);
		double[,] C = MathUtils.Matrix.Multiply(combInv, V);
		
		// Currents
		double[,] I = MathUtils.Matrix.Multiply (LInv, C);
		
		
		// Check that we get V - if not we have an unsolvable set of equations and 
		// it means we have a loop of zero resistance with a voltage different in it
		double[,] testV = MathUtils.Matrix.Multiply(R, I);
		
		bool failed = false;
		for (int i = 0; i < numValidLoops; ++i){
			// f we find a bad loop
			if (Math.Abs (V[i, 0] - testV[i, 0]) > epsilon){
				// Then follow this loop finding all the voltage sources and put them in Emergency mode
				for (int j = 0; j < loops[i].Count; ++j){
					BranchAddress thisAddr = loops[i][j];
					CircuitElement thisElement = Circuit.singleton.GetElement(new GridPoint(thisAddr.x, thisAddr.y));
					if (Mathf.Abs (thisElement.GetVoltageDrop(thisAddr.dir, true)) > epsilon){
						thisElement.TriggerEmergency();
						failed = true;
					}
					BranchAddress oppAddr = GetOppositeAddress(thisAddr);
					CircuitElement oppElement = Circuit.singleton.GetElement(new GridPoint(oppAddr.x, oppAddr.y));
					if (Mathf.Abs (oppElement.GetVoltageDrop(oppAddr.dir, false)) > epsilon){
						oppElement.TriggerEmergency();
						failed = true;
					}				
				}
			}
		}
		
		//		Debug.Log ("L = " + L.GetLength(0) + " X " + L.GetLength (1));
		//		Debug.Log ("I = " + I.GetLength(0) + " X " + I.GetLength (1));
		//		
		//		
		//		double[,] currentTest = MathUtils.Matrix.Multiply (L, I);
		//		
		//		double[,] testLoops = MathUtils.Matrix.Multiply (LInv, C);
		//		double[,] doubleCheck1 = MathUtils.Matrix.Multiply (L, I);
		//		double[,] doubleCheck2 = MathUtils.Matrix.Multiply (L, testLoops);
		//		
		if (failed) return false;
		
		
		
		
		
		loopCurrents = new float[loops.Count];
		if (I.GetLength(0) != 0){
			for (int i = 0; i < numValidLoops; ++i){
				loopCurrents[i] = (float)I[i,0];
			}
			// For the invalid loops, just set the current to zero
			for (int i = numValidLoops; i < loops.Count; ++i){
				loopCurrents[i] = 0f;
			}
			
		}
		// all went well
		return true;
		
	}
	
	bool SolveForCurrentsOptBranches(){
		
		int numInvalidLoops = 0;
		// Go through the loops and remove any which are outer loops or of zero length or an invlaid loop
		// e.g. an outer loop and place them at the end
		for (int i = loops.Count-1; i >=0 ; --i){
			if (loops[i].Count == 0 || GetBranchData(loops[i][0]).isOuterLoop){
				int newIndex = CreateLoop ();
				loops[newIndex] = loops[i];
				loops.RemoveAt (i);
				
				++numInvalidLoops;
			}
		}
		
		// It is useful rearrange the loops (see above), for the calculation of currents.
		// However, for the purposes of voltage analysis, it is better to have then in their original aorder
		// (as we can guarantee that the starting point of a loop starts connected to a loop we have already traverse
		// unless we are the first loop in a group.
		// This array is a way to store that original order  - we can calcukate it based on the fact that the loop Ids
		// have not yet neen recalcualted
		
		voltageLoopOrder = new int[loops.Count];
		for (int i = 0; i < loops.Count; ++i){
			for (int j = 0; j < loops.Count; ++j){
				if (GetBranchData(loops[j][0]).loopId == i){
					voltageLoopOrder[i] = j;
				}
			}
			
		}		
		
		
		
		
		numValidLoops = loops.Count - numInvalidLoops;
		
		// Now renumber our loops (up to the valid ones  the invalid ones should retain an ID of -1 or whatever)
		for (int i = 0; i < loops.Count; ++i){
			for (int j= 0; j < loops[i].Count; ++j){
				GetBranchData(loops[i][j]).loopId = i;
			}
		}
		
		
		// Create arrays need to solve equation coeffs
		double [,] R = new double[numValidLoops, numValidLoops];
		double [,] V = new double[numValidLoops, 1];
		
		Dictionary<Eppy.Tuple<int, int>, bool> loopCombinations = new Dictionary<Eppy.Tuple<int, int>, bool>();
		//List<Eppy.Tuple<int, int>> loopCombinations = new List<Eppy.Tuple<int, int>>();
		
		// run through each loop in ruen (for each row in the matrices)
		for (int i = 0; i < numValidLoops; ++i){
			// For each connection in the loop, check the resistance and any voltage drop
			// We do this by considering the properties of the node we are leaving
			for (int j = 0; j < loops[i].Count; ++j){
				// This connection
				BranchAddress thisAddress = loops[i][j];
				CircuitElement thisElement = Circuit.singleton.GetElement (new GridPoint(thisAddress.x, thisAddress.y));
				BranchData thisData = GetBranchData (thisAddress);
				
				// Our data loopID should be the same as i
				if (thisData.loopId != i) Debug.LogError ("LoopId/i missmatch!@");
				
				// Get current ID for current travelling in the opposite direction
				BranchAddress oppAddress = GetOppositeAddress(thisAddress);
				BranchData oppData	= GetBranchData (oppAddress);
				CircuitElement oppElement = Circuit.singleton.GetElement(new GridPoint(oppAddress.x, oppAddress.y));
				
				
				// Work out the resistance in this branch
				float resistance = thisElement.GetResistance(thisAddress.dir) + oppElement.GetResistance(oppAddress.dir);
				
				
				// We build up the resistance matrix 
				R[i, thisData.loopId] += resistance;
				if (!oppData.isOuterLoop){
					R[i, oppData.loopId] -= resistance;
				}
				
				V[i, 0] += thisElement.GetVoltageDrop(thisAddress.dir, true);
				V[i, 0] += oppElement.GetVoltageDrop(oppAddress.dir, false);
				
				// Use for dictionary version  (for non dictionary version we don;t get them from this loop)
				
				// Also build a list of unique combinations of loops which corresponds to wires which 
				// sit on those loops as we want to spread the current out euqally amonst these combinations
				int val0 = thisData.isOuterLoop ? -1 : thisData.loopId;
				int val1 = oppData.isOuterLoop ? -1 : oppData.loopId;
				
				if (val0 > val1){
					int temp = val0;
					val0 = val1;
					val1 = temp;
				}
				
				
				
				Eppy.Tuple<int, int> key = new Eppy.Tuple<int, int>(val0, val1);
				
				
				if (!loopCombinations.ContainsKey(key)){
					loopCombinations.Add (key, true);
				}
				
			}
		}  
		

		// Create "Loop Matrix" which maps loop currents onto the currents on the branches of our graph
		double [,] L = new double[loopCombinations.Count, numValidLoops];
		
		
		// Dictionary version
		//		int k = 0;
		//		foreach (KeyValuePair<Eppy.Tuple<int, int>, bool> entry in loopCombinations){
		//			if (entry.Key.Item1 != -1) L[k, entry.Key.Item1] = 1;
		//			if (entry.Key.Item2 != -1) L[k, entry.Key.Item2] = -1;
		//			++k;
		//		}
		
		int k = 0;
		foreach (KeyValuePair<Eppy.Tuple<int, int>, bool> entry in loopCombinations){
			if (entry.Key.Item1 != -1) L[k, entry.Key.Item1] = 1;
			if (entry.Key.Item2 != -1) L[k, entry.Key.Item2] = -1;
			++k;
		}
		
		// If we just wanted to calculate A solution for the loops, we would write:
		// Since we may not have full rank,, we find a solution using the Moore-Pensrose Pseudo-inverse
		//		double[,] RInv = CalcPseudoInverse(R);
		//		I = MathUtils.Matrix.Multiply(RInv, V); 
		
		// However, this doesn't distribute the current well over the different branches of the Circuit.singleton if there is
		// no reisstance in it
		// Instead, we calculate the crrents for each brnach (using a pseudo inverse) and then reverse calculate
		// what the loops should be (in the final version we should just lose the whole Loop business)
		
		// Find the pseudo inverse of this matrix
		double[,] LInv = CalcPseudoInverse (L);
		
		// Suppose C are the individual currents in our mesh, then
		// V = R x LInv x C
		
		// So, find R X LInv, and then find the Pseudo inverse of thast
		double[,] comb = MathUtils.Matrix.Multiply(R, LInv);
		double[,] combInv = CalcPseudoInverse(comb);
		double[,] C = MathUtils.Matrix.Multiply(combInv, V);
		
		// Currents
		double[,] I = MathUtils.Matrix.Multiply (LInv, C);
		
		
		// Check that we get V - if not we have an unsolvable set of equations and 
		// it means we have a loop of zero resistance with a voltage different in it
		double[,] testV = MathUtils.Matrix.Multiply(R, I);
		
		bool failed = false;
		for (int i = 0; i < numValidLoops; ++i){
			// f we find a bad loop
			if (Math.Abs (V[i, 0] - testV[i, 0]) > epsilon){
				// Then follow this loop finding all the voltage sources and put them in Emergency mode
				for (int j = 0; j < loops[i].Count; ++j){
					BranchAddress thisAddr = loops[i][j];
					CircuitElement thisElement = Circuit.singleton.GetElement(new GridPoint(thisAddr.x, thisAddr.y));
					if (Mathf.Abs (thisElement.GetVoltageDrop(thisAddr.dir, true)) > epsilon){
						thisElement.TriggerEmergency();
						failed = true;
					}
					BranchAddress oppAddr = GetOppositeAddress(thisAddr);
					CircuitElement oppElement = Circuit.singleton.GetElement(new GridPoint(oppAddr.x, oppAddr.y));
					if (Mathf.Abs (oppElement.GetVoltageDrop(oppAddr.dir, false)) > epsilon){
						oppElement.TriggerEmergency();
						failed = true;
					}
				}
			}
		}
		
		//		Debug.Log ("L = " + L.GetLength(0) + " X " + L.GetLength (1));
		//		Debug.Log ("I = " + I.GetLength(0) + " X " + I.GetLength (1));
		//		
		//		
		//		double[,] currentTest = MathUtils.Matrix.Multiply (L, I);
		//		
		//		double[,] testLoops = MathUtils.Matrix.Multiply (LInv, C);
		//		double[,] doubleCheck1 = MathUtils.Matrix.Multiply (L, I);
		//		double[,] doubleCheck2 = MathUtils.Matrix.Multiply (L, testLoops);
		//		
		if (failed) return false;
		
		
		
		
		
		loopCurrents = new float[loops.Count];
		if (I.GetLength(0) != 0){
			for (int i = 0; i < numValidLoops; ++i){
				loopCurrents[i] = (float)I[i,0];
			}
			// For the invalid loops, just set the current to zero
			for (int i = numValidLoops; i < loops.Count; ++i){
				loopCurrents[i] = 0f;
			}
			
		}
		// all went well
		return true;
		
	}
	
	
	bool SolveForCurrentsOptBranchesElements(){
		
		int numInvalidLoops = 0;
		// Go through the loops and remove any which are outer loops or of zero length or an invlaid loop
		// e.g. an outer loop and place them at the end
		for (int i = loops.Count-1; i >=0 ; --i){
			if (loops[i].Count == 0 || GetBranchData(loops[i][0]).isOuterLoop){
				int newIndex = CreateLoop ();
				loops[newIndex] = loops[i];
				loops.RemoveAt (i);
				
				++numInvalidLoops;
			}
		}
		
		// It is useful rearrange the loops (see above), for the calculation of currents.
		// However, for the purposes of voltage analysis, it is better to have then in their original aorder
		// (as we can guarantee that the starting point of a loop starts connected to a loop we have already traverse
		// unless we are the first loop in a group.
		// This array is a way to store that original order  - we can calcukate it based on the fact that the loop Ids
		// have not yet neen recalcualted
		
		voltageLoopOrder = new int[loops.Count];
		for (int i = 0; i < loops.Count; ++i){
			for (int j = 0; j < loops.Count; ++j){
				if (GetBranchData(loops[j][0]).loopId == i){
					voltageLoopOrder[i] = j;
				}
			}
			
		}		
		
		
		
		
		numValidLoops = loops.Count - numInvalidLoops;
		
		// Now renumber our loops (up to the valid ones  the invalid ones should retain an ID of -1 or whatever)
		for (int i = 0; i < loops.Count; ++i){
			for (int j= 0; j < loops[i].Count; ++j){
				GetBranchData(loops[i][j]).loopId = i;
			}
		}
		
		
		// Create arrays need to solve equation coeffs
		double [,] R = new double[numValidLoops, numValidLoops];
		double [,] V = new double[numValidLoops, 1];
		
		Dictionary<Eppy.Tuple<int, int>, float> loopCombinations = new Dictionary<Eppy.Tuple<int, int>, float>();
		//List<Eppy.Tuple<int, int>> loopCombinations = new List<Eppy.Tuple<int, int>>();
		
		// run through each loop in ruen (for each row in the matrices)
		for (int i = 0; i < numValidLoops; ++i){
			// For each connection in the loop, check the resistance and any voltage drop
			// We do this by considering the properties of the node we are leaving
			for (int j = 0; j < loops[i].Count; ++j){
				// This connection
				BranchAddress thisAddress = loops[i][j];
				CircuitElement thisElement = Circuit.singleton.GetElement (new GridPoint(thisAddress.x, thisAddress.y));
				BranchData thisData = GetBranchData (thisAddress);
				
				// Our data loopID should be the same as i
				if (thisData.loopId != i){
					Debug.LogError ("LoopId/i missmatch!@");
				}
				
				// Get current ID for current travelling in the opposite direction
				BranchAddress oppAddress = GetOppositeAddress(thisAddress);
				BranchData oppData	= GetBranchData (oppAddress);
				CircuitElement oppElement = Circuit.singleton.GetElement(new GridPoint(oppAddress.x, oppAddress.y));
				
				
				// Work out the resistance in this branch
				float resistance = thisElement.GetResistance(thisAddress.dir) + oppElement.GetResistance(oppAddress.dir);
				
				
				// We build up the resistance matrix 
				R[i, thisData.loopId] += resistance;
				if (!oppData.isOuterLoop){
					R[i, oppData.loopId] -= resistance;
				}
				
				V[i, 0] += thisElement.GetVoltageDrop(thisAddress.dir, true);
				V[i, 0] += oppElement.GetVoltageDrop(oppAddress.dir, false);
				
				// Use for dictionary version  (for non dictionary version we don;t get them from this loop)
				
				// Also build a list of unique combinations of loops which corresponds to wires which 
				// sit on those loops as we want to spread the current out euqally amonst these combinations
				int val0 = thisData.isOuterLoop ? -1 : thisData.loopId;
				int val1 = oppData.isOuterLoop ? -1 : oppData.loopId;
				
				if (val0 > val1){
					int temp = val0;
					val0 = val1;
					val1 = temp;
				}
				
				// If one of the entries is an outer loop, then the value  we add is doubled?
				float val = 1f;
				if (val0 == -1){
					val = 2f;
				}
				
				
				
				Eppy.Tuple<int, int> key = new Eppy.Tuple<int, int>(val0, val1);
				
				
				if (!loopCombinations.ContainsKey(key)){
					loopCombinations.Add (key, val);
				}
				else{
					loopCombinations[key] += val;
				}
				
			}
		}  
		
		
		// Create "Loop Matrix" which maps loop currents onto the currents on the branches of our graph
		double [,] L = new double[loopCombinations.Count, numValidLoops];
		
		
		// Dictionary version
		//		int k = 0;
		//		foreach (KeyValuePair<Eppy.Tuple<int, int>, bool> entry in loopCombinations){
		//			if (entry.Key.Item1 != -1) L[k, entry.Key.Item1] = 1;
		//			if (entry.Key.Item2 != -1) L[k, entry.Key.Item2] = -1;
		//			++k;
		//		}
		
		int k = 0;
		foreach (KeyValuePair<Eppy.Tuple<int, int>, float> entry in loopCombinations){
			if (entry.Key.Item1 != -1) L[k, entry.Key.Item1] = Mathf.Sqrt(entry.Value);
			if (entry.Key.Item2 != -1) L[k, entry.Key.Item2] = -Mathf.Sqrt(entry.Value);
			++k;
		}
		
		// If we just wanted to calculate A solution for the loops, we would write:
		// Since we may not have full rank,, we find a solution using the Moore-Pensrose Pseudo-inverse
		//		double[,] RInv = CalcPseudoInverse(R);
		//		I = MathUtils.Matrix.Multiply(RInv, V); 
		
		// However, this doesn't distribute the current well over the different branches of the Circuit.singleton if there is
		// no reisstance in it
		// Instead, we calculate the crrents for each brnach (using a pseudo inverse) and then reverse calculate
		// what the loops should be (in the final version we should just lose the whole Loop business)
		
		// Find the pseudo inverse of this matrix
		double[,] LInv = CalcPseudoInverse (L);
		
		// Suppose C are the individual currents in our mesh, then
		// V = R x LInv x C
		
		// So, find R X LInv, and then find the Pseudo inverse of thast
		double[,] comb = MathUtils.Matrix.Multiply(R, LInv);
		double[,] combInv = CalcPseudoInverse(comb);
		double[,] C = MathUtils.Matrix.Multiply(combInv, V);
		
		// Now we need to reduce our resultant currents
		/*
		k = 0;
		foreach (KeyValuePair<Eppy.Tuple<int, int>, float> entry in loopCombinations){
			C[k,0] = C[k,0] / Mathf.Sqrt(entry.Value);
			++k;
		}
		*/
		
		// Currents
		double[,] I = MathUtils.Matrix.Multiply (LInv, C);
		
		
		// Check that we get V - if not we have an unsolvable set of equations and 
		// it means we have a loop of zero resistance with a voltage different in it
		double[,] testV = MathUtils.Matrix.Multiply(R, I);
		
		bool failed = false;
		for (int i = 0; i < numValidLoops; ++i){
			// f we find a bad loop
			if (Math.Abs (V[i, 0] - testV[i, 0]) > epsilon){
				// Then follow this loop finding all the voltage sources and put them in Emergency mode
				for (int j = 0; j < loops[i].Count; ++j){
					BranchAddress thisAddr = loops[i][j];
					CircuitElement thisElement = Circuit.singleton.GetElement(new GridPoint(thisAddr.x, thisAddr.y));
					if (Mathf.Abs (thisElement.GetVoltageDrop(thisAddr.dir, true)) > epsilon){
						thisElement.TriggerEmergency();
						failed = true;
					}
					BranchAddress oppAddr = GetOppositeAddress(thisAddr);
					CircuitElement oppElement = Circuit.singleton.GetElement(new GridPoint(oppAddr.x, oppAddr.y));
					if (Mathf.Abs (oppElement.GetVoltageDrop(oppAddr.dir, false)) > epsilon){
						oppElement.TriggerEmergency();
						failed = true;
					}
				}
			}
		}
		
		//		Debug.Log ("L = " + L.GetLength(0) + " X " + L.GetLength (1));
		//		Debug.Log ("I = " + I.GetLength(0) + " X " + I.GetLength (1));
		//		
		//		
		//		double[,] currentTest = MathUtils.Matrix.Multiply (L, I);
		//		
		//		double[,] testLoops = MathUtils.Matrix.Multiply (LInv, C);
		//		double[,] doubleCheck1 = MathUtils.Matrix.Multiply (L, I);
		//		double[,] doubleCheck2 = MathUtils.Matrix.Multiply (L, testLoops);
		//		
		if (failed) return false;
		
		
		
		
		
		loopCurrents = new float[loops.Count];
		if (I.GetLength(0) != 0){
			for (int i = 0; i < numValidLoops; ++i){
				loopCurrents[i] = (float)I[i,0];
			}
			// For the invalid loops, just set the current to zero
			for (int i = numValidLoops; i < loops.Count; ++i){
				loopCurrents[i] = 0f;
			}
			
		}
		// all went well
		return true;
		
	}
	
	
	bool SolveForCurrentsOptLoop(){
	
		
		int numInvalidLoops = 0;
		// Go through the loops and remove any which are outer loops or of zero length or an invlaid loop
		// e.g. an outer loop and place them at the end
		for (int i = loops.Count-1; i >=0 ; --i){
			if (loops[i].Count == 0 || GetBranchData(loops[i][0]).isOuterLoop){
				int newIndex = CreateLoop ();
				loops[newIndex] = loops[i];
				loops.RemoveAt (i);

				++numInvalidLoops;
			}
		}
		
		// It is useful rearrange the loops (see above), for the calculation of currents.
		// However, for the purposes of voltage analysis, it is better to have then in their original aorder
		// (as we can guarantee that the starting point of a loop starts connected to a loop we have already traverse
		// unless we are the first loop in a group.
		// This array is a way to store that original order  - we can calcukate it based on the fact that the loop Ids
		// have not yet neen recalcualted
			
		voltageLoopOrder = new int[loops.Count];
		for (int i = 0; i < loops.Count; ++i){
			for (int j = 0; j < loops.Count; ++j){
				if (GetBranchData(loops[j][0]).loopId == i){
					voltageLoopOrder[i] = j;
				}
			}
			
		}		

		
		
		
		numValidLoops = loops.Count - numInvalidLoops;

		// Now renumber our loops (up to the valid ones  the invalid ones should retain an ID of -1 or whatever)
		for (int i = 0; i < loops.Count; ++i){
			for (int j= 0; j < loops[i].Count; ++j){
				GetBranchData(loops[i][j]).loopId = i;
			}
		}
		

		// Create arrays need to solve equation coeffs
		double [,] R = new double[numValidLoops, numValidLoops];
		double [,] V = new double[numValidLoops, 1];
		
		// For through each loop in tune (for each row in the matrices)
		for (int i = 0; i < numValidLoops; ++i){
			// For each connection in the loop, check the resistance and any voltage drop
			// We do this by considering the properties of the node we are leaving
			for (int j = 0; j < loops[i].Count; ++j){
				// This connection
				BranchAddress thisAddress = loops[i][j];
				CircuitElement thisElement = Circuit.singleton.GetElement (new GridPoint(thisAddress.x, thisAddress.y));
				BranchData thisData = GetBranchData (thisAddress);

								// Our data loopID shoudl be the same as i
				if (thisData.loopId != i) Debug.LogError ("LoopId/i missmatch!@");
				
				// Get current ID for current travelling in the opposite direction
				BranchAddress oppAddress = GetOppositeAddress(thisAddress);
				BranchData oppData	= GetBranchData (oppAddress);
				CircuitElement  oppElement = Circuit.singleton.GetElement(new GridPoint(oppAddress.x, oppAddress.y));
				
				
				// Work out the resistance in this branch
				float resistance = thisElement.GetResistance(thisAddress.dir) + oppElement.GetResistance(oppAddress.dir);
				
				
				// We build up the resistance matrix 
				R[i, thisData.loopId] += resistance;
				if (!oppData.isOuterLoop){
					R[i, oppData.loopId] -= resistance;
				}
				
				V[i, 0] += thisElement.GetVoltageDrop(thisAddress.dir, true);
				V[i, 0] += oppElement.GetVoltageDrop(oppAddress.dir, false);
			}
		}  

		// Currents
		double[,] I = new double[0,0];
		
		// IF we do not have full rankm then find a solution using the Moore-Pensrose Pseudo-inverse
		// Method taken from here: http://en.wikipedia.org/wiki/Moore%E2%80%93Penrose_pseudoinverse#The_iterative_method_of_Ben-Israel_and_Cohen
		// search for "A computationally simple and accurate way to compute the pseudo inverse " ont his page
		
		
//		if (R.GetLength (0) != R.GetLength (1)){
//			Debug.LogError ("Matrix is not square, yet we expect it to be!");
//		}
//		
		
		// First we need to calc the SVD of the matrix
		double[] W = null;	// S matrix as a vector (leading diagonal)
		double[,] U = null;
		double[,] Vt = null;
		alglib.svd.rmatrixsvd(R, R.GetLength (0), R.GetLength (1), 2, 2, 2, ref W, ref U, ref Vt);
		
		double[,] S = new double[W.GetLength (0), W.GetLength (0)];
		
		for (int i = 0; i < R.GetLength (0); ++i){
			S[i,i] = W[i];
		}
		//				MathUtils.Matrix.SVD(R, out S, out U, out Vt);
		// Log the results
		
//		double[,] testR0 = MathUtils.Matrix.Multiply(U, S);
//		double[,] testR1 = MathUtils.Matrix.Multiply(testR0, Vt);
		
		double[,] Ut = MathUtils.Matrix.Transpose(U);
		double[,] Vtt = MathUtils.Matrix.Transpose (Vt);
		
		// Get the psuedo inverse of the U matrix (which is diagonal)
		// The way we do this is by taking the recipricol of each diagonal element, leaving the (close to) zero's in place
		// and transpose (actually we don't need to transpose because we always have square matricies)
		
		// I assume this gets initialised with zeros
		double[,] SInv = new double[S.GetLength(0), S.GetLength(0)];
		
		for (int i = 0; i < S.GetLength(0); ++i){
			if (Math.Abs (S[i,i]) > epsilon){
				SInv[i,i] = 1.0 / S[i,i];
				
			}					
		}
		
		//Rinv = Vtt Uinv St
		double[,] RInvTemp = MathUtils.Matrix.Multiply (Vtt, SInv);
		double[,] RInv = MathUtils.Matrix.Multiply (RInvTemp, Ut);
		
//		// Test thast we have a psueoinverse
//		double[,] res0 = MathUtils.Matrix.Multiply(R, RInv);
//		double[,] res1 = MathUtils.Matrix.Multiply(res0, R);
		
		I = new double[numValidLoops,1];
		I = MathUtils.Matrix.Multiply(RInv, V); 
		
		// Check that we get V - if not we have an unsolvable set of equations and 
		// it means we have a loop of zero resistance with a voltage different in it
		double[,] testV = MathUtils.Matrix.Multiply(R, I);
		
		bool failed = false;
		for (int i = 0; i < numValidLoops; ++i){
			// f we find a bad loop
			if (Math.Abs (V[i, 0] - testV[i, 0]) > epsilon){
				// Then follow this loop finding all the voltage sources and put them in Emergency mode
				for (int j = 0; j < loops[i].Count; ++j){
					BranchAddress thisAddr = loops[i][j];
					CircuitElement thisElement = Circuit.singleton.GetElement(new GridPoint(thisAddr.x, thisAddr.y));
					if (Mathf.Abs (thisElement.GetVoltageDrop(thisAddr.dir, true)) > epsilon){
						thisElement.TriggerEmergency();
						failed = true;
					}
					BranchAddress oppAddr = GetOppositeAddress(thisAddr);
					CircuitElement oppElement = Circuit.singleton.GetElement(new GridPoint(oppAddr.x, oppAddr.y));
					if (Mathf.Abs (oppElement.GetVoltageDrop(oppAddr.dir, false)) > epsilon){
						oppElement.TriggerEmergency();
						failed = true;
					}
				}
			}
		}
		if (failed) return false;
		

		
		loopCurrents = new float[loops.Count];
		if (I.GetLength(0) != 0){
			for (int i = 0; i < numValidLoops; ++i){
				loopCurrents[i] = (float)I[i,0];
			}
			// For the invalid loops, just set the current to zero
			for (int i = numValidLoops; i < loops.Count; ++i){
				loopCurrents[i] = 0f;
			}
			
		}
		// all went well
		return true;
		
	}
	
	// Given some address this returns the central address corresponding to it
	BranchAddress GetCentreAddress(BranchAddress addr){
		return new BranchAddress(addr.x, addr.y, Circuit.kCentre);
	}
	
	
	// Assuming we have the currents sorted, this find the voltages at each node
	void SolveForVoltages(){
		if (loops.Count == 0) return;
		
		int groupLoopIdIndex = 0;
		while (groupLoopIdIndex != loops.Count){
			
			float initialVoltage = 0;
			float minGroupVoltage = 99f;
			int loopIdIndex = 0;
			while (!MathUtils.FP.Feq (minGroupVoltage, 0f)){
				minGroupVoltage = 0;
				
				// We always need to seed a group  with a known voltge (as we don't have a "ground")
				// Just do this with the first node that we have
				loopIdIndex = groupLoopIdIndex;
				int loopId = voltageLoopOrder[loopIdIndex];
				int groupStartId = GetBranchData(loops[loopId][0]).groupId;
				
				SetInitVoltage(GetCentreAddress(loops[loopId][0]), initialVoltage);
		
		
				// For voltages, we also include the non-valid loops (such as the outer one) as it may include spokes which need 
				// traversing
				while (loopIdIndex < loops.Count && GetBranchData(loops[loopId][0]).groupId == groupStartId){
					
					
					BranchData startData = GetBranchData(GetCentreAddress(loops[loopId][0]));
					if (startData.initialVolt == false){
						Debug.LogError ("No initial voltage");
					}
					
					// Get data from the initial node in the group
					float currentVoltage = startData.totalVoltage;
					
					// Each connection consits of two spokes - one exiting the first node, the next entering the next
					// I have assumed that the opposite branch for each node in the loop is centred at the next node in the loop
					for (int i = 0; i < loops[loopId].Count; ++i){
						// Current Voltage is set to the centre point of the current node in the loop
						
						// Get the voltage for the voltage at the end of the spoke
						BranchAddress thisAddr = loops[loopId][i];
						CircuitElement thisElement = Circuit.singleton.GetElement(new GridPoint(thisAddr.x, thisAddr.y));
						BranchData thisData = GetBranchData(thisAddr);
					
						currentVoltage += thisElement.GetVoltageDrop(thisAddr.dir, true) - thisData.totalCurrent * thisElement.GetResistance(thisAddr.dir);
						
						SetInitVoltage(thisAddr, currentVoltage);
						
						if (currentVoltage < minGroupVoltage) minGroupVoltage = currentVoltage;

												// Now we can set this to be the voltage at the end of the opposing spoke
						BranchAddress oppAddr = GetOppositeAddress(thisAddr);
						SetInitVoltage(oppAddr, currentVoltage);
						
						// Now traverse the other spoke to the centre of the next node
						CircuitElement oppElement = Circuit.singleton.GetElement(new GridPoint(oppAddr.x, oppAddr.y));
						BranchData oppData = GetBranchData(oppAddr);
						// Note to change signs on current as we are going agsint the flow
						currentVoltage += oppElement.GetVoltageDrop(oppAddr.dir, false) + oppData.totalCurrent * oppElement.GetResistance(oppAddr.dir);
						
						SetInitVoltage(GetCentreAddress(oppAddr), currentVoltage);
						
		
						if (currentVoltage < minGroupVoltage) minGroupVoltage = currentVoltage;
						
						// Check my assumptions about the opposite branch being the next node
						// DEBUG only
						BranchAddress testAddr = loops[loopId][(i+1) % loops[loopId].Count];
						CircuitElement testElement = Circuit.singleton.GetElement(new GridPoint(testAddr.x, testAddr.y));
						if (testElement != oppElement){
							Debug.LogError ("Assumptions about opposite branches in the loops as failed");
						}
					}
					++loopIdIndex;
					// %Length just to stop it over flowing
					loopId = voltageLoopOrder[loopIdIndex % voltageLoopOrder.Length];
				}
				if (!MathUtils.FP.Feq(minGroupVoltage, 0f)){
					// if the minimum voltage around the certcuit is not zero, then do it all again, but with a new minimum voltage
					initialVoltage = -minGroupVoltage;
					minGroupVoltage= 99f;
				}
	
			}
			groupLoopIdIndex  = loopIdIndex;		
		
		}
		
		
		
	}
	
	void SetInitVoltage(BranchAddress addr, float currentVoltage){
		GetBranchData(addr).initialVolt = true;
		GetBranchData(addr).totalVoltage = currentVoltage;
	}
	
	
	// This is pretty hacky - we shouldn't have to check if this is a wire or not
	void SetInitVoltageOld(BranchAddress addr, float currentVoltage){
		CircuitElement element = Circuit.singleton.GetElement(new GridPoint(addr.x, addr.y));
		
		// Check if this is a wire (because then the voltage is the same accross all its connections)
		if (GameObject.ReferenceEquals(element.GetType (), typeof(CircuitElementWire))){
			SetInitWireVoltage(addr, element, currentVoltage);
		}
		else{
			SetInitNonWireVoltage(addr, currentVoltage);
		}
		
	}
	
	void SetInitWireVoltage(BranchAddress addr, CircuitElement element, float currentVoltage){
		// Do all the branches coming from this node
		for (int i = 0; i < 4; ++i){
			if (element.IsConnected(i)){
				BranchAddress setAddr = new BranchAddress(addr.x, addr.y, i);
				BranchAddress setOppAddr = GetOppositeAddress(setAddr);
				GetBranchData(setAddr).initialVolt = true;
				GetBranchData(setAddr).totalVoltage = currentVoltage;
				GetBranchData(setOppAddr).initialVolt = true;
				GetBranchData(setOppAddr).totalVoltage = currentVoltage;
			}
			
		}
	}
	
	void SetInitNonWireVoltage(BranchAddress addr, float currentVoltage){
		BranchAddress oppAddr = GetOppositeAddress(addr);

		GetBranchData(addr).initialVolt = true;
		GetBranchData(addr).totalVoltage = currentVoltage;
		GetBranchData(oppAddr).initialVolt = true;
		GetBranchData(oppAddr).totalVoltage = currentVoltage;
	}	
	
	
	bool Simulate(){
		// Find all the loops
		bool ok1 = FindLoops();
		if (!ok1) return false;
		// Flag the ones going round the outside of disjoint Circuit.singletons as 0 (so we can ignore them)
		FlagOuterLoops();
		// Go through each loop and remove any elements which are from a "Spoke" - i.e., not a loop
		//TrimSpokes();

				// Set up equations and solve them (repeat if failed the first time as the failure situation triggers emergency measures which should make it ok)
		bool ok = false;
		switch (minMode){
			case MinMode.kLoops:
			{
				ok = SolveForCurrentsOptLoop();
				break;
			}
			case MinMode.kBranches:
			{
				ok = SolveForCurrentsOptBranches();
				break;
			}
			case MinMode.kElements:
			{
				ok = SolveForCurrentsOptElements();
				break;
			}
			case MinMode.kElementsOpt:
			{
				ok = SolveForCurrentsOptBranchesElements();
				break;
			}
		};
		if (!ok){
			return false;
		}
		CalcTotalCurrents();
		// Use the currents we have found to calculate the voltages
		if (solveVoltges){
			SolveForVoltages();
		}
		return true;
	

	}
	
	void BuildArrays(){
		width = Grid.singleton.gridWidth;
		height = Grid.singleton.gridHeight;
		
		loops = new List<List<BranchAddress>>();
		// We have branch data for each direction from a node and its centre (even though the centre only really contains a voltage)
		branchData = new BranchData[width, height, 5];
		
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				for (int i = 0; i < 5; ++i){
					branchData[x,y,i] = new BranchData();
				}
				
			}
		}
		loopCurrents = new float[0];
		voltageLoopOrder = new int[0];
	}
	
	public void ClearSimulation(){

		ClearLoopCurrentVis();		
		ClearCurrentVis();
		ClearVoltageVis();
		// Are these two necessary?
		ClearLoops();
		ClearBranchData();
		
	}
	
	void ClearLoops(){
		loops = new List<List<BranchAddress>>();
	}
	
	void ClearBranchData(){
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				for (int i = 0; i < 4; ++i){
					branchData[x,y,i] = new BranchData();
				}
				
			}
		}	
	}
	
	// Not really unique, but does an OK job
	Color GetUniqueColor(int index){
		// don't want to use black for valid loops
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
		float offsetSize = 0.3f;
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
					
					BranchData data = GetBranchData (thisAddress);
					// ALweaus remder the loops
					if (data.traversed){
						Vector3 from = new Vector3(x, y, 0f) + offsets[i];
						Vector3 to = new Vector3(nextAddress.x, nextAddress.y, 0f) + offsets[i];
						Vector3 avPos = (from + to) * 0.5f;
						from = avPos + (from - avPos) * lengthScale;
						to = avPos + (to - avPos) * lengthScale;
						Color col = GetUniqueColor((visMode == VisMode.kLoops) ? (data.isOuterLoop ? -1 : data.loopId): data.groupId);
						
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
					
					BranchData thisData = GetBranchData (thisAddress);
					if (thisData.traversed && thisData.loopId >= 0){
						BranchData oppData = GetBranchData (oppAddress);
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
				int groupId = -1;
				bool isOuter = false;
				for (int i = 0; i < 4; ++i){
					if (Grid.singleton.IsPointInGrid(squareAddresses[i].x, squareAddresses[i].y)){
						BranchData data = GetBranchData(squareAddresses[i]);
						if (data.traversed){
							loopId = data.loopId;
							groupId = data.groupId;
							isOuter = data.isOuterLoop;
						}
						
					}
				}
				if (loopId != -1){
					debugTextBoxes[x,y].SetActive(true);
					TextMesh textMesh = debugTextBoxes[x,y].GetComponent<TextMesh>();
					textMesh.color = GetUniqueColor((visMode == VisMode.kLoops) ? (isOuter ? -1 : loopId): groupId);
					textMesh.text = Mathf.Abs (loopCurrents[loopId]).ToString("0.00") + "A";
				}
				else{
					debugTextBoxes[x,y].SetActive(false);
				}
				
			}
		}
	}
	

	
	void CalcTotalCurrents(){
		// The coodinates of a square are the coordinates of the bottom right corner
		for (int x = 0; x < width-1; ++x){
			for (int y = 0; y < height-1; ++y){
				
				// Do branch going right
				float rightCurrent = 0f;
				BranchAddress rightAddr = new BranchAddress(x, y, Circuit.kRight);
				BranchAddress rightAddrInv = GetOppositeAddress(rightAddr);
				BranchData rightData = GetBranchData (rightAddr);
				BranchData rightInvData = GetBranchData (rightAddrInv);
				
				if (rightData.traversed){
					rightCurrent += loopCurrents[GetBranchData (rightAddr).loopId];
				}
				if (rightInvData.traversed ){
					rightCurrent -= loopCurrents[GetBranchData (rightAddrInv).loopId];
				}
				GetBranchData (rightAddr).totalCurrent = rightCurrent;
				GetBranchData (rightAddrInv).totalCurrent = -rightCurrent;
				
				
				
				// Do branch going up
				
				float upCurrent = 0f;
				BranchAddress upAddr = new BranchAddress(x, y, Circuit.kUp);
				BranchAddress upAddrInv = GetOppositeAddress(upAddr);
				BranchData upData = GetBranchData (upAddr);
				BranchData upInvData = GetBranchData (upAddrInv);
				
				
				if (upData.traversed){
					upCurrent += loopCurrents[GetBranchData (upAddr).loopId];
				}
				if (upInvData.traversed ){
					upCurrent -= loopCurrents[GetBranchData (upAddrInv).loopId];
				}
				
				GetBranchData (upAddr).totalCurrent = upCurrent;
				GetBranchData (upAddrInv).totalCurrent = -upCurrent;
				
				
				
				
			}
		}
	}
	
	// Go through each of the squares and find edges which have a loop current in them
	// NOte that in our current setup, a single square only descrbes a single loop current
	void DebugRenderCurrentVis(){
		// The coodinates of a square are the coordinates of the bottom right corner
		for (int x = 0; x < width-1; ++x){
			for (int y = 0; y < height-1; ++y){

				BranchAddress rightAddr = new BranchAddress(x, y, Circuit.kRight);

				BranchData rightData = GetBranchData (rightAddr);
				
				
				if (rightData.traversed){
					debugCurrentTextBoxesRight[x,y].SetActive(true);
					TextMesh textMesh = debugCurrentTextBoxesRight[x,y].GetComponent<TextMesh>();
					textMesh.color = Color.white;
					textMesh.text = Mathf.Abs (rightData.totalCurrent).ToString("0.00") + "A / " + rightData.totalVoltage.ToString("0.00") + "V";
				}
				else{
					debugCurrentTextBoxesUp[x,y].SetActive(false);
				}

				
				BranchAddress upAddr = new BranchAddress(x, y, Circuit.kUp);
				
				BranchData upData = GetBranchData (upAddr);
				
				if (upData.traversed){
					debugCurrentTextBoxesUp[x,y].SetActive(true);
					TextMesh textMesh = debugCurrentTextBoxesUp[x,y].GetComponent<TextMesh>();
					textMesh.color = Color.white;
					textMesh.text = Mathf.Abs (upData.totalCurrent).ToString("0.00") + "A / " + upData.totalVoltage.ToString("0.00") + "V";
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
	
	void DebugCreateVoltageVis(){
		debugVoltageTextBoxes = new GameObject[width, height];
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				debugVoltageTextBoxes[x,y] = Instantiate (textMeshGO, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
				debugVoltageTextBoxes[x,y].transform.parent = transform;
				debugVoltageTextBoxes[x,y].SetActive(false);
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
	}	

	void ClearVoltageVis(){
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				debugVoltageTextBoxes[x,y].SetActive(false);
			}		
		}
	}
	
	void FindMeshRenderers(GameObject go, List<MeshRenderer> list){
		
		MeshRenderer mesh = go.transform.GetComponent<MeshRenderer>();
		if (mesh != null){
			list.Add (mesh);
		}
		foreach (Transform child in go.transform){
			FindMeshRenderers (child.gameObject, list);
		}
	}

		// GameUpdate is called once per frame in a specific order
	public void GameUpdate () {
	
		bool finished = false;
		int attempts = 3;
		while (!finished && attempts > 0){
			ClearSimulation();
			finished = Simulate();
			attempts--;
		}
		if (attempts == 0){
			Debug.LogError ("Failed to solve circuit equations");
		}
		
		if (visMode == VisMode.kGroups || visMode == VisMode.kLoops){
			DebugRenderLoops();	
			DebugRenderLoopCurrentVis();
		}
		else if (visMode == VisMode.kCurrents){
			DebugRenderCurrents();	
			DebugRenderCurrentVis();
			//DebugRenderVoltages();
		}
		
		// update the material animatoins
		for (int x = 0; x < width; ++x){
			for (int y = 0; y < height; ++y){
				GridPoint thisPoint = new GridPoint(x, y);
				CircuitElement element = Circuit.singleton.GetElement(thisPoint);
				if (element != null){
					// Get the 
					List<MeshRenderer> meshes = new List<MeshRenderer>();
					FindMeshRenderers(Circuit.singleton.GetGameObject (thisPoint), meshes);
					for (int k = 0; k < meshes.Count; ++k){
						MeshRenderer mesh = meshes[k];
						float componentVolage = 0f;
						for (int i = 0; i < 4; ++i){
							BranchData data = GetBranchData(new BranchAddress(x, y, i));
							float thisVoltage = 0;
							float thisCurrent = 0;
							int orient = Circuit.singleton.GetElement(thisPoint).orient;
							if (data.traversed){
								thisVoltage = data.totalVoltage;
								thisCurrent = data.totalCurrent;
							}
							// If this branch has not been traversed, but there is an element here, then this element is
							// not connected to antthing via this branch - so we ask the element what the numbers should be
							else{
								thisVoltage = element.GetUnconnectedVoltage(i);
								thisCurrent = 0;
							}
							// Now set up the material to visualise thse numbers
							float speed = -currentMulVis * thisCurrent;
							float cappedSpeed = Mathf.Min (15f, Mathf.Abs(speed)) *( (speed < 0) ? -1f : 1f);
							mesh.materials[0].SetFloat ("_Speed" + ((i+ orient) % 4), cappedSpeed);
							mesh.materials[0].SetFloat ("_Voltage" + ((i+ orient) % 4),  thisVoltage);
							
							// For testing 3D visualisation
							componentVolage = data.totalVoltage;
							if (enableVoltsgeAsHeight){
								if (float.IsNaN(componentVolage) || float.IsInfinity(componentVolage)) componentVolage = 0f;
								
								Vector3 pos = Circuit.singleton.GetGameObject(thisPoint).transform.position;
								pos.z = -componentVolage;
								Circuit.singleton.GetGameObject(thisPoint).transform.position = pos;
							}
							
						}
					}
				}
			}		
		}

	
	}
	
	
	void OnGUI () {
//		guiTextDisplay.GUIResetTextLayout();
//		guiTextDisplay.GUIPrintText( "Number of disjoint Circuit.singletons: " + numGroups, Color.white);
//		guiTextDisplay.GUIPrintText( "Number of loops: " + numValidLoops, Color.white);
		
	}	
}
