using UnityEngine;
using System.Collections;

public class CamControl : MonoBehaviour {


	public GameObject		sidePanel;
	public GameObject		topPanel;
	public float 			zoomSpeed;
	public bool				ignoreSide = false;
	Vector3					prevMousePos = new Vector3();
	
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
			
			float sideX = sidePanel.GetComponent<RectTransform>().anchorMax.x;
			float topY = 1;
			if (topPanel.activeSelf){
				topY = topPanel.GetComponent<RectTransform>().anchorMin.y;
			}
			Rect camRect = GetComponent<Camera>().rect;
			camRect.x = sideX;
			camRect.height = topY;		
			GetComponent<Camera>().rect = camRect;			
			
			
			// This means that the screen's width is actuall 1-that
			float adjustedAspect = propHScreen * Camera.main.aspect /  propVScreen;
		
			// Get range and work out orthographic size			
			float border = 1;
			float vRange = Mathf.Max (bounds.height + 2 * border, (bounds.width + 2 * border) / adjustedAspect);
			transform.GetComponent<Camera>().orthographicSize = vRange * 0.5f;
			
			// calc how much extra of the scene to add on to the left to ensure we are looking in the middle
			float extraX = (bounds.width + 2 * border) *(1f/propHScreen - 1);
			float extraY = (bounds.height + 2 * border) *(1f/propVScreen - 1);
			
			Vector2 centre = new Vector2(((bounds.xMin - extraX) + bounds.xMax) / 2f, (bounds.yMin + extraY + bounds.yMax) / 2f);
			Vector3 newCamPos = new Vector3(centre.x, centre.y, -10);
			transform.position = newCamPos;
			
		}
	}
}
