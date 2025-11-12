using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ScoreRowController : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI rankText;
    public Image playerIcon;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI scoreDiffText;
    
    [Header("Optional Components")]
    public Image backgroundRow;
    public Image rankBackground;
    
    [Header("Animation Settings")]
    public bool animateScoreChange = true;
    public float animationDuration = 1f;
    public AnimationCurve scoreCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Colors")]
    public Color positiveScoreColor = Color.green;
    public Color negativeScoreColor = Color.red;
    public Color neutralScoreColor = Color.white;
    public Color winnerRowColor = new Color(1f, 0.84f, 0f, 0.3f); // Gold tint
    public Color loserRowColor = new Color(1f, 0.3f, 0.3f, 0.2f); // Red tint
    
    private PlayerData playerData;
    private int currentScore = 0;
    private int previousScore = 0;
    
    void Awake()
    {
        // Auto-find components if not assigned
        if (rankText == null)
        {
            rankText = transform.Find("Rank")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (playerIcon == null)
        {
            playerIcon = transform.Find("PlayerIcon")?.GetComponent<Image>();
        }
        
        if (playerNameText == null)
        {
            playerNameText = transform.Find("PlayerName")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (scoreText == null)
        {
            scoreText = transform.Find("Score")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (scoreDiffText == null)
        {
            scoreDiffText = transform.Find("ScoreDiff")?.GetComponent<TextMeshProUGUI>();
        }
    }
    
    // === PUBLIC METHODS ===
    
    public void Initialize(PlayerData data, int rank, int previousScoreValue = 0)
    {
        playerData = data;
        previousScore = previousScoreValue;
        currentScore = data.scorePercentage;
        
        // Set rank
        if (rankText != null)
        {
            rankText.text = rank.ToString();
        }
        
        // Set player name
        if (playerNameText != null)
        {
            playerNameText.text = data.playerName;
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
        
        // Set score
        if (animateScoreChange && previousScore != currentScore)
        {
            StartCoroutine(AnimateScore(previousScore, currentScore));
        }
        else
        {
            SetScore(currentScore);
        }
        
        // Calculate and display score difference
        int scoreDiff = currentScore - previousScore;
        DisplayScoreDiff(scoreDiff);
    }
    
    void SetScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = score + "%";
        }
    }
    
    void DisplayScoreDiff(int diff)
    {
        if (scoreDiffText == null) return;
        
        if (diff > 0)
        {
            scoreDiffText.text = "+" + diff + "%";
            scoreDiffText.color = positiveScoreColor;
        }
        else if (diff < 0)
        {
            scoreDiffText.text = diff + "%";
            scoreDiffText.color = negativeScoreColor;
        }
        else
        {
            scoreDiffText.text = "0%";
            scoreDiffText.color = neutralScoreColor;
        }
    }
    
    public void HighlightAsWinner()
    {
        if (backgroundRow != null)
        {
            backgroundRow.color = winnerRowColor;
        }
        
        if (rankBackground != null)
        {
            rankBackground.color = Color.gold;
        }
    }
    
    public void HighlightAsLoser()
    {
        if (backgroundRow != null)
        {
            backgroundRow.color = loserRowColor;
        }
        
        if (rankBackground != null)
        {
            rankBackground.color = Color.red;
        }
    }
    
    public void SetRank(int rank)
    {
        if (rankText != null)
        {
            rankText.text = rank.ToString();
        }
    }
    
    public PlayerData GetPlayerData()
    {
        return playerData;
    }
    
    // === ANIMATIONS ===
    
    IEnumerator AnimateScore(int fromScore, int toScore)
    {
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float curvedT = scoreCurve.Evaluate(t);
            
            int displayScore = Mathf.RoundToInt(Mathf.Lerp(fromScore, toScore, curvedT));
            SetScore(displayScore);
            
            yield return null;
        }
        
        // Ensure final score is exact
        SetScore(toScore);
    }
}
