using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class RoundResultsScreen : MonoBehaviour
{
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public GameObject resultsPanelContainer;
    
    [Header("Panel 1 - True, Robot Answers")]
    public GameObject panel1;
    public Image panel1Background;
    public TextMeshProUGUI panel1Headline;
    public TextMeshProUGUI trueResponse;
    public TextMeshProUGUI robotResponse;
    public TextMeshProUGUI scoreDiffTrue; // Score difference for getting it right

    [Header("Panel 1 Icon Zones")]
    public GameObject trueAnswerIconPrefab; // Icon prefab for True Answer zone
    public Transform trueAnswerIconZone; // Container for players who voted for True answer
    public GameObject robotFooledIconPrefab; // Icon prefab for Robot Fooled zone
    public Transform robotFooledIconZone; // Container for players fooled by Robot
    public GameObject robotNotFooledIconPrefab; // Icon prefab for Robot Not Fooled zone
    public Transform robotNotFooledIconZone; // Container for players not fooled by Robot

    [Header("Panel 2 - Player Responses")]
    public GameObject panel2;
    public Image panel2Background;
    public TextMeshProUGUI panel2Headline;
    public Transform panel2ResultsContainer; // Container for ResultsPlayerResponse prefabs
    public GameObject resultsPlayerResponsePrefab; // Prefab with: ScoreDiff, PlayerResponse, Number of Testers Fooled

    [Header("Panel 2 Prefab Component Names")]
    [SerializeField] private string panel2PlayerResponseName = "PlayerResponse";
    [SerializeField] private string panel2ScoreDiffName = "ScoreDiff";
    [SerializeField] private string panel2ScoreNumberName = "ScoreNumber";
    [SerializeField] private string panel2NumberOfFooledName = "NumberOfFooled";
    [SerializeField] private string panel2ResponseBackgroundName = "ResponseBackground";

    [Header("Panel 3 - Game Standings")]
    public GameObject panel3;
    public Image panel3Background;
    public TextMeshProUGUI panel3Headline;
    public Transform panel3ResultsContainer; // Container for ResultsPlayerRank prefabs
    public GameObject resultsPlayerRankPrefab;
    public GameObject panel3ResultsPlayerIconPrefab; // Icon prefab for Panel 3

    [Header("Panel 3 Prefab Component Names")]
    [SerializeField] private string panel3PlayerNameName = "PlayerName";
    [SerializeField] private string panel3PlayerCumulativeScoreName = "PlayerCumulitiveScore";
    [SerializeField] private string panel3PlayerIconName = "PlayerIcon";
    
    [Header("Navigation")]
    public Button nextRoundButton;
    public Button finalResultsButton;

    [Header("Round Hero Images (Hero1 - Hero12)")]
    public Image[] heroImages = new Image[12];

    [Header("Round Labels")]
    public TextMeshProUGUI[] roundLabels = new TextMeshProUGUI[12];
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public GameObject mobileResultsContainer;
    public TextMeshProUGUI roundResultsMobileHeadline;
    public TextMeshProUGUI mobilePlayerScore;
    public TextMeshProUGUI mobileScoreDiff;
    public TextMeshProUGUI mobileTrueResponse;
    public TextMeshProUGUI mobileRobotResponse;
    public TextMeshProUGUI mobilePlayerRank;
    public Image mobilePlayerIcon;
    public Image mobileBackground;

    [Header("Prefabs")]
    public GameObject playerIconPrefab; // For PlayerIconZone containers (fooled/not fooled zones)
    
    [Header("State")]
    private bool isMobile = false;
    private string playerID = "";
    private int currentPanel = 0;
    private float panelTimer = 0f;
    private const float PANEL_DISPLAY_DURATION = 5f;

    void Start()
    {
        isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

        if (isMobile)
        {
            playerID = PlayerAuthSystem.Instance != null ? PlayerAuthSystem.Instance.GetLocalPlayerID() : GetLocalPlayerID();
        }

        // Show appropriate display
        ShowAppropriateDisplay();

        // Display results
        DisplayResults();

        // Setup navigation buttons
        if (nextRoundButton != null)
        {
            nextRoundButton.onClick.AddListener(OnNextRoundClicked);
        }

        if (finalResultsButton != null)
        {
            finalResultsButton.onClick.AddListener(OnFinalResultsClicked);
        }

        // Show/hide appropriate button
        UpdateNavigationButtons();

        // Play results music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayResultsMusic();
        }

        // Start panel sequence for desktop
        if (!isMobile)
        {
            StartPanelSequence();
        }
    }

    void Update()
    {
        // Handle panel sequence timing for desktop
        if (!isMobile && currentPanel > 0 && currentPanel <= 3)
        {
            panelTimer += Time.deltaTime;

            if (panelTimer >= PANEL_DISPLAY_DURATION)
            {
                AdvanceToNextPanel();
            }
        }
    }

    void StartPanelSequence()
    {
        // Hide all panels initially
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);
        if (panel3 != null) panel3.SetActive(false);

        // Start with panel 1
        currentPanel = 1;
        panelTimer = 0f;
        ShowCurrentPanel();
    }

    void ShowCurrentPanel()
    {
        // Hide all panels
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);
        if (panel3 != null) panel3.SetActive(false);

        // Show the current panel
        switch (currentPanel)
        {
            case 1:
                if (panel1 != null) panel1.SetActive(true);
                break;
            case 2:
                if (panel2 != null) panel2.SetActive(true);
                break;
            case 3:
                if (panel3 != null) panel3.SetActive(true);
                break;
        }
    }

    void AdvanceToNextPanel()
    {
        currentPanel++;
        panelTimer = 0f;

        if (currentPanel <= 3)
        {
            ShowCurrentPanel();
        }
        else
        {
            // All panels shown, advance to next screen
            if (GameManager.Instance != null && GameManager.Instance.IsServer)
            {
                GameManager.Instance.AdvanceToNextScreen();
            }
            else
            {
                Debug.LogWarning("[RoundResults] Only the host can advance to the next screen.");
            }
        }
    }
    
    void ShowAppropriateDisplay()
    {
        if (desktopDisplay != null)
        {
            desktopDisplay.SetActive(!isMobile);
        }
        
        if (mobileDisplay != null)
        {
            mobileDisplay.SetActive(isMobile);
        }
    }
    
    void DisplayResults()
    {
        string correctAnswer = GameManager.Instance.GetCorrectAnswer();
        string robotAnswer = GameManager.Instance.GetRobotAnswer();
        string eliminatedAnswer = GameManager.Instance.GetEliminatedAnswer();
        
        if (isMobile)
        {
            DisplayMobileResults(correctAnswer, robotAnswer);
        }
        else
        {
            DisplayDesktopResults(correctAnswer, robotAnswer, eliminatedAnswer);
        }
        
        // Update round label and hero image
        UpdateRoundLabel();
        UpdateRoundHeroImage();
    }
    
    void DisplayDesktopResults(string correctAnswer, string robotAnswer, string eliminatedAnswer)
    {
        // Get voting results
        var votingResults = GameManager.Instance.GetVotingResults();

        // Panel 1: Player's answer and score changes
        if (panel1 != null)
        {
            // Set answers
            if (trueResponse != null)
            {
                trueResponse.text = correctAnswer;
            }

            if (this.robotResponse != null)
            {
                this.robotResponse.text = robotAnswer;
            }

            // Set score difference for voting correctly
            if (scoreDiffTrue != null)
            {
                int correctVotePoints = GameManager.Instance.GetCorrectVotePoints();
                scoreDiffTrue.text = "+" + correctVotePoints;
            }

            // Populate player icons based on who voted correctly
            PopulatePanel1PlayerIcons(correctAnswer, robotAnswer);
        }

        // Panel 2: Show all player responses with fooled counts
        if (panel2 != null)
        {
            PopulatePanel2PlayerResponses();
        }

        // Panel 3: Show game standings (all players ranked)
        if (panel3 != null)
        {
            PopulatePanel3GameStandings();
        }
    }
    
    void DisplayMobileResults(string correctAnswer, string robotAnswer)
    {
        PlayerData player = GameManager.Instance.GetPlayer(playerID);

        if (player != null)
        {
            // Show player's personal results
            if (mobilePlayerScore != null)
            {
                mobilePlayerScore.text = player.scorePercentage + "%";
            }

            // Show player icon
            if (mobilePlayerIcon != null && PlayerManager.Instance != null)
            {
                mobilePlayerIcon.sprite = PlayerManager.Instance.GetPlayerIcon(player.iconName);
            }
        }

        // Show correct and robot answers
        if (mobileTrueResponse != null)
        {
            mobileTrueResponse.text = correctAnswer;
        }

        if (mobileRobotResponse != null)
        {
            mobileRobotResponse.text = robotAnswer;
        }

        // Show player's rank
        if (mobilePlayerRank != null)
        {
            List<PlayerData> rankedPlayers = GameManager.Instance.GetPlayersByRank();
            int rank = rankedPlayers.FindIndex(p => p.playerID == playerID) + 1;
            mobilePlayerRank.text = "Rank: " + rank;
        }
    }
    
    void PopulatePanel1PlayerIcons(string correctAnswer, string robotAnswer)
    {
        Debug.Log($"Panel1: correctAnswer='{correctAnswer}', robotAnswer='{robotAnswer}'");

        // Clear existing icons from all zones
        if (trueAnswerIconZone != null)
        {
            foreach (Transform child in trueAnswerIconZone)
            {
                Destroy(child.gameObject);
            }
        }

        if (robotFooledIconZone != null)
        {
            foreach (Transform child in robotFooledIconZone)
            {
                Destroy(child.gameObject);
            }
        }

        if (robotNotFooledIconZone != null)
        {
            foreach (Transform child in robotNotFooledIconZone)
            {
                Destroy(child.gameObject);
            }
        }

        // Get voting results
        var votingVotes = GameManager.Instance.VotingVotes;
        List<PlayerData> allPlayers = GameManager.Instance.GetAllPlayers();

        int trueCount = 0, robotCount = 0, otherCount = 0;

        foreach (PlayerData player in allPlayers)
        {
            // Check if player voted
            if (!votingVotes.ContainsKey(player.playerID))
                continue;

            string playerVote = votingVotes[player.playerID];

            Debug.Log($"Panel1: Player {player.playerName} voted for '{playerVote}'");

            // Determine which zone to add icon to
            Transform targetZone = null;
            GameObject iconPrefab = null;

            if (playerVote == correctAnswer)
            {
                // Voted for True answer
                targetZone = trueAnswerIconZone;
                iconPrefab = trueAnswerIconPrefab;
                trueCount++;
            }
            else if (playerVote == robotAnswer)
            {
                // Fooled by Robot answer
                targetZone = robotFooledIconZone;
                iconPrefab = robotFooledIconPrefab;
                robotCount++;
            }
            else
            {
                // Voted for other wrong answer (not fooled by robot)
                targetZone = robotNotFooledIconZone;
                iconPrefab = robotNotFooledIconPrefab;
                otherCount++;
            }

            if (targetZone != null && iconPrefab != null)
            {
                GameObject iconObj = Instantiate(iconPrefab, targetZone);
                iconObj.SetActive(true);

                Debug.Log($"Panel1: Instantiated icon for '{player.playerName}' in zone '{targetZone.name}' (activeSelf={iconObj.activeSelf}, activeInHierarchy={iconObj.activeInHierarchy})");
                Debug.Log($"Panel1: Icon prefab children: {string.Join(", ", GetAllChildrenNames(iconObj.transform))}");

                // Find the PlayerIcon child that has the Image component
                Transform playerIconTransform = FindDeepChild(iconObj.transform, "PlayerIcon");
                if (playerIconTransform != null)
                {
                    playerIconTransform.gameObject.SetActive(true);
                    Image iconImage = playerIconTransform.GetComponent<Image>();

                    if (iconImage != null && PlayerManager.Instance != null)
                    {
                        iconImage.enabled = true;
                        Sprite iconSprite = PlayerManager.Instance.GetPlayerIcon(player.iconName);
                        if (iconSprite != null)
                        {
                            iconImage.sprite = iconSprite;
                            Debug.Log($"Panel1: Set icon sprite for '{player.playerName}' to '{player.iconName}'");
                        }
                        else
                        {
                            Debug.LogError($"Panel1: Failed to get icon sprite for '{player.iconName}'");
                        }
                    }
                    else if (iconImage == null)
                    {
                        Debug.LogError($"Panel1: Image component is NULL on PlayerIcon child");
                    }
                }
                else
                {
                    Debug.LogError($"Panel1: Could not find 'PlayerIcon' child in instantiated icon prefab");
                }
            }
            else
            {
                Debug.LogError($"Panel1: Cannot instantiate - targetZone={targetZone != null}, iconPrefab={iconPrefab != null}");
            }
        }

        Debug.Log($"Panel1: True={trueCount}, RobotFooled={robotCount}, Other={otherCount}");
    }

    void PopulatePanel2PlayerResponses()
    {
        if (panel2ResultsContainer == null)
        {
            Debug.LogError("Panel2ResultsContainer is null!");
            return;
        }

        if (resultsPlayerResponsePrefab == null)
        {
            Debug.LogError("resultsPlayerResponsePrefab is null!");
            return;
        }

        // Clear existing entries
        foreach (Transform child in panel2ResultsContainer)
        {
            Destroy(child.gameObject);
        }

        // Get player answers and voting results
        var playerAnswers = GameManager.Instance.CurrentRoundAnswers;
        var votingResults = GameManager.Instance.GetVotingResults();
        List<PlayerData> allPlayers = GameManager.Instance.GetAllPlayers();

        // Build sortable data so the panel always displays highest fooled counts first
        List<PlayerData> playersWithAnswers = new List<PlayerData>();
        Dictionary<string, string> answersByPlayer = new Dictionary<string, string>();
        Dictionary<string, int> fooledCountByPlayer = new Dictionary<string, int>();

        foreach (PlayerData player in allPlayers)
        {
            if (!playerAnswers.TryGetValue(player.playerID, out string playerAnswer) || string.IsNullOrEmpty(playerAnswer))
            {
                continue;
            }

            playersWithAnswers.Add(player);
            answersByPlayer[player.playerID] = playerAnswer;
            fooledCountByPlayer[player.playerID] = votingResults.ContainsKey(playerAnswer) ? votingResults[playerAnswer] : 0;
        }

        playersWithAnswers.Sort((a, b) =>
        {
            int fooledComparison = fooledCountByPlayer[b.playerID].CompareTo(fooledCountByPlayer[a.playerID]);
            if (fooledComparison != 0)
            {
                return fooledComparison;
            }

            int scoreComparison = b.scorePercentage.CompareTo(a.scorePercentage);
            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            return string.Compare(a.playerName, b.playerName, System.StringComparison.OrdinalIgnoreCase);
        });

        // Create an entry for each player's response, now in sorted order
        foreach (PlayerData player in playersWithAnswers)
        {
            string playerAnswer = answersByPlayer[player.playerID];
            int fooledCount = fooledCountByPlayer[player.playerID];

            GameObject responseObj = Instantiate(resultsPlayerResponsePrefab, panel2ResultsContainer);

            RectTransform responseRect = responseObj.GetComponent<RectTransform>();
            Debug.Log($"Panel2: Spawned response for '{player.playerName}' (activeSelf={responseObj.activeSelf}, activeInHierarchy={responseObj.activeInHierarchy}, anchoredPos={responseRect?.anchoredPosition})");

            // Debug: Log all children names
            Debug.Log($"Panel2: Prefab children: {string.Join(", ", GetAllChildrenNames(responseObj.transform))}");

            // Find components in the prefab using configured names (search all descendants, not just direct children)
            Transform playerResponseTransform = FindDeepChild(responseObj.transform, panel2PlayerResponseName);
            Transform responseBackgroundTransform = FindDeepChild(responseObj.transform, panel2ResponseBackgroundName);
            Transform scoreDiffContainerTransform = FindDeepChild(responseObj.transform, panel2ScoreDiffName);
            Transform scoreNumberTransform = FindDeepChild(responseObj.transform, panel2ScoreNumberName);
            Transform fooledCountTransform = FindDeepChild(responseObj.transform, panel2NumberOfFooledName);

            Debug.Log($"Panel2: Looking for '{panel2PlayerResponseName}' - Found: {playerResponseTransform != null}");
            Debug.Log($"Panel2: Looking for '{panel2ResponseBackgroundName}' - Found: {responseBackgroundTransform != null}");
            Debug.Log($"Panel2: Looking for '{panel2ScoreDiffName}' (container) - Found: {scoreDiffContainerTransform != null}");
            Debug.Log($"Panel2: Looking for '{panel2ScoreNumberName}' - Found: {scoreNumberTransform != null}");
            Debug.Log($"Panel2: Looking for '{panel2NumberOfFooledName}' - Found: {fooledCountTransform != null}");

            TextMeshProUGUI playerResponseText = playerResponseTransform?.GetComponent<TextMeshProUGUI>();
            Image responseBackground = responseBackgroundTransform?.GetComponent<Image>();
            Image scoreDiffBackground = scoreDiffContainerTransform?.GetComponent<Image>();
            TextMeshProUGUI scoreNumberText = scoreNumberTransform?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI fooledCountText = fooledCountTransform?.GetComponent<TextMeshProUGUI>();

            Debug.Log($"Panel2: Component checks - PlayerResponse: {playerResponseText != null}, ResponseBackground: {responseBackground != null}, ScoreDiff (Image): {scoreDiffBackground != null}, ScoreNumber: {scoreNumberText != null}, NumberOfFooled: {fooledCountText != null}");

            // Activate and set PlayerResponse
            if (playerResponseTransform != null) playerResponseTransform.gameObject.SetActive(true);
            if (playerResponseText != null)
            {
                playerResponseText.text = playerAnswer;
                playerResponseText.enabled = true;
                Debug.Log($"Panel2: Set PlayerResponse to '{playerAnswer}' (enabled={playerResponseText.enabled}, gameObject.activeSelf={playerResponseText.gameObject.activeSelf})");
            }
            else
            {
                Debug.LogError($"Panel2: PlayerResponseText component is NULL on transform '{playerResponseTransform?.name}'");
            }

            // Activate ResponseBackground
            if (responseBackgroundTransform != null)
            {
                responseBackgroundTransform.gameObject.SetActive(true);
                if (responseBackground != null) responseBackground.enabled = true;
                Debug.Log($"Panel2: ResponseBackground activated (hasImage={responseBackground != null})");
            }

            // Activate ScoreDiff container
            if (scoreDiffContainerTransform != null)
            {
                scoreDiffContainerTransform.gameObject.SetActive(true);
                if (scoreDiffBackground != null) scoreDiffBackground.enabled = true;
                Debug.Log($"Panel2: ScoreDiff container activated (hasImage={scoreDiffBackground != null})");
            }

            // Activate and set ScoreNumber (points received for this answer)
            if (scoreNumberTransform != null) scoreNumberTransform.gameObject.SetActive(true);
            if (scoreNumberText != null)
            {
                // Calculate score from votes received on this answer
                int pointsPerVote = GameManager.Instance.GetVoteReceivedPoints();
                int totalScore = fooledCount * pointsPerVote;

                scoreNumberText.text = "+" + totalScore;
                scoreNumberText.enabled = true;
                Debug.Log($"Panel2: Set ScoreNumber to '+{totalScore}' (enabled={scoreNumberText.enabled}, gameObject.activeSelf={scoreNumberText.gameObject.activeSelf})");
            }
            else
            {
                Debug.LogError($"Panel2: ScoreNumberText component is NULL on transform '{scoreNumberTransform?.name}'");
            }

            // Activate and set NumberOfFooled
            if (fooledCountTransform != null) fooledCountTransform.gameObject.SetActive(true);
            if (fooledCountText != null)
            {
                fooledCountText.text = fooledCount.ToString();
                fooledCountText.enabled = true;
                Debug.Log($"Panel2: Set NumberOfFooled to '{fooledCount}' (enabled={fooledCountText.enabled}, gameObject.activeSelf={fooledCountText.gameObject.activeSelf})");
            }
            else
            {
                Debug.LogError($"Panel2: FooledCountText component is NULL on transform '{fooledCountTransform?.name}'");
            }
        }

        Debug.Log($"Panel2: Total instantiated responses={panel2ResultsContainer.childCount}, container activeInHierarchy={panel2ResultsContainer.gameObject.activeInHierarchy}");

        // Force layout rebuild to ensure instantiated UI elements are visible
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel2ResultsContainer.GetComponent<RectTransform>());
    }

    void PopulatePanel3GameStandings()
    {
        if (panel3ResultsContainer == null)
        {
            Debug.LogError("Panel3ResultsContainer is null!");
            return;
        }

        if (resultsPlayerRankPrefab == null)
        {
            Debug.LogError("resultsPlayerRankPrefab is null!");
            return;
        }

        // Clear existing entries
        foreach (Transform child in panel3ResultsContainer)
        {
            Destroy(child.gameObject);
        }

        // Get players ranked by score
        List<PlayerData> rankedPlayers = GameManager.Instance.GetPlayersByRank();

        // Create a rank entry for each player
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            PlayerData player = rankedPlayers[i];
            GameObject rankObj = Instantiate(resultsPlayerRankPrefab, panel3ResultsContainer);

            RectTransform rankRect = rankObj.GetComponent<RectTransform>();
            Debug.Log($"Panel3: Spawned rank entry for '{player.playerName}' (activeSelf={rankObj.activeSelf}, activeInHierarchy={rankObj.activeInHierarchy}, anchoredPos={rankRect?.anchoredPosition})");

            // Debug: Log all children names
            Debug.Log($"Panel3: Prefab children: {string.Join(", ", GetAllChildrenNames(rankObj.transform))}");

            // Find components in the prefab using configured names (search all descendants, not just direct children)
            Transform playerNameTransform = FindDeepChild(rankObj.transform, panel3PlayerNameName);
            Transform scoreTransform = FindDeepChild(rankObj.transform, panel3PlayerCumulativeScoreName);
            Transform playerIconTransform = FindDeepChild(rankObj.transform, panel3PlayerIconName);

            Debug.Log($"Panel3: Looking for '{panel3PlayerNameName}' - Found: {playerNameTransform != null}");
            Debug.Log($"Panel3: Looking for '{panel3PlayerCumulativeScoreName}' - Found: {scoreTransform != null}");
            Debug.Log($"Panel3: Looking for '{panel3PlayerIconName}' - Found: {playerIconTransform != null}");

            TextMeshProUGUI playerNameText = playerNameTransform?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI scoreText = scoreTransform?.GetComponent<TextMeshProUGUI>();
            Image playerIconImage = playerIconTransform?.GetComponent<Image>();

            Debug.Log($"Panel3: Component checks - PlayerName: {playerNameText != null}, PlayerCumulitiveScore: {scoreText != null}, PlayerIcon: {playerIconImage != null}");

            // Activate and set PlayerName
            if (playerNameTransform != null) playerNameTransform.gameObject.SetActive(true);
            if (playerNameText != null)
            {
                playerNameText.text = player.playerName;
                playerNameText.enabled = true;
                Debug.Log($"Panel3: Set PlayerName to '{player.playerName}' (enabled={playerNameText.enabled}, gameObject.activeSelf={playerNameText.gameObject.activeSelf})");
            }
            else
            {
                Debug.LogError($"Panel3: PlayerNameText component is NULL on transform '{playerNameTransform?.name}'");
            }

            // Activate and set PlayerCumulitiveScore
            if (scoreTransform != null) scoreTransform.gameObject.SetActive(true);
            if (scoreText != null)
            {
                scoreText.text = player.scorePercentage + "%";
                scoreText.enabled = true;
                Debug.Log($"Panel3: Set PlayerCumulitiveScore to '{player.scorePercentage}%' (enabled={scoreText.enabled}, gameObject.activeSelf={scoreText.gameObject.activeSelf})");
            }
            else
            {
                Debug.LogError($"Panel3: ScoreText component is NULL on transform '{scoreTransform?.name}'");
            }

            // Activate and set PlayerIcon
            if (playerIconTransform != null) playerIconTransform.gameObject.SetActive(true);
            if (playerIconImage != null && PlayerManager.Instance != null)
            {
                Sprite iconSprite = PlayerManager.Instance.GetPlayerIcon(player.iconName);
                if (iconSprite != null)
                {
                    playerIconImage.sprite = iconSprite;
                    playerIconImage.enabled = true;
                    Debug.Log($"Panel3: Set PlayerIcon sprite (enabled={playerIconImage.enabled}, gameObject.activeSelf={playerIconImage.gameObject.activeSelf})");
                }
                else
                {
                    Debug.LogError($"Panel3: Failed to get icon sprite for '{player.iconName}'");
                }
            }
            else if (playerIconImage == null)
            {
                Debug.LogError($"Panel3: PlayerIconImage component is NULL on transform '{playerIconTransform?.name}'");
            }
        }

        Debug.Log($"Panel3: Total instantiated ranks={panel3ResultsContainer.childCount}, container activeInHierarchy={panel3ResultsContainer.gameObject.activeInHierarchy}");

        // Force layout rebuild to ensure instantiated UI elements are visible
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel3ResultsContainer.GetComponent<RectTransform>());
    }

    void DisplayFooledPlayers(Dictionary<string, int> votingResults)
    {
        // This data is now displayed via ResultsPlayerResponse prefabs in Panel 2
        // Each prefab shows the number of testers fooled by that specific answer
    }

    void UpdateRoundLabel()
    {
        int currentRound = GameManager.Instance.GetCurrentRound();

        // Hide all round labels
        foreach (var label in roundLabels)
        {
            if (label != null)
            {
                label.gameObject.SetActive(false);
            }
        }

        // Show current round label
        if (currentRound > 0 && currentRound <= roundLabels.Length)
        {
            if (roundLabels[currentRound - 1] != null)
            {
                roundLabels[currentRound - 1].gameObject.SetActive(true);
            }
        }
    }

    void UpdateRoundHeroImage()
    {
        int currentRound = GameManager.Instance.GetCurrentRound();

        // Hide all hero images
        foreach (var hero in heroImages)
        {
            if (hero != null)
            {
                hero.gameObject.SetActive(false);
            }
        }

        // Show current round's hero image
        if (currentRound > 0 && currentRound <= heroImages.Length)
        {
            if (heroImages[currentRound - 1] != null)
            {
                heroImages[currentRound - 1].gameObject.SetActive(true);
            }
        }
    }
    
    void UpdateNavigationButtons()
    {
        int currentRound = GameManager.Instance.GetCurrentRound();
        int totalRounds = (GameManager.Instance.gameMode.Value == GameManager.GameMode.EightQuestions) ? 8 : 12;
        
        bool isLastRound = currentRound >= totalRounds;
        
        if (nextRoundButton != null)
        {
            nextRoundButton.gameObject.SetActive(!isLastRound);
        }
        
        if (finalResultsButton != null)
        {
            finalResultsButton.gameObject.SetActive(isLastRound);
        }
    }
    
    public void OnNextRoundClicked()
    {
        MobileHaptics.MediumImpact();

        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            Debug.LogWarning("[RoundResults] Only the host can advance to the next screen.");
        }
    }

    public void OnFinalResultsClicked()
    {
        MobileHaptics.HeavyImpact();

        // Only server can load scenes and set game state
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            // Use GameManager's LoadScene method which uses NetworkSceneManager
            // LoadScene is already server-only in GameManager
            GameManager.Instance.LoadScene("FinalResults");
            GameManager.Instance.currentGameState.Value = GameManager.GameState.FinalResults;
        }
        else
        {
            Debug.LogWarning("[RoundResults] Non-server tried to load FinalResults - this should be server-driven");
        }
    }
    
    string GetLocalPlayerID()
    {
        return "player_" + SystemInfo.deviceUniqueIdentifier;
    }

    // Helper method to find child by name recursively (search all descendants)
    Transform FindDeepChild(Transform parent, string childName)
    {
        // Check direct children first
        Transform result = parent.Find(childName);
        if (result != null)
            return result;

        // Search all descendants
        foreach (Transform child in parent)
        {
            result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }

    // Helper method to get all children names recursively
    List<string> GetAllChildrenNames(Transform parent)
    {
        List<string> names = new List<string>();
        foreach (Transform child in parent)
        {
            names.Add(child.name);
            names.AddRange(GetAllChildrenNames(child));
        }
        return names;
    }
    
    void OnDestroy()
    {
        if (nextRoundButton != null)
        {
            nextRoundButton.onClick.RemoveListener(OnNextRoundClicked);
        }
        
        if (finalResultsButton != null)
        {
            finalResultsButton.onClick.RemoveListener(OnFinalResultsClicked);
        }
    }
}