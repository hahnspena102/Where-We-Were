using FirebaseWebGL.Scripts.FirebaseBridge;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatabaseLinker : MonoBehaviour
{
	public event Action<Entry> EntryLoadedFromDatabase;

	[Serializable]
	private class FirebaseEntryPayload
	{
		public int id;
		public int promt_id;
		public string answer;
		public string dataPosted;
		public float positionX;
		public float positionY;
		public float positionZ;
		public string spriteFileName;
		public string spriteMimeType;
		public string spriteBase64;
	}

	[SerializeField] private string databasePath = "demo/hello";
	[SerializeField] private string entriesPath = "entries/prompt_0";
	[SerializeField] private bool writeHelloWorldOnStart;
	[SerializeField] private bool autoReconnectOnReadError = true;
	[SerializeField] [Min(0)] private int maxReadRetryAttempts = 5;
	[SerializeField] [Min(0.1f)] private float baseReadRetryDelaySeconds = 1.5f;
	[SerializeField] [Min(0)] private int maxWriteRetryAttempts = 3;
	[SerializeField] [Min(0.1f)] private float writeRetryDelaySeconds = 1.0f;

	private struct PendingWrite
	{
		public string path;
		public string json;
		public int attempt;
	}

	private readonly Queue<PendingWrite> pendingWrites = new Queue<PendingWrite>();
	private PendingWrite activeWrite;
	private bool isWriteInFlight;

	private bool readRequested;
	private bool isListeningForEntries;
	private int currentReadRetryAttempt;

	private void Start()
	{
		if (!writeHelloWorldOnStart)
		{
			return;
		}

		if (!IsRuntimeFirebaseAvailable())
		{
			Debug.LogWarning("Firebase is unavailable. FirebaseWebGL calls only work in a WebGL player build.");
			return;
		}

		// FirebaseDatabase expects JSON, so a string must include quotes.
		EnqueueWrite(databasePath, "\"hello world\"");
	}

	private void OnDisable()
	{
		StopAllCoroutines();
		readRequested = false;
		isListeningForEntries = false;
	}

	public void OnWriteSuccess(string response)
	{
		if (isWriteInFlight)
		{
			Debug.Log($"Firebase write succeeded at '{activeWrite.path}'. Response: {response}");
			isWriteInFlight = false;
			TrySendNextWrite();
			return;
		}

		Debug.Log($"Firebase write succeeded. Response: {response}");
	}

	public void OnWriteError(string error)
	{
		if (!isWriteInFlight)
		{
			Debug.LogError($"Failed writing to Firebase. Error: {error}");
			return;
		}

		if (activeWrite.attempt < maxWriteRetryAttempts)
		{
			activeWrite.attempt++;
			float delay = writeRetryDelaySeconds * Mathf.Pow(2f, activeWrite.attempt - 1);
			Debug.LogWarning($"Firebase write failed at '{activeWrite.path}'. Retrying in {delay:0.0}s (attempt {activeWrite.attempt}/{maxWriteRetryAttempts}). Error: {error}");
			isWriteInFlight = false;
			StartCoroutine(RetryWriteAfterDelay(activeWrite, delay));
			return;
		}

		Debug.LogError($"Failed writing to Firebase at '{activeWrite.path}' after {maxWriteRetryAttempts} retries. Error: {error}");
		isWriteInFlight = false;
		TrySendNextWrite();
	}

	public void WriteEntryToDatabase(Entry entry)
	{
		WriteEntryToDatabase(entry, null, null);
	}

	public void WriteEntryToDatabase(Entry entry, byte[] spritePngBytes, string spriteFileName)
	{
		if (!IsRuntimeFirebaseAvailable())
		{
			Debug.LogWarning("Skipped Firebase write because Firebase is unavailable in this runtime.");
			return;
		}

		FirebaseEntryPayload payload = new FirebaseEntryPayload
		{
			id = entry.id,
			promt_id = entry.promt_id,
			answer = entry.answer,
			dataPosted = entry.dataPosted,
			positionX = entry.position.x,
			positionY = entry.position.y,
			positionZ = entry.position.z,
			spriteFileName = string.IsNullOrWhiteSpace(spriteFileName) ? $"entry_{entry.id}.png" : spriteFileName,
			spriteMimeType = "image/png",
			spriteBase64 = spritePngBytes != null && spritePngBytes.Length > 0 ? Convert.ToBase64String(spritePngBytes) : null
		};

		string json = JsonUtility.ToJson(payload);
		EnqueueWrite($"entries/prompt_{entry.promt_id}/{entry.id}", json);
	}

	public void ReadEntriesFromDatabase()
	{
		if (!IsRuntimeFirebaseAvailable())
		{
			Debug.LogWarning("Skipped Firebase read because Firebase is unavailable in this runtime.");
			return;
		}

		if (string.IsNullOrWhiteSpace(entriesPath))
		{
			Debug.LogWarning("Skipped Firebase read because entries path is not configured.");
			return;
		}

		if (isListeningForEntries)
		{
			return;
		}

		readRequested = true;
		currentReadRetryAttempt = 0;
		StartListeningForEntries();
	}

	public void StopReadingEntriesFromDatabase()
	{
		readRequested = false;
		currentReadRetryAttempt = 0;

		if (!IsRuntimeFirebaseAvailable())
		{
			return;
		}

		if (!isListeningForEntries)
		{
			return;
		}

		FirebaseDatabase.StopListeningForChildAdded(entriesPath, gameObject.name, nameof(OnReadSuccess), nameof(OnReadError));
		isListeningForEntries = false;
	}

	public void OnChildAddedSuccess(string response)
	{
		currentReadRetryAttempt = 0;

		if (string.IsNullOrWhiteSpace(response) || response == "null")
		{
			return;
		}

		Entry parsedEntry = TryParseEntryFromJson(response);
		if (parsedEntry != null)
		{
			EntryLoadedFromDatabase?.Invoke(parsedEntry);
		}
	}

	public void OnReadSuccess(string response)
	{
		Debug.Log($"Read/listen callback from Firebase at '{entriesPath}'. Response: {response}");
	}

	public void OnReadError(string error)
	{
		isListeningForEntries = false;
		Debug.LogError($"Failed reading from Firebase at '{entriesPath}'. Error: {error}");

		if (!autoReconnectOnReadError || !readRequested)
		{
			return;
		}

		if (currentReadRetryAttempt >= maxReadRetryAttempts)
		{
			Debug.LogError($"Stopped retrying Firebase listen after {maxReadRetryAttempts} attempts at '{entriesPath}'.");
			return;
		}

		currentReadRetryAttempt++;
		float delay = baseReadRetryDelaySeconds * Mathf.Pow(2f, currentReadRetryAttempt - 1);
		Debug.LogWarning($"Retrying Firebase listen in {delay:0.0}s (attempt {currentReadRetryAttempt}/{maxReadRetryAttempts}) at '{entriesPath}'.");
		StartCoroutine(RetryReadAfterDelay(delay));
	}

	public bool IsRuntimeFirebaseAvailable()
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		return true;
#else
		return false;
#endif
	}

	public bool HasDatabasePathConfigured()
	{
		return !string.IsNullOrWhiteSpace(databasePath) && !string.IsNullOrWhiteSpace(entriesPath);
	}

	private void EnqueueWrite(string path, string json)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			Debug.LogWarning("Skipped Firebase write because target path is not configured.");
			return;
		}

		pendingWrites.Enqueue(new PendingWrite
		{
			path = path,
			json = json,
			attempt = 0
		});

		TrySendNextWrite();
	}

	private void TrySendNextWrite()
	{
		if (isWriteInFlight || pendingWrites.Count == 0)
		{
			return;
		}

		activeWrite = pendingWrites.Dequeue();
		isWriteInFlight = true;
		FirebaseDatabase.PostJSON(activeWrite.path, activeWrite.json, gameObject.name, nameof(OnWriteSuccess), nameof(OnWriteError));
	}

	private IEnumerator RetryWriteAfterDelay(PendingWrite write, float delay)
	{
		yield return new WaitForSeconds(delay);
		pendingWrites.Enqueue(write);
		TrySendNextWrite();
	}

	private void StartListeningForEntries()
	{
		if (!readRequested || isListeningForEntries)
		{
			return;
		}

		FirebaseDatabase.ListenForChildAdded(entriesPath, gameObject.name, nameof(OnChildAddedSuccess), nameof(OnReadError));
		isListeningForEntries = true;
	}

	private IEnumerator RetryReadAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		StartListeningForEntries();
	}

	private Entry TryParseEntryFromJson(string json)
	{
		try
		{
			FirebaseEntryPayload payload = JsonUtility.FromJson<FirebaseEntryPayload>(json);
			if (payload == null)
			{
				return null;
			}

			Sprite sprite = null;
			if (!string.IsNullOrWhiteSpace(payload.spriteBase64))
			{
				byte[] bytes = Convert.FromBase64String(payload.spriteBase64);
				Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				texture.LoadImage(bytes);
				sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
			}

			return new Entry
			{
				id = payload.id,
				promt_id = payload.promt_id,
				answer = payload.answer,
				dataPosted = payload.dataPosted,
				position = new Vector3(payload.positionX, payload.positionY, payload.positionZ),
				sprite = sprite
			};
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"Failed parsing Firebase entry JSON. {ex.Message}");
			return null;
		}
	}

}
