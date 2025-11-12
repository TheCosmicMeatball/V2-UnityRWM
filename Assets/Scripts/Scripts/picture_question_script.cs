using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PictureQuestionScreen : MonoBehaviour
{
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public GameObject pictureContainer;
    public Image picture;
    public Image pictureFrame;
    public TextMeshProUGUI timerCountdown;
    public Transform playerIconContainer;
    public GameObject playerIconPictureQuestionPrefab;
    public GameObject timerContainer;
    public TextMeshProUGUI tipText;
    public Image desktopBackground;
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public Image mobilePictureFrame;
    public GameObject mobilePictureContainer;
    public Image mobilePicture;
    public TMP_InputField answerInput;
    public Button answerSubmitButton;
    public Image mobileBackground;
    
    [Header("State")]
    private bool isMobile = false;
    private string playerID = "";
    private List<GameObject> spawnedPlayerIcons = new List<GameObject>();
    
    void Start()
    {
        isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

        // Get player ID for mobile
        if (isMobile)
        {
            playerID = GetLocalPlayerID();
        }

        // Show appropriate display
        ShowAppropriateDisplay();

        // Display picture question
        // Setup submit button
        if (answerSubmitButton != null)
        {
            answerSubmitButton.onClick.AddListener(OnSubmitAnswer);
        }
        
        // Setup input field
        if (answerInput != null)
        {
            answerInput.characterLimit = 100;
        }
        
        // Update tip text
        if (tipText != null)
        {
            tipText.text = "DOUBLE POINTS! Describe what you see";
        }
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuestionUpdated += HandleQuestionUpdated;
            HandleQuestionUpdated(GameManager.Instance.GetCurrentQuestion());
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuestionUpdated -= HandleQuestionUpdated;
        }
    }
    
    void Update()
    {
        // Update timer display
        UpdateTimerDisplay();
        
        // Update player status indicators (desktop only)
        if (!isMobile)
        {
            UpdatePlayerStatusIndicators();
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
    
    void HandleQuestionUpdated(Question updatedQuestion)
    {
        DisplayPictureQuestion(updatedQuestion);
    }

    void DisplayPictureQuestion(Question currentQuestion)
    {
        if (currentQuestion == null)
        {
            Debug.LogWarning("PictureQuestionScreen - question payload not yet available");
            return;
        }

        ResetAnswerInput();

        if (!string.IsNullOrEmpty(currentQuestion.imageURL))
        {
            LoadPicture(currentQuestion.imageURL);
        }

        Debug.Log("Picture Question - Round " + GameManager.Instance.GetCurrentRound());
    }

    void LoadPicture(string imageURL)
    {
        if (PictureQuestionLoader.Instance == null)
        {
            Debug.LogWarning("PictureQuestionScreen - PictureQuestionLoader not ready");
            return;
        }

        Sprite pictureSprite = PictureQuestionLoader.Instance.GetPictureByImageURL(imageURL);

        if (pictureSprite == null)
        {
            Debug.LogWarning("PictureQuestionScreen - Could not find sprite for " + imageURL);
            return;
        }

        if (picture != null)
        {
            picture.sprite = pictureSprite;
            picture.preserveAspect = true;
        }

        if (mobilePicture != null)
        {
            mobilePicture.sprite = pictureSprite;
            mobilePicture.preserveAspect = true;
        }

        Debug.Log("Loaded picture question sprite: " + imageURL);
    }

    void ResetAnswerInput()
    {
        if (answerInput != null)
        {
            answerInput.text = string.Empty;
            answerInput.interactable = true;
        }

        if (answerSubmitButton != null)
        {
            answerSubmitButton.interactable = true;
        }
    }
    
    void UpdateTimerDisplay()
    {
        if (timerCountdown != null)
        {
            timerCountdown.text = GameManager.Instance.GetTimerDisplay();
        }
    }
    
    void UpdatePlayerStatusIndicators()
    {
        if (playerIconContainer == null) return;
        
        List<PlayerData> players = GameManager.Instance.GetAllPlayers();
        
        // Clear existing icons if count changed
        if (spawnedPlayerIcons.Count != players.Count)
        {
            foreach (GameObject icon in spawnedPlayerIcons)
            {
                Destroy(icon);
            }
            spawnedPlayerIcons.Clear();
            
            // Spawn new icons
            foreach (PlayerData player in players)
            {
                if (playerIconPictureQuestionPrefab != null)
                {
                    GameObject iconObj = Instantiate(playerIconPictureQuestionPrefab, playerIconContainer);
                    
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
                    if (iconTransform != null)
                    {
                        Image iconImage = iconTransform.GetComponent<Image>();
                        if (iconImage != null)
                        {
                            // Load the icon sprite
                            // iconImage.sprite = Resources.Load<Sprite>("PlayerIcons/" + player.iconName);
                        }
                    }
                    
                    spawnedPlayerIcons.Add(iconObj);
                }
            }
        }
        
        // Update status indicators (show icon when player has submitted)
        for (int i = 0; i < spawnedPlayerIcons.Count && i < players.Count; i++)
        {
            GameObject iconObj = spawnedPlayerIcons[i];
            PlayerData player = players[i];

            // Check if this player has submitted an answer
            bool hasSubmitted = GameManager.Instance.HasPlayerSubmittedAnswer(player.playerID);

            // Show/hide entire icon based on submission (matching regular Questions behavior)
            iconObj.SetActive(hasSubmitted);
        }
    }
    
    void OnSubmitAnswer()
    {
        MobileHaptics.MediumImpact();

        if (answerInput == null || string.IsNullOrEmpty(answerInput.text))
        {
            Debug.LogWarning("Please enter an answer!");
            MobileHaptics.Failure();
            return;
        }
        
        // Submit answer to GameManager
        GameManager.Instance.SubmitPlayerAnswer(playerID, answerInput.text);
        
        // Disable input and button
        answerInput.interactable = false;
        answerSubmitButton.interactable = false;
        
        Debug.Log("Picture answer submitted: " + answerInput.text);
    }
    
    string GetLocalPlayerID()
    {
        return "player_" + SystemInfo.deviceUniqueIdentifier;
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

    void OnDestroy()
    {
        if (answerSubmitButton != null)
        {
            answerSubmitButton.onClick.RemoveListener(OnSubmitAnswer);
        }
    }
}