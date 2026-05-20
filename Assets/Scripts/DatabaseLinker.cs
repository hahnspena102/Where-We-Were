using FirebaseWebGL.Scripts.FirebaseBridge;
using System;
using System.Collections;
using System.Collections.Generic;
using FullSerializer;
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
	private string entriesPath = "";
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
		


		if (!IsRuntimeFirebaseAvailable())
		{
			Debug.LogWarning("Firebase is unavailable. FirebaseWebGL calls only work in a WebGL player build.");
			return;
		}

		// FirebaseDatabase expects JSON, so a string must include quotes.
		EnqueueWrite(databasePath, "\"hello world\"");
	}

	private void Awake()
	{
		// Override any serialized entriesPath in scene YAML with the GameManager's current prompt path.
		var gm = FindAnyObjectByType<GameManager>();
		if (gm != null && gm.CurrentPromptData != null && !string.IsNullOrWhiteSpace(gm.CurrentPromptData.DatabasePath))
		{
			entriesPath = gm.CurrentPromptData.DatabasePath;
			Debug.Log($"DatabaseLinker.Awake overriding entriesPath with GameManager.CurrentPromptData.DatabasePath='{entriesPath}'");
		}
		else
		{
			Debug.Log("DatabaseLinker.Awake: GameManager.CurrentPromptData.DatabasePath not available to override entriesPath.");
		}
	}

	private void Update()
	{
		// Keep entriesPath in sync with the GameManager's current prompt path.
		var gm = FindAnyObjectByType<GameManager>();
		if (gm == null || gm.CurrentPromptData == null)
		{
			return;
		}

		string gmPath = gm.CurrentPromptData.DatabasePath;
		if (string.IsNullOrWhiteSpace(gmPath))
		{
			return;
		}

		if (entriesPath != gmPath)
		{
			string previous = entriesPath;
			entriesPath = gmPath;
			Debug.Log($"DatabaseLinker.Update: entriesPath updated to '{entriesPath}' from GameManager (previous '{previous}')");

			if (isListeningForEntries)
			{
				Debug.Log("DatabaseLinker.Update: entriesPath changed while listening; restarting listener.");
				// Stop listening on the previous path and restart for the new path.
				if (!string.IsNullOrWhiteSpace(previous))
				{
					FirebaseDatabase.StopListeningForChildAdded(previous, gameObject.name, nameof(OnChildAddedSuccess), nameof(OnReadError));
				}
				isListeningForEntries = false;
				if (readRequested)
				{
					StartListeningForEntries();
				}
			}
		}
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
		EnqueueWrite($"{entriesPath}/{entry.id}", json);
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
			Debug.Log($"ReadEntriesFromDatabase called but entriesPath is empty. Current entriesPath='{entriesPath}'");
			return;
		}

		if (isListeningForEntries)
		{
			return;
		}

		Debug.Log($"ReadEntriesFromDatabase called. entriesPath='{entriesPath}', isListeningForEntries={isListeningForEntries}");
		readRequested = true;
		currentReadRetryAttempt = 0;
		FirebaseDatabase.GetJSON(entriesPath, gameObject.name, nameof(OnReadSnapshotSuccess), nameof(OnReadError));
		StartListeningForEntries();
	}

	public void SetEntriesPath(string path)
	{
		entriesPath = path;
		Debug.Log($"DatabaseLinker entriesPath set to '{entriesPath}'");
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

		FirebaseDatabase.StopListeningForChildAdded(entriesPath, gameObject.name, nameof(OnChildAddedSuccess), nameof(OnReadError));
		isListeningForEntries = false;
	}

	public void OnChildAddedSuccess(string response)
	{
		currentReadRetryAttempt = 0;

		if (string.IsNullOrWhiteSpace(response) || response == "null")
		{
			return;
		}

		Debug.Log($"OnChildAddedSuccess received response: {response}");
		Entry parsedEntry = TryParseEntryFromJson(response);
		if (parsedEntry != null)
		{
			Debug.Log($"Parsed remote entry id={parsedEntry.id} promt_id={parsedEntry.promt_id} position={parsedEntry.position} sprite={(parsedEntry.sprite!=null?"yes":"no")}");
			EntryLoadedFromDatabase?.Invoke(parsedEntry);
		}
	}

	public void OnReadSuccess(string response)
	{
		Debug.Log($"Read/listen callback from Firebase at '{entriesPath}'. Response: {response}");
	}

	public void OnReadSnapshotSuccess(string response)
	{
		if (string.IsNullOrWhiteSpace(response) || response == "null")
		{
			Debug.Log($"OnReadSnapshotSuccess received empty response for '{entriesPath}'.");
			return;
		}

		Debug.Log($"OnReadSnapshotSuccess received snapshot for '{entriesPath}': {response}");

		try
		{
			fsData parsedData = fsJsonParser.Parse(response);
			if (parsedData == null || parsedData.IsNull)
			{
				Debug.LogWarning($"OnReadSnapshotSuccess parsed null data for '{entriesPath}'.");
				return;
			}

			if (parsedData.IsDictionary)
			{
				Dictionary<string, fsData> children = parsedData.AsDictionary;
				Debug.Log($"Snapshot for '{entriesPath}' contained {children.Count} child nodes.");

				foreach (KeyValuePair<string, fsData> child in children)
				{
					if (child.Value == null || child.Value.IsNull)
					{
						continue;
					}

					Entry parsedEntry = TryParseEntryFromJson(fsJsonPrinter.CompressedJson(child.Value));
					if (parsedEntry != null)
					{
						Debug.Log($"Snapshot entry parsed from '{entriesPath}/{child.Key}' -> id={parsedEntry.id} promt_id={parsedEntry.promt_id}");
						EntryLoadedFromDatabase?.Invoke(parsedEntry);
					}
				}
				return;
			}

			if (parsedData.IsList)
			{
				List<fsData> children = parsedData.AsList;
				Debug.Log($"Snapshot for '{entriesPath}' contained a list with {children.Count} items.");

				for (int i = 0; i < children.Count; i++)
				{
					fsData childData = children[i];
					if (childData == null || childData.IsNull)
					{
						continue;
					}

					Entry parsedEntry = TryParseEntryFromJson(fsJsonPrinter.CompressedJson(childData));
					if (parsedEntry != null)
					{
						Debug.Log($"Snapshot entry parsed from '{entriesPath}[{i}]' -> id={parsedEntry.id} promt_id={parsedEntry.promt_id}");
						EntryLoadedFromDatabase?.Invoke(parsedEntry);
					}
				}
				return;
			}

			Debug.LogWarning($"Snapshot at '{entriesPath}' was neither an object nor a list.");
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"Failed parsing Firebase snapshot at '{entriesPath}'. {ex.Message}");
		}
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
		Debug.Log($"StartListeningForEntries invoked. entriesPath='{entriesPath}', readRequested={readRequested}, isListeningForEntries={isListeningForEntries}");

		if (!readRequested || isListeningForEntries)
		{
			return;
		}

		Debug.Log($"Beginning Firebase ListenForChildAdded on '{entriesPath}'");
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

			Debug.Log($"TryParseEntryFromJson payload id={payload.id} promt_id={payload.promt_id} hasSpriteBase64={(string.IsNullOrWhiteSpace(payload.spriteBase64)?"no":"yes")}");

			Sprite sprite = null;
			if (!string.IsNullOrWhiteSpace(payload.spriteBase64))
			{
				byte[] bytes = Convert.FromBase64String(payload.spriteBase64);
				Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				texture.LoadImage(bytes);
				Debug.Log($"Loaded texture from base64: {texture.width}x{texture.height}");
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
