using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles player authentication, ID management, and mapping between network clients and game players.
/// Persists player data across scenes.
/// </summary>
public class PlayerAuthSystem : MonoBehaviour
{
    public static PlayerAuthSystem Instance;
    
    [Header("Local Player Info")]
    public string localPlayerID = "";
    public string localPlayerName = "";
    public string localPlayerIcon = "";
    
    [Header("Network Mapping")]
    // Client-side storage for player mapping
    private Dictionary<string, string> playerDataMap = new Dictionary<string, string>(); // playerID -> playerName
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Generate or load persistent player ID
        InitializeLocalPlayer();
    }
    
    // === LOCAL PLAYER INITIALIZATION ===
    
    void InitializeLocalPlayer()
    {
        // Check if player ID is saved in PlayerPrefs
        if (PlayerPrefs.HasKey("PlayerID"))
        {
            localPlayerID = PlayerPrefs.GetString("PlayerID");
            Debug.Log("Loaded existing player ID: " + localPlayerID);
        }
        else
        {
            // Generate new player ID
            localPlayerID = GeneratePlayerID();
            PlayerPrefs.SetString("PlayerID", localPlayerID);
            PlayerPrefs.Save();
            Debug.Log("Generated new player ID: " + localPlayerID);
        }
    }
    
    string GeneratePlayerID()
    {
        // Use a GUID so we avoid relying on GetHashCode (which is not stable between sessions)
        // and guarantee a high entropy identifier for the player.
        string guid = System.Guid.NewGuid().ToString("N");

        // Keep the prefix for readability but trim the guid to a manageable length.
        return "player_" + guid.Substring(0, 12);
    }
    
    // === PLAYER REGISTRATION ===
    
    public void RegisterPlayer(string playerName, string playerIcon)
    {
        localPlayerName = playerName;
        localPlayerIcon = playerIcon;

        // Save to PlayerPrefs for persistence
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("PlayerIcon", playerIcon);
        PlayerPrefs.Save();

        Debug.Log($"Player registered: {playerName} ({localPlayerID})");

        // Add to local storage
        playerDataMap[localPlayerID] = playerName;

        // If connected to network, send to server via Netcode
        if (RWMNetworkManager.Instance != null && RWMNetworkManager.Instance.isConnected)
        {
            RWMNetworkManager.Instance.AddPlayer(playerName, playerIcon);
        }
        else if (GameManager.Instance != null)
        {
            // Offline fallback so local sessions still work
            GameManager.Instance.AddPlayer(localPlayerID, playerName, playerIcon);
        }
    }

    // === PLAYER ID RETRIEVAL ===
    
    public string GetLocalPlayerID()
    {
        return localPlayerID;
    }
    
    public string GetLocalPlayerName()
    {
        return localPlayerName;
    }
    
    public string GetLocalPlayerIcon()
    {
        return localPlayerIcon;
    }
    
    public string GetPlayerNameByID(string playerID)
    {
        if (playerDataMap.ContainsKey(playerID))
        {
            return playerDataMap[playerID];
        }

        Debug.LogWarning($"No player name found for player ID {playerID}");
        return "";
    }

    // === UTILITY ===
    
    public bool IsPlayerRegistered()
    {
        return !string.IsNullOrEmpty(localPlayerName) && !string.IsNullOrEmpty(localPlayerIcon);
    }
    
    public void ClearPlayerData()
    {
        localPlayerID = "";
        localPlayerName = "";
        localPlayerIcon = "";
        
        PlayerPrefs.DeleteKey("PlayerID");
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.DeleteKey("PlayerIcon");
        PlayerPrefs.Save();
    }
}
