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
            sfxSource.PlayOneShot(clip);
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
