using UnityEngine;
using System.Collections;

//C#
public static class AppHelper
{
	public static string webplayerQuitURL = "http://google.com";

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