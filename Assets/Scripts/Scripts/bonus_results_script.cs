using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BonusResultsScreen : MonoBehaviour
{
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public Transform resultsContainer;
    public TextMeshProUGUI resultsHeadline;
    public Image desktopBackground;
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public Transform mobileResultsContainer;
    public TextMeshProUGUI mobileResultsHeadline;
    public Image mobileBackground;
    
    [Header("Navigation")]
    public Button continueButton;
    
    [Header("Prefabs")]
    public GameObject resultRowPrefab; // Desktop prefab
    public GameObject mobileResultRowPrefab; // Mobile prefab

    [Header("Prefab Component Names")]
    [SerializeField] private string rankComponentName = "Rank";
    [SerializeField] private string playerNameComponentName = "PlayerName";
    [SerializeField] private string questionTextComponentName = "QuestionText";
    [SerializeField] private string scoreComponentName = "Score";
    [SerializeField] private string playerIconComponentName = "PlayerIcon";

    [Header("State")]
    private bool isMobile = false;
    private string playerID = "";
    
    void Start()
    {
        isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();
        
        if (isMobile)
        {
            playerID = GetLocalPlayerID();
        }
        
        // Show appropriate display
        ShowAppropriateDisplay();
        
        // Display bonus round results
        DisplayBonusResults();
        
        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        // Set headline
        if (resultsHeadline != null)
        {
            resultsHeadline.text = "BONUS ROUND COMPLETE!";
        }
        
        if (mobileResultsHeadline != null)
        {
            mobileResultsHeadline.text = "Bonus Round Results";
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
    
    void DisplayBonusResults()
    {
        Debug.Log("DisplayBonusResults called");

        Transform container = isMobile ? mobileResultsContainer : resultsContainer;

        Debug.Log($"Container is null: {container == null}, isMobile: {isMobile}");

        if (container == null)
        {
            Debug.LogError("Results container is NULL! Cannot display results.");
            return;
        }

        // Clear existing content
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        List<BonusQuestionResultInfo> bonusResults = null;

        if (GameManager.Instance != null)
        {
            bonusResults = GameManager.Instance.GetBonusQuestionResults();
        }

        if (bonusResults != null && bonusResults.Count > 0)
        {
            Debug.Log($"Rendering {bonusResults.Count} mini bonus question results");

            for (int i = 0; i < bonusResults.Count; i++)
            {
                CreateBonusResultRow(bonusResults[i], i + 1, container);
            }
        }
        else
        {
            Debug.LogWarning("No bonus question results recorded; falling back to player standings display.");
            DisplayPlayerStandingsFallback(container);
        }

        Debug.Log("DisplayBonusResults complete");
    }

    void DisplayPlayerStandingsFallback(Transform container)
    {
        List<PlayerData> rankedPlayers = GameManager.Instance != null ? GameManager.Instance.GetPlayersByRank() : new List<PlayerData>();

        Debug.Log($"Fallback standings count: {rankedPlayers.Count}");

        int rank = 1;
        foreach (PlayerData player in rankedPlayers)
        {
            CreatePlayerStandingsRow(player, rank, container);
            rank++;
        }
    }

    void CreatePlayerStandingsRow(PlayerData player, int rank, Transform parent)
    {
        GameObject rowObj;

        // Use appropriate prefab based on mobile/desktop
        GameObject prefabToUse = isMobile ? mobileResultRowPrefab : resultRowPrefab;

        Debug.Log($"CreateResultRow - prefab is null: {prefabToUse == null}, isMobile: {isMobile}");

        if (prefabToUse != null)
        {
            rowObj = Instantiate(prefabToUse, parent);
            rowObj.SetActive(true);
            Debug.Log($"Instantiated prefab for {player.playerName}");
        }
        else
        {
            // Create row programmatically
            Debug.LogWarning("Prefab is NULL, creating row programmatically");
            rowObj = new GameObject("ResultRow");
            rowObj.transform.SetParent(parent);
            rowObj.AddComponent<RectTransform>();
        }

        // Find and activate components using configurable names
        Transform rankTransform = FindDeepChild(rowObj.transform, rankComponentName);
        Transform nameTransform = FindDeepChild(rowObj.transform, playerNameComponentName);
        Transform questionTransform = FindDeepChild(rowObj.transform, questionTextComponentName);
        Transform scoreTransform = ResolveScoreTransform(rowObj.transform);
        Transform iconTransform = FindDeepChild(rowObj.transform, playerIconComponentName);

        // Activate and set rank
        if (rankTransform != null) rankTransform.gameObject.SetActive(true);
        TextMeshProUGUI rankText = rankTransform?.GetComponent<TextMeshProUGUI>();
        if (rankText != null)
        {
            rankText.enabled = true;
            rankText.text = rank.ToString();
        }

        // Activate and set player name
        if (nameTransform != null) nameTransform.gameObject.SetActive(true);
        TextMeshProUGUI nameText = nameTransform?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.enabled = true;
            string displayName = player.playerName;
            if (rankText == null)
            {
                displayName = $"{rank}. {displayName}";
            }

            nameText.text = displayName;
        }

        // Hide question text for standings rows
        if (questionTransform != null)
        {
            questionTransform.gameObject.SetActive(false);
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
    }

    void CreateBonusResultRow(BonusQuestionResultInfo result, int questionNumber, Transform parent)
    {
        GameObject rowObj;

        GameObject prefabToUse = isMobile ? mobileResultRowPrefab : resultRowPrefab;

        if (prefabToUse != null)
        {
            rowObj = Instantiate(prefabToUse, parent);
            rowObj.SetActive(true);
            Debug.Log($"Instantiated bonus result row for question {questionNumber}");
        }
        else
        {
            Debug.LogWarning("Prefab is NULL, creating bonus result row programmatically");
            rowObj = new GameObject("BonusResultRow");
            rowObj.transform.SetParent(parent);
            rowObj.AddComponent<RectTransform>();
        }

        Transform rankTransform = FindDeepChild(rowObj.transform, rankComponentName);
        Transform nameTransform = FindDeepChild(rowObj.transform, playerNameComponentName);
        Transform questionTransform = FindDeepChild(rowObj.transform, questionTextComponentName);
        Transform scoreTransform = ResolveScoreTransform(rowObj.transform);
        Transform iconTransform = FindDeepChild(rowObj.transform, playerIconComponentName);

        if (rankTransform != null) rankTransform.gameObject.SetActive(true);
        TextMeshProUGUI rankText = rankTransform?.GetComponent<TextMeshProUGUI>();
        if (rankText != null)
        {
            rankText.enabled = true;
            rankText.text = questionNumber.ToString();
        }

        if (nameTransform != null) nameTransform.gameObject.SetActive(true);
        TextMeshProUGUI nameText = nameTransform?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.enabled = true;
            nameText.text = result.hasWinner ? result.winnerNames : "No votes cast";
        }

        if (questionTransform != null) questionTransform.gameObject.SetActive(true);
        TextMeshProUGUI questionText = questionTransform?.GetComponent<TextMeshProUGUI>();
        if (questionText != null)
        {
            questionText.enabled = true;

            // Some legacy prefabs do not include a dedicated rank label. In that case we
            // prefix the question text so players still see which prompt the row
            // represents instead of rendering a blank number column.
            string resolvedQuestionText = result.questionText ?? string.Empty;
            if (rankText == null)
            {
                resolvedQuestionText = $"Q{questionNumber}: {resolvedQuestionText}";
            }

            questionText.text = resolvedQuestionText;
        }

        if (scoreTransform != null) scoreTransform.gameObject.SetActive(true);
        TextMeshProUGUI scoreText = scoreTransform?.GetComponent<TextMeshProUGUI>();
        if (scoreText != null)
        {
            scoreText.enabled = true;
            int pointsAwarded = result.hasWinner ? result.pointsAwarded : 0;
            string scoreLabel = pointsAwarded > 0 ? $"+{pointsAwarded} pts" : "+0 pts";
            if (result.hasWinner && result.winningVoteCount > 0)
            {
                scoreLabel += $"\n({result.winningVoteCount} votes)";
            }
            scoreText.text = scoreLabel;
        }

        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            bool shouldShowIcon = result.hasWinner && !string.IsNullOrEmpty(result.winnerIcon) && PlayerManager.Instance != null;

            if (iconImage != null)
            {
                if (shouldShowIcon)
                {
                    Sprite iconSprite = PlayerManager.Instance.GetPlayerIcon(result.winnerIcon);
                    if (iconSprite != null)
                    {
                        iconTransform.gameObject.SetActive(true);
                        iconImage.enabled = true;
                        iconImage.sprite = iconSprite;
                    }
                    else
                    {
                        iconTransform.gameObject.SetActive(false);
                    }
                }
                else
                {
                    iconTransform.gameObject.SetActive(false);
                }
            }
            else
            {
                iconTransform.gameObject.SetActive(false);
            }
        }
    }

    Transform ResolveScoreTransform(Transform parent)
    {
        Transform scoreTransform = FindDeepChild(parent, scoreComponentName);

        if (scoreTransform == null && scoreComponentName != "ScoreDiff")
        {
            scoreTransform = FindDeepChild(parent, "ScoreDiff");
        }

        return scoreTransform;
    }
    
    public void OnContinueClicked()
    {
        MobileHaptics.MediumImpact();

        // Continue to next round (Round 5 for 8Q, Round 7 for 12Q)
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            Debug.LogWarning("[BonusResults] Only the host can advance to the next screen.");
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
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }
}