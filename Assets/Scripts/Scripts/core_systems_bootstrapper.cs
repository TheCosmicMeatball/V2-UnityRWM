using UnityEngine;

/// <summary>
/// Ensures that all core singleton-style systems are present in the scene graph
/// before gameplay logic attempts to access them. This keeps runtime code from
/// trying to dynamically compensate for missing design-time objects and avoids
/// duplicate manager creation across scenes.
/// </summary>
public static class CoreSystemsBootstrapper
{
    // === DEBUG FLAG - SET TO FALSE TO REMOVE ALL DEBUG LOGS ===
    private const bool ENABLE_DEBUG_LOGS = true;

    private static bool _isEnsuring;

    /// <summary>
    /// Verify the presence of every globally required manager. Missing managers
    /// are instantiated with minimal bootstrap GameObjects so their own Awake
    /// logic can configure themselves (including DontDestroyOnLoad registration).
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_isEnsuring)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[CoreSystemsBootstrapper] Already ensuring initialization, skipping");

            return;
        }

        _isEnsuring = true;

        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[CoreSystemsBootstrapper] ===== Starting Core Systems Initialization =====");

        EnsureManagerExists<GameManager>("GameManager");
        EnsureManagerExists<QuestionLoader>("QuestionLoader");
        EnsureManagerExists<PictureQuestionLoader>("PictureQuestionLoader");
        EnsureManagerExists<AudioManager>("AudioManager");
        EnsureManagerExists<PlayerManager>("PlayerManager");
        EnsureManagerExists<SceneTransitionManager>("SceneTransitionManager");
        EnsureManagerExists<DeviceDetector>("DeviceDetector");
        EnsureManagerExists<ContentFilterManager>("ContentFilterManager");
        EnsureManagerExists<DebugManager>("DebugManager");
        EnsureManagerExists<RWMNetworkManager>("NetworkManager");
        EnsureManagerExists<PlayerAuthSystem>("PlayerAuthSystem");

        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[CoreSystemsBootstrapper] ===== Core Systems Initialization Complete =====");

        _isEnsuring = false;
    }

    private static void EnsureManagerExists<T>(string managerName) where T : MonoBehaviour
    {
        var existing = FindExistingComponent<T>();

        if (existing != null)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[CoreSystemsBootstrapper] ✓ {managerName} already exists");

            return;
        }

        if (typeof(T) == typeof(RWMNetworkManager))
        {
            // FATAL ERROR: NetworkManager must be preconfigured in the scene, not created at runtime
            // Runtime-created NetworkManager lacks NetworkConfig, prefabs, connection approval, etc.
            Debug.LogError("[CoreSystemsBootstrapper] FATAL: RWMNetworkManager not found in scene. " +
                "A preconfigured NetworkManager with all required settings must exist in the scene. " +
                "Please add the NetworkManager prefab to your scene before running.");

            throw new System.InvalidOperationException(
                "RWMNetworkManager must be preconfigured in the scene with NetworkManager, " +
                "NetworkObject, and UnityTransport components. Runtime creation is not supported.");
        }

        if (typeof(T) == typeof(GameManager))
        {
            Debug.LogError("[CoreSystemsBootstrapper] FATAL: GameManager not found in scene. " +
                "GameManager is a networked singleton and must be present in the bootstrap scene so " +
                "the host can spawn the synchronized instance for all clients. Please add the " +
                "GameManager prefab to your scene before running.");

            throw new System.InvalidOperationException(
                "GameManager must be preconfigured in the scene. Runtime creation would prevent " +
                "Netcode from spawning a synchronized GameManager on clients.");
        }

        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[CoreSystemsBootstrapper] Creating {managerName}...");

        var managerObj = new GameObject(managerName);
        managerObj.AddComponent<T>();

        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[CoreSystemsBootstrapper] ✓ {managerName} created");
    }

    private static T FindExistingComponent<T>() where T : Component
    {
        var instances = Resources.FindObjectsOfTypeAll<T>();

        foreach (var instance in instances)
        {
            if (instance == null)
            {
                continue;
            }

            if (instance.hideFlags != HideFlags.None)
            {
                continue;
            }

            if (!instance.gameObject.scene.IsValid())
            {
                continue;
            }

            return instance;
        }

        return null;
    }
}
