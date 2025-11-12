using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideoScreen : MonoBehaviour
{
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public VideoPlayer introVideo;
    public RawImage videoDisplay;
    public Image desktopBackground;
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public Image mobileBackground;
    
    [Header("Settings")]
    public bool skipOnClick = true;
    public bool autoAdvanceOnVideoEnd = true;
    
    void Start()
    {
        // Show appropriate display
        ShowAppropriateDisplay();
        
        // Setup video player
        if (introVideo != null)
        {
            introVideo.prepareCompleted += OnVideoPrepared;
            introVideo.loopPointReached += OnVideoFinished;
            introVideo.errorReceived += OnVideoError;
            
            // Start preparing the video
            introVideo.Prepare();
        }
        
        // Allow skipping with click/tap
        if (skipOnClick)
        {
            // Add click detection for skipping
        }
    }
    
    void Update()
    {
        // Check for skip input
        if (skipOnClick && Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            SkipVideo();
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
    
    void OnVideoPrepared(VideoPlayer source)
    {
        Debug.Log("Intro video prepared and ready to play");
        
        // Auto-play when ready
        if (introVideo != null)
        {
            introVideo.Play();
        }
    }
    
    void OnVideoFinished(VideoPlayer source)
    {
        Debug.Log("Intro video finished");
        
        if (autoAdvanceOnVideoEnd)
        {
            AdvanceToGame();
        }
    }
    
    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("Video error: " + message);
        
        // If video fails, just advance to game
        AdvanceToGame();
    }
    
    void SkipVideo()
    {
        Debug.Log("Skipping intro video");
        
        // Stop video
        if (introVideo != null && introVideo.isPlaying)
        {
            introVideo.Stop();
        }
        
        AdvanceToGame();
    }
    
    void AdvanceToGame()
    {
        // Only server can advance scenes
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            // Desktop goes to LandingScreen (mobile should not be running IntroVideo)
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            Debug.LogWarning("[IntroVideoScreen] Non-server tried to advance scene - this should not happen");
        }
    }
    
    void OnDestroy()
    {
        // Clean up video listeners
        if (introVideo != null)
        {
            introVideo.prepareCompleted -= OnVideoPrepared;
            introVideo.loopPointReached -= OnVideoFinished;
            introVideo.errorReceived -= OnVideoError;
        }
    }
}