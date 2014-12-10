using UnityEngine;
using System;
using System.Collections.Generic;

class SerializationUtils
{
	// Sets the destination value to the source and sets hasCHanged to true if we did so (if we did not change anything,
	// then hasChanged is left as it is).
	public static void UpdateIfChanged<T>(ref T dest, T src, ref bool hasChanged){
		if (EqualityComparer<T>.Default.Equals(dest, src)) return;
		dest = src;
		hasChanged = true;
	}

}