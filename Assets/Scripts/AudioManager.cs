using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips")]
    public AudioClip pelletClip;
    public AudioClip powerPelletClip;
    public AudioClip cherryClip;
    public AudioClip ghostEatClip;
    public AudioClip playerCaughtClip;
    public AudioClip switchModeClip;
    public AudioClip raveStartClip;
    public AudioClip gameOverClip;

    [Header("Music")]
    public AudioClip normalMusic;
    public AudioClip raveMusic;
    public AudioClip switchMusic;

    [Header("Volumes")]
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    private AudioSource _sfxSource;
    private AudioSource _musicSource;

    void Awake(){
        if (Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _sfxSource = GetComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;
    }

    void OnEnable(){
        GameEvents.OnPelletEaten += HandlePelletEaten;
        GameEvents.OnPowerPelletEaten += HandlePowerPelletEaten;
        GameEvents.OnCherryEaten += HandleCherryEaten;
        GameEvents.OnGhostEaten += HandleGhostEaten;
        GameEvents.OnPlayerCaught += HandlePlayerCaught;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnEnterNormalMode += HandleEnterNormalMode;
        GameEvents.OnEnterRaveMode += HandleEnterRaveMode;
        GameEvents.OnEnterSwitchMode += HandleEnterSwitchMode;
    }

    void OnDisable(){
        GameEvents.OnPelletEaten -= HandlePelletEaten;
        GameEvents.OnPowerPelletEaten -= HandlePowerPelletEaten;
        GameEvents.OnCherryEaten -= HandleCherryEaten;
        GameEvents.OnGhostEaten -= HandleGhostEaten;
        GameEvents.OnPlayerCaught -= HandlePlayerCaught;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnEnterNormalMode -= HandleEnterNormalMode;
        GameEvents.OnEnterRaveMode -= HandleEnterRaveMode;
        GameEvents.OnEnterSwitchMode -= HandleEnterSwitchMode;
    }

    private void HandlePelletEaten() => Play(pelletClip);
    private void HandlePowerPelletEaten() => Play(powerPelletClip);
    private void HandleCherryEaten() => Play(cherryClip);
    private void HandleGhostEaten() => Play(ghostEatClip);
    private void HandlePlayerCaught() => Play(playerCaughtClip);
    private void HandleGameOver() => Play(gameOverClip);
    private void HandleEnterNormalMode() => PlayMusic(normalMusic);
    private void HandleEnterRaveMode(){
        Play(raveStartClip);
        PlayMusic(raveMusic);
    }
    private void HandleEnterSwitchMode(){
        Play(switchModeClip);
        PlayMusic(switchMusic);
    }

    public void Play(AudioClip clip){
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayMusic(AudioClip clip){
        if (clip == null) return;
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;
        _musicSource.clip = clip;
        _musicSource.volume = musicVolume;
        _musicSource.Play();
    }

    public void UpdateMusicVolume(float value){
        _musicSource.volume = value;
    }
}