using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BonusQuestionScreen : MonoBehaviour
{
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public GameObject bonusQuestionCard;
    public TextMeshProUGUI questionText;
    public Transform playerIconContainer;
    public GameObject playerIconBonusPrefab;
    public Transform optionListContainer;
    public TextMeshProUGUI timerCountdown;
    public GameObject timerContainer;
    public TextMeshProUGUI tipText;
    public Image desktopBackground;
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public Transform bonusListMobile;
    public Button bonusSubmitButton;
    public Image mobileBackground;
    public GameObject bonusResponse;
    
    [Header("Player Button Prefab")]
    public GameObject playerButtonPrefab;
    
    [Header("State")]
    private bool isMobile = false;
    private string playerID = "";
    private string selectedPlayerID = "";
    private List<GameObject> spawnedPlayerButtons = new List<GameObject>();
    private List<GameObject> spawnedPlayerIcons = new List<GameObject>();
    
    void Start()
    {
        isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

        if (isMobile)
        {
            playerID = PlayerAuthSystem.Instance != null ? PlayerAuthSystem.Instance.GetLocalPlayerID() : GetLocalPlayerID();
        }

        // Show appropriate display
        ShowAppropriateDisplay();

        // Initialize the bonus round
        InitializeBonusQuestion();

        // Setup mobile submit button
        if (bonusSubmitButton != null)
        {
            bonusSubmitButton.onClick.AddListener(OnSubmitVote);
            bonusSubmitButton.interactable = false;
        }

        // Set tip text
        if (tipText != null)
        {
            tipText.text = "Vote for a player!";
        }

        // Play appropriate music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBonusQuestionMusic();
        }
    }

    void InitializeBonusQuestion()
    {
        Debug.Log("InitializeBonusQuestion called for question index: " + GameManager.Instance.currentBonusQuestion.Value);

        // Display current bonus question
        DisplayBonusQuestion();

        // Display player selection options (this will recreate icons hidden)
        DisplayPlayerOptions();

        // Reset selected player
        selectedPlayerID = "";

        // Re-enable buttons for new question on mobile
        if (isMobile)
        {
            if (bonusSubmitButton != null)
            {
                bonusSubmitButton.interactable = false;
            }

            // Re-enable all player buttons
            foreach (GameObject btn in spawnedPlayerButtons)
            {
                Button button = btn.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = true;
                }

                // Reset button colors
                Image btnImage = btn.GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = Color.white;
                }

                // Hide circle indicators
                Transform circle = FindCircleTransform(btn);
                if (circle != null)
                {
                    circle.gameObject.SetActive(false);
                }
            }
        }
    }

    private int lastBonusQuestionIndex = -1;

    void Update()
    {
        // Check if we moved to next bonus question
        int currentQuestionIndex = GameManager.Instance.currentBonusQuestion.Value;
        int totalBonusQuestions = GameManager.Instance.GetBonusQuestionCount();
        if (currentQuestionIndex != lastBonusQuestionIndex && lastBonusQuestionIndex != -1 && currentQuestionIndex < totalBonusQuestions)
        {
            // New bonus question started (only if still within the total questions)
            Debug.Log("Detected new bonus question: " + currentQuestionIndex);
            InitializeBonusQuestion();
        }
        lastBonusQuestionIndex = currentQuestionIndex;

        // Update timer display
        UpdateTimerDisplay();

        // Update vote counts on desktop
        if (!isMobile)
        {
            UpdateVoteCounts();
        }

        // Check timer warning for audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.CheckTimerWarning(GameManager.Instance.GetTimeRemaining());
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
    
    void DisplayBonusQuestion()
    {
        string currentQuestion = GameManager.Instance.GetCurrentBonusQuestion();

        if (questionText != null)
        {
            questionText.text = currentQuestion;
        }

        // Show question number (1/4, 2/4, etc.)
        int questionNum = GameManager.Instance.currentBonusQuestion.Value + 1;
        int totalQuestions = GameManager.Instance.GetBonusQuestionCount();
        Debug.Log("Bonus Question " + questionNum + "/" + totalQuestions);
    }
    
    void DisplayPlayerOptions()
    {
        List<PlayerData> players = GameManager.Instance.GetAllPlayers();
        
        if (isMobile)
        {
            // Mobile: Show as selectable list
            DisplayMobilePlayerList(players);
        }
        else
        {
            // Desktop: Show player icons
            DisplayDesktopPlayerIcons(players);
        }
    }
    
    void DisplayMobilePlayerList(List<PlayerData> players)
    {
        if (bonusListMobile == null) return;
        
        // Clear existing buttons
        foreach (GameObject btn in spawnedPlayerButtons)
        {
            Destroy(btn);
        }
        spawnedPlayerButtons.Clear();
        
        // Create button for each player
        foreach (PlayerData player in players)
        {
            CreatePlayerButton(player, bonusListMobile);
        }
    }
    
    void CreatePlayerButton(PlayerData player, Transform parent)
    {
        GameObject buttonObj;
        
        if (playerButtonPrefab != null)
        {
            buttonObj = Instantiate(playerButtonPrefab, parent);
        }
        else
        {
            // Create button programmatically
            buttonObj = new GameObject("PlayerButton");
            buttonObj.transform.SetParent(parent);
            buttonObj.AddComponent<RectTransform>();
            buttonObj.AddComponent<Image>();
            buttonObj.AddComponent<Button>();
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            textObj.AddComponent<RectTransform>();
            textObj.AddComponent<TextMeshProUGUI>();
        }

        // Ensure circle indicator starts hidden on new buttons
        Transform circle = FindCircleTransform(buttonObj);
        if (circle != null)
        {
            circle.gameObject.SetActive(false);
        }

        // Set button text
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = player.playerName;
        }
        
        // Set player icon
        Transform iconTransform = FindPlayerIconTransform(buttonObj);
        if (iconTransform != null && PlayerManager.Instance != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = PlayerManager.Instance.GetPlayerIcon(player.iconName);
            }
        }
        
        // Add click listener
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnPlayerSelected(player.playerID, buttonObj));
        }
        
        spawnedPlayerButtons.Add(buttonObj);
    }
    
    void DisplayDesktopPlayerIcons(List<PlayerData> players)
    {
        if (playerIconContainer == null) return;

        // Clear existing icons
        foreach (GameObject icon in spawnedPlayerIcons)
        {
            Destroy(icon);
        }
        spawnedPlayerIcons.Clear();

        // Create icon for each player (visible but greyed out initially)
        foreach (PlayerData player in players)
        {
            if (playerIconBonusPrefab != null)
            {
                GameObject iconObj = Instantiate(playerIconBonusPrefab, playerIconContainer);

                // Set player name
                Transform nameTransform = FindPlayerNameTransform(iconObj);
                if (nameTransform != null)
                {
                    TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = player.playerName;
                    }
                }

                // Set player icon
                Transform iconTransform = FindPlayerIconTransform(iconObj);
                if (iconTransform != null && PlayerManager.Instance != null)
                {
                    Image iconImage = iconTransform.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        iconImage.sprite = PlayerManager.Instance.GetPlayerIcon(player.iconName);
                    }
                }

                // Start greyed out with circle hidden
                SetIconGreyedOut(iconObj, true);

                spawnedPlayerIcons.Add(iconObj);
            }
        }
    }
    
    void OnPlayerSelected(string votedPlayerID, GameObject buttonObj)
    {
        MobileHaptics.SelectionChanged();

        selectedPlayerID = votedPlayerID;
        
        // Visual feedback
        foreach (GameObject btn in spawnedPlayerButtons)
        {
            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = Color.white;
            }

            // Hide circle indicators
            Transform circle = FindCircleTransform(btn);
            if (circle != null)
            {
                circle.gameObject.SetActive(false);
            }
        }

        // Highlight selected
        Image selectedImage = buttonObj.GetComponent<Image>();
        if (selectedImage != null)
        {
            selectedImage.color = Color.yellow;
        }

        // Show circle on selected
        Transform selectedCircle = FindCircleTransform(buttonObj);
        if (selectedCircle != null)
        {
            selectedCircle.gameObject.SetActive(true);
        }
        
        if (isMobile)
        {
            // Enable submit button
            if (bonusSubmitButton != null)
            {
                bonusSubmitButton.interactable = true;
            }
        }
        // Desktop is display-only, doesn't submit votes
        
        Debug.Log("Player selected: " + votedPlayerID);
    }
    
    public void OnSubmitVote()
    {
        MobileHaptics.MediumImpact();

        if (string.IsNullOrEmpty(selectedPlayerID))
        {
            Debug.LogWarning("No player selected!");
            MobileHaptics.Failure();
            return;
        }

        SubmitBonusVote();
    }
    
    void SubmitBonusVote()
    {
        // Submit vote via NetworkManager
        if (RWMNetworkManager.Instance != null && RWMNetworkManager.Instance.isConnected)
        {
            RWMNetworkManager.Instance.SubmitBonusVote(selectedPlayerID);
        }
        else
        {
            // Fallback for local testing without network
            GameManager.Instance.SubmitBonusVote(playerID, selectedPlayerID);
        }

        // Disable all buttons
        foreach (GameObject btn in spawnedPlayerButtons)
        {
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }

        if (bonusSubmitButton != null)
        {
            bonusSubmitButton.interactable = false;
        }

        Debug.Log("Bonus vote submitted for: " + selectedPlayerID);
    }
    
    void UpdateTimerDisplay()
    {
        if (timerCountdown != null)
        {
            timerCountdown.text = GameManager.Instance.GetTimerDisplay();
        }
    }
    
    void UpdateVoteCounts()
    {
        // Update icon appearance for players who have voted
        List<PlayerData> allPlayers = GameManager.Instance.GetAllPlayers();
        var bonusVotes = GameManager.Instance.BonusVotes;

        for (int i = 0; i < allPlayers.Count && i < spawnedPlayerIcons.Count; i++)
        {
            PlayerData player = allPlayers[i];
            GameObject iconObj = spawnedPlayerIcons[i];

            // Check if player has voted
            bool hasVoted = bonusVotes.ContainsKey(player.playerID);

            // Update visual state (greyed out if not voted, full color if voted)
            SetIconGreyedOut(iconObj, !hasVoted);
        }
    }

    void SetIconGreyedOut(GameObject iconObj, bool isGreyedOut)
    {
        if (iconObj == null) return;

        // Find all image components and grey them out or restore color
        Image[] images = iconObj.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            if (isGreyedOut)
            {
                img.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey
            }
            else
            {
                img.color = Color.white; // Full color
            }
        }

        // Find circle and animate it
        Transform circleTransform = FindCircleTransform(iconObj);
        if (circleTransform != null)
        {
            if (isGreyedOut)
            {
                // Hide circle when greyed out
                circleTransform.gameObject.SetActive(false);
                circleTransform.localScale = Vector3.zero;
            }
            else
            {
                // Show and animate circle scaling up when activated
                circleTransform.gameObject.SetActive(true);
                StartCoroutine(ScaleUpCircle(circleTransform));
            }
        }
    }

    System.Collections.IEnumerator ScaleUpCircle(Transform circleTransform)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out effect
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            circleTransform.localScale = Vector3.Lerp(startScale, targetScale, easedT);
            yield return null;
        }

        circleTransform.localScale = targetScale;
    }

    Transform FindCircleTransform(GameObject buttonObj)
    {
        if (buttonObj == null)
        {
            return null;
        }

        Transform circle = buttonObj.transform.Find("Circle");
        if (circle != null)
        {
            return circle;
        }

        Transform icon = buttonObj.transform.Find("PlayerIcon");
        if (icon != null)
        {
            circle = icon.Find("Circle");
        }

        return circle;
    }

    Transform FindPlayerIconTransform(GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }

        // Direct child search
        Transform icon = obj.transform.Find("PlayerIcon");
        if (icon != null)
        {
            return icon;
        }

        // Could add additional fallback logic here if needed
        return null;
    }

    Transform FindPlayerNameTransform(GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }

        // Direct child search
        Transform nameTransform = obj.transform.Find("PlayerName");
        if (nameTransform != null)
        {
            return nameTransform;
        }

        // Could add additional fallback logic here if needed
        return null;
    }

    string GetLocalPlayerID()
    {
        return "player_" + SystemInfo.deviceUniqueIdentifier;
    }
    
    void OnDestroy()
    {
        if (bonusSubmitButton != null)
        {
            bonusSubmitButton.onClick.RemoveListener(OnSubmitVote);
        }
        
        foreach (GameObject btn in spawnedPlayerButtons)
        {
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
}