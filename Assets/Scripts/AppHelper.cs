using UnityEngine;
using System.Collections;

//C#
public static class AppHelper
{
	public static string webplayerQuitURL = "http://i-want-to-study-engineering.org/game/rating/leccy/";
	
	public static void Quit()
	{
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#elif UNITY_WEBPLAYER
		Application.OpenURL(webplayerQuitURL);
		#else
		Application.OpenURL(webplayerQuitURL);
		Application.Quit();
		#endif
	}
}