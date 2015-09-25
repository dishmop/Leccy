using UnityEngine;
using System.Collections;

public class TutorialObject : MonoBehaviour {
	float startTime;
	public float delay = 1;
	bool visible = true;

	void OnEnable(){
		startTime = Time.time;
		SetChildrenVisible(transform, false);
		visible = false;
	}
	
	void Update(){
		if (!visible && Time.time > startTime + delay){
			SetChildrenVisible(transform, true);
			visible = true;
			
		}
	}
	
	public void SetAlpha(float a){
		SetChildrenAlpha(transform, a);
	}
	
	void SetChildrenVisible(Transform trans, bool visible){
		foreach (Transform child in trans){
			Renderer renderer = child.GetComponent<Renderer>();
			if (renderer != null){
				renderer.enabled = visible;
			}
			SetChildrenVisible(child, visible);
			
		}
	}
	
	void SetChildrenAlpha(Transform trans, float a){
		foreach (Transform child in trans){
			TextMesh textMesh = child.GetComponent<TextMesh>();
			if (textMesh != null){
				Color col = textMesh.color;
				col.a = a;
				textMesh.color = col;
			}
			SpriteRenderer spriteRenderer = child.GetComponent<SpriteRenderer>();
			if (spriteRenderer != null){
				Color col = spriteRenderer.color;
				col.a = a;
				spriteRenderer.color = col;
			}
			SetChildrenAlpha(child, a);
			
		}
	}
	
	

}
