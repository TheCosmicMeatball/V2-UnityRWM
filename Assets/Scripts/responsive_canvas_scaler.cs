using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically adjusts Canvas Scaler settings based on device type (mobile vs desktop).
/// Attach this to any Canvas GameObject that needs responsive scaling.
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class ResponsiveCanvasScaler : MonoBehaviour
{
    // === DEBUG FLAG - SET TO FALSE TO REMOVE ALL DEBUG LOGS ===
    private const bool ENABLE_DEBUG_LOGS = true;

    [Header("Desktop Settings")]
    [Tooltip("Reference resolution for desktop displays")]
    public Vector2 desktopReferenceResolution = new Vector2(1920, 1080);

    [Header("Mobile Settings")]
    [Tooltip("Reference resolution for mobile displays")]
    public Vector2 mobileReferenceResolution = new Vector2(1080, 1920);

    [Header("Scale Mode Settings")]
    [Tooltip("Match width (0) or height (1) - 0.5 is balanced")]
    [Range(0f, 1f)]
    public float desktopMatchWidthOrHeight = 0.5f;

    [Range(0f, 1f)]
    public float mobileMatchWidthOrHeight = 0.5f;

    private CanvasScaler canvasScaler;

    void Awake()
    {
        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[ResponsiveCanvasScaler] Awake on GameObject: {gameObject.name}");

        canvasScaler = GetComponent<CanvasScaler>();

        if (canvasScaler == null)
        {
            Debug.LogError($"[ResponsiveCanvasScaler] MISSING CanvasScaler component on {gameObject.name}!");
        }
    }

    void Start()
    {
        if (canvasScaler == null)
        {
            return;
        }

        if (ENABLE_DEBUG_LOGS)
        {
            Debug.Log($"[ResponsiveCanvasScaler] Start on GameObject: {gameObject.name}");
            Debug.Log($"[ResponsiveCanvasScaler] Current Screen: {Screen.width}x{Screen.height}");
            Debug.Log($"[ResponsiveCanvasScaler] DeviceDetector.Instance exists: {DeviceDetector.Instance != null}");
            if (DeviceDetector.Instance != null)
            {
                Debug.Log($"[ResponsiveCanvasScaler] DeviceDetector.IsMobile(): {DeviceDetector.Instance.IsMobile()}");
            }
        }

        ApplyCanvasScalerSettings();
    }

    void ApplyCanvasScalerSettings()
    {
        bool isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

        if (ENABLE_DEBUG_LOGS)
        {
            Debug.Log($"[ResponsiveCanvasScaler] Applying settings - isMobile: {isMobile}");
            Debug.Log($"[ResponsiveCanvasScaler] Current canvasScaler.referenceResolution BEFORE: {canvasScaler.referenceResolution}");
            Debug.Log($"[ResponsiveCanvasScaler] Current canvasScaler.matchWidthOrHeight BEFORE: {canvasScaler.matchWidthOrHeight}");
        }

        // Set UI Scale Mode
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Set reference resolution based on device type
        if (isMobile)
        {
            canvasScaler.referenceResolution = mobileReferenceResolution;
            canvasScaler.matchWidthOrHeight = mobileMatchWidthOrHeight;

            if (ENABLE_DEBUG_LOGS)
            {
                Debug.Log($"[ResponsiveCanvasScaler] ✓ MOBILE configuration applied on {gameObject.name}");
                Debug.Log($"[ResponsiveCanvasScaler]   Reference Resolution: {mobileReferenceResolution.x}x{mobileReferenceResolution.y}");
                Debug.Log($"[ResponsiveCanvasScaler]   Match W/H: {mobileMatchWidthOrHeight}");
            }
        }
        else
        {
            canvasScaler.referenceResolution = desktopReferenceResolution;
            canvasScaler.matchWidthOrHeight = desktopMatchWidthOrHeight;

            if (ENABLE_DEBUG_LOGS)
            {
                Debug.Log($"[ResponsiveCanvasScaler] ✓ DESKTOP configuration applied on {gameObject.name}");
                Debug.Log($"[ResponsiveCanvasScaler]   Reference Resolution: {desktopReferenceResolution.x}x{desktopReferenceResolution.y}");
                Debug.Log($"[ResponsiveCanvasScaler]   Match W/H: {desktopMatchWidthOrHeight}");
            }
        }

        // Set screen match mode
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        if (ENABLE_DEBUG_LOGS)
        {
            Debug.Log($"[ResponsiveCanvasScaler] Final canvasScaler.referenceResolution: {canvasScaler.referenceResolution}");
            Debug.Log($"[ResponsiveCanvasScaler] Final canvasScaler.matchWidthOrHeight: {canvasScaler.matchWidthOrHeight}");
            Debug.Log($"[ResponsiveCanvasScaler] Final canvasScaler.uiScaleMode: {canvasScaler.uiScaleMode}");
            Debug.Log($"[ResponsiveCanvasScaler] Final canvasScaler.screenMatchMode: {canvasScaler.screenMatchMode}");
        }
    }
}
