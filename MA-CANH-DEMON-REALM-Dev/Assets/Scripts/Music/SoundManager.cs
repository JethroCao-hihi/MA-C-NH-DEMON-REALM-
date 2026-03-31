using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("References")]
    [SerializeField] private SoundLibrary soundLibrary;
    [SerializeField] private AudioSource sfxSource;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private readonly Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null)
                transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;

        if (soundLibrary == null)
            soundLibrary = FindObjectOfType<SoundLibrary>();

        CacheFromLibrary(soundLibrary);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (soundLibrary == null)
            soundLibrary = FindObjectOfType<SoundLibrary>();

        CacheFromLibrary(soundLibrary);
    }

    public void PlaySfx(string sfxName)
    {
        if (sfxSource == null)
            return;

        if (string.IsNullOrEmpty(sfxName))
            return;

        if (!sfxCache.TryGetValue(sfxName, out AudioClip clip) || clip == null)
        {
            if (soundLibrary == null)
                soundLibrary = FindObjectOfType<SoundLibrary>();

            CacheFromLibrary(soundLibrary);
            sfxCache.TryGetValue(sfxName, out clip);
        }

        if (clip == null)
        {
            Debug.LogWarning("SoundManager: SFX not found: " + sfxName);
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayPlayerSfx(string sfxName) => PlaySfx("Player_" + sfxName);
    public void PlayEnemySfx(string sfxName) => PlaySfx("Enemy_" + sfxName);
    public void PlayBossSfx(string sfxName) => PlaySfx("Boss_" + sfxName);

    public void SetVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    private void CacheFromLibrary(SoundLibrary library)
    {
        if (library == null || library.Tracks == null)
            return;

        SoundTrack[] tracks = library.Tracks;
        for (int i = 0; i < tracks.Length; i++)
        {
            if (string.IsNullOrEmpty(tracks[i].sfxName) || tracks[i].clip == null)
                continue;

            sfxCache[tracks[i].sfxName] = tracks[i].clip;
        }
    }
}
