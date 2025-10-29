using UnityEngine;
using UnityEngine.Events;

public class DeviceDetector : MonoBehaviour
{
    // === DEBUG FLAG - SET TO FALSE TO REMOVE ALL DEBUG LOGS ===
    private const bool ENABLE_DEBUG_LOGS = true;

    public static DeviceDetector Instance;
    
    [Header("Device Type")]
    public DeviceType currentDevice = DeviceType.Desktop;
    
    [Header("Device Detection Settings")]
    public bool autoDetectOnStart = true;
    public bool forceDeviceType = false;
    public DeviceType forcedDeviceType = DeviceType.Desktop;
    
    [Header("Events")]
    public UnityEvent onDesktopDetected;
    public UnityEvent onMobileDetected;
    
    public enum DeviceType
    {
        Desktop,
        Mobile
    }
    
    void Awake()
    {
        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[DeviceDetector] Awake called");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] Instance set, DontDestroyOnLoad applied");
        }
        else
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] Duplicate instance found, destroying");

            Destroy(gameObject);
            return;
        }

        if (autoDetectOnStart)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] Auto-detecting device type...");

            DetectDevice();
        }
    }
    
    // === DEVICE DETECTION ===
    
    public void DetectDevice()
    {
        if (ENABLE_DEBUG_LOGS)
        {
            Debug.Log($"[DeviceDetector] DetectDevice called");
            Debug.Log($"[DeviceDetector] Screen size: {Screen.width}x{Screen.height}");
            Debug.Log($"[DeviceDetector] Application.platform: {Application.platform}");
            Debug.Log($"[DeviceDetector] Application.isMobilePlatform: {Application.isMobilePlatform}");
            Debug.Log($"[DeviceDetector] Input.touchSupported: {Input.touchSupported}");
        }

        if (forceDeviceType)
        {
            currentDevice = forcedDeviceType;

            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] ✓ Device type FORCED to: {currentDevice}");
        }
        else
        {
            // Use Unity's built-in platform detection
            currentDevice = DetermineDeviceType();

            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] ✓ Device detected as: {currentDevice}");
        }

        // Trigger events
        if (currentDevice == DeviceType.Desktop)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] Triggering onDesktopDetected event");

            onDesktopDetected?.Invoke();
        }
        else
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] Triggering onMobileDetected event");

            onMobileDetected?.Invoke();
        }
    }
    
    DeviceType DetermineDeviceType()
    {
        // Check if running on mobile platform
        if (Application.isMobilePlatform)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] DetermineDeviceType: Application.isMobilePlatform = true → MOBILE");

            return DeviceType.Mobile;
        }

        // Check specific platforms
        RuntimePlatform platform = Application.platform;

        switch (platform)
        {
            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
                if (ENABLE_DEBUG_LOGS)
                    Debug.Log($"[DeviceDetector] DetermineDeviceType: Platform {platform} → MOBILE");

                return DeviceType.Mobile;

            case RuntimePlatform.WebGLPlayer:
                if (ENABLE_DEBUG_LOGS)
                    Debug.Log($"[DeviceDetector] DetermineDeviceType: WebGLPlayer → checking heuristics");

                // For WebGL, check screen size and touch support
                return DetectWebGLDevice();

            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxEditor:
            default:
                if (ENABLE_DEBUG_LOGS)
                    Debug.Log($"[DeviceDetector] DetermineDeviceType: Platform {platform} → DESKTOP");

                return DeviceType.Desktop;
        }
    }

    DeviceType DetectWebGLDevice()
    {
        // For WebGL builds, use additional heuristics

        // Check for touch support
        if (Input.touchSupported)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] DetectWebGLDevice: Touch supported → MOBILE");

            return DeviceType.Mobile;
        }

        // Check screen size (mobile devices typically < 768px width)
        if (Screen.width < 768)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] DetectWebGLDevice: Screen width {Screen.width} < 768 → MOBILE");

            return DeviceType.Mobile;
        }

        // Check aspect ratio (mobile devices typically have portrait or narrow aspect)
        float aspectRatio = (float)Screen.width / Screen.height;
        if (aspectRatio < 1.0f || (aspectRatio < 1.5f && Screen.width < 1024))
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[DeviceDetector] DetectWebGLDevice: Aspect ratio {aspectRatio:F2} suggests mobile → MOBILE");

            return DeviceType.Mobile;
        }

        // Default to desktop for WebGL
        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[DeviceDetector] DetectWebGLDevice: All checks passed → DESKTOP");

        return DeviceType.Desktop;
    }
    
    // === PUBLIC QUERY METHODS ===
    
    public bool IsMobile()
    {
        return currentDevice == DeviceType.Mobile;
    }
    
    public bool IsDesktop()
    {
        return currentDevice == DeviceType.Desktop;
    }
    
    public string GetDeviceTypeString()
    {
        return currentDevice.ToString().ToLower();
    }
    
    // === INPUT DETECTION ===
    
    public bool IsUsingTouch()
    {
        // Check if user is actively using touch input
        return Input.touchCount > 0 || (Input.touchSupported && currentDevice == DeviceType.Mobile);
    }
    
    public bool IsUsingMouse()
    {
        // Check if user is using mouse input
        return Input.mousePresent && currentDevice == DeviceType.Desktop;
    }
    
    // === SCREEN ORIENTATION (Mobile) ===
    
    public ScreenOrientation GetOrientation()
    {
        return Screen.orientation;
    }
    
    public bool IsPortrait()
    {
        return Screen.height > Screen.width;
    }
    
    public bool IsLandscape()
    {
        return Screen.width > Screen.height;
    }
    
    // === MANUAL OVERRIDE ===
    
    public void SetDeviceType(DeviceType type)
    {
        currentDevice = type;
        forceDeviceType = true;
        forcedDeviceType = type;
        
        Debug.Log("Device type manually set to: " + currentDevice);
        
        // Trigger events
        if (currentDevice == DeviceType.Desktop)
        {
            onDesktopDetected?.Invoke();
        }
        else
        {
            onMobileDetected?.Invoke();
        }
    }
    
    public void ResetToAutoDetect()
    {
        forceDeviceType = false;
        DetectDevice();
    }
}
