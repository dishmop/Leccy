using UnityEngine;
using System.Collections;

public class CamControl : MonoBehaviour {


	public float 	zoomSpeed;
	Vector3			prevMousePos = new Vector3();
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		
		Camera camera = gameObject.GetComponent<Camera>();
		float wheelVal = Input.GetAxis("Mouse ScrollWheel");
		
		// We need to offset the camera so that the mouse pointer 
		// remains in the same position - in which case, get its current position in world space
		Vector3 mousePos = Input.mousePosition;
		mousePos.z = transform.position.z - Camera.main.transform.position.z;
		
		
		if (wheelVal != 0){
			mousePos.z = transform.position.z - Camera.main.transform.position.z;
			Vector3 mouseOldWorldPos = camera.ScreenToWorldPoint( mousePos);

			camera.orthographicSize *= Mathf.Pow (zoomSpeed, -wheelVal);
		
			Vector3 mouseNewWorldPos = camera.ScreenToWorldPoint( mousePos);
			
			// Offset the camera so that the mouse is pointing at the same world position
			Vector3 camPos = gameObject.transform.position;
			camPos += (mouseOldWorldPos - mouseNewWorldPos);
			gameObject.transform.position = camPos;
		}
		
		
		if (Input.GetMouseButton(1) || (Input.GetMouseButton(0) && Input.GetKey (KeyCode.LeftControl))){
			Vector3 mouseOldWorldPos = camera.ScreenToWorldPoint( prevMousePos);
			Vector3 mouseNewWorldPos = camera.ScreenToWorldPoint( mousePos);
			
			// Offset the camera so that the mouse is pointing at the same world position
			Vector3 camPos = gameObject.transform.position;
			camPos += (mouseOldWorldPos - mouseNewWorldPos);
			gameObject.transform.position = camPos;
			
		
		}
		prevMousePos = mousePos;
			
		
	}
	
	public void CentreCamera(){
		Rect bounds = Circuit.singleton.bounds;
		Vector2 centre = new Vector2((bounds.xMin + bounds.xMax) / 2f, (bounds.yMin + bounds.yMax) / 2f);
		Vector3 newCamPos = new Vector3(centre.x, centre.y, -10);
		transform.position = newCamPos;
		
		float range = Mathf.Max (bounds.height, bounds.width) + 2;

		transform.GetComponent<Camera>().orthographicSize = range * 0.55f;
	}
}
