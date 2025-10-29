using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance;
    
    [Header("Debug Settings")]
    public bool debugModeEnabled = false;
    public KeyCode debugMenuKey = KeyCode.F1;
    public bool showDebugGUI = false;
    
    [Header("Quick Actions")]
    public KeyCode skipToRoundKey = KeyCode.F2;
    public KeyCode addTestPlayersKey = KeyCode.F3;
    public KeyCode simulateAnswersKey = KeyCode.F4;
    public KeyCode toggleTimerKey = KeyCode.F5;
    public KeyCode add100PointsKey = KeyCode.F6;
    public KeyCode testShuffleKey = KeyCode.F7;
    
    [Header("Test Players")]
    public int numberOfTestPlayers = 4;
    public List<string> testPlayerNames = new List<string> 
    { 
        "TestPlayer1", "TestPlayer2", "TestPlayer3", "TestPlayer4",
        "TestPlayer5", "TestPlayer6", "TestPlayer7", "TestPlayer8"
    };
    
    private Rect debugWindowRect = new Rect(20, 20, 300, 400);
    private Vector2 scrollPosition;
    private int targetRound = 1;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (!debugModeEnabled) return;
        
        // Toggle debug menu
        if (Input.GetKeyDown(debugMenuKey))
        {
            showDebugGUI = !showDebugGUI;
        }
        
        // Quick actions
        if (Input.GetKeyDown(addTestPlayersKey))
        {
            AddTestPlayers();
        }
        
        if (Input.GetKeyDown(simulateAnswersKey))
        {
            // Simulate appropriate action based on current screen
            string currentScene = SceneManager.GetActiveScene().name;
            Debug.Log("F4 pressed on scene: " + currentScene);

            if (currentScene == "QuestionScreen" || currentScene == "PictureQuestionScreen")
            {
                SimulateAllAnswers();
            }
            else if (currentScene == "EliminationScreen" || currentScene == "VotingScreen")
            {
                Debug.Log("Calling SimulateRandomVotes for " + currentScene);
                SimulateRandomVotes();
            }
            else if (currentScene == "BonusQuestionScreen")
            {
                Debug.Log("Calling SimulateBonusVotes for BonusQuestionScreen");
                SimulateBonusVotes();
            }
            else
            {
                Debug.LogWarning("F4 (Simulate) not supported on current scene: " + currentScene);
            }
        }
        
        if (Input.GetKeyDown(toggleTimerKey))
        {
            ToggleTimer();
        }
        
        if (Input.GetKeyDown(add100PointsKey))
        {
            AddPointsToAllPlayers(100);
        }

        if (Input.GetKeyDown(testShuffleKey))
        {
            ShuffleUtility.TestShuffle();
        }
    }
    
    void OnGUI()
    {
        if (!debugModeEnabled || !showDebugGUI) return;
        
        debugWindowRect = GUI.Window(0, debugWindowRect, DebugWindow, "Debug Tools");
    }
    
    void DebugWindow(int windowID)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("=== GAME STATE ===");
        GUILayout.Label("Current Round: " + GameManager.Instance.currentRound.Value);
        GUILayout.Label("Game Mode: " + GameManager.Instance.gameMode.Value);
        GUILayout.Label("Players: " + GameManager.Instance.GetAllPlayers().Count);
        GUILayout.Label("Timer: " + GameManager.Instance.GetTimerDisplay());
        
        GUILayout.Space(10);
        
        // === SCENE JUMPING ===
        GUILayout.Label("=== SCENE NAVIGATION ===");

        if (GUILayout.Button("Landing Screen"))
        {
            TryLoadScene("LandingScreen");
        }

        if (GUILayout.Button("Lobby Screen"))
        {
            TryLoadScene("LobbyScreen");
        }

        if (GUILayout.Button("Question Screen"))
        {
            TryLoadScene("QuestionScreen");
        }

        if (GUILayout.Button("Elimination Screen"))
        {
            TryLoadScene("EliminationScreen");
        }

        if (GUILayout.Button("Voting Screen"))
        {
            TryLoadScene("VotingScreen");
        }

        if (GUILayout.Button("Results Screen"))
        {
            TryLoadScene("ResultsScreen");
        }

        if (GUILayout.Button("Final Results"))
        {
            TryLoadScene("FinalResults");
        }
        
        GUILayout.Space(10);
        
        // === ROUND CONTROL ===
        GUILayout.Label("=== ROUND CONTROL ===");
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Skip to Round:");
        targetRound = (int)GUILayout.HorizontalSlider(targetRound, 1, 12);
        GUILayout.Label(targetRound.ToString());
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("Skip to Round " + targetRound))
        {
            SkipToRound(targetRound);
        }
        
        if (GUILayout.Button("Next Round"))
        {
            if (EnsureServerAuthority("advance to the next round"))
            {
                GameManager.Instance.currentRound.Value++;
                GameManager.Instance.AdvanceToNextScreen();
            }
        }
        
        GUILayout.Space(10);
        
        // === PLAYER MANAGEMENT ===
        GUILayout.Label("=== PLAYER MANAGEMENT ===");
        
        if (GUILayout.Button("Add " + numberOfTestPlayers + " Test Players"))
        {
            AddTestPlayers();
        }
        
        if (GUILayout.Button("Clear All Players"))
        {
            ClearAllPlayers();
        }
        
        GUILayout.Space(10);
        
        // === SIMULATION ===
        GUILayout.Label("=== SIMULATION ===");
        
        if (GUILayout.Button("Simulate All Answers"))
        {
            SimulateAllAnswers();
        }
        
        if (GUILayout.Button("Simulate Random Votes"))
        {
            SimulateRandomVotes();
        }
        
        if (GUILayout.Button("Auto-Complete Round"))
        {
            AutoCompleteRound();
        }
        
        GUILayout.Space(10);
        
        // === SCORING ===
        GUILayout.Label("=== SCORING ===");
        
        if (GUILayout.Button("Add 100% to All Players"))
        {
            AddPointsToAllPlayers(100);
        }
        
        if (GUILayout.Button("Reset All Scores"))
        {
            ResetAllScores();
        }
        
        if (GUILayout.Button("Randomize Scores"))
        {
            RandomizeScores();
        }
        
        GUILayout.Space(10);
        
        // === TIMER ===
        GUILayout.Label("=== TIMER ===");
        
        if (GUILayout.Button("Pause/Resume Timer"))
        {
            ToggleTimer();
        }
        
        if (GUILayout.Button("Set Timer to 5 seconds"))
        {
            if (EnsureServerAuthority("set the timer to 5 seconds"))
            {
                GameManager.Instance.currentTimerValue.Value = 5f;
            }
        }

        if (GUILayout.Button("Skip Timer"))
        {
            if (EnsureServerAuthority("skip the timer"))
            {
                GameManager.Instance.currentTimerValue.Value = 0f;
            }
        }

        GUILayout.Space(10);

        // === GAME MODE ===
        GUILayout.Label("=== GAME MODE ===");

        if (GUILayout.Button("Switch to 8Q Mode"))
        {
            if (EnsureServerAuthority("switch to 8Q mode"))
            {
                GameManager.Instance.gameMode.Value = GameManager.GameMode.EightQuestions;
            }
        }

        if (GUILayout.Button("Switch to 12Q Mode"))
        {
            if (EnsureServerAuthority("switch to 12Q mode"))
            {
                GameManager.Instance.gameMode.Value = GameManager.GameMode.TwelveQuestions;
            }
        }
        
        GUILayout.EndScrollView();

        GUI.DragWindow();
    }

    bool EnsureServerAuthority(string action)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"[DebugManager] Cannot {action}: GameManager is not available.");
            return false;
        }

        if (!GameManager.Instance.IsServer)
        {
            Debug.LogWarning($"[DebugManager] Cannot {action}: host authority required.");
            return false;
        }

        return true;
    }

    void TryLoadScene(string sceneName)
    {
        if (EnsureServerAuthority($"load scene {sceneName}"))
        {
            GameManager.Instance.LoadScene(sceneName);
        }
    }

    // === DEBUG FUNCTIONS ===

    public void AddTestPlayers()
    {
        if (!GameManager.Instance.IsServer)
        {
            Debug.LogWarning("[DebugManager] Only server can add test players");
            return;
        }

        // Find next available test player index by checking NetworkList
        int startIndex = 0;
        bool foundAvailable = false;
        while (!foundAvailable)
        {
            bool exists = false;
            for (int j = 0; j < GameManager.Instance.networkPlayers.Count; j++)
            {
                if (GameManager.Instance.networkPlayers[j].playerID.ToString() == "test_player_" + startIndex)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                foundAvailable = true;
            }
            else
            {
                startIndex++;
            }
        }

        for (int i = 0; i < numberOfTestPlayers; i++)
        {
            int playerIndex = startIndex + i;

            if (playerIndex >= testPlayerNames.Count)
            {
                Debug.LogWarning("Reached maximum test players (" + testPlayerNames.Count + ")");
                break;
            }

            string playerID = "test_player_" + playerIndex;
            string playerName = testPlayerNames[playerIndex];
            string iconName = "player icon (" + ((playerIndex % 20) + 1) + ")";

            GameManager.Instance.AddPlayer(playerID, playerName, iconName);
        }

        Debug.Log("Added " + numberOfTestPlayers + " test players (starting from index " + startIndex + ")");
    }
    
    public void ClearAllPlayers()
    {
        if (EnsureServerAuthority("clear all players"))
        {
            GameManager.Instance.ClearAllPlayers();
        }
    }

    public void SimulateAllAnswers()
    {
        if (!EnsureServerAuthority("simulate answers")) return;

        string[] sampleAnswers = new string[]
        {
            "This is a test answer",
            "Another sample response",
            "Testing the answer system",
            "Random answer here",
            "Simulated player response",
            "Debug answer text",
            "Example answer content",
            "Test submission"
        };

        // Set up correct and robot answers if not already set (using replicated payload)
        var payload = GameManager.Instance.currentQuestionPayload.Value;
        if (!payload.hasData)
        {
            Debug.LogWarning("[DebugManager] Cannot simulate answers - no active question payload");
        }
        else
        {
            bool payloadChanged = false;

            if (string.IsNullOrEmpty(payload.correctAnswer.ToString()))
            {
                payload.correctAnswer = "The Correct Answer (Debug)";
                payloadChanged = true;
            }

            if (string.IsNullOrEmpty(payload.robotAnswer.ToString()))
            {
                payload.robotAnswer = "Robot Answer (Debug)";
                payloadChanged = true;
            }

            if (payloadChanged)
            {
                GameManager.Instance.currentQuestionPayload.Value = payload;
            }
        }

        int index = 0;
        // Use GetAllPlayers() instead of transient dictionary
        foreach (var player in GameManager.Instance.GetAllPlayers())
        {
            string answer = sampleAnswers[index % sampleAnswers.Length];
            GameManager.Instance.SubmitPlayerAnswer(player.playerID, answer);
            index++;
        }

        Debug.Log("Simulated answers for all players");
    }

    public void SimulateRandomVotes()
    {
        if (!EnsureServerAuthority("simulate votes")) return;

        List<string> answers;

        // Use appropriate answer list based on current screen
        if (SceneManager.GetActiveScene().name == "EliminationScreen")
        {
            answers = GameManager.Instance.GetAllAnswers();
        }
        else if (SceneManager.GetActiveScene().name == "VotingScreen")
        {
            answers = GameManager.Instance.GetRemainingAnswers();
        }
        else
        {
            Debug.LogWarning("SimulateRandomVotes called from unsupported scene");
            return;
        }

        if (answers.Count == 0)
        {
            Debug.LogWarning("No answers available to vote on");
            return;
        }

        // Use GetAllPlayers() instead of transient dictionary
        foreach (var player in GameManager.Instance.GetAllPlayers())
        {
            // Filter out null/empty answers
            List<string> validAnswers = new List<string>();
            foreach (var answer in answers)
            {
                if (!string.IsNullOrEmpty(answer))
                {
                    validAnswers.Add(answer);
                }
            }

            if (validAnswers.Count == 0)
            {
                Debug.LogWarning("No valid answers to vote on for player " + player.playerID);
                continue;
            }

            string randomAnswer = validAnswers[Random.Range(0, validAnswers.Count)];

            // Simulate elimination or voting depending on current screen
            if (SceneManager.GetActiveScene().name == "EliminationScreen")
            {
                GameManager.Instance.SubmitEliminationVote(player.playerID, randomAnswer);
            }
            else if (SceneManager.GetActiveScene().name == "VotingScreen")
            {
                GameManager.Instance.SubmitVotingVote(player.playerID, randomAnswer);
            }
        }

        Debug.Log("Simulated random votes for all players");
    }

    public void SimulateBonusVotes()
    {
        if (!EnsureServerAuthority("simulate bonus votes")) return;

        // Get all players
        List<PlayerData> allPlayers = GameManager.Instance.GetAllPlayers();

        if (allPlayers.Count == 0)
        {
            Debug.LogWarning("No players available to vote for in bonus round");
            return;
        }

        // Each player votes for a random player (already using GetAllPlayers correctly)
        foreach (var voter in allPlayers)
        {
            // Pick a random player to vote for
            PlayerData randomPlayer = allPlayers[Random.Range(0, allPlayers.Count)];

            GameManager.Instance.SubmitBonusVote(voter.playerID, randomPlayer.playerID);
        }

        Debug.Log("Simulated bonus votes for all players");
    }

    public void AutoCompleteRound()
    {
        if (EnsureServerAuthority("auto-complete the round"))
        {
            StartCoroutine(AutoCompleteRoundCoroutine());
        }
    }

    System.Collections.IEnumerator AutoCompleteRoundCoroutine()
    {
        if (!EnsureServerAuthority("auto-complete the round")) yield break;

        // Simulate entire round automatically
        SimulateAllAnswers();
        yield return new WaitForSeconds(0.5f);

        GameManager.Instance.AdvanceToNextScreen();
        yield return new WaitForSeconds(0.5f);

        SimulateRandomVotes();
        yield return new WaitForSeconds(0.5f);

        GameManager.Instance.AdvanceToNextScreen();
        yield return new WaitForSeconds(0.5f);

        SimulateRandomVotes();

        Debug.Log("Auto-completed round");
    }

    public void SkipToRound(int round)
    {
        if (EnsureServerAuthority($"skip to round {round}"))
        {
            GameManager.Instance.currentRound.Value = round - 1;
            GameManager.Instance.AdvanceToNextScreen();
            Debug.Log("Skipped to round " + round);
        }
    }

    public void AddPointsToAllPlayers(int points)
    {
        if (EnsureServerAuthority("add points to all players"))
        {
            GameManager.Instance.AddPointsToAllPlayers(points);
        }
    }

    public void ResetAllScores()
    {
        if (EnsureServerAuthority("reset all player scores"))
        {
            GameManager.Instance.ResetAllPlayerScores();
        }
    }

    public void RandomizeScores()
    {
        if (EnsureServerAuthority("randomize scores"))
        {
            // Directly manipulate NetworkList on server
            for (int i = 0; i < GameManager.Instance.networkPlayers.Count; i++)
            {
                var player = GameManager.Instance.networkPlayers[i];
                player.scorePercentage = Random.Range(-50, 150);
                GameManager.Instance.networkPlayers[i] = player;
            }
            Debug.Log("Randomized all player scores");
        }
    }

    public void ToggleTimer()
    {
        if (EnsureServerAuthority("toggle the timer"))
        {
            GameManager.Instance.timerActive.Value = !GameManager.Instance.timerActive.Value;
            Debug.Log("Timer " + (GameManager.Instance.timerActive.Value ? "resumed" : "paused"));
        }
    }
    
    // === COROUTINE HELPER ===
    
    IEnumerator<WaitForSeconds> yield(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
    
    // === LOGGING ===
    
    public void LogGameState()
    {
        Debug.Log("=== GAME STATE ===");
        Debug.Log("Round: " + GameManager.Instance.currentRound.Value);
        Debug.Log("Mode: " + GameManager.Instance.gameMode.Value);
        Debug.Log("Players: " + GameManager.Instance.GetAllPlayers().Count);
        Debug.Log("Timer: " + GameManager.Instance.GetTimeRemaining() + "s");
        Debug.Log("Current Scene: " + SceneManager.GetActiveScene().name);
    }
}
