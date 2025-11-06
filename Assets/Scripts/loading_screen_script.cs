using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    // === DEBUG FLAG - SET TO FALSE TO REMOVE ALL DEBUG LOGS ===
    private const bool ENABLE_DEBUG_LOGS = true;
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public Image gameLogo;
    public Image background;
    public Image tips;
    
    [Header("Mobile UI Elements")]
    public GameObject mobileDisplay;
    public Image mobileBackground;
    
    [Header("Settings")]
    public float minimumLoadTime = 2f; // Show loading screen for at least 2 seconds
    public bool clickToAdvance = true;
    public bool autoAdvanceOnMobile = true; // Auto-advance on mobile devices
    
    private bool isLoaded = false;
    private float loadTimer = 0f;
    
    void Awake()
    {
        CoreSystemsBootstrapper.EnsureInitialized();
        AssertNoNetworkObjects();
    }
    
    void Start()
    {
        if (ENABLE_DEBUG_LOGS)
        {
            Debug.Log("[LoadingScreen] Start called");
            Debug.Log($"[LoadingScreen] Screen size: {Screen.width}x{Screen.height}");
        }

        // Show appropriate display
        ShowAppropriateDisplay();

        // Start loading process
        StartCoroutine(LoadGame());
    }
    
    void Update()
    {
        loadTimer += Time.deltaTime;
        
        // Allow clicking logo to advance (if enabled and loaded)
        if (clickToAdvance && isLoaded && loadTimer >= minimumLoadTime)
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                AdvanceToLanding();
            }
        }
    }
    
    void ShowAppropriateDisplay()
    {
        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[LoadingScreen] ShowAppropriateDisplay called");

        bool isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

        if (ENABLE_DEBUG_LOGS)
        {
            Debug.Log($"[LoadingScreen] DeviceDetector.Instance exists: {DeviceDetector.Instance != null}");
            if (DeviceDetector.Instance != null)
            {
                Debug.Log($"[LoadingScreen] isMobile: {isMobile}");
            }
        }

        if (desktopDisplay != null)
        {
            desktopDisplay.SetActive(!isMobile);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[LoadingScreen] desktopDisplay set to: {!isMobile}");
        }

        if (mobileDisplay != null)
        {
            mobileDisplay.SetActive(isMobile);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[LoadingScreen] mobileDisplay set to: {isMobile}");
        }
    }

    IEnumerator LoadGame()
    {
        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[LoadingScreen] LoadGame coroutine started - calling CoreSystemsBootstrapper.EnsureInitialized()");

        // Initialize all global managers
        CoreSystemsBootstrapper.EnsureInitialized();

        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[LoadingScreen] CoreSystemsBootstrapper.EnsureInitialized() completed");

        // Simulate loading or perform actual loading tasks
        yield return new WaitForSeconds(0.5f);

        // Load any necessary assets
        // yield return StartCoroutine(LoadAssets());

        isLoaded = true;

        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[LoadingScreen] Loading complete, waiting for minimum load time");

        // Wait for minimum load time
        while (loadTimer < minimumLoadTime)
        {
            yield return null;
        }

        // Auto-advance if click-to-advance is disabled OR if on mobile and autoAdvanceOnMobile is true
        bool isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();
        if (!clickToAdvance || (isMobile && autoAdvanceOnMobile))
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[LoadingScreen] Auto-advancing (clickToAdvance=" + clickToAdvance + ", isMobile=" + isMobile + ", autoAdvanceOnMobile=" + autoAdvanceOnMobile + ")");

            AdvanceToLanding();
        }
    }
    
    void AdvanceToLanding()
    {
        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[LoadingScreen] AdvanceToLanding invoked");

        bool isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();
        string targetScene = isMobile ? "LobbyScreen" : "LandingScreen";

        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[LoadingScreen] Routing to pre-network scene '{targetScene}' (isMobile={isMobile})");

        SceneFlow.LoadLocal(targetScene);
    }

    void AssertNoNetworkObjects()
    {
        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "LoadingScreen" && sceneName != "LandingScreen" && sceneName != "LobbyScreen")
        {
            return;
        }

        NetworkObject[] networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        bool hasForbiddenObject = false;

        for (int i = 0; i < networkObjects.Length; i++)
        {
            NetworkObject netObj = networkObjects[i];
            if (netObj == null)
            {
                continue;
            }

            string ownerScene = netObj.gameObject.scene.name;
            if (ownerScene == "LoadingScreen" || ownerScene == "LandingScreen" || ownerScene == "LobbyScreen")
            {
                hasForbiddenObject = true;
                break;
            }
        }

        Debug.Assert(!hasForbiddenObject, "Pre-network scenes must not contain NetworkObjects.");
    }
    
    // Optional: Add actual asset loading here
    IEnumerator LoadAssets()
    {
        // Example: Load questions from JSON
        // TextAsset questionsJson = Resources.Load<TextAsset>("questions");
        // if (questionsJson != null)
        // {
        //     // Parse JSON and populate GameManager
        // }
        
        yield return null;
    }

}
