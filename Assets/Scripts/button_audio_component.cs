using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this component to any Button to automatically play the correct sound when clicked.
/// Handles Desktop vs Mobile button sounds automatically.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonAudioComponent : MonoBehaviour
{
    [Header("Button Type")]
    public ButtonType buttonType = ButtonType.Standard;
    
    public enum ButtonType
    {
        Standard,           // Desktop button click
        GameModeSwitch,     // 8Q/12Q switch buttons
        MobileButton        // Mobile-specific buttons
    }
    
    private Button button;
    
    void Awake()
    {
        button = GetComponent<Button>();
        
        if (button != null)
        {
            button.onClick.AddListener(PlayButtonSound);
        }
    }
    
    void PlayButtonSound()
    {
        MobileHaptics.LightImpact();

        if (AudioManager.Instance == null) return;

        switch (buttonType)
        {
            case ButtonType.Standard:
                AudioManager.Instance.PlayDesktopButtonSFX();
                break;
                
            case ButtonType.GameModeSwitch:
                AudioManager.Instance.PlayButtonSwitchSFX();
                break;
                
            case ButtonType.MobileButton:
                // Mobile buttons don't have sound effects in your spec
                // But you could add one if needed
                break;
        }
    }
    
    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayButtonSound);
        }
    }
}
