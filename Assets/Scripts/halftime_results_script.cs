using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class HalftimeResultsScreen : MonoBehaviour
{
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    
    [Header("Winner Section")]
    public GameObject winnerSection;
    public TextMeshProUGUI winnerHeadline;
    public TextMeshProUGUI winnerName;
    public Transform winnerIconContainer;
    public Image winBackground;
    public TextMeshProUGUI scoreDiffWin;

    [Header("Loser Section")]
    public GameObject loserSection;
    public TextMeshProUGUI loserHeadline;
    public TextMeshProUGUI loserName;
    public Transform loserIconContainer;
    public Image loseBackground;
    public TextMeshProUGUI scoreDiffLose;
    
    [Header("Navigation")]
    public Button nextRoundButton;
    public GameObject buttonSlideout;
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public GameObject mobileResultsContainer;
    public TextMeshProUGUI mobilePlayerName;
    public TextMeshProUGUI mobilePlayerScore;
    public TextMeshProUGUI mobilePlayerRank;
    public Image mobileBackground;

    [Header("Prefabs")]
    public GameObject halftimeWinningPrefab; // Desktop winning section prefab
    public GameObject halftimeLosingPrefab; // Desktop losing section prefab
    public GameObject playerIconHalftimePrefab; // Mobile prefab

    [Header("Prefab Component Names")]
    [SerializeField] private string playerNameComponentName = "PlayerName";
    [SerializeField] private string playerScoreComponentName = "Score";
    [SerializeField] private string playerIconComponentName = "PlayerIcon";

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
            playerID = GetLocalPlayerID();
        }

        // Show appropriate display
        ShowAppropriateDisplay();

        // Display halftime results
        DisplayHalftimeResults();

        // Setup continue button
        if (nextRoundButton != null)
        {
            nextRoundButton.onClick.AddListener(OnContinueClicked);
        }

        // Set headlines
        if (winnerHeadline != null)
        {
            winnerHeadline.text = "LEADING THE PACK";
        }

        if (loserHeadline != null)
        {
            loserHeadline.text = "STILL IN THE RUNNING";
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
        if (!isMobile && currentPanel > 0 && currentPanel <= 2)
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
        // Hide both sections initially
        if (loserSection != null) loserSection.SetActive(false);
        if (winnerSection != null) winnerSection.SetActive(false);

        // Start with loser panel
        currentPanel = 1;
        panelTimer = 0f;
        ShowCurrentPanel();
    }

    void ShowCurrentPanel()
    {
        // Hide both sections
        if (loserSection != null) loserSection.SetActive(false);
        if (winnerSection != null) winnerSection.SetActive(false);

        // Show the current panel
        switch (currentPanel)
        {
            case 1:
                if (loserSection != null) loserSection.SetActive(true);
                break;
            case 2:
                if (winnerSection != null) winnerSection.SetActive(true);
                break;
        }
    }

    void AdvanceToNextPanel()
    {
        currentPanel++;
        panelTimer = 0f;

        if (currentPanel <= 2)
        {
            ShowCurrentPanel();
        }
        else
        {
            // Both panels shown, advance to next screen
            if (GameManager.Instance != null && GameManager.Instance.IsServer)
            {
                GameManager.Instance.AdvanceToNextScreen();
            }
            else
            {
                Debug.LogWarning("[HalftimeResults] Only the host can advance to the next screen.");
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
    
    void DisplayHalftimeResults()
    {
        List<PlayerData> rankedPlayers = GameManager.Instance.GetPlayersByRank();

        if (rankedPlayers.Count == 0) return;

        if (isMobile)
        {
            DisplayMobileHalftimeResults(rankedPlayers);
        }
        else
        {
            DisplayDesktopHalftimeResults(rankedPlayers);
        }
    }

    void DisplayDesktopHalftimeResults(List<PlayerData> rankedPlayers)
    {
        // Split players into top half (winners) and bottom half (losers)
        int midpoint = Mathf.CeilToInt(rankedPlayers.Count / 2f);

        List<PlayerData> winners = rankedPlayers.Take(midpoint).ToList();
        List<PlayerData> losers = rankedPlayers.Skip(midpoint).ToList();

        // Display winner section
        if (winnerSection != null)
        {
            // Set winner name (top player)
            if (winnerName != null && winners.Count > 0)
            {
                winnerName.text = winners[0].playerName;
            }

            // Display winner icons
            if (winnerIconContainer != null)
            {
                DisplayPlayerGroup(winners, winnerIconContainer, true);
            }
        }

        // Display loser section
        if (loserSection != null)
        {
            // Set loser name (bottom player)
            if (loserName != null && losers.Count > 0)
            {
                loserName.text = losers[losers.Count - 1].playerName;
            }

            // Display loser icons
            if (loserIconContainer != null)
            {
                DisplayPlayerGroup(losers, loserIconContainer, false);
            }
        }

        UpdateScoreDifferences(winners, losers);
    }

    void DisplayMobileHalftimeResults(List<PlayerData> rankedPlayers)
    {
        PlayerData localPlayer = GameManager.Instance.GetPlayer(playerID);

        if (localPlayer != null)
        {
            // Display player name
            if (mobilePlayerName != null)
            {
                mobilePlayerName.text = localPlayer.playerName;
            }

            // Display player score
            if (mobilePlayerScore != null)
            {
                mobilePlayerScore.text = localPlayer.scorePercentage + "%";
            }

            // Display player rank
            if (mobilePlayerRank != null)
            {
                int rank = rankedPlayers.FindIndex(p => p.playerID == playerID) + 1;
                mobilePlayerRank.text = "Rank: " + rank + " / " + rankedPlayers.Count;
            }

            // Display player icon
            if (mobileResultsContainer != null && playerIconHalftimePrefab != null)
            {
                // Clear existing icons
                foreach (Transform child in mobileResultsContainer.transform)
                {
                    if (child.GetComponent<Image>() != null) // Only destroy icon objects
                    {
                        Destroy(child.gameObject);
                    }
                }

                // Instantiate player icon
                GameObject iconObj = Instantiate(playerIconHalftimePrefab, mobileResultsContainer.transform);

                // Set player icon sprite
                Image iconImage = iconObj.GetComponent<Image>();
                if (iconImage != null && PlayerManager.Instance != null)
                {
                    iconImage.sprite = PlayerManager.Instance.GetPlayerIcon(localPlayer.iconName);
                }
            }
        }
    }
    
    void DisplayPlayerGroup(List<PlayerData> players, Transform container, bool isWinnerSection)
    {
        // Clear existing content
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // Display each player
        foreach (PlayerData player in players)
        {
            GameObject playerRow = CreatePlayerRow(player, container, isWinnerSection);

            // Additional styling for winner/loser sections could go here
        }
    }

    GameObject CreatePlayerRow(PlayerData player, Transform parent, bool isWinnerSection)
    {
        GameObject rowObj;

        // Use appropriate prefab based on winner/loser section
        GameObject prefabToUse = isWinnerSection ? halftimeWinningPrefab : halftimeLosingPrefab;

        if (prefabToUse != null)
        {
            rowObj = Instantiate(prefabToUse, parent);
            rowObj.SetActive(true);
        }
        else
        {
            // Create row programmatically
            rowObj = new GameObject("PlayerRow");
            rowObj.transform.SetParent(parent);
            rowObj.AddComponent<RectTransform>();
        }

        // Find and activate components using configurable names
        Transform nameTransform = FindDeepChild(rowObj.transform, playerNameComponentName);
        Transform scoreTransform = FindDeepChild(rowObj.transform, playerScoreComponentName);
        Transform iconTransform = FindDeepChild(rowObj.transform, playerIconComponentName);

        // Activate and set player name
        if (nameTransform != null) nameTransform.gameObject.SetActive(true);
        TextMeshProUGUI nameText = nameTransform?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.enabled = true;
            nameText.text = player.playerName;
        }

        // Activate and set score
        if (scoreTransform != null) scoreTransform.gameObject.SetActive(true);
        TextMeshProUGUI scoreText = scoreTransform?.GetComponent<TextMeshProUGUI>();
        if (scoreText != null)
        {
            scoreText.enabled = true;
            scoreText.text = player.scorePercentage + "%";
        }

        // Activate and set player icon
        if (iconTransform != null) iconTransform.gameObject.SetActive(true);
        Image iconImage = iconTransform?.GetComponent<Image>();
        if (iconImage != null && PlayerManager.Instance != null)
        {
            iconImage.enabled = true;
            Sprite iconSprite = PlayerManager.Instance.GetPlayerIcon(player.iconName);
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
            }
        }

        return rowObj;
    }

    void UpdateScoreDifferences(List<PlayerData> winners, List<PlayerData> losers)
    {
        if (scoreDiffWin == null && scoreDiffLose == null)
        {
            return;
        }

        bool hasWinner = winners != null && winners.Count > 0;
        bool hasLoser = losers != null && losers.Count > 0;

        if (!hasWinner || !hasLoser)
        {
            if (scoreDiffWin != null && hasWinner)
            {
                scoreDiffWin.text = winners[0].scorePercentage + "%";
            }

            if (scoreDiffLose != null && hasLoser)
            {
                scoreDiffLose.text = losers[0].scorePercentage + "%";
            }

            return;
        }

        int rawDifference = winners[0].scorePercentage - losers[0].scorePercentage;

        if (scoreDiffWin != null)
        {
            scoreDiffWin.text = FormatScoreDifference(rawDifference, true);
        }

        if (scoreDiffLose != null)
        {
            scoreDiffLose.text = FormatScoreDifference(rawDifference, false);
        }
    }

    string FormatScoreDifference(int rawDifference, bool forWinner)
    {
        if (rawDifference == 0)
        {
            return "0%";
        }

        int signedValue = forWinner ? rawDifference : -rawDifference;
        string prefix = signedValue > 0 ? "+" : "-";
        return $"{prefix}{Mathf.Abs(rawDifference)}%";
    }
    
    public void OnContinueClicked()
    {
        MobileHaptics.MediumImpact();

        // Continue to Bonus Round
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            Debug.LogWarning("[HalftimeResults] Only the host can advance to the next screen.");
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

    void OnDestroy()
    {
        if (nextRoundButton != null)
        {
            nextRoundButton.onClick.RemoveListener(OnContinueClicked);
        }
    }
}