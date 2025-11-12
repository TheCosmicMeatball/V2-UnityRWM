using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneFlow
{
    public static void LoadLocal(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneFlow] LoadLocal called with empty scene name");
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public static void LoadNetworked(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneFlow] LoadNetworked called with empty scene name");
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[SceneFlow] LoadNetworked requested without server authority. Ignoring.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
