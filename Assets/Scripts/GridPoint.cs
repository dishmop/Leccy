using UnityEngine;
using System.Collections;

[System.Serializable]
public class GridPoint{

	// Only valid if positive
	public int x;
	public int y;
	
	public GridPoint(){
		this.x = -1;
		this.y = -1;
	}
	
	public GridPoint(GridPoint other){
		this.x = other.x;
		this.y = other.y;
	}
	
	public GridPoint(int x, int y){
		this.x = x;
		this.y = y;
	}
	
	public bool IsValid(){
		return x != -1 && y != -1;
	}
	
	public  bool IsEqual(System.Object obj)
	{
		// If parameter is null return false.
		if (obj == null)
		{
			return false;
		}
		
		// If parameter cannot be cast to GridPoint return false.
		GridPoint p = obj as GridPoint;
		if ((System.Object)p == null)
		{
			return false;
		}
		
		// Return true if the fields match:
		return (x == p.x) && (y == p.y);
	}
	
	public static GridPoint operator + (GridPoint p1, GridPoint p2) 
	{
		return new GridPoint(p1.x + p2.x, p1.y + p2.y);
	}	
	
}
