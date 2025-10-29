using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

public class EliminationScreen : MonoBehaviour
{
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public TextMeshProUGUI headerText;
    public Transform answerListContainer;
    public TextMeshProUGUI timerCountdown;
    public Image hero;
    public Image desktopBackground;
    public GameObject timerContainer;
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public TextMeshProUGUI mobileHeaderText;
    public Transform elimListMobile;
    public Button elimSubmitButton;
    public Image mobileBackground;
    
    [Header("Prefabs")]
    public GameObject desktopAnswerPrefab; // Desktop: Read-only answer display
    public GameObject mobileAnswerButtonPrefab; // Mobile: Selectable answer button

    [Header("State")]
    private bool isMobile = false;
    private string playerID = "";
    private string selectedAnswer = "";
    private List<GameObject> spawnedAnswerButtons = new List<GameObject>();
    private Coroutine subscriptionCoroutine;
    private bool isSubscribedToAnswerUpdates = false;

    void OnEnable()
    {
        BeginSubscriptionRoutine();
    }

    void OnDisable()
    {
        ResetSelectionState();
        UnsubscribeFromAnswerUpdates();

        if (subscriptionCoroutine != null)
        {
            StopCoroutine(subscriptionCoroutine);
            subscriptionCoroutine = null;
        }
    }
    
    void Start()
    {
        isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

        // Get player ID for mobile
        if (isMobile)
        {
            playerID = PlayerAuthSystem.Instance != null ? PlayerAuthSystem.Instance.GetLocalPlayerID() : GetLocalPlayerID();
        }
        
        // Show appropriate display
        ShowAppropriateDisplay();
        
        // Set header text
        if (headerText != null)
        {
            headerText.text = "FIND THE ROBOT";
        }
        
        if (mobileHeaderText != null)
        {
            mobileHeaderText.text = "Vote to Eliminate";
        }
        
        // Display answers
        DisplayAnswers();
        
        // Setup mobile submit button
        if (elimSubmitButton != null)
        {
            elimSubmitButton.onClick.AddListener(OnSubmitVote);
            elimSubmitButton.interactable = false; // Disabled until selection made
        }

        // Play appropriate music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEliminationMusic();
        }

