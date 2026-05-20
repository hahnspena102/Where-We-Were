using UnityEngine;

[System.Serializable]
public class SoundtrackEntry
{
    public AudioClip clip;
    public string name;
    public float volume = 1f;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    [SerializeField] private AudioSource soundFXObject;
    [SerializeField] private AudioSource soundtrackSource;
    [SerializeField]private SoundtrackEntry[] soundtrackEntries;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        if (soundtrackSource != null)
        {
            soundtrackSource.loop = true;
        }
    }

    public void PlaySoundFXClip(AudioClip clip, Transform transform, float volume = 1f)
    {
        if (clip == null || soundFXObject == null)
        {
            Debug.LogWarning("SoundFX clip or SoundFX object is not assigned.");
            return;
        }
        
        AudioSource audioSource = Instantiate(soundFXObject, transform.position, Quaternion.identity);
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        Destroy(audioSource.gameObject, clip.length);
    }

    public void PlayMusic(string name)
    {
        SoundtrackEntry entry = System.Array.Find(soundtrackEntries, e => e.name == name);
        
        if (entry != null && soundtrackSource != null)
        {
            if (soundtrackSource.clip == entry.clip && soundtrackSource.isPlaying)
            {
                return;
            }

            soundtrackSource.clip = entry.clip;
            soundtrackSource.volume = entry.volume;
            soundtrackSource.loop = true;
            soundtrackSource.Play();
        }
        else
        {
            Debug.LogWarning($"Soundtrack with name '{name}' not found or Soundtrack source is not assigned.");
        }
    }
}
