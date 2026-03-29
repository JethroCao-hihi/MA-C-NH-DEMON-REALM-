using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public struct SceneMusicEntry
{
    public string sceneName;
    public string trackName;
}

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("References")]
    [SerializeField] private MusicLirbary musicLirbary;
    [SerializeField] private AudioSource musicSource;

    [Header("Music Keys")]
    [SerializeField] private string menuTrackName = "Menu";
    [SerializeField] private string game1TrackName = "Game1";
    [SerializeField] private string bossTrackName = "Boss";

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
    [SerializeField] private bool autoPlayByScene = true;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private SceneMusicEntry[] sceneMusicMap;

    private string currentTrack;
    private readonly Dictionary<string, AudioClip> musicCache = new Dictionary<string, AudioClip>();

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

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        if (musicLirbary == null)
            musicLirbary = FindObjectOfType<MusicLirbary>();

        CacheFromLibrary(musicLirbary);

        if (musicLirbary == null)
            Debug.LogWarning("MusicManager: MusicLirbary is not assigned.");
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (!playOnStart) return;

        if (autoPlayByScene)
            PlayMusicForScene(SceneManager.GetActiveScene().name);
        else
            PlayMenuMusic();
    }

    public void PlayMenuMusic() => PlayMusic(menuTrackName);
    public void PlayGame1Music() => PlayMusic(game1TrackName);
    public void PlayBossMusic() => PlayMusic(bossTrackName);

    public void PlayMusic(string trackName)
    {
        if (musicLirbary == null || musicSource == null)
            return;

        if (currentTrack == trackName && musicSource.isPlaying)
            return;

        AudioClip clip = musicLirbary.GetAudioClip(trackName);
        if (clip == null)
        {
            Debug.LogWarning("MusicManager: Track not found: " + trackName);
            return;
        }

        currentTrack = trackName;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (musicLirbary == null)
            musicLirbary = FindObjectOfType<MusicLirbary>();

        CacheFromLibrary(musicLirbary);

        if (!autoPlayByScene) return;
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        if (sceneMusicMap != null)
        {
            for (int i = 0; i < sceneMusicMap.Length; i++)
            {
                if (string.Equals(sceneMusicMap[i].sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    PlayMusic(sceneMusicMap[i].trackName);
                    return;
                }
            }
        }

        if (string.Equals(sceneName, "Menu", System.StringComparison.OrdinalIgnoreCase))
            PlayMenuMusic();
        else if (sceneName.ToLowerInvariant().Contains("boss"))
            PlayBossMusic();
        else
            PlayGame1Music();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;

        musicSource.Stop();
        currentTrack = null;
    }

    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    private void CacheFromLibrary(MusicLirbary library)
    {
        if (library == null || library.Tracks == null)
            return;

        MusicTrack[] tracks = library.Tracks;
        for (int i = 0; i < tracks.Length; i++)
        {
            if (string.IsNullOrEmpty(tracks[i].trackName) || tracks[i].clip == null)
                continue;

            musicCache[tracks[i].trackName] = tracks[i].clip;
        }
    }
}
