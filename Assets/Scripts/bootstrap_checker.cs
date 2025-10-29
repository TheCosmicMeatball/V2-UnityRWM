using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Add this to any scene that might be loaded directly during development.
/// It ensures managers are loaded before the scene starts.
/// </summary>
public class BootstrapChecker : MonoBehaviour
{
    void Awake()
    {
        CoreSystemsBootstrapper.EnsureInitialized();

        // If something prevented initialization we still fall back to loading the bootstrap scene.
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager not found! Loading LoadingScreen first...");

            var transitionManager = SceneTransitionManager.Instance ?? FindFirstObjectByType<SceneTransitionManager>();

            if (transitionManager != null)
            {
                if (!transitionManager.LoadSceneLocally("LoadingScreen"))
                {
                    Debug.LogWarning("[BootstrapChecker] SceneTransitionManager rejected local load for LoadingScreen. Falling back to direct SceneManager.LoadScene.");
                    SceneManager.LoadScene("LoadingScreen");
                }
            }
            else
            {
                Debug.LogWarning("[BootstrapChecker] SceneTransitionManager unavailable. Falling back to direct SceneManager.LoadScene.");
                SceneManager.LoadScene("LoadingScreen");
            }
        }
    }
}