        // Ensure we refresh once data is available
        BeginSubscriptionRoutine();
    }

    void Update()
    {
        // Update timer display
        UpdateTimerDisplay();

        // Check timer warning for audio
        if (AudioManager.Instance != null && GameManager.Instance != null)
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

    void DisplayAnswers()
    {
        List<string> answers;
        Transform container;

        ResetSelectionState();

        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("DisplayAnswers called before GameManager was ready");
            return;
        }

        if (isMobile)
        {
            // Mobile: Filter out player's own answer
            answers = gameManager.GetEliminationAnswersForMobile(playerID);
            container = elimListMobile;
        }
        else
        {
            // Desktop: Show all answers
            answers = gameManager.GetAllAnswers();
            container = answerListContainer;
        }
        
        if (container == null)
        {
            Debug.LogError("Answer container is null!");
            return;
        }
        
        // Clear existing buttons
        foreach (GameObject btn in spawnedAnswerButtons)
        {
            Destroy(btn);
        }
        spawnedAnswerButtons.Clear();
        
        // Create button for each answer
        foreach (string answer in answers)
        {
            CreateAnswerButton(answer, container);
        }
    }

    void ResetSelectionState()
    {
        selectedAnswer = string.Empty;

        if (elimSubmitButton != null)
        {
            elimSubmitButton.interactable = false;
        }
    }

    void BeginSubscriptionRoutine()
    {
        if (subscriptionCoroutine == null)
        {
            subscriptionCoroutine = StartCoroutine(EnsureAnswerSubscription());
        }
    }

    System.Collections.IEnumerator EnsureAnswerSubscription()
    {
        while (GameManager.Instance == null)
        {
            yield return null;
        }

        SubscribeToAnswerUpdates();
        DisplayAnswers();
        subscriptionCoroutine = null;
    }

    void SubscribeToAnswerUpdates()
    {
        if (isSubscribedToAnswerUpdates)
        {
            return;
        }

        var gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            return;
        }

        gameManager.allAnswers.OnListChanged += HandleAllAnswersChanged;
        isSubscribedToAnswerUpdates = true;
    }

    void HandleAllAnswersChanged(NetworkListEvent<FixedString128Bytes> changeEvent)
    {
        DisplayAnswers();
    }

    void UnsubscribeFromAnswerUpdates()
    {
        if (!isSubscribedToAnswerUpdates)
        {
            return;
        }

        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.allAnswers.OnListChanged -= HandleAllAnswersChanged;
        }

        isSubscribedToAnswerUpdates = false;
    }

    void CreateAnswerButton(string answerText, Transform parent)
    {
        GameObject buttonObj;

        // Use appropriate prefab based on device
        GameObject prefabToUse = isMobile ? mobileAnswerButtonPrefab : desktopAnswerPrefab;

        if (prefabToUse != null)
        {
            // Use prefab if provided
            buttonObj = Instantiate(prefabToUse, parent);
        }
        else
        {
            // Create button programmatically
            buttonObj = new GameObject("AnswerButton");
            buttonObj.transform.SetParent(parent);
            buttonObj.AddComponent<RectTransform>();
            buttonObj.AddComponent<Image>();
            buttonObj.AddComponent<Button>();

            // Add text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            textObj.AddComponent<RectTransform>();
            textObj.AddComponent<TextMeshProUGUI>();
        }

        // Set button text
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = answerText;
        }

        // Add click listener
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnAnswerSelected(answerText, buttonObj));
        }

        spawnedAnswerButtons.Add(buttonObj);

        // Animate slide-in from left (Desktop only)
        if (!isMobile && buttonObj.GetComponent<RectTransform>() != null)
        {
            float delay = spawnedAnswerButtons.Count * 0.1f; // Stagger animations
            StartCoroutine(SlideInFromLeft(buttonObj.GetComponent<RectTransform>(), delay));
        }
    }

    System.Collections.IEnumerator SlideInFromLeft(RectTransform rectTransform, float delay)
    {
        if (rectTransform == null) yield break;

        // Wait for stagger delay
        yield return new WaitForSeconds(delay);

        float duration = 0.6f;
        float elapsed = 0f;

        // Store original position
        Vector2 targetPosition = rectTransform.anchoredPosition;

        // Start position (off screen to the left)
        Vector2 startPosition = new Vector2(targetPosition.x - Screen.width, targetPosition.y);
        rectTransform.anchoredPosition = startPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Ease out cubic
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedT);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }
    
    void OnAnswerSelected(string answer, GameObject buttonObj)
    {
        MobileHaptics.SelectionChanged();

        selectedAnswer = answer;

        // Visual feedback - highlight selected button
        foreach (GameObject btn in spawnedAnswerButtons)
        {
            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null)
            {
                // Reset all buttons to default color
                btnImage.color = Color.white;
            }
        }
        
        // Highlight selected button
        Image selectedImage = buttonObj.GetComponent<Image>();
        if (selectedImage != null)
        {
            selectedImage.color = Color.yellow; // Or your highlight color
        }
        
        if (isMobile)
        {
            // Enable submit button on mobile
            if (elimSubmitButton != null)
            {
                elimSubmitButton.interactable = true;
            }
        }
        // Desktop is display-only, doesn't submit votes
        
        Debug.Log("Answer selected for elimination: " + answer);
    }
    
    public void OnSubmitVote()
    {
        MobileHaptics.MediumImpact();

        if (string.IsNullOrEmpty(selectedAnswer))
        {
            Debug.LogWarning("No answer selected!");
            MobileHaptics.Failure();
            return;
        }
        
        SubmitEliminationVote();
    }
    
    void SubmitEliminationVote()
    {
        // Submit vote via NetworkManager
        if (RWMNetworkManager.Instance != null && RWMNetworkManager.Instance.isConnected)
        {
            RWMNetworkManager.Instance.SubmitEliminationVote(selectedAnswer);
        }
        else if (GameManager.Instance != null)
        {
            // Fallback for local testing without network
            GameManager.Instance.SubmitEliminationVote(playerID, selectedAnswer);
        }

        // Disable all buttons
        foreach (GameObject btn in spawnedAnswerButtons)
        {
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }

        // Disable submit button
        if (elimSubmitButton != null)
        {
            elimSubmitButton.interactable = false;
        }

        Debug.Log("Elimination vote submitted: " + selectedAnswer);
    }
    
    void UpdateTimerDisplay()
    {
        if (timerCountdown != null && GameManager.Instance != null)
        {
            timerCountdown.text = GameManager.Instance.GetTimerDisplay();
        }
    }
    
    string GetLocalPlayerID()
    {
        return "player_" + SystemInfo.deviceUniqueIdentifier;
    }
    
    void OnDestroy()
    {
        OnDisable();

        if (elimSubmitButton != null)
        {
            elimSubmitButton.onClick.RemoveListener(OnSubmitVote);
        }

        // Clean up button listeners
        foreach (GameObject btn in spawnedAnswerButtons)
        {
            Button button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
}