using UnityEngine;

public class PreNetworkNavigator : MonoBehaviour
{
    [SerializeField] private string landingSceneName = "LandingScreen";
    [SerializeField] private string lobbySceneName = "LobbyScreen";

    public void GoToLanding()
    {
        if (string.IsNullOrEmpty(landingSceneName))
        {
            Debug.LogError("[PreNetworkNavigator] Landing scene name is not set.");
            return;
        }

        SceneFlow.LoadLocal(landingSceneName);
    }

    public void GoToLobby()
    {
        if (string.IsNullOrEmpty(lobbySceneName))
        {
            Debug.LogError("[PreNetworkNavigator] Lobby scene name is not set.");
            return;
        }

        SceneFlow.LoadLocal(lobbySceneName);
    }
}
