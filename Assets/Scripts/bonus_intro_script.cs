using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BonusIntroScreen : MonoBehaviour
{
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public Image[] introImages; // Multiple Image components for intro art
    public Button continueButton;
    public GameObject buttonSlideout;
    public Image desktopBackground;
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public Image mobileBackground;
    
    [Header("Settings")]
    public float autoAdvanceDelay = 4f; // Auto-advance after 4 seconds
    
    private bool isHost;
    private bool continueButtonListenerAdded;

    void Start()
    {
        isHost = GameManager.Instance != null && GameManager.Instance.IsServer;

        // Show appropriate display
        ShowAppropriateDisplay();

        // Setup continue button (host only)
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(isHost);

            if (isHost)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                continueButtonListenerAdded = true;
            }
        }

        // Auto-advance to bonus questions (host drives scene flow)
        if (isHost)
        {
            StartCoroutine(AutoAdvanceAfterDelay());
        }
    }
    
    void ShowAppropriateDisplay()
    {
        bool isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

        if (desktopDisplay != null)
        {
            desktopDisplay.SetActive(!isMobile);
        }

        if (mobileDisplay != null)
        {
            mobileDisplay.SetActive(isMobile);
        }
    }
    
    IEnumerator AutoAdvanceAfterDelay()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);

        if (!isHost)
        {
            yield break;
        }

        AdvanceToBonusQuestions();
    }

    public void OnContinueClicked()
    {
        MobileHaptics.MediumImpact();

        if (isHost)
        {
            // Stop auto-advance
            StopAllCoroutines();
        }
        AdvanceToBonusQuestions();
    }
    
    void AdvanceToBonusQuestions()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            Debug.LogWarning("[BonusIntroScreen] Only the host can advance to the next screen.");
        }
    }
    
    void OnDestroy()
    {
        if (continueButton != null && continueButtonListenerAdded)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }
}