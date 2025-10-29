using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

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
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create fade canvas if not exists
            if (fadeImage == null)
            {
                CreateFadeCanvas();
            }
        }
        else
        {
            Destroy(gameObject);
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
    /// DEPRECATED: Use GameManager.LoadScene instead for networked games
    /// This method bypasses Unity Netcode and will cause desyncs
    /// </summary>
    [System.Obsolete("Use GameManager.LoadScene for networked games")]
    public void LoadScene(string sceneName)
    {
        Debug.LogWarning("[SceneTransitionManager] LoadScene is deprecated. Use GameManager.LoadScene for networked games.");

        // For backwards compatibility in single-player or debug scenarios only
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(sceneName));
        }
    }

    /// <summary>
    /// DEPRECATED: Use GameManager.LoadScene instead for networked games
    /// </summary>
    [System.Obsolete("Use GameManager.LoadScene for networked games")]
    public void LoadSceneWithDelay(string sceneName, float delay)
    {
        Debug.LogWarning("[SceneTransitionManager] LoadSceneWithDelay is deprecated. Use GameManager.LoadScene for networked games.");

        if (!isTransitioning)
        {
            StartCoroutine(TransitionToSceneWithDelay(sceneName, delay));
        }
    }

    /// <summary>
    /// DEPRECATED: Use GameManager.LoadScene instead for networked games
    /// </summary>
    [System.Obsolete("Use GameManager.LoadScene for networked games")]
    public void LoadSceneImmediate(string sceneName)
    {
        Debug.LogWarning("[SceneTransitionManager] LoadSceneImmediate is deprecated. Use GameManager.LoadScene for networked games.");
        SceneManager.LoadScene(sceneName);
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
        yield return TransitionToScene(sceneName);
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
}
