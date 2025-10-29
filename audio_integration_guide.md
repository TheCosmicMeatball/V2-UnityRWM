# AUDIO SYSTEM INTEGRATION GUIDE

## File Structure Setup

Place all audio files in Unity at: `Assets/Resources/audio/`

```
Assets/Resources/audio/
├── music/
│   ├── 30sec.mp3
│   ├── 60sec.mp3
│   ├── bonus.mp3
│   ├── credits.mp3
│   ├── finalresults.mp3
│   ├── landingpage.mp3
│   └── results.mp3
├── sfx/
│   ├── Lose.mp3
│   ├── Join.mp3
│   ├── TextInputReceived.mp3
│   ├── Win.mp3
│   ├── PlayerIconPop1.mp3
│   ├── PlayerIconPop2.mp3
│   ├── PlayerIconPop3.mp3
│   ├── TimerLoop.mp3
│   ├── DesktopButton.mp3
│   └── ButtonSwitch.mp3
└── vo/
    └── (VO files - to be implemented later)
```

---

## Quick Setup

### 1. Create AudioManager GameObject
- Create empty GameObject in your Loading scene
- Name it "AudioManager"
- Add `AudioManager.cs` component
- Check "Don't Destroy On Load" is working

### 2. Add ScreenAudioTrigger to Each Scene
For each screen scene, add the `ScreenAudioTrigger` component:

**LandingScreen.unity:**
- Add component, set Screen Type: `LandingPage`

**LobbyScreen.unity:**
- Add component, set Screen Type: `Lobby`

**QuestionScreen.unity:**
- Add component, set Screen Type: `Question`
- ✓ Monitor Timer enabled automatically

**EliminationScreen.unity:**
- Add component, set Screen Type: `Elimination`
- ✓ Monitor Timer enabled automatically

**VotingScreen.unity:**
- Add component, set Screen Type: `Voting`
- ✓ Monitor Timer enabled automatically

**ResultsScreen.unity:**
- Add component, set Screen Type: `RoundResults`

**And so on for all screens...**

### 3. Add Button Audio Components
On any desktop button that needs click sounds:
- Add `ButtonAudioComponent` to the Button GameObject
- Set Button Type:
  - `Standard` - for most buttons
  - `GameModeSwitch` - for 8Q/12Q toggle buttons

---

## Code Integration Examples

### Example 1: Player Joins Game (LobbyScreen.cs)

```csharp
void OnJoinButtonClicked()
{
    if (nameInput == null || string.IsNullOrEmpty(nameInput.text))
    {
        Debug.LogWarning("Please enter a name!");
        return;
    }
    
    string playerID = System.Guid.NewGuid().ToString();
    GameManager.Instance.AddPlayer(playerID, nameInput.text, selectedPlayerIconName);
    
    // AUDIO: Play join sound on mobile
    GameplayAudioHelper.OnPlayerJoined();
    
    ShowJoinWait();
}
```

### Example 2: Answer Submitted (QuestionScreen.cs)

```csharp
void OnSubmitAnswer()
{
    if (answerInput == null || string.IsNullOrEmpty(answerInput.text))
    {
        Debug.LogWarning("Please enter an answer!");
        return;
    }
    
    GameManager.Instance.SubmitPlayerAnswer(playerID, answerInput.text);
    
    // AUDIO: Play text input received on mobile
    GameplayAudioHelper.OnAnswerSubmitted();
    
    answerInput.interactable = false;
    answerSubmitButton.interactable = false;
}
```

### Example 3: Player Icon Pops (LobbyScreen.cs)

```csharp
void UpdatePlayerList()
{
    // ... existing code to spawn player icons ...
    
    foreach (PlayerData player in players)
    {
        GameObject iconObj = Instantiate(playerIconLobbyPrefab, playerIconContainer);
        
        // AUDIO: Play pop sound when icon appears (desktop only)
        GameplayAudioHelper.OnPlayerIconUpdate();
        
        // ... rest of setup code ...
    }
}
```

### Example 4: Winner/Loser Reveal (FinalResultsScreen.cs)

```csharp
void DisplayMobileFinalResults(List<PlayerData> rankedPlayers)
{
    PlayerData winner = rankedPlayers[0];
    PlayerData loser = rankedPlayers[rankedPlayers.Count - 1];
    
    // AUDIO: Play win/lose sounds on appropriate mobile devices
    GameplayAudioHelper.OnWinnerRevealed(playerID, winner.playerID);
    GameplayAudioHelper.OnLoserRevealed(playerID, loser.playerID);
    
    // ... rest of display code ...
}
```

### Example 5: Game Mode Switch (LobbyScreen.cs)

```csharp
// On your 8Q and 12Q buttons, add ButtonAudioComponent
// and set Button Type to: GameModeSwitch

// The component automatically plays ButtonSwitch.mp3 on click
```

---

## Music Mapping Reference

| Screen | Music File | Notes |
|--------|-----------|-------|
| LandingScreen | landingpage.mp3 | Desktop only |
| LobbyScreen | landingpage.mp3 | Desktop only |
| IntroVideoScreen | (none) | Video has own audio |
| RoundArtScreen | credits.mp3 | Desktop only |
| QuestionScreen | 60sec.mp3 | Desktop only, 60s timer |
| PlayerQuestionScreen | 60sec.mp3 | Desktop only, 60s timer |
| PictureQuestionScreen | 60sec.mp3 | Desktop only, 60s timer |
| EliminationScreen | 30sec.mp3 | Desktop only, 30s timer |
| VotingScreen | 30sec.mp3 | Desktop only, 30s timer |
| RoundResultsScreen | results.mp3 | Desktop only |
| HalftimeResultsScreen | finalresults.mp3 | Desktop only |
| BonusIntroScreen | bonus.mp3 | Desktop only |
| BonusQuestionScreen | bonus.mp3 | Desktop only |
| BonusResultsScreen | bonus.mp3 | Desktop only |
| FinalResultsScreen | finalresults.mp3 | Desktop only |
| CreditsScreen | credits.mp3 | Desktop only |

