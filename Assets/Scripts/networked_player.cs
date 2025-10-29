using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

/// <summary>
/// Networked player data structure using Unity Netcode best practices
/// Each player is represented as a NetworkObject with synchronized state
/// </summary>
public class NetworkedPlayer : NetworkBehaviour
{
    // Network-synced player data
    public NetworkVariable<FixedString64Bytes> playerID = new NetworkVariable<FixedString64Bytes>();
    public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();
    public NetworkVariable<FixedString64Bytes> iconName = new NetworkVariable<FixedString64Bytes>();
    public NetworkVariable<int> scorePercentage = new NetworkVariable<int>();
    public NetworkVariable<bool> isHost = new NetworkVariable<bool>();
    public NetworkVariable<FixedString32Bytes> deviceType = new NetworkVariable<FixedString32Bytes>();

    // Events for UI updates when values change
    public event Action<int> OnScoreChanged;
    public event Action<string> OnPlayerNameChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to value changes for UI updates
        scorePercentage.OnValueChanged += OnScoreValueChanged;
        playerName.OnValueChanged += OnPlayerNameValueChanged;

        Debug.Log($"[NetworkedPlayer] Spawned: {playerName.Value} (Score: {scorePercentage.Value})");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // Unsubscribe from value changes
        scorePercentage.OnValueChanged -= OnScoreValueChanged;
        playerName.OnValueChanged -= OnPlayerNameValueChanged;
    }

    private void OnScoreValueChanged(int oldValue, int newValue)
    {
        Debug.Log($"[NetworkedPlayer] Score changed for {playerName.Value}: {oldValue} → {newValue}");
        OnScoreChanged?.Invoke(newValue);
    }

    private void OnPlayerNameValueChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        OnPlayerNameChanged?.Invoke(newValue.ToString());
    }

    /// <summary>
    /// Initialize player data (Server only)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void InitializeServerRpc(string id, string name, string icon, ulong clientId)
    {
        if (!IsServer) return;

        playerID.Value = id;
        playerName.Value = name;
        iconName.Value = icon;
        scorePercentage.Value = 0;
        isHost.Value = (clientId == 0); // Server is clientId 0
        deviceType.Value = isHost.Value ? "desktop" : "mobile";

        Debug.Log($"[NetworkedPlayer] Initialized: {name} (ID: {id}, ClientID: {clientId})");
    }

    /// <summary>
    /// Update player score (Server only)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void UpdateScoreServerRpc(int newScore)
    {
        if (!IsServer) return;
        scorePercentage.Value = newScore;
    }

    /// <summary>
    /// Convert to PlayerData struct for compatibility with existing code
    /// </summary>
    public PlayerData ToPlayerData()
    {
        return new PlayerData
        {
            playerID = playerID.Value.ToString(),
            playerName = playerName.Value.ToString(),
            iconName = iconName.Value.ToString(),
            scorePercentage = scorePercentage.Value,
            isHost = isHost.Value,
            deviceType = deviceType.Value.ToString(),
            clientId = OwnerClientId
        };
    }
}
