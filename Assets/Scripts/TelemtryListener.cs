using UnityEngine;

public interface TelemetryListener {


	void OnEvent(Telemetry.Event e);
	void OnEvent(Telemetry.Event e, string text);
	
	void OnNewGame();
}
 