using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource timerLoopSource;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    
    [Header("Music Clips")]
    private AudioClip music30Sec;
    private AudioClip music60Sec;
    private AudioClip musicBonus;
    private AudioClip musicCredits;
    private AudioClip musicFinalResults;
    private AudioClip musicLandingPage;
    private AudioClip musicResults;
    
    [Header("SFX Clips")]
    private AudioClip sfxLose;
    private AudioClip sfxJoin;
    private AudioClip sfxTextInputReceived;
    private AudioClip sfxWin;
    private AudioClip sfxPlayerIconPop1;
    private AudioClip sfxPlayerIconPop2;
    private AudioClip sfxPlayerIconPop3;
    private AudioClip sfxTimerLoop;
    private AudioClip sfxDesktopButton;
    private AudioClip sfxButtonSwitch;
    
    [Header("State")]
    private bool isTimerLoopPlaying = false;
    private int playerIconPopIndex = 0;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create audio sources if they don't exist
            CreateAudioSources();
            
            // Load all audio clips
            LoadAllAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void CreateAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        
        if (timerLoopSource == null)
        {
            GameObject timerObj = new GameObject("TimerLoopSource");
            timerObj.transform.SetParent(transform);
            timerLoopSource = timerObj.AddComponent<AudioSource>();
            timerLoopSource.loop = true;
            timerLoopSource.playOnAwake = false;
        }
    }
    
    void LoadAllAudio()
    {
        // Load Music
        music30Sec = Resources.Load<AudioClip>("audio/music/30sec");
        music60Sec = Resources.Load<AudioClip>("audio/music/60sec");
        musicBonus = Resources.Load<AudioClip>("audio/music/bonus");
        musicCredits = Resources.Load<AudioClip>("audio/music/credits");
        musicFinalResults = Resources.Load<AudioClip>("audio/music/finalresults");
        musicLandingPage = Resources.Load<AudioClip>("audio/music/landingpage");
        musicResults = Resources.Load<AudioClip>("audio/music/results");
        
        // Load SFX
        sfxLose = Resources.Load<AudioClip>("audio/sfx/Lose");
        sfxJoin = Resources.Load<AudioClip>("audio/sfx/Join");
        sfxTextInputReceived = Resources.Load<AudioClip>("audio/sfx/TextInputReceived");
        sfxWin = Resources.Load<AudioClip>("audio/sfx/Win");
        sfxPlayerIconPop1 = Resources.Load<AudioClip>("audio/sfx/PlayerIconPop1");
        sfxPlayerIconPop2 = Resources.Load<AudioClip>("audio/sfx/PlayerIconPop2");
        sfxPlayerIconPop3 = Resources.Load<AudioClip>("audio/sfx/PlayerIconPop3");
        sfxTimerLoop = Resources.Load<AudioClip>("audio/sfx/TimerLoop");
        sfxDesktopButton = Resources.Load<AudioClip>("audio/sfx/DesktopButton");
        sfxButtonSwitch = Resources.Load<AudioClip>("audio/sfx/ButtonSwitch");
        
        Debug.Log("AudioManager: All audio clips loaded");
    }
    
    void Update()
    {
        // Update volumes
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
        
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
        
        if (timerLoopSource != null)
        {
            timerLoopSource.volume = sfxVolume;
        }
    }
    
    // === MUSIC PLAYBACK ===
    
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        
        // Only play music on desktop
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            return;
        }
        
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return; // Already playing this clip
        }
        
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }
    
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
    
    public void FadeOutMusic(float duration = 1f)
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            StartCoroutine(FadeOut(musicSource, duration));
        }
    }
    
    IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        
        source.Stop();
        source.volume = startVolume;
    }
    
    // === SCREEN-SPECIFIC MUSIC ===
    
    public void PlayLandingPageMusic()
    {
        PlayMusic(musicLandingPage);
    }
    
    public void PlayQuestionMusic()
    {
        // 60 second music for Question screens
        PlayMusic(music60Sec);
    }
    
    public void PlayEliminationMusic()
    {
        // 30 second music for Elimination screen
        PlayMusic(music30Sec);
    }
    
    public void PlayVotingMusic()
    {
        // 30 second music for Voting screen
        PlayMusic(music30Sec);
    }
    
    public void PlayRoundArtMusic()
    {
        // Credits music for all Round Art screens
        PlayMusic(musicCredits);
    }
    
    public void PlayResultsMusic()
    {
        PlayMusic(musicResults);
    }
    
    public void PlayHalftimeMusic()
    {
        // Final results music used for Halftime
        PlayMusic(musicFinalResults);
    }
    
    public void PlayBonusIntroMusic()
    {
        PlayMusic(musicBonus);
    }
    
    public void PlayBonusQuestionMusic()
    {
        PlayMusic(musicBonus);
    }
    
    public void PlayBonusResultsMusic()
    {
        PlayMusic(musicBonus);
    }
    
    public void PlayFinalResultsMusic()
    {
        PlayMusic(musicFinalResults);
    }
    
    public void PlayCreditsMusic()
    {
        PlayMusic(musicCredits);
    }
    
    // === SFX PLAYBACK ===
    
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
    
    // === SPECIFIC SFX ===
    
    public void PlayWinSFX()
    {
        // Only play on mobile for winning player
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            PlaySFX(sfxWin);
        }
    }
    
    public void PlayLoseSFX()
    {
        // Only play on mobile for losing player
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            PlaySFX(sfxLose);
        }
    }
    
    public void PlayJoinSFX()
    {
        // Play on mobile when player joins
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            PlaySFX(sfxJoin);
        }
    }
    
    public void PlayTextInputReceivedSFX()
    {
        // Play on mobile when player submits answer
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            PlaySFX(sfxTextInputReceived);
        }
    }
    
    public void PlayPlayerIconPopSFX()
    {
        // Only play on desktop
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            return;
        }
        
        // Cycle through the 3 pop sounds
        AudioClip popClip = null;
        
        switch (playerIconPopIndex)
        {
            case 0:
                popClip = sfxPlayerIconPop1;
                break;
            case 1:
                popClip = sfxPlayerIconPop2;
                break;
            case 2:
                popClip = sfxPlayerIconPop3;
                break;
        }
        
        PlaySFX(popClip);
        
        // Cycle to next pop sound
        playerIconPopIndex = (playerIconPopIndex + 1) % 3;
    }
    
    public void PlayDesktopButtonSFX()
    {
        // Only play on desktop
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            return;
        }
        
        PlaySFX(sfxDesktopButton);
    }
    
    public void PlayButtonSwitchSFX()
    {
        // Only play on desktop (8Q/12Q switch)
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            return;
        }
        
        PlaySFX(sfxButtonSwitch);
    }
    
    // === TIMER LOOP ===
    
    public void StartTimerLoop()
    {
        // Timer loop only plays on desktop
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            return;
        }
        
        if (timerLoopSource != null && sfxTimerLoop != null && !isTimerLoopPlaying)
        {
            timerLoopSource.clip = sfxTimerLoop;
            timerLoopSource.loop = true;
            timerLoopSource.Play();
            isTimerLoopPlaying = true;
        }
    }
    
    public void StopTimerLoop()
    {
        if (timerLoopSource != null && isTimerLoopPlaying)
        {
            timerLoopSource.Stop();
            isTimerLoopPlaying = false;
        }
    }
    
    public void CheckTimerWarning(float timeRemaining)
    {
        // Only on desktop
        if (DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile())
        {
            return;
        }
        
        // Start timer loop at 10 seconds
        if (timeRemaining <= 10f && timeRemaining > 0f && !isTimerLoopPlaying)
        {
            StartTimerLoop();
        }
        else if (timeRemaining > 10f && isTimerLoopPlaying)
        {
            StopTimerLoop();
        }
        else if (timeRemaining <= 0f && isTimerLoopPlaying)
        {
            StopTimerLoop();
        }
    }
    
    // === VOLUME CONTROL ===
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }
    
    public void MuteMusic()
    {
        if (musicSource != null)
        {
            musicSource.mute = true;
        }
    }
    
    public void UnmuteMusic()
    {
        if (musicSource != null)
        {
            musicSource.mute = false;
        }
    }
    
    public void MuteSFX()
    {
        if (sfxSource != null)
        {
            sfxSource.mute = true;
        }
        
        if (timerLoopSource != null)
        {
            timerLoopSource.mute = true;
        }
    }
    
    public void UnmuteSFX()
    {
        if (sfxSource != null)
        {
            sfxSource.mute = false;
        }
        
        if (timerLoopSource != null)
        {
            timerLoopSource.mute = false;
        }
    }
    
    // === CLEANUP ===
    
    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
