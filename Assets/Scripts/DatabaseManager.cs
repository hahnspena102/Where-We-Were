using FirebaseWebGL.Scripts.FirebaseBridge;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
	[SerializeField] private string databasePath = "demo/hello";

	private void Start()
	{
		if (Application.platform != RuntimePlatform.WebGLPlayer)
		{
			Debug.LogWarning("FirebaseWebGL calls only work in a WebGL build.");
			return;
		}

		// FirebaseDatabase expects JSON, so a string must include quotes.
		FirebaseDatabase.PostJSON(databasePath, "\"hello world\"", gameObject.name, nameof(OnWriteSuccess),
			nameof(OnWriteError));
	}

	public void OnWriteSuccess(string response)
	{
		Debug.Log($"Wrote 'hello world' to '{databasePath}'. Response: {response}");
	}

	public void OnWriteError(string error)
	{
		Debug.LogError($"Failed writing to Firebase at '{databasePath}'. Error: {error}");
	}
}
