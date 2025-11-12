using UnityEngine;

/// <summary>
/// Attach this to each screen to automatically play the correct music when the scene loads.
/// Also handles timer loop integration.
/// </summary>
public class ScreenAudioTrigger : MonoBehaviour
{
    [Header("Screen Type")]
    public ScreenType screenType;
    
    [Header("Auto-Play Settings")]
    public bool playMusicOnStart = true;
    public bool monitorTimer = false;
    
    public enum ScreenType
    {
        LandingPage,
        Lobby,
        IntroVideo,
        RoundArt,
        Question,
        PlayerQuestion,
        PictureQuestion,
        Elimination,
        Voting,
        RoundResults,
        Halftime,
        BonusIntro,
        BonusQuestion,
        BonusResults,
        FinalResults,
        Credits,
        Loading
    }
    
    void Start()
    {
        if (playMusicOnStart)
        {
            PlayScreenMusic();
        }
    }
    
    void Update()
    {
        if (monitorTimer && AudioManager.Instance != null && GameManager.Instance != null)
        {
            // Monitor timer and trigger warning loop at 10 seconds
            float timeRemaining = GameManager.Instance.GetTimeRemaining();
            AudioManager.Instance.CheckTimerWarning(timeRemaining);
        }
    }
    
    void PlayScreenMusic()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioManager not found");
            return;
        }
        
        switch (screenType)
        {
            case ScreenType.LandingPage:
                AudioManager.Instance.PlayLandingPageMusic();
                break;
                
            case ScreenType.Lobby:
                AudioManager.Instance.PlayLandingPageMusic(); // Same as landing
                break;
                
            case ScreenType.IntroVideo:
                AudioManager.Instance.StopMusic(); // Video has its own audio
                break;
                
            case ScreenType.RoundArt:
                AudioManager.Instance.PlayRoundArtMusic();
                break;
                
            case ScreenType.Question:
            case ScreenType.PlayerQuestion:
            case ScreenType.PictureQuestion:
                AudioManager.Instance.PlayQuestionMusic();
                monitorTimer = true; // Enable timer monitoring
                break;
                
            case ScreenType.Elimination:
                AudioManager.Instance.PlayEliminationMusic();
                monitorTimer = true; // Enable timer monitoring
                break;
                
            case ScreenType.Voting:
                AudioManager.Instance.PlayVotingMusic();
                monitorTimer = true; // Enable timer monitoring
                break;
                
            case ScreenType.RoundResults:
                AudioManager.Instance.PlayResultsMusic();
                AudioManager.Instance.StopTimerLoop(); // Ensure timer loop stops
                break;
                
            case ScreenType.Halftime:
                AudioManager.Instance.PlayHalftimeMusic();
                break;
                
            case ScreenType.BonusIntro:
                AudioManager.Instance.PlayBonusIntroMusic();
                break;
                
            case ScreenType.BonusQuestion:
                AudioManager.Instance.PlayBonusQuestionMusic();
                monitorTimer = true; // Enable timer monitoring
                break;
                
            case ScreenType.BonusResults:
                AudioManager.Instance.PlayBonusResultsMusic();
                break;
                
            case ScreenType.FinalResults:
                AudioManager.Instance.PlayFinalResultsMusic();
                break;
                
            case ScreenType.Credits:
                AudioManager.Instance.PlayCreditsMusic();
                break;
                
            case ScreenType.Loading:
                AudioManager.Instance.StopMusic();
                break;
        }
    }
    
    void OnDestroy()
    {
        // Stop timer loop when leaving timed screens
        if (monitorTimer && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopTimerLoop();
        }
    }
}
