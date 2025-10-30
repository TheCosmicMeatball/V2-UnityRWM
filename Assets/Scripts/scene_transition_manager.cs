using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 0.5f;
    public Color fadeColor = Color.black;
    
    [Header("Loading Screen")]
    public GameObject loadingScreenPrefab;
    private GameObject activeLoadingScreen;
    
    [Header("Transition State")]
    private bool isTransitioning = false;

    [Header("Network State")]
    [SerializeField]
    private List<string> preConnectionSceneNames = new List<string>
    {
        "LoadingScreen",
        "LandingScreen",
        "LobbyScreen",
        "JoinRoomScreen",
        "IntroVideoScreen",
        "CreditsScreen",
        "GameTerminatedScreen"
    };

    private HashSet<string> preConnectionScenes;
    private bool hasGameStarted = false;
    private NetworkManager cachedNetworkManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (preConnectionScenes == null)
            {
                preConnectionScenes = new HashSet<string>(preConnectionSceneNames, System.StringComparer.Ordinal);
            }

            // Create fade canvas if not exists
            if (fadeImage == null)
            {
                CreateFadeCanvas();
            }

            CacheNetworkManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        CacheNetworkManager();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void CreateFadeCanvas()
    {
        // Create a full-screen canvas for fading
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Always on top
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create fade image
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);

        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0); // Start transparent
        fadeImage.raycastTarget = false; // Don't block clicks when transparent

        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
    
    // === PUBLIC TRANSITION METHODS ===
    
    /// <summary>
    /// DEPRECATED: Use LoadSceneLocally instead.
    /// </summary>
    [System.Obsolete("Use SceneTransitionManager.LoadSceneLocally instead.")]
    public void LoadScene(string sceneName)
    {
        Debug.LogWarning("[SceneTransitionManager] LoadScene() is deprecated. Routing to LoadSceneLocally().");
        LoadSceneLocally(sceneName);
    }

    /// <summary>
    /// DEPRECATED: Use LoadSceneLocally instead.
    /// </summary>
    [System.Obsolete("Use SceneTransitionManager.LoadSceneLocally instead.")]
    public void LoadSceneWithDelay(string sceneName, float delay)
    {
        Debug.LogWarning("[SceneTransitionManager] LoadSceneWithDelay() is deprecated. Routing to LoadSceneLocally after delay.");

        if (!isTransitioning)
        {
            StartCoroutine(TransitionToSceneWithDelay(sceneName, delay));
        }
    }

    /// <summary>
    /// DEPRECATED: Use LoadSceneLocally instead.
    /// </summary>
    [System.Obsolete("Use SceneTransitionManager.LoadSceneLocally instead.")]
    public void LoadSceneImmediate(string sceneName)
    {
        Debug.LogWarning("[SceneTransitionManager] LoadSceneImmediate() is deprecated. Routing to LoadSceneLocally().");
        LoadSceneLocally(sceneName);
    }

    public bool LoadSceneLocally(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneTransitionManager] Refusing to load a null or empty scene name locally.");
            return false;
        }

        if (!IsPreConnectionScene(sceneName))
        {
            Debug.LogWarning($"[SceneTransitionManager] Scene '{sceneName}' is not marked as a pre-connection scene. Local load requests will be rejected to avoid desyncs.");
            return false;
        }

        var networkManager = NetworkManager.Singleton;

        if (networkManager != null && (networkManager.IsClient || networkManager.IsServer))
        {
            Debug.LogWarning($"[SceneTransitionManager] Refusing to locally load '{sceneName}' while a network session is active.");
            return false;
        }

        Debug.Log($"[SceneTransitionManager] Loading scene locally: {sceneName}");

        if (isTransitioning)
        {
            Debug.LogWarning($"[SceneTransitionManager] A transition is already in progress. Local load for '{sceneName}' will be ignored.");
            return false;
        }

        StartCoroutine(LocalSceneTransition(sceneName));
        return true;
    }

    public bool StartNetworkedGameplay(string firstGameplayScene)
    {
        if (string.IsNullOrEmpty(firstGameplayScene))
        {
            Debug.LogError("[SceneTransitionManager] Cannot start networked gameplay with an empty scene name.");
            return false;
        }

        var networkManager = CacheNetworkManager();

        if (networkManager == null)
        {
            Debug.LogError("[SceneTransitionManager] NetworkManager singleton missing. Ensure the NetworkManager prefab is present and Use Scene Management is enabled.");
            return false;
        }

        if (!networkManager.IsHost)
        {
            Debug.LogWarning($"[SceneTransitionManager] Client attempted to start networked gameplay by loading '{firstGameplayScene}'. Only the host can do this.");
            return false;
        }

        if (hasGameStarted)
        {
            Debug.LogWarning($"[SceneTransitionManager] Networked gameplay already started. Routing '{firstGameplayScene}' through LoadSceneNetworked().");
            return LoadSceneNetworked(firstGameplayScene);
        }

        if (!networkManager.IsListening)
        {
            Debug.LogError("[SceneTransitionManager] NetworkManager is not listening. Host must start hosting before loading gameplay scenes.");
            return false;
        }

        var sceneManager = networkManager.SceneManager;

        if (sceneManager == null)
        {
            Debug.LogError("[SceneTransitionManager] NetworkSceneManager unavailable. Verify Use Scene Management is enabled on NetworkManager.");
            return false;
        }

        Debug.Log($"[SceneTransitionManager] Host starting networked gameplay with scene '{firstGameplayScene}'.");
        var status = sceneManager.LoadScene(firstGameplayScene, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"[SceneTransitionManager] Failed to start networked gameplay for '{firstGameplayScene}'. Status: {status}");
            return false;
        }

        hasGameStarted = true;
        Debug.Log($"[SceneTransitionManager] Networked gameplay flag set. Scene '{firstGameplayScene}' load initiated.");
        return true;
    }

    public bool LoadSceneNetworked(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneTransitionManager] Cannot load a null or empty scene over the network.");
            return false;
        }

        var networkManager = CacheNetworkManager();

        if (networkManager == null)
        {
            Debug.LogError($"[SceneTransitionManager] NetworkManager singleton missing. Unable to load '{sceneName}' networked.");
            return false;
        }

        if (!networkManager.IsHost)
        {
            Debug.LogWarning($"[SceneTransitionManager] Client attempted to network-load '{sceneName}'. Only the host can trigger synchronized transitions.");
            return false;
        }

        if (!hasGameStarted)
        {
            if (IsPreConnectionScene(sceneName))
            {
                Debug.LogWarning($"[SceneTransitionManager] '{sceneName}' is marked as a pre-connection scene. Use LoadSceneLocally or StartNetworkedGameplay for the first synchronized scene.");
            }
            else
            {
                Debug.LogWarning($"[SceneTransitionManager] Networked gameplay has not started yet. Use StartNetworkedGameplay('{sceneName}') for the first scene.");
            }

            return false;
        }

        if (!networkManager.IsListening)
        {
            Debug.LogError($"[SceneTransitionManager] NetworkManager is not listening. Cannot network-load '{sceneName}'.");
            return false;
        }

        var sceneManager = networkManager.SceneManager;

        if (sceneManager == null)
        {
            Debug.LogError("[SceneTransitionManager] NetworkSceneManager unavailable. Verify Use Scene Management is enabled on NetworkManager.");
            return false;
        }

        Debug.Log($"[SceneTransitionManager] Host initiating networked load for '{sceneName}'.");
        var status = sceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"[SceneTransitionManager] Failed to start network scene load for '{sceneName}'. Status: {status}");
            return false;
        }

        return true;
    }

    // === FADE METHODS ===
    
    public void FadeOut(System.Action onComplete = null)
    {
        StartCoroutine(Fade(0f, 1f, fadeDuration, onComplete));
    }
    
    public void FadeIn(System.Action onComplete = null)
    {
        StartCoroutine(Fade(1f, 0f, fadeDuration, onComplete));
    }
    
    // === TRANSITION COROUTINES ===
    
    IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;

        // Fade out
        yield return Fade(0f, 1f, fadeDuration);
        
        // Load scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        // Wait for scene to load
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        
        // Activate scene
        asyncLoad.allowSceneActivation = true;
        
        // Wait one frame for scene to initialize
        yield return new WaitForEndOfFrame();
        
        // Fade in
        yield return Fade(1f, 0f, fadeDuration);
        
        isTransitioning = false;
    }
    
    IEnumerator TransitionToSceneWithDelay(string sceneName, float delay)
    {
        isTransitioning = true;

        // Wait for delay
        yield return new WaitForSeconds(delay);

        // Then transition normally
        yield return LocalSceneTransition(sceneName);
    }

    IEnumerator LocalSceneTransition(string sceneName)
    {
        isTransitioning = true;

        // Fade out before loading the new scene
        yield return Fade(0f, 1f, fadeDuration);

        SceneManager.LoadScene(sceneName);

        // Wait one frame for scene initialization
        yield return null;

        // Fade back in
        yield return Fade(1f, 0f, fadeDuration);

        isTransitioning = false;
    }
    
    IEnumerator Fade(float startAlpha, float endAlpha, float duration, System.Action onComplete = null)
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("Fade image is null, cannot fade");
            onComplete?.Invoke();
            yield break;
        }
        
        float elapsed = 0f;
        Color color = fadeImage.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;
            
            yield return null;
        }
        
        // Ensure final alpha is exact
        color.a = endAlpha;
        fadeImage.color = color;
        
        onComplete?.Invoke();
    }
    
    // === LOADING SCREEN ===
    
    public void ShowLoadingScreen()
    {
        if (loadingScreenPrefab != null && activeLoadingScreen == null)
        {
            activeLoadingScreen = Instantiate(loadingScreenPrefab);
            DontDestroyOnLoad(activeLoadingScreen);
        }
    }
    
    public void HideLoadingScreen()
    {
        if (activeLoadingScreen != null)
        {
            Destroy(activeLoadingScreen);
            activeLoadingScreen = null;
        }
    }
    
    // === UTILITY ===
    
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    public void SetFadeDuration(float duration)
    {
        fadeDuration = duration;
    }
    
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (fadeImage != null)
        {
            Color current = fadeImage.color;
            fadeImage.color = new Color(color.r, color.g, color.b, current.a);
        }
    }

    public bool HasGameStarted()
    {
        return hasGameStarted;
    }

    private NetworkManager CacheNetworkManager()
    {
        var singleton = NetworkManager.Singleton;

        if (singleton == null)
        {
            if (cachedNetworkManager != null)
            {
                Debug.Log("[SceneTransitionManager] NetworkManager unavailable. Resetting gameplay state to allow local scene loads.");
            }

            cachedNetworkManager = null;
            hasGameStarted = false;
            return null;
        }

        cachedNetworkManager = singleton;
        return cachedNetworkManager;
    }

    private bool IsPreConnectionScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return false;
        }

        if (preConnectionScenes == null)
        {
            preConnectionScenes = new HashSet<string>(preConnectionSceneNames, System.StringComparer.Ordinal);
        }

        return preConnectionScenes.Contains(sceneName);
    }
}
