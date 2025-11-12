using UnityEngine;

/// <summary>
/// Lightweight helper for triggering platform-specific haptic feedback on mobile devices.
/// </summary>
public static class MobileHaptics
{
    private const float MinInterval = 0.05f; // Prevent spamming the haptics engine
    private static float _lastTriggerTime = -10f;

    private enum ImpactStrength
    {
        Light,
        Medium,
        Heavy
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static readonly AndroidJavaObject Vibrator;
    private static readonly int AndroidSdkVersion;
    private static readonly bool HasAmplitudeControl;

    static MobileHaptics()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity != null)
                {
                    Vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }

            using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                AndroidSdkVersion = version.GetStatic<int>("SDK_INT");
            }

            if (Vibrator != null && AndroidSdkVersion >= 26)
            {
                HasAmplitudeControl = Vibrator.Call<bool>("hasAmplitudeControl");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"MobileHaptics initialization failed: {ex.Message}");
            Vibrator = null;
            AndroidSdkVersion = 0;
            HasAmplitudeControl = false;
        }
    }
#endif

    public static void LightImpact()
    {
        Trigger(ImpactStrength.Light);
    }

    public static void MediumImpact()
    {
        Trigger(ImpactStrength.Medium);
    }

    public static void HeavyImpact()
    {
        Trigger(ImpactStrength.Heavy);
    }

    public static void SelectionChanged()
    {
        Trigger(ImpactStrength.Light);
    }

    public static void Success()
    {
        Trigger(ImpactStrength.Medium);
    }

    public static void Failure()
    {
        Trigger(ImpactStrength.Heavy);
    }

    private static void Trigger(ImpactStrength strength)
    {
        if (!Application.isMobilePlatform)
        {
            return;
        }

        if (Time.unscaledTime - _lastTriggerTime < MinInterval)
        {
            return;
        }

        _lastTriggerTime = Time.unscaledTime;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (Vibrator == null)
        {
            Handheld.Vibrate();
            return;
        }

        try
        {
            long duration = GetDuration(strength);

            if (AndroidSdkVersion >= 26)
            {
                using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                {
                    int amplitude = HasAmplitudeControl ? GetAmplitude(strength) : vibrationEffectClass.GetStatic<int>("DEFAULT_AMPLITUDE");
                    AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", duration, amplitude);
                    Vibrator.Call("vibrate", effect);
                }
            }
            else
            {
                Vibrator.Call("vibrate", duration);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"MobileHaptics vibrate failed: {ex.Message}");
#if !UNITY_WEBGL
            Handheld.Vibrate();
#endif
        }
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        // iOS/tvOS do not expose fine-grained control without plugins, fall back to default vibration
        Handheld.Vibrate();
#elif (UNITY_IOS || UNITY_ANDROID) && !UNITY_WEBGL && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static long GetDuration(ImpactStrength strength)
    {
        switch (strength)
        {
            case ImpactStrength.Medium:
                return 40L;
            case ImpactStrength.Heavy:
                return 60L;
            case ImpactStrength.Light:
            default:
                return 25L;
        }
    }

    private static int GetAmplitude(ImpactStrength strength)
    {
        switch (strength)
        {
            case ImpactStrength.Medium:
                return 160;
            case ImpactStrength.Heavy:
                return 255;
            case ImpactStrength.Light:
            default:
                return 90;
        }
    }
#endif
}
