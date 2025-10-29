using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinalResultsScreen : MonoBehaviour
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
    public TextMeshProUGUI winnerScore;
    
    [Header("Loser Section")]
    public GameObject loserSection;
    public TextMeshProUGUI loserHeadline;
    public TextMeshProUGUI loserName;
    public Transform loserIconContainer;
    public Image loseBackground;
    public TextMeshProUGUI scoreDiffLose;
    
    [Header("Desktop Navigation")]
    public Button creditsButton; // Go to Credits scene
    public Button newGameButton; // Play Again - back to lobby
    public GameObject buttonSlideout;
    public Image hero;

    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public GameObject mobileResultsContainer;
    public TextMeshProUGUI mobileWinnerName;
    public TextMeshProUGUI mobilePlayerRank;
    public TextMeshProUGUI mobilePlayerScore;
    public TextMeshProUGUI mobilePlayerName;
    public Button mobileShareButton;
    public Button mobileWebButton;
    public Image mobileHero;
    public Image mobileBackground;

    [Header("Share Configuration")]
    [SerializeField] private Sprite winnerShareTemplate;
    [SerializeField] private Sprite participantShareTemplate;
    [SerializeField] private TMP_FontAsset havoksFont;
    [SerializeField] private Vector2Int shareImageResolution = new Vector2Int(1080, 1920);
    [SerializeField] private float shareNameFontSize = 120f;
    [SerializeField] private float shareScoreFontSize = 90f;
    [SerializeField] private Color shareNameColor = Color.white;
    [SerializeField] private Color shareScoreColor = Color.white;
    [SerializeField] private Vector2 shareNameAnchor = new Vector2(0.5f, 0.2f);
    [SerializeField] private Vector2 shareScoreAnchor = new Vector2(0.5f, 0.08f);
    [SerializeField] private Vector2 shareNameOffset = Vector2.zero;
    [SerializeField] private Vector2 shareScoreOffset = Vector2.zero;
    [SerializeField] private Vector2 shareTextBounds = new Vector2(960f, 240f);
    [SerializeField] private string winnerShareMessage = "{0} just won Robots Wearing Moustaches with {1}% human!";
    [SerializeField] private string participantShareMessage = "{0} scored {1}% human in Robots Wearing Moustaches!";

    [Header("Prefabs")]
    public GameObject finalWinningPrefab; // Desktop winning section prefab
    public GameObject finalLosingPrefab; // Desktop losing section prefab

    [Header("Prefab Component Names")]
    [SerializeField] private string playerNameComponentName = "PlayerName";
    [SerializeField] private string playerIconComponentName = "PlayerIcon";

    [Header("State")]
    private bool isMobile = false;
    private string playerID = "";
    private int currentPanel = 0;
    private float panelTimer = 0f;
    private const float PANEL_DISPLAY_DURATION = 5f;
    private bool isSharing = false;
    
    void Start()
    {
        isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

        if (isMobile)
        {
            playerID = GetLocalPlayerID();
        }

        // Show appropriate display
        ShowAppropriateDisplay();

        // Display final results
        DisplayFinalResults();

        // Preload credits data for faster transition
        PreloadCreditsData();

        // Setup buttons
        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(OnCreditsClicked);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        if (mobileShareButton != null)
        {
            mobileShareButton.onClick.AddListener(OnShareClicked);
        }

        if (mobileWebButton != null)
        {
            mobileWebButton.onClick.AddListener(OnWebsiteClicked);
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
        // After both panels shown, stay on screen (no auto-advance for final results)
    }

    void PreloadCreditsData()
    {
        // Preload the credits JSON file so it's cached for CreditsScreen
        TextAsset creditsFile = Resources.Load<TextAsset>("credits");

        if (creditsFile != null)
        {
            Debug.Log("Credits data preloaded successfully");
        }
        else
        {
            Debug.LogWarning("Could not preload credits data - credits.json not found in Resources folder");
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
    
    void DisplayFinalResults()
    {
        List<PlayerData> rankedPlayers = GameManager.Instance.GetPlayersByRank();
        
        if (rankedPlayers.Count == 0) return;
        
        if (isMobile)
        {
            DisplayMobileFinalResults(rankedPlayers);
        }
        else
        {
            DisplayDesktopFinalResults(rankedPlayers);
        }
    }
    
    void DisplayDesktopFinalResults(List<PlayerData> rankedPlayers)
    {
        // Get winner (highest score)
        PlayerData winner = rankedPlayers[0];

        // Get loser (lowest score)
        PlayerData loser = rankedPlayers[rankedPlayers.Count - 1];

        // Display winner section
        if (winnerSection != null)
        {
            if (winnerHeadline != null)
            {
                winnerHeadline.text = "MOST HUMAN";
            }

            if (winnerName != null)
            {
                winnerName.text = winner.playerName;
            }

            if (winnerScore != null)
            {
                winnerScore.text = winner.scorePercentage + "%";
            }

            UpdateWinnerScoreDifference(rankedPlayers, winner, loser);

            // Display winner icon
            if (winnerIconContainer != null)
            {
                DisplayPlayerIcon(winner, winnerIconContainer, true);
            }
        }

        // Display loser section (bottom player)
        if (loserSection != null)
        {
            if (loserHeadline != null)
            {
                loserHeadline.text = "PROBABLY A ROBOT";
            }

            if (loserName != null)
            {
                loserName.text = loser.playerName;
            }

            UpdateLoserScoreDifference(rankedPlayers, winner, loser);

            // Display loser icon
            if (loserIconContainer != null)
            {
                DisplayPlayerIcon(loser, loserIconContainer, false);
            }
        }

        // Display all players in ranked order
        DisplayFullLeaderboard(rankedPlayers);
    }

    void UpdateWinnerScoreDifference(List<PlayerData> rankedPlayers, PlayerData winner, PlayerData loser)
    {
        if (scoreDiffWin == null)
        {
            return;
        }

        bool sharesComponentWithWinnerScore = winnerScore != null && ReferenceEquals(scoreDiffWin, winnerScore);
        int? scoreGap = CalculateScoreGap(rankedPlayers, winner, loser);

        if (sharesComponentWithWinnerScore)
        {
            // The scene assigns the same Text component for the winner score and difference.
            // Keep the plain score that was already written to avoid overwriting it with a diff label.
            return;
        }

        if (!scoreGap.HasValue)
        {
            scoreDiffWin.text = winner.scorePercentage + "%";
            return;
        }

        scoreDiffWin.text = FormatScoreDifference(scoreGap.Value, true);
    }

    void UpdateLoserScoreDifference(List<PlayerData> rankedPlayers, PlayerData winner, PlayerData loser)
    {
        if (scoreDiffLose == null)
        {
            return;
        }

        int? scoreGap = CalculateScoreGap(rankedPlayers, winner, loser);

        if (!scoreGap.HasValue)
        {
            scoreDiffLose.text = loser.scorePercentage + "%";
            return;
        }

        scoreDiffLose.text = FormatScoreDifference(scoreGap.Value, false);
    }

    int? CalculateScoreGap(List<PlayerData> rankedPlayers, PlayerData winner, PlayerData loser)
    {
        if (rankedPlayers == null || rankedPlayers.Count <= 1 || winner == null || loser == null)
        {
            return null;
        }

        return winner.scorePercentage - loser.scorePercentage;
    }

    string FormatScoreDifference(int rawDifference, bool forWinner)
    {
        if (rawDifference == 0)
        {
            return "0%";
        }

        int signedValue = forWinner ? rawDifference : -rawDifference;
        int magnitude = Mathf.Abs(rawDifference);
        string prefix = signedValue > 0 ? "+" : "-";
        return $"{prefix}{magnitude}%";
    }
    
    void DisplayMobileFinalResults(List<PlayerData> rankedPlayers)
    {
        PlayerData winner = rankedPlayers[0];
        PlayerData localPlayer = GameManager.Instance.GetPlayer(playerID);
        
        // Show winner
        if (mobileWinnerName != null)
        {
            mobileWinnerName.text = winner.playerName + " WINS!";
        }
        
        // Show local player's results
        if (localPlayer != null)
        {
            if (mobilePlayerName != null)
            {
                mobilePlayerName.text = localPlayer.playerName;
            }
            
            if (mobilePlayerScore != null)
            {
                mobilePlayerScore.text = localPlayer.scorePercentage + "%";
            }
            
            int rank = rankedPlayers.FindIndex(p => p.playerID == playerID) + 1;
            if (mobilePlayerRank != null)
            {
                mobilePlayerRank.text = "Rank: " + rank + " / " + rankedPlayers.Count;
            }
        }
    }
    
    void DisplayPlayerIcon(PlayerData player, Transform container, bool isWinnerSection)
    {
        // Clear existing
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // Use appropriate prefab based on winner/loser section
        GameObject prefabToUse = isWinnerSection ? finalWinningPrefab : finalLosingPrefab;

        // Create player icon
        if (prefabToUse != null)
        {
            GameObject iconObj = Instantiate(prefabToUse, container);
            iconObj.SetActive(true);

            // Find and activate components using configurable names
            Transform nameTransform = FindDeepChild(iconObj.transform, playerNameComponentName);
            Transform iconTransform = FindDeepChild(iconObj.transform, playerIconComponentName);

            // Activate and set player name
            if (nameTransform != null) nameTransform.gameObject.SetActive(true);
            TextMeshProUGUI nameText = nameTransform?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.enabled = true;
                nameText.text = player.playerName;
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
    }
    
    void DisplayFullLeaderboard(List<PlayerData> rankedPlayers)
    {
        // This would populate a full leaderboard display
        // Implementation depends on your UI design
    }
    
    public void OnCreditsClicked()
    {
        MobileHaptics.MediumImpact();

        // Only server can load scenes
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            Debug.LogWarning("[FinalResults] Non-server tried to load Credits - this should be server-driven");
        }
    }

    public void OnNewGameClicked()
    {
        MobileHaptics.HeavyImpact();

        // Only server can reset game state and load scenes
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            // Use server-authoritative reset method
            GameManager.Instance.ResetGameState();

            // Load lobby scene using NetworkSceneManager
            GameManager.Instance.LoadScene("LobbyScreen");
        }
        else
        {
            Debug.LogWarning("[FinalResults] Non-server tried to reset game - this should be server-driven");
        }
    }

    public void OnShareClicked()
    {
        MobileHaptics.LightImpact();

        if (!isMobile)
        {
            Debug.LogWarning("Share button is intended for mobile devices only.");
            return;
        }

        if (isSharing)
        {
            Debug.Log("Share already in progress, ignoring additional tap.");
            return;
        }

        StartCoroutine(ShareResultsRoutine());
    }

    IEnumerator ShareResultsRoutine()
    {
        isSharing = true;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("Unable to share results - GameManager missing.");
            isSharing = false;
            yield break;
        }

        PlayerData localPlayer = GameManager.Instance.GetPlayer(playerID);
        if (localPlayer == null)
        {
            Debug.LogWarning("Unable to share results - Local player not found.");
            isSharing = false;
            yield break;
        }

        List<PlayerData> rankedPlayers = GameManager.Instance.GetPlayersByRank();
        bool isWinner = rankedPlayers.Count > 0 && rankedPlayers[0].playerID == localPlayer.playerID;

        Sprite template = isWinner ? winnerShareTemplate : participantShareTemplate;
        if (template == null)
        {
            Debug.LogWarning("Unable to share results - Share template not assigned.");
            isSharing = false;
            yield break;
        }

        if (havoksFont == null)
        {
            Debug.LogWarning("Unable to share results - Havoks font asset not assigned.");
            isSharing = false;
            yield break;
        }

        string shareDisplayName = string.IsNullOrWhiteSpace(localPlayer.playerName) ? "Player" : localPlayer.playerName;
        int shareScore = localPlayer.scorePercentage;

        Texture2D shareTexture = null;
        yield return StartCoroutine(BuildShareTexture(template, localPlayer, shareNameAnchor, shareScoreAnchor, tex => shareTexture = tex));

        if (shareTexture == null)
        {
            Debug.LogWarning("Share generation failed.");
            isSharing = false;
            yield break;
        }

        string cacheDirectory = Application.temporaryCachePath;
        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }

        string fileName = $"final-results-{localPlayer.playerID}-{DateTime.Now:yyyyMMddHHmmss}.png";
        string filePath = Path.Combine(cacheDirectory, fileName);

        try
        {
            File.WriteAllBytes(filePath, shareTexture.EncodeToPNG());
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save share image: {ex.Message}");
            isSharing = false;
            yield break;
        }

        string messageTemplate = isWinner ? winnerShareMessage : participantShareMessage;
        if (string.IsNullOrWhiteSpace(messageTemplate))
        {
            messageTemplate = isWinner
                ? "{0} just won Robots Wearing Moustaches with {1}% human!"
                : "{0} scored {1}% human in Robots Wearing Moustaches!";
        }

        string safeShareName = shareDisplayName.Replace("{", "{{").Replace("}", "}}");
        string shareMessage;
        try
        {
            shareMessage = string.Format(messageTemplate, safeShareName, shareScore);
        }
        catch (FormatException ex)
        {
            Debug.LogWarning($"Invalid share message template '{messageTemplate}': {ex.Message}");
            shareMessage = string.Format("{0} scored {1}% human in Robots Wearing Moustaches!", safeShareName, shareScore);
        }

        yield return StartCoroutine(ShareImage(filePath, shareMessage));

        Destroy(shareTexture);

        isSharing = false;
    }

    IEnumerator BuildShareTexture(Sprite template, PlayerData player, Vector2 nameAnchor, Vector2 scoreAnchor, Action<Texture2D> onComplete)
    {
        if (shareImageResolution.x <= 0 || shareImageResolution.y <= 0)
        {
            Debug.LogWarning("Invalid share resolution specified.");
            onComplete?.Invoke(null);
            yield break;
        }

        RenderTexture renderTexture = new RenderTexture(shareImageResolution.x, shareImageResolution.y, 24, RenderTextureFormat.ARGB32);
        GameObject cameraGO = new GameObject("ShareRenderCamera");
        Camera renderCamera = cameraGO.AddComponent<Camera>();
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = new Color(0, 0, 0, 0);
        renderCamera.orthographic = true;
        renderCamera.orthographicSize = shareImageResolution.y * 0.5f;
        renderCamera.nearClipPlane = -10f;
        renderCamera.farClipPlane = 10f;
        renderCamera.targetTexture = renderTexture;

        GameObject canvasGO = new GameObject("ShareRenderCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = renderCamera;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = shareImageResolution;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        Image backgroundImage = new GameObject("ShareBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        backgroundImage.transform.SetParent(canvas.transform, false);
        backgroundImage.sprite = template;
        backgroundImage.preserveAspect = false;
        RectTransform bgRect = backgroundImage.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        string displayName = string.IsNullOrWhiteSpace(player.playerName) ? "PLAYER" : player.playerName.ToUpperInvariant();
        string scoreValue = player.scorePercentage + "% HUMAN";

        TextMeshProUGUI nameText = CreateShareText(canvas.transform, "SharePlayerName", displayName, shareNameFontSize, shareNameColor, nameAnchor, shareNameOffset);
        nameText.characterSpacing = 4f;
        TextMeshProUGUI scoreText = CreateShareText(canvas.transform, "SharePlayerScore", scoreValue, shareScoreFontSize, shareScoreColor, scoreAnchor, shareScoreOffset);
        scoreText.characterSpacing = 2f;

        yield return new WaitForEndOfFrame();

        renderCamera.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D result = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
        result.Apply(false, false);
        RenderTexture.active = previous;

        renderCamera.targetTexture = null;
        renderTexture.Release();
        Destroy(renderTexture);

        Destroy(canvasGO);
        Destroy(cameraGO);

        onComplete?.Invoke(result);
    }

    TextMeshProUGUI CreateShareText(Transform parent, string objectName, string value, float fontSize, Color color, Vector2 anchor, Vector2 offset)
    {
        GameObject textGO = new GameObject(objectName, typeof(RectTransform));
        textGO.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.font = havoksFont;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = value;
        tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
        tmp.richText = false;

        RectTransform rect = tmp.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = shareTextBounds;
        rect.anchoredPosition = offset;

        return tmp;
    }

    IEnumerator ShareImage(string filePath, string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        yield return null;
        try
        {
            using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent"))
            {
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));

                // Use FileProvider for Android 7.0+ compatibility
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaClass fileProviderClass = new AndroidJavaClass("androidx.core.content.FileProvider"))
                using (AndroidJavaObject fileObject = new AndroidJavaObject("java.io.File", filePath))
                {
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    string authority = Application.identifier + ".fileprovider";
                    AndroidJavaObject uri = fileProviderClass.CallStatic<AndroidJavaObject>("getUriForFile", currentActivity, authority, fileObject);

                    intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uri);
                    intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));
                }

                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), message);
                intentObject.Call<AndroidJavaObject>("setType", "image/png");

                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Share Results");
                    currentActivity.Call("startActivity", chooser);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Android share failed: {ex.Message}");
        }
#elif UNITY_IOS && !UNITY_EDITOR
        yield return null;
        ShareImageIOS(filePath, message);
#else
        Debug.Log("Share image generated: " + filePath);
        yield return null;
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _ShareImage(string imagePath, string message);

    private void ShareImageIOS(string filePath, string message)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"iOS share failed: File not found at {filePath}");
            return;
        }

        _ShareImage(filePath, message);
    }
#endif

    public void OnWebsiteClicked()
    {
        MobileHaptics.LightImpact();

        // Open game website
        Debug.Log("Website clicked");
        Application.OpenURL("https://robotswearingmoustaches.com"); // Replace with actual URL
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
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveListener(OnCreditsClicked);
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveListener(OnNewGameClicked);
        }

        if (mobileShareButton != null)
        {
            mobileShareButton.onClick.RemoveListener(OnShareClicked);
        }

        if (mobileWebButton != null)
        {
            mobileWebButton.onClick.RemoveListener(OnWebsiteClicked);
        }
    }
}