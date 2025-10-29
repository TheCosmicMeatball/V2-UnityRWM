using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerIconController : MonoBehaviour
{
    [Header("UI Components")]
    public Image namebar;
    public Image circle;
    public Image playerIcon;
    public TextMeshProUGUI playerName;
    
    [Header("Optional Components")]
    public Image backgroundGlow; // For highlighting winners/losers
    public TextMeshProUGUI scoreText; // For displaying scores
    
    [Header("State Indicators")]
    public bool showCircleWhenSubmitted = true;
    public bool showCircleWhenSelected = true;
    
    [Header("Colors")]
    public Color normalCircleColor = Color.white;
    public Color submittedCircleColor = Color.green;
    public Color selectedCircleColor = Color.yellow;
    public Color winnerGlowColor = Color.gold;
    public Color loserGlowColor = Color.red;
    
    private PlayerData playerData;
    private bool hasSubmitted = false;
    private bool isSelected = false;
    
    void Awake()
    {
        // Find components if not assigned
        if (namebar == null)
        {
            namebar = transform.Find("Namebar")?.GetComponent<Image>();
        }
        
        if (circle == null)
        {
            circle = transform.Find("Circle")?.GetComponent<Image>();
        }
        
        if (playerIcon == null)
        {
            playerIcon = transform.Find("PlayerIcon")?.GetComponent<Image>();
        }
        
        if (playerName == null)
        {
            playerName = transform.Find("PlayerName")?.GetComponent<TextMeshProUGUI>();
        }
        
        // Hide circle by default
        if (circle != null)
        {
            circle.gameObject.SetActive(false);
        }
        
        // Hide glow by default
        if (backgroundGlow != null)
        {
            backgroundGlow.gameObject.SetActive(false);
        }
    }
    
    // === PUBLIC METHODS ===
    
    public void Initialize(PlayerData data)
    {
        playerData = data;
        
        // Set player name
        if (playerName != null)
        {
            playerName.text = data.playerName;
        }
        
        // Load and set player icon
        if (playerIcon != null && PlayerManager.Instance != null)
        {
            Sprite iconSprite = PlayerManager.Instance.GetPlayerIcon(data.iconName);
            if (iconSprite != null)
            {
                playerIcon.sprite = iconSprite;
            }
        }
        
        // Set score if score text exists
        if (scoreText != null)
        {
            scoreText.text = data.scorePercentage + "%";
        }
        
        UpdateVisuals();
    }
    
    public void SetSubmitted(bool submitted)
    {
        hasSubmitted = submitted;
        UpdateVisuals();
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
    }
    
    public void SetScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score + "%";
        }
        
        if (playerData != null)
        {
            playerData.scorePercentage = score;
        }
    }
    
    public void HighlightAsWinner()
    {
        if (backgroundGlow != null)
        {
            backgroundGlow.gameObject.SetActive(true);
            backgroundGlow.color = winnerGlowColor;
        }
    }
    
    public void HighlightAsLoser()
    {
        if (backgroundGlow != null)
        {
            backgroundGlow.gameObject.SetActive(true);
            backgroundGlow.color = loserGlowColor;
        }
    }
    
    public void ClearHighlight()
    {
        if (backgroundGlow != null)
        {
            backgroundGlow.gameObject.SetActive(false);
        }
    }
    
    public PlayerData GetPlayerData()
    {
        return playerData;
    }
    
    // === VISUAL UPDATES ===
    
    void UpdateVisuals()
    {
        if (circle == null) return;
        
        // Show circle based on state
        bool shouldShowCircle = false;
        Color circleColor = normalCircleColor;
        
        if (hasSubmitted && showCircleWhenSubmitted)
        {
            shouldShowCircle = true;
            circleColor = submittedCircleColor;
        }
        
        if (isSelected && showCircleWhenSelected)
        {
            shouldShowCircle = true;
            circleColor = selectedCircleColor;
        }
        
        circle.gameObject.SetActive(shouldShowCircle);
        circle.color = circleColor;
    }
}
