using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class AnswerButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Button button;
    public TextMeshProUGUI answerText;
    public Image background;
    public Image selectionIndicator; // Optional: circle, checkmark, etc.
    
    [Header("Visual States")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 0.9f);
    public Color selectedColor = Color.yellow;
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f);
    
    [Header("State")]
    private bool isSelected = false;
    private bool isHovered = false;
    private string answerContent = "";
    private UnityAction buttonClickAction;
    
    void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        
        if (background == null)
        {
            background = GetComponent<Image>();
        }
        
        if (answerText == null)
        {
            answerText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // Hide selection indicator by default
        if (selectionIndicator != null)
        {
            selectionIndicator.gameObject.SetActive(false);
        }
    }
    
    void Start()
    {
        UpdateVisuals();
    }
    
    // === PUBLIC METHODS ===
    
    public void Initialize(string answer, System.Action<string> onClickCallback)
    {
        answerContent = answer;
        
        if (answerText != null)
        {
            answerText.text = answer;
        }
        
        if (button != null)
        {
            if (buttonClickAction != null)
            {
                button.onClick.RemoveListener(buttonClickAction);
            }

            buttonClickAction = () =>
            {
                MobileHaptics.SelectionChanged();
                onClickCallback?.Invoke(answerContent);
            };

            button.onClick.AddListener(buttonClickAction);
        }

        UpdateVisuals();
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectionIndicator != null)
        {
            selectionIndicator.gameObject.SetActive(selected);
        }
        
        UpdateVisuals();
    }
    
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
        
        UpdateVisuals();
    }
    
    public string GetAnswerContent()
    {
        return answerContent;
    }
    
    // === VISUAL UPDATES ===
    
    void UpdateVisuals()
    {
        if (background == null) return;

        if (button != null && !button.interactable)
        {
            // Disabled state
            background.color = disabledColor;
        }
        else if (isSelected)
        {
            // Selected state
            background.color = selectedColor;
        }
        else if (isHovered)
        {
            // Hover state
            background.color = hoverColor;
        }
        else
        {
            // Normal state
            background.color = normalColor;
        }
    }
    
    // === HOVER HANDLERS ===
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable)
        {
            isHovered = true;
            UpdateVisuals();
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisuals();
    }

    void OnDestroy()
    {
        if (button != null && buttonClickAction != null)
        {
            button.onClick.RemoveListener(buttonClickAction);
        }
    }
}
