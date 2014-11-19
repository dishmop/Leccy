using UnityEngine;
using System.Collections;

public class UIMesh : MonoBehaviour {

	public GameObject prefabMesh;
	public GameObject emptyGO;

	
	Bounds	bounds;
	
	void ConstructCanvasRenderer(GameObject thisObj, GameObject refMesh){
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
				if (meshFilter.mesh.uv1.Length  == numVerts) 		newVerts[i].uv1 = 		meshFilter.mesh.uv2[vertIndex];
				if (meshFilter.mesh.tangents.Length  == numVerts) 	newVerts[i].tangent = 	meshFilter.mesh.tangents[vertIndex];
				
				bounds.Encapsulate(newVerts[i].position);
				
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

	// Use this for initialization
	void Awake () {
		// Need to make an instaniation of the prefab and then access that to read the verx data
		// otherwise we end up destroying the prefab (from some reason).
		GameObject mesh = Instantiate(prefabMesh, transform.position, transform.rotation) as GameObject;
		mesh.transform.parent = transform;
		
		GameObject scalingChild = InstantiateEmptyChild(gameObject);
		scalingChild.name = "scalingObj";
		ConstructCanvasRenderer(scalingChild, mesh);
		
		GameObject.Destroy(mesh);
		
		// Set up the scaling to ensure we fit exactly inside out rectange
		RectTransform rectTrans =  GetComponent<RectTransform>();
		float xScale = rectTrans.rect.width / bounds.size.x;
		float yScale = rectTrans.rect.height / bounds.size.y;
		float scale = Mathf.Min (xScale, yScale);
		scalingChild.transform.localScale = new Vector3(scale, scale, 1f);
		
	}
	
	void OnDestroy(){
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
