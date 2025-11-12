using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    
    [Header("Player Icon Settings")]
    public string playerIconPath = "sprites/icons/";
    public int totalPlayerIcons = 20;
    
    [Header("Loaded Icons")]
    private Dictionary<string, Sprite> playerIconSprites = new Dictionary<string, Sprite>();
    
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
        
        LoadPlayerIcons();
    }
    
    void LoadPlayerIcons()
    {
        playerIconSprites.Clear();
        
        // Load all 20 player icons
        for (int i = 1; i <= totalPlayerIcons; i++)
        {
            string iconName = "player icon (" + i + ")";
            string iconPath = playerIconPath + iconName;
            
            Sprite iconSprite = Resources.Load<Sprite>(iconPath);
            
            if (iconSprite != null)
            {
                playerIconSprites.Add(iconName, iconSprite);
            }
            else
            {
                Debug.LogWarning("Could not load player icon: " + iconPath);
            }
        }
        
        Debug.Log("Loaded " + playerIconSprites.Count + " player icons");
    }
    
    // === PUBLIC METHODS ===
    
    public Sprite GetPlayerIcon(string iconName)
    {
        if (playerIconSprites.ContainsKey(iconName))
        {
            return playerIconSprites[iconName];
        }
        
        // Debug.LogWarning("Player icon not found: " + iconName);
        return null;
    }
    
    public Sprite GetPlayerIconByIndex(int index)
    {
        if (index < 1 || index > totalPlayerIcons)
        {
            Debug.LogWarning("Player icon index out of range: " + index);
            return null;
        }
        
        string iconName = "player icon (" + index + ")";
        return GetPlayerIcon(iconName);
    }
    
    public List<string> GetAllPlayerIconNames()
    {
        List<string> iconNames = new List<string>();
        
        for (int i = 1; i <= totalPlayerIcons; i++)
        {
            iconNames.Add("player icon (" + i + ")");
        }
        
        return iconNames;
    }
    
    public List<Sprite> GetAllPlayerIcons()
    {
        return new List<Sprite>(playerIconSprites.Values);
    }
    
    public string GetRandomIconName()
    {
        int randomIndex = Random.Range(1, totalPlayerIcons + 1);
        return "player icon (" + randomIndex + ")";
    }

    public Sprite GetRandomIcon()
    {
        string randomName = GetRandomIconName();
        return GetPlayerIcon(randomName);
    }

    // Get icons that are not currently selected by any player
    public List<string> GetAvailableIconNames()
    {
        // Get all player icon names
        List<string> allIcons = GetAllPlayerIconNames();

        // Get icons currently in use
        HashSet<string> usedIcons = new HashSet<string>();
        if (GameManager.Instance != null)
        {
            foreach (var player in GameManager.Instance.players.Values)
            {
                usedIcons.Add(player.iconName);
            }
        }

        // Filter out used icons
        List<string> availableIcons = new List<string>();
        foreach (string iconName in allIcons)
        {
            if (!usedIcons.Contains(iconName))
            {
                availableIcons.Add(iconName);
            }
        }

        return availableIcons;
    }

    // Check if an icon is available (not currently selected)
    public bool IsIconAvailable(string iconName)
    {
        if (GameManager.Instance == null) return true;

        foreach (var player in GameManager.Instance.players.Values)
        {
            if (player.iconName == iconName)
            {
                return false; // Icon is already in use
            }
        }

        return true; // Icon is available
    }
    
    // === PLAYER ID GENERATION ===
    
    public string GeneratePlayerID()
    {
        return System.Guid.NewGuid().ToString();
    }
    
    public string GetDevicePlayerID()
    {
        // Use device unique identifier for consistent player ID across sessions
        return "player_" + SystemInfo.deviceUniqueIdentifier;
    }
}
