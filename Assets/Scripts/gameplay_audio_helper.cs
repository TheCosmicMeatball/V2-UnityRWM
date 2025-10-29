using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper methods for triggering gameplay-specific audio events.
/// These can be called from screen scripts at the appropriate moments.
/// </summary>
public static class GameplayAudioHelper
{
    // === PLAYER ACTIONS ===
    
    /// <summary>
    /// Call when a player joins the lobby (on mobile device)
    /// </summary>
    public static void OnPlayerJoined()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJoinSFX();
        }
    }
    
    /// <summary>
    /// Call when a player submits their text answer during Question phase (on mobile device)
    /// </summary>
    public static void OnAnswerSubmitted()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTextInputReceivedSFX();
        }
    }
    
    /// <summary>
    /// Call when a player icon appears or updates on desktop (lobby or question screen)
    /// This cycles through 3 different pop sounds
    /// </summary>
    public static void OnPlayerIconUpdate()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPlayerIconPopSFX();
        }
    }
    
    // === RESULTS SCREEN ===
    
    /// <summary>
    /// Call on mobile device for the winning player at Halftime or Final Results
    /// Pass the local player's ID and the winner's ID
    /// </summary>
    public static void OnWinnerRevealed(string localPlayerID, string winnerPlayerID)
    {
        if (AudioManager.Instance == null) return;
        
        // Only play on the winning player's mobile device
        if (localPlayerID == winnerPlayerID)
        {
            AudioManager.Instance.PlayWinSFX();
        }
    }
    
    /// <summary>
    /// Call on mobile device for the losing player at Halftime or Final Results
    /// Pass the local player's ID and the loser's ID
    /// </summary>
    public static void OnLoserRevealed(string localPlayerID, string loserPlayerID)
    {
        if (AudioManager.Instance == null) return;
        
        // Only play on the losing player's mobile device
        if (localPlayerID == loserPlayerID)
        {
            AudioManager.Instance.PlayLoseSFX();
        }
    }
    
    // === TIMER ===
    
    /// <summary>
    /// Call this in Update() on timed screens to automatically handle the timer loop
    /// The loop will start at 10 seconds remaining and stop when time expires
    /// </summary>
    public static void UpdateTimerAudio()
    {
        if (AudioManager.Instance != null && GameManager.Instance != null)
        {
            float timeRemaining = GameManager.Instance.GetTimeRemaining();
            AudioManager.Instance.CheckTimerWarning(timeRemaining);
        }
    }
}

/// <summary>
/// Extension methods for easy audio triggering from UI components
/// </summary>
public static class AudioExtensions
{
    /// <summary>
    /// Add this to a Button's onClick event to play desktop button sound
    /// </summary>
    public static void PlayButtonClick(this Button button)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDesktopButtonSFX();
        }
    }
}
