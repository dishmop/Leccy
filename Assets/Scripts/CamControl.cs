using UnityEngine;
using System.Collections;

public class CamControl : MonoBehaviour {

	public GameObject		sidePanel;
	public float 			zoomSpeed;
	public bool				ignoreSide = false;
	Vector3					prevMousePos = new Vector3();
	public float			border = 1;
	
	int jumpCountdown = 0;

	float lerpProp = 0.05f;
	
	public void TriggerJumpView(){
		jumpCountdown = 10;
	}
	
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
		
		if (GameModeManager.singleton.enableEditor){
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
		CentreCamera();
			
		
	}
	
	public void CentreCamera(){
//		Debug.DrawLine(Tutorial.singleton.bounds.min, Tutorial.singleton.bounds.max, Color.red);
//		Debug.DrawLine(Circuit.singleton.bounds.min, Circuit.singleton.bounds.max, Color.green);
		
		
		Bounds allBounds = Tutorial.singleton.bounds;
		
		allBounds.Encapsulate(Circuit.singleton.bounds.min);
		allBounds.Encapsulate(Circuit.singleton.bounds.max);
		
		Rect bounds = new Rect(allBounds.min,allBounds.max - allBounds.min);
		
		if (bounds.width >= 1 || bounds.height >= 1){
		
			// What proportion of the screen is taken up with the side panel
//			float hPropSide  = 0f;
//			if (!ignoreSide){
//				hPropSide = sidePanel.GetComponent<RectTransform>().anchorMax.x;
//			}
//			float propHScreen = (1f-hPropSide);
//			
//			float vPropSide = 0f;
//			if (topPanel.activeSelf){
//				vPropSide = 1f - topPanel.GetComponent<RectTransform>().anchorMin.y;
//			}
//			float propVScreen = (1f-vPropSide);
//			
			// Override this stuff
			float propVScreen = 1;
			float propHScreen = 1;
			

//			Rect camRect = GetComponent<Camera>().rect;
			
//			camRect.x = sideX;
//			camRect.height = topY;	
//				
//			GetComponent<Camera>().rect = camRect;			
			
			
			// This means that the screen's width is actuall 1-that
			float adjustedAspect = propHScreen * Camera.main.aspect /  propVScreen;
		
			// Get range and work out orthographic size			
			float vRange = Mathf.Max (bounds.height + 2 * border, (bounds.width + 2 * border) / adjustedAspect);
			float currentSize = transform.GetComponent<Camera>().orthographicSize;
			float desSize = vRange * 0.5f;
			if (jumpCountdown <= 0){
				transform.GetComponent<Camera>().orthographicSize = Mathf.Lerp(currentSize, desSize, lerpProp);
			}
			else{
				transform.GetComponent<Camera>().orthographicSize = desSize;
			}
			
			
			Vector2 centre = (bounds.min + bounds.max) * 0.5f;
			Vector3 newCamPos = new Vector3(centre.x, centre.y, -10);
			if (jumpCountdown <= 0){
				transform.position = Vector3.Lerp(transform.position , newCamPos, lerpProp);
			}
			else{
				transform.position = newCamPos;
			}
			if (jumpCountdown > 0) jumpCountdown--;
			
		}
	}
}
