# Button Wiring Guide for Unity Editor

## ✅ FIXED: All button handler methods are now PUBLIC

All button click handler methods have been made `public` so they can be called by Unity's persistent event system. This enables both:
1. Code-based `AddListener()` calls (existing)
2. Unity Editor persistent OnClick events (needs manual wiring)

## Issue (Partially Resolved)
All major gameplay screen buttons have empty `m_OnClick` handler lists in the Unity scenes. The code now supports persistent events, but **you must manually wire them in the Unity Editor** for maximum reliability.

## Required Button Wiring in Unity Editor

### 1. LandingScreen (Assets/Scenes/LandingScreen.unity)

**Desktop:**
- **"Start Test" Button** → `LandingScreen.OnStartGameClicked()`

**Mobile:**
- **"Join Game" Button** → `LandingScreen.OnJoinGameClicked()`

---

### 2. LobbyScreen (Assets/Scenes/LobbyScreen.unity)

**Desktop:**
- **"Start Test" Button** → `LobbyScreen.OnStartGameClicked()`
- **"8Q" Button** → `LobbyScreen.OnEightQuestionsClicked()`
- **"12Q" Button** → `LobbyScreen.OnTwelveQuestionsClicked()`

**Mobile:**
- **"Join Game" Button** (shows form) → `LobbyScreen.OnJoinGameButtonClicked()`
- **"Join" Button** (submits form) → `LobbyScreen.OnJoinButtonClicked()`

---

### 3. QuestionScreen (Assets/Scenes/QuestionScreen.unity)

**Mobile:**
- **"Submit" Button** → `QuestionScreen.OnSubmitAnswer()`

---

### 4. EliminationScreen (Assets/Scenes/EliminationScreen.unity)

**Mobile:**
- **"Submit" Button** → `EliminationScreen.OnSubmitVote()`

---

### 5. VotingScreen (Assets/Scenes/VotingScreen.unity)

**Mobile:**
- **"Submit" Button** → `VotingScreen.OnSubmitVote()`

---

### 6. RoundArtScreen (Assets/Scenes/RoundArtScreen.unity)

**Desktop:**
- **"Continue" Button** → `RoundArtScreen.OnContinueClicked()`

---

### 7. ResultsScreen (Assets/Scenes/ResultsScreen.unity)

**Desktop:**
- **"Continue" Button** → `ResultsScreen.OnContinueClicked()`

---

### 8. HalftimeResultsScreen (Assets/Scenes/HalftimeResultsScreen.unity)

**Desktop:**
- **"Continue" Button** → `HalftimeResultsScreen.OnContinueClicked()`

---

### 9. FinalResults (Assets/Scenes/FinalResults.unity)

**Desktop:**
- **"Credits" Button** → `FinalResults.OnCreditsClicked()`
- **"New Game" Button** → `FinalResults.OnNewGameClicked()`
- **"Share" Button** → `FinalResults.OnShareClicked()`
- **"Website" Button** → `FinalResults.OnWebsiteClicked()`

---

### 10. BonusIntroScreen (Assets/Scenes/BonusIntroScreen.unity)

**Desktop:**
- **"Continue" Button** → `BonusIntroScreen.OnContinueClicked()`

---

### 11. BonusQuestionScreen (Assets/Scenes/BonusQuestionScreen.unity)

**Mobile:**
- **"Submit" Button** → `BonusQuestionScreen.OnSubmitVote()`

---

### 12. BonusResultsScreen (Assets/Scenes/BonusResultsScreen.unity)

**Desktop:**
- **"Continue" Button** → `BonusResultsScreen.OnContinueClicked()`

---

### 13. CreditsScreen (Assets/Scenes/CreditsScreen.unity)

**Desktop:**
- **"Website" Button** → `CreditsScreen.OnWebsiteClicked()`
- **"New Game" Button** → `CreditsScreen.OnNewGameClicked()`

---

## How to Wire Buttons in Unity Editor

For each button listed above:

1. **Open the scene** in Unity Editor
2. **Select the button** GameObject in the Hierarchy
3. **In the Inspector**, find the **Button** component
4. **Scroll to "On Click ()" section**
5. **Click the "+" button** to add a persistent listener
6. **Drag the script GameObject** (e.g., the GameObject with `LobbyScreen` script) into the Object field
7. **Select the function** from the dropdown: `Component > ClassName > MethodName`
8. **Save the scene**

## Why This Matters

Without persistent OnClick events:
- Players cannot start games
- Players cannot submit answers
- Players cannot vote
- Players cannot advance between screens
- Game progression is completely broken

## Alternative: Ensure Script References Are Set

If you prefer to keep the `AddListener()` approach, you MUST ensure:
1. Each scene has a GameObject with the appropriate screen script component
2. ALL button references in the Inspector are properly connected
3. The script's `Start()` method runs before any user interaction

**Recommendation**: Use BOTH approaches (persistent events + AddListener) for maximum reliability.
