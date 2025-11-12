using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class LandingScreen : MonoBehaviour
{
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public Button startTestButton;
    public VideoPlayer rulesVideo;
    public RawImage rulesVideoDisplay;
    public Image desktopBackground;
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public Button joinGameButton;
    public Image mobileBackground;
    
    [Header("Scene Names")]
    [SerializeField] private string lobbySceneName = "LobbyScreen";

    private bool automatedAdvanceAttempted = false;

    void Start()
    {
        // Setup button listeners
        if (startTestButton != null)
        {
            startTestButton.onClick.AddListener(OnStartGameClicked);
        }

        if (joinGameButton != null)
        {
            joinGameButton.onClick.AddListener(OnJoinGameClicked);
        }

        // Setup video if present
        if (rulesVideo != null)
        {
            rulesVideo.prepareCompleted += OnVideoPrepared;
            rulesVideo.loopPointReached += OnVideoFinished;
        }

        // Determine which display to show based on device
        ShowAppropriateDisplay();
    }
    
    void ShowAppropriateDisplay()
    {
        // Check if this is a mobile device or desktop
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
    
    public void OnStartGameClicked()
    {
        AttemptAdvanceToNextScreen(false);
    }

    public void OnJoinGameClicked()
    {
        MobileHaptics.MediumImpact();

        SceneFlow.LoadLocal(lobbySceneName);
    }
    
    void OnVideoPrepared(VideoPlayer source)
    {
        // Video is ready to play
        Debug.Log("Rules video prepared");
    }
    
    void OnVideoFinished(VideoPlayer source)
    {
        // Video finished playing
        Debug.Log("Rules video finished");
        AttemptAdvanceToNextScreen(true);
    }
    
    void OnDestroy()
    {
        // Clean up button listeners
        if (startTestButton != null)
        {
            startTestButton.onClick.RemoveListener(OnStartGameClicked);
        }
        
        if (joinGameButton != null)
        {
            joinGameButton.onClick.RemoveListener(OnJoinGameClicked);
        }

        // Clean up video listeners
        if (rulesVideo != null)
        {
            rulesVideo.prepareCompleted -= OnVideoPrepared;
            rulesVideo.loopPointReached -= OnVideoFinished;
        }
    }

    private void AttemptAdvanceToNextScreen(bool triggeredByVideo)
    {
        if (triggeredByVideo)
        {
            if (automatedAdvanceAttempted)
            {
                Debug.Log("[LandingScreen] Automated advance already attempted. Ignoring repeated trigger.");
                return;
            }

            automatedAdvanceAttempted = true;
        }
        else
        {
            MobileHaptics.MediumImpact();
        }

        Debug.Log("Attempting to transition to Lobby");
        SceneFlow.LoadLocal(lobbySceneName);
    }
}
