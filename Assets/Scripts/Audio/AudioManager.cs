using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Attach to: persistent "AudioManager" GameObject
// Required: 2x AudioSource children (MusicSource, SFXSource)
// Dependencies: SpeedSystem
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    [Header("Music tracks")]
    [SerializeField] AudioClip trackAmbient;   // Track 1 — low tension
    [SerializeField] AudioClip trackTension;   // Track 2 — mid tension
    [SerializeField] AudioClip trackOverload;  // Track 3 — high tension
    [SerializeField] AudioClip trackClone;     // Clone phase
    [SerializeField] AudioClip trackEndScreen; // End screen

    [Header("SFX clips — key must match PlaySFX(key) calls")]
    [SerializeField] SFXEntry[] sfxEntries;

    Dictionary<string, AudioClip> _sfxMap = new();
    AudioClip _currentTrack;
    bool _clonePlaying;

    const string MUSIC_VOLUME_KEY = "MusicVolume";
    const string SFX_VOLUME_KEY   = "SFXVolume";

    public float MusicVolume => musicSource != null ? musicSource.volume : 1f;
    public float SFXVolume   => sfxSource   != null ? sfxSource.volume   : 1f;

    [System.Serializable]
    public struct SFXEntry
    {
        public string   key;
        public AudioClip clip;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var e in sfxEntries)
            if (!string.IsNullOrEmpty(e.key) && e.clip != null)
                _sfxMap[e.key] = e.clip;

        if (musicSource != null) musicSource.volume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        if (sfxSource   != null) sfxSource.volume   = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
    }

    public void SetMusicVolume(float v)
    {
        if (musicSource != null) musicSource.volume = v;
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, v);
    }

    public void SetSFXVolume(float v)
    {
        if (sfxSource != null) sfxSource.volume = v;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, v);
    }

    void OnEnable()
    {
        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged += OnSpeedChanged;
    }

    void OnDisable()
    {
        if (SpeedSystem.Instance != null)
            SpeedSystem.Instance.OnSpeedChanged -= OnSpeedChanged;
    }

    public void PlaySFX(string key)
    {
        if (_sfxMap.TryGetValue(key, out var clip))
        {
            // Slight random pitch per play — the dot pickup sound alone can fire dozens of
            // times a level; identical pitch every time reads as a repetitive machine-gun.
            sfxSource.pitch = Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null) return;
        if (_currentTrack == clip && musicSource.isPlaying) return;
        _currentTrack = clip;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlayClonePhaseMusic()
    {
        if (_clonePlaying) return;
        StartCoroutine(CloneMusicCoroutine());
    }

    IEnumerator CloneMusicCoroutine()
    {
        _clonePlaying = true;
        var normal = _currentTrack;
        PlayMusic(trackClone, false);
        yield return new WaitForSeconds(8f);
        _clonePlaying = false;
        if (normal != null) PlayMusic(normal);
    }

    void OnSpeedChanged(float m)
    {
        if (_clonePlaying) return;
        if (m >= 2.2f)      PlayMusic(trackOverload);
        else if (m >= 1.5f) PlayMusic(trackTension);
        else                PlayMusic(trackAmbient);
    }

    public void PlayEndScreenMusic()  => PlayMusic(trackEndScreen, true);
    public void PlayAmbientMusic()    => PlayMusic(trackAmbient, true);
}
