using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIMesh : MonoBehaviour {

	public GameObject emptyGO;

	
	GameObject prefabMesh;
	Bounds	bounds;

	
	public void OnChange(){
		UncreateSelf ();
		CreateSelf();
	}
	
	public void SetPrefabMesh(GameObject prefab){
		prefabMesh = prefab;
		prefabMesh.transform.localPosition = new Vector3(0, 0, 0);
		OnChange ();
		
	}
	
	public GameObject GetPrefab(){
		return prefabMesh;
	}

	void ConstructCanvasRenderer(GameObject thisObj, GameObject refMesh){
	
		// Enusre the scale etc. are set up correctly
		thisObj.transform.localScale = refMesh.transform.localScale;
		thisObj.transform.localRotation = refMesh.transform.localRotation;
		thisObj.transform.localPosition = refMesh.transform.localPosition;;
		//thisObj.transform.localPosition = refMesh.transform.localPosition;;
		
		MeshFilter meshFilter = refMesh.GetComponent<MeshFilter>();
		MeshRenderer meshRenderer = refMesh.GetComponent<MeshRenderer>();
		// If we have one, then create a corresponding CanvasRendeer and fill it with data
		if (meshFilter && meshRenderer){
			CanvasRenderer canvasRenderer = thisObj.AddComponent<CanvasRenderer>();
			
			// Copy the vertices over
			// NOte that the mesh seems to use an indexed primitvs while the canvasrender uses a non indexted triangle list. 
			int numVerts =  meshFilter.mesh.vertexCount;
			int[] triangles = meshFilter.mesh.triangles;
			int numTriVerts = triangles.Length;
			
			// It seems that the canvas is expecting a multiple of 4 quads (implying it is expecting 1uads, rathe rthan triangles).
			// So, we rpeat every 3rd tirnagle. 
			int numQuadVerts = 4 * numTriVerts / 3;
			UIVertex[]	newVerts = new UIVertex[numQuadVerts];
			

			// copy data from the normal MeshRenderer into the canvasRenderer
			int j = 0;
			for (int i = 0; i < numQuadVerts; ++i){
				int vertIndex = triangles[j];
			
				newVerts[i] = new UIVertex();
				
				if (meshFilter.mesh.vertices.Length  == numVerts) 	newVerts[i].position = 	meshFilter.mesh.vertices[vertIndex];
				if (meshFilter.mesh.normals.Length  == numVerts) 	newVerts[i].normal = 	meshFilter.mesh.normals[vertIndex];
				if (meshFilter.mesh.colors.Length  == numVerts) 	newVerts[i].color = 	meshFilter.mesh.colors[vertIndex];
				if (meshFilter.mesh.uv.Length  == numVerts) 		newVerts[i].uv0 = 		meshFilter.mesh.uv[vertIndex];		// same as uv1 apparently
				if (meshFilter.mesh.uv2.Length  == numVerts) 		newVerts[i].uv1 = 		meshFilter.mesh.uv2[vertIndex];
				if (meshFilter.mesh.tangents.Length  == numVerts) 	newVerts[i].tangent = 	meshFilter.mesh.tangents[vertIndex];
				
				// Transform the point to where it will be and then add it to thebounds
				Vector3 worldPos = thisObj.transform.TransformPoint(newVerts[i].position);
				bounds.Encapsulate(worldPos);
				
				if (i % 4 != 2) j++;				
			}
			canvasRenderer.SetVertices(newVerts, numQuadVerts);
			
			canvasRenderer.SetMaterial(meshRenderer.material, null);
			canvasRenderer.SetColor(Color.white);
			canvasRenderer.SetAlpha(1f);
			
			// Now make it fit the rectangle
		}
		foreach (Transform child in refMesh.transform){
			GameObject newObj = InstantiateEmptyChild(thisObj);
			newObj.name = child.gameObject.name;

			
			ConstructCanvasRenderer(newObj, child.gameObject);
		}
	}
	
	GameObject InstantiateEmptyChild(GameObject thisObj){
		GameObject newObj = Instantiate (emptyGO, thisObj.transform.position, thisObj.transform.rotation) as GameObject;
		newObj.transform.parent = thisObj.transform;
		return newObj;
	}
	
	void CreateSelf(){
		// If we don't have a prefab, then cannot make ourselves
		if (!prefabMesh) return;
		
		// Need to make an instaniation of the prefab and then access that to read the verx data
		// otherwise we end up destroying the prefab (from some reason).
		GameObject mesh = Instantiate(prefabMesh) as GameObject;
		mesh.transform.parent = transform;
		mesh.transform.localPosition = new Vector3(0, 0, 0);
				
		GameObject scalingChild = InstantiateEmptyChild(gameObject);
		scalingChild.name = "scalingObj";
		// Set up the bounds around the scalingChild (rather than the origin)
		bounds.min = scalingChild.transform.position;
		bounds.max = scalingChild.transform.position;
		
		ConstructCanvasRenderer(scalingChild, mesh);
		
		GameObject.Destroy(mesh);
		
		// Set up the scaling to ensure we fit exactly inside out rectange
		RectTransform rectTrans =  GetComponent<RectTransform>();
		float xScale = rectTrans.rect.width / bounds.size.x;
		float yScale = rectTrans.rect.height / bounds.size.y;
		float scale = Mathf.Min (xScale, yScale);
		scalingChild.transform.localScale = new Vector3(scale, scale, 1f);	
	}

	// Use this for initialization
	void Start () {
	}
	

	
	void UncreateSelf(){
		List<GameObject> children = new List<GameObject>();
		foreach (Transform child in transform) children.Add(child.gameObject);
		children.ForEach(child => Destroy(child));	
	}
	
	
}
	