---

## SFX Trigger Reference

| Sound | When to Play | Device | Code Example |
|-------|-------------|--------|--------------|
| Join.mp3 | Player joins lobby | Mobile | `GameplayAudioHelper.OnPlayerJoined()` |
| TextInputReceived.mp3 | Answer submitted | Mobile | `GameplayAudioHelper.OnAnswerSubmitted()` |
| PlayerIconPop1/2/3.mp3 | Player icon appears/updates | Desktop | `GameplayAudioHelper.OnPlayerIconUpdate()` |
| Win.mp3 | Winner revealed | Mobile (winner) | `GameplayAudioHelper.OnWinnerRevealed(localID, winnerID)` |
| Lose.mp3 | Loser revealed | Mobile (loser) | `GameplayAudioHelper.OnLoserRevealed(localID, loserID)` |
| TimerLoop.mp3 | Last 10 seconds of timer | Desktop | Automatic via ScreenAudioTrigger |
| DesktopButton.mp3 | Any button click | Desktop | Add ButtonAudioComponent |
| ButtonSwitch.mp3 | 8Q/12Q toggle | Desktop | ButtonAudioComponent (GameModeSwitch type) |

---

## Important Notes

### Music Only Plays on Desktop
All music files are **automatically filtered** by the AudioManager to only play on desktop devices. Mobile devices will never hear background music.

### Timer Loop Behavior
- **Automatically starts** at 10 seconds remaining on Question, Elimination, and Voting screens
- **Desktop only** - never plays on mobile devices
- **2-second loop** that repeats until timer expires
- **Automatically stops** when timer reaches 0 or screen changes
- Handled by `ScreenAudioTrigger` component - no manual code needed

### Player Icon Pops
- Three different pop sounds that **cycle** in order (Pop1 → Pop2 → Pop3 → Pop1...)
- Play on **desktop only**
- Trigger whenever a player icon appears or updates in:
  - Lobby Screen (when players join)
  - Question Screen (when players submit answers)

### Win/Lose Sounds
- Only play on the **specific player's mobile device**
- Must pass both local player ID and winner/loser ID
- Triggered on:
  - Halftime Results (top player = win, bottom player = lose)
  - Final Results (1st place = win, last place = lose)

---

## Testing Checklist

### Desktop Audio
- [ ] Landing page music plays
- [ ] Round art plays credits music
- [ ] Question screens play 60sec music
- [ ] Elimination/Voting play 30sec music
- [ ] Results music plays correctly
- [ ] Halftime/Final Results play finalresults music
- [ ] Bonus screens play bonus music
- [ ] Button clicks play DesktopButton.mp3
- [ ] 8Q/12Q switch plays ButtonSwitch.mp3
- [ ] Player icons pop when appearing
- [ ] Timer loop plays last 10 seconds

### Mobile Audio
- [ ] Join sound plays when joining
- [ ] Text input sound plays when submitting answer
- [ ] Win sound plays for winner
- [ ] Lose sound plays for loser
- [ ] NO timer loop on mobile
- [ ] NO background music plays

### Timer Loop Specifically
- [ ] Starts exactly at 10 seconds
- [ ] Loops seamlessly (2-second clip)
- [ ] Stops when timer expires
- [ ] Stops when advancing screen early
- [ ] Works on Question screens
- [ ] Works on Elimination screen
- [ ] Works on Voting screen
- [ ] Works on Bonus Question screen

---

## Volume Controls (for future settings menu)

```csharp
// Set music volume (0.0 to 1.0)
AudioManager.Instance.SetMusicVolume(0.7f);

// Set SFX volume (0.0 to 1.0)
AudioManager.Instance.SetSFXVolume(1.0f);

// Mute/Unmute
AudioManager.Instance.MuteMusic();
AudioManager.Instance.UnmuteMusic();
AudioManager.Instance.MuteSFX();
AudioManager.Instance.UnmuteSFX();
```

---

## Troubleshooting

**Music not playing:**
- Check AudioManager exists in scene
- Verify files are in `Resources/audio/music/` folder
- Check you're on desktop device (music never plays on mobile)
- Check volume settings

**SFX not playing:**
- Verify files are in `Resources/audio/sfx/` folder
- Check device type (some SFX are device-specific)
- Verify AudioManager.Instance is not null

**Timer loop not starting:**
- Ensure ScreenAudioTrigger has "Monitor Timer" enabled
- Check timer is actually counting down
- Verify TimerLoop.mp3 is loaded correctly

**Button sounds not working:**
- Verify ButtonAudioComponent is attached to Button
- Check button is actually on desktop device
- Make sure button's onClick is firing

---

## Future: VO System

The VO system will be added later with the same structure:
```
Assets/Resources/audio/vo/
├── [screen_name]/
│   ├── intro.mp3
│   ├── line1.mp3
│   └── etc...
```

A separate `VOManager.cs` will handle voice-over playback with timing, queuing, and screen-specific sequences.
