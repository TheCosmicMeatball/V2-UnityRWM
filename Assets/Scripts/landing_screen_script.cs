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
    
    private bool awaitingHostStart = false;

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
        MobileHaptics.MediumImpact();

        // Desktop host starts the game - go to lobby
        Debug.Log("Start Game clicked - transitioning to Lobby");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL! Cannot advance to next screen. Make sure to start from LoadingScreen scene.");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            // Desktop should always host. If host networking hasn't spun up yet, start it now.
            bool isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

            if (!isMobile && RWMNetworkManager.Instance != null)
            {
                if (!awaitingHostStart)
                {
                    awaitingHostStart = true;
                    RWMNetworkManager.Instance.OnRoomCreated += OnLandingHostReady;
                    Debug.Log("[LandingScreen] Host authority not yet established. Starting host now.");
                    RWMNetworkManager.Instance.StartHost();
                }
                else
                {
                    Debug.Log("[LandingScreen] Waiting for host startup confirmation...");
                }
            }
            else
            {
                Debug.LogWarning("[LandingScreen] Only the host can advance to the next screen.");
            }
        }
    }

    public void OnJoinGameClicked()
    {
        MobileHaptics.MediumImpact();

        // Mobile player wants to join - go to lobby/join screen
        Debug.Log("Join Game clicked - transitioning to Lobby");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL! Cannot advance to next screen. Make sure to start from LoadingScreen scene.");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            Debug.LogWarning("[LandingScreen] Only the host can advance to the next screen.");
        }
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
    }
    
    void OnDestroy()
    {
        if (RWMNetworkManager.Instance != null)
        {
            RWMNetworkManager.Instance.OnRoomCreated -= OnLandingHostReady;
        }

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

    private void OnLandingHostReady(string roomCode)
    {
        if (RWMNetworkManager.Instance != null)
        {
            RWMNetworkManager.Instance.OnRoomCreated -= OnLandingHostReady;
        }

        awaitingHostStart = false;

        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            Debug.Log("[LandingScreen] Host started successfully from landing. Advancing to next screen.");
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            Debug.LogWarning("[LandingScreen] Host reported ready but GameManager does not have server authority yet.");
        }
    }
}