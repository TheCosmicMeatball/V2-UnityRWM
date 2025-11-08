using UnityEngine;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Unity Netcode-based NetworkManager for RWM multiplayer
/// Desktop acts as Host (Server + Client), mobile devices connect as Clients
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class RWMNetworkManager : NetworkBehaviour
{
    public static RWMNetworkManager Instance;

    [Header("Component References")]
    [SerializeField]
    private NetworkManager networkManagerOverride;

    [SerializeField]
    private UnityTransport unityTransportOverride;

    [Header("Network Settings")]
    public string roomCode = "";
    public bool isHost = false;
    public ushort port = 7778;

    [Tooltip("IP address clients should use to connect when this device is hosting. Leave blank to use the transport's configured address.")]
    public string hostAddress = "";

    [Tooltip("Network interface address the host should bind to. Defaults to all interfaces.")]
    public string listenAddress = "0.0.0.0";

    [Header("Connection Status")]
    public bool isConnected = false;
    public string playerId = "";

    private bool transportSettingsApplied = false;
    private string defaultBindAddress = "0.0.0.0";

    // Event delegates for network messages
    public event Action<string> OnRoomCreated;
    public event Action<string> OnRoomJoined;
    public event Action<string> OnPlayerJoined;
    public event Action<string> OnPlayerLeft;
    public event Action<string, string, string> OnPlayerAdded; // playerID, playerName, iconName
#pragma warning disable CS0067 // Event is declared but never used - reserved for future functionality
    public event Action<string> OnGameStateChanged;
    public event Action<string, string> OnAnswerSubmitted; // playerID, answer
    public event Action<string, string> OnVoteSubmitted; // playerID, votedTarget
    public event Action<string, int> OnScoreUpdated; // playerID, newScore
    public event Action<string> OnSceneChanged; // sceneName
    public event Action<float, bool> OnTimerSync; // timerValue, isActive
#pragma warning restore CS0067
    public event Action OnConnectionError;
    public event Action OnTransitionToElimination;

    private NetworkManager networkManager;
    private NetworkObject networkObject;
    private UnityTransport unityTransport;
    private bool componentsValidated;

    // void Awake()
    // {
    //     if (Instance == null)
    //     {
    //         Instance = this;
    //         DontDestroyOnLoad(gameObject);
    //         Debug.Log("[NetworkManager] Instance created");

    //         EnsureNetworkingComponents();
    //         // Persist UnityTransport configuration across scenes
    //         ApplyTransportSettings();
    //         UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
    //         {
    //             ApplyTransportSettings();
    //         };

    //     }
    //     else
    //     {
    //         Destroy(gameObject);
    //     }
    // }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure runtime flags start clean regardless of serialized scene values
        isHost = false;
        isConnected = false;

        // WebGL always uses WebSockets under the hood; enable flag to silence warnings
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            try
            {
                if (unityTransport == null)
                {
                    // Will be assigned in EnsureNetworkingComponents, but we can try early
                    var nm = NetworkManager.Singleton ?? GetComponent<NetworkManager>() ?? FindFirstObjectByType<NetworkManager>();
                    if (nm != null)
                    {
                        unityTransport = nm.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                    }
                }
                if (unityTransport != null)
                {
                    unityTransport.UseWebSockets = true;
                }
            }
            catch { }
        }

        if (!EnsureNetworkingComponents())
        {
            Debug.LogError("[NetworkManager] Unable to initialize networking components during Awake().");
            enabled = false;
            return;
        }

        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        if (networkManager == null)
        {
            networkManager = GetComponent<NetworkManager>();
        }

        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<NetworkManager>();
        }

        if (networkManager == null)
        {
            Debug.LogError("[NetworkManager] NetworkManager instance could not be located during Awake().");
            enabled = false;
            return;
        }

        if (unityTransport == null)
        {
            unityTransport = networkManager.GetComponent<UnityTransport>();
        }

        if (unityTransport == null)
        {
            var singleton = NetworkManager.Singleton;
            if (singleton != null)
            {
                unityTransport = singleton.GetComponent<UnityTransport>();
            }
        }

        if (unityTransport == null)
        {
            unityTransport = FindFirstObjectByType<UnityTransport>();
        }

        if (unityTransport == null)
        {
            Debug.LogError("[NetworkManager] UnityTransport component is missing and could not be created during Awake().");
            enabled = false;
            return;
        }

        if (networkManager.NetworkConfig.NetworkTransport != unityTransport)
        {
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
        }

        // Apply preferred connection data *before* Netcode starts
        var advertisedAddress = string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress;
        var bindAddress = string.IsNullOrWhiteSpace(listenAddress) ? "0.0.0.0" : listenAddress;
        unityTransport.SetConnectionData(advertisedAddress, port, bindAddress);
        Debug.Log($"[NetworkManager] Applied transport config at Awake() -> Port {port}");

        // Subscribe to reapply after scene load (safety)
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (unityTransport == null)
            {
                Debug.LogError("[NetworkManager] UnityTransport missing when reapplying transport settings after scene load.");
                return;
            }

            unityTransport.SetConnectionData(advertisedAddress, port, bindAddress);
            Debug.Log($"[NetworkManager] Reapplied transport settings in scene '{scene.name}'");
        };
    }

    private void ApplyTransportSettings()
    {
        if (unityTransport == null)
        {
            unityTransport = networkManager.GetComponent<UnityTransport>();
            if (unityTransport == null) return;
        }

        if (transportSettingsApplied) return;

        unityTransport.SetConnectionData(
            string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress,
            port,
            string.IsNullOrWhiteSpace(listenAddress) ? defaultBindAddress : listenAddress
        );

        transportSettingsApplied = true;
        Debug.Log($"[NetworkManager] Transport settings applied and locked → Port={port}, Address={hostAddress}");
    }

    void Start()
    {
        if (!EnsureNetworkingComponents())
        {
            Debug.LogError("[NetworkManager] Missing required networking components.");
            enabled = false;
            return;
        }

        if (networkManager.NetworkConfig.NetworkTransport != unityTransport)
        {
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
        }

        // Load or generate player ID
        if (PlayerPrefs.HasKey("PlayerID"))
        {
            playerId = PlayerPrefs.GetString("PlayerID");
        }
        else
        {
            playerId = "player_" + UnityEngine.Random.Range(100000000, 999999999);
            PlayerPrefs.SetString("PlayerID", playerId);
        }

        Debug.Log($"[NetworkManager] Player ID: {playerId}");

        // Register connection callbacks
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        networkManager.OnServerStarted += OnServerStarted;
        // Handle transport failures (host: auto-recreate Relay; client: surface error)
        networkManager.OnTransportFailure += HandleTransportFailure;
    }

    private bool _restartInProgress;
    private void HandleTransportFailure()
    {
        Debug.LogError("[NetworkManager] OnTransportFailure signaled");

        // Client surface error
        if (!networkManager.IsServer)
        {
            OnConnectionError?.Invoke();
            return;
        }

        // Host: attempt to recreate Relay allocation and restart host
        if (_restartInProgress)
        {
            return;
        }

        _restartInProgress = true;
        StartCoroutine(RestartHostAfterFailure());
    }

    System.Collections.IEnumerator RestartHostAfterFailure()
    {
        // Give Netcode a moment to shut down
        float start = Time.realtimeSinceStartup;
        while (networkManager.IsListening && (Time.realtimeSinceStartup - start) < 2f)
        {
            yield return null;
        }

        // Ensure fully shutdown
        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
            yield return null;
        }

        Debug.Log("[NetworkManager] Restarting host after transport failure...");
        _restartInProgress = false;
        TryStartHostRelayOrLocal();
    }

    // === CONNECTION CALLBACKS ===

    private void OnServerStarted()
    {
        Debug.Log("[NetworkManager] Server started successfully");
        isConnected = true;
        isHost = true;
        if (!networkObject.IsSpawned)
        {
            networkObject.Spawn();
        }
        OnRoomCreated?.Invoke(roomCode);

        var gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            var gmNetworkObject = gameManager.GetComponent<NetworkObject>();

            if (gmNetworkObject != null && !gmNetworkObject.IsSpawned)
            {
                gmNetworkObject.Spawn();
                Debug.Log("[NetworkManager] Spawned GameManager NetworkObject on host start");
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[NetworkManager] Client connected: {clientId}");

        if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
        {
            isConnected = true;
            OnRoomJoined?.Invoke(roomCode);
        }

        OnPlayerJoined?.Invoke(clientId.ToString());
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NetworkManager] Client disconnected: {clientId}");

        if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
        {
            isConnected = false;
            OnConnectionError?.Invoke();
        }

        string disconnectedPlayerId = clientId.ToString();

        if (NetworkManager.Singleton.IsServer)
        {
            var gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();

            if (gameManager != null && gameManager.TryGetPlayerIdByClientId(clientId, out string resolvedPlayerId))
            {
                disconnectedPlayerId = resolvedPlayerId;

                gameManager.RemovePlayer(resolvedPlayerId);

                if (IsSpawned)
                {
                    RemovePlayerClientRpc(resolvedPlayerId);
                }
            }
            else
            {
                Debug.LogWarning($"[NetworkManager] Unable to match disconnected client {clientId} to a player entry.");
            }
        }

        OnPlayerLeft?.Invoke(disconnectedPlayerId);
    }

    // === HOST METHODS ===

    public Task StartHostAsync()
    {
        StartHost();
        return Task.CompletedTask;
    }

    public void StartHost()
    {
        if (!EnsureNetworkingComponents())
        {
            Debug.LogError("[NetworkManager] Cannot start host because networking components are not ready.");
            OnConnectionError?.Invoke();
            return;
        }

        if (networkManager == null || unityTransport == null)
        {
            Debug.LogError("[NetworkManager] Cannot start host because NetworkManager or UnityTransport is missing.");
            OnConnectionError?.Invoke();
            return;
        }

        isHost = true;
        
        // Attempt Relay start first if available; fallback to local UDP host
        TryStartHostRelayOrLocal();
    }

    private async void TryStartHostRelayOrLocal()
    {
        Debug.Log($"[NetworkManager] Relay availability: {RelayAdapter.IsAvailable}");
#if UNITY_MULTIPLAYER
        Debug.Log("[NetworkManager] UNITY_MULTIPLAYER define is present");
#else
        Debug.Log("[NetworkManager] UNITY_MULTIPLAYER define is NOT present");
#endif
#if UNITY_RELAY
        Debug.Log("[NetworkManager] UNITY_RELAY define is present");
#else
        Debug.Log("[NetworkManager] UNITY_RELAY define is NOT present");
#endif
        if (RelayAdapter.IsAvailable)
        {
            var result = await RelayAdapter.StartHostAsync(networkManager, unityTransport, maxConnections: 16);
            if (result.ok)
            {
                // Create or update Lobby and surface its LobbyCode as the user-facing code
                var lobbyRes = await LobbyAdapter.CreateOrUpdateLobbyAsync(activeLobbyId, "RWM5", 16, result.joinCode);
                if (lobbyRes.ok)
                {
                    activeLobbyId = lobbyRes.lobbyId;
                    activeLobbyCode = lobbyRes.lobbyCode;
                    roomCode = activeLobbyCode; // Display LobbyCode to users
                    Debug.Log($"[NetworkManager] Host started via Relay. JoinCode: {result.joinCode} | LobbyCode: {activeLobbyCode}");
                    try { OnRoomCreated?.Invoke(roomCode); } catch { }
                }
                else
                {
                    // Fallback: display Relay JoinCode if Lobby creation failed
                    roomCode = result.joinCode;
                    Debug.LogWarning($"[NetworkManager] Lobby creation failed: {lobbyRes.error}. Showing Relay JoinCode as room code.");
                    try { OnRoomCreated?.Invoke(roomCode); } catch { }
                }
                return;
            }
            Debug.LogWarning($"[NetworkManager] Relay host failed: {result.error}. Falling back to local UDP host.");
        }

        // Fallback: Local UDP hosting
        GenerateRoomCode();

        string advertisedAddress = hostAddress;
        if (string.IsNullOrWhiteSpace(advertisedAddress))
        {
            advertisedAddress = unityTransport.ConnectionData.Address;
            if (string.IsNullOrWhiteSpace(advertisedAddress))
            {
                advertisedAddress = "127.0.0.1";
            }
        }
        string bindAddress = string.IsNullOrWhiteSpace(listenAddress) ? "0.0.0.0" : listenAddress;
        unityTransport.SetConnectionData(advertisedAddress, port, bindAddress);

        bool success = networkManager.StartHost();
        if (success)
        {
            Debug.Log($"[NetworkManager] Host started (local UDP). Room code: {roomCode}");
        }
        else
        {
            Debug.LogError("[NetworkManager] Failed to start host");
            OnConnectionError?.Invoke();
        }
    }

    void GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Text.StringBuilder code = new System.Text.StringBuilder();

        for (int i = 0; i < 5; i++)
        {
            code.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
        }

        roomCode = code.ToString();
    }

    // === CLIENT METHODS ===

    public Task StartClientAsync(string code, string hostIP = "127.0.0.1")
    {
        JoinGame(code, hostIP);
        return Task.CompletedTask;
    }

    public bool JoinGame(string code, string hostIP = "127.0.0.1")
    {
        isHost = false;
        roomCode = code.Trim().ToUpper();

        // Resolve LobbyCode -> Relay JoinCode, then join Relay; fallback to UDP if lobby resolution fails
        Debug.Log($"[NetworkManager] Attempting lobby join with LobbyCode: {roomCode}");
        TryJoinLobbyThenRelay(roomCode, hostIP);
        return true; // async path handles success/failure and fallback if needed
    }

    private async void TryJoinLobbyThenRelay(string lobbyCode, string fallbackHostIP)
    {
        var lobbyRes = await LobbyAdapter.ResolveRelayJoinCodeByLobbyCodeAsync(lobbyCode);
        if (lobbyRes.ok && !string.IsNullOrEmpty(lobbyRes.relayJoinCode))
        {
            var result = await RelayAdapter.JoinAsync(networkManager, unityTransport, lobbyRes.relayJoinCode);
            if (result.ok)
            {
                Debug.Log($"[NetworkManager] Joining Relay with JoinCode: {lobbyRes.relayJoinCode} (from LobbyCode {lobbyCode})");
                return;
            }
            else
            {
                Debug.LogError($"[NetworkManager] Relay join failed after lobby resolve: {result.error}");
            }
        }
        else
        {
            Debug.LogError($"[NetworkManager] Lobby resolve failed: {lobbyRes.error}");
        }
        Debug.LogWarning($"[NetworkManager] Falling back to local UDP {fallbackHostIP}:{port}");
        unityTransport.SetConnectionData(fallbackHostIP, port);
        bool success = networkManager.StartClient();
        if (success)
        {
            Debug.Log($"[NetworkManager] Attempting to join UDP host at {fallbackHostIP}:{port}");
        }
        else
        {
            Debug.LogError("[NetworkManager] Failed to start client after Relay fallback");
            OnConnectionError?.Invoke();
        }
    }

    // Active lobby tracking (host only)
    private string activeLobbyId;
    public string activeLobbyCode;

    // Heuristic not needed anymore; we always try services first when available

    // === PLAYER MANAGEMENT ===

    public void AddPlayer(string playerName, string iconName)
    {
        if (!IsSpawned) return;

        AddPlayerServerRpc(playerId, playerName, iconName);
        Debug.Log($"[NetworkManager] Adding player: {playerName}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerServerRpc(string playerIdParam, string playerName, string iconName, ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;

        // Add to GameManager on server
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPlayer(playerIdParam, playerName, iconName, clientId);
        }

        // Broadcast to all clients
        AddPlayerClientRpc(playerIdParam, playerName, iconName);
    }

    [ClientRpc]
    private void AddPlayerClientRpc(string playerIdParam, string playerName, string iconName)
    {
        OnPlayerAdded?.Invoke(playerIdParam, playerName, iconName);

        // Add to GameManager on all clients
        if (GameManager.Instance != null && !GameManager.Instance.players.ContainsKey(playerIdParam))
        {
            GameManager.Instance.AddPlayer(playerIdParam, playerName, iconName);
        }
    }

    public void RemovePlayer(string playerIdToRemove)
    {
        if (!IsSpawned) return;

        RemovePlayerServerRpc(playerIdToRemove);
        Debug.Log($"[NetworkManager] Removing player: {playerIdToRemove}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemovePlayerServerRpc(string playerIdToRemove)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemovePlayer(playerIdToRemove);
        }

        RemovePlayerClientRpc(playerIdToRemove);
    }

    [ClientRpc]
    private void RemovePlayerClientRpc(string playerIdToRemove)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsClient && NetworkManager.Singleton.LocalClientId == NetworkManager.ServerClientId)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemovePlayer(playerIdToRemove);
        }
    }

    // === GAME STATE SYNC ===

    // REMOVED: Redundant sync methods - GameManager NetworkVariables handle replication automatically
    // NetworkVariables in GameManager already sync from server to clients automatically.
    // These manual RPC sync methods were duplicating that functionality and trying to
    // write to server-owned NetworkVariables from clients, which violates Netcode authority.
    //
    // If UI needs to react to state changes, use NetworkVariable.OnValueChanged callbacks
    // in GameManager instead of these RPCs.

    // === ANSWER SUBMISSION ===

    public void SubmitAnswer(string answer)
    {
        if (!IsSpawned) return;

        SubmitAnswerServerRpc(playerId, answer);
        Debug.Log($"[NetworkManager] Submitting answer: {answer}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitAnswerServerRpc(string playerIdParam, string answer)
    {
        OnAnswerSubmitted?.Invoke(playerIdParam, answer);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitPlayerAnswer(playerIdParam, answer);
        }

        // Broadcast to all clients
        SubmitAnswerClientRpc(playerIdParam, answer);
    }

    [ClientRpc]
    private void SubmitAnswerClientRpc(string playerIdParam, string answer)
    {
        if (isHost) return; // Host already processed

        OnAnswerSubmitted?.Invoke(playerIdParam, answer);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitPlayerAnswer(playerIdParam, answer);
        }
    }

    // === VOTING ===

    public void SubmitEliminationVote(string votedAnswer)
    {
        if (!IsSpawned) return;

        SubmitEliminationVoteServerRpc(playerId, votedAnswer);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitEliminationVoteServerRpc(string playerIdParam, string votedAnswer)
    {
        OnVoteSubmitted?.Invoke(playerIdParam, votedAnswer);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitEliminationVote(playerIdParam, votedAnswer);
        }

        SubmitEliminationVoteClientRpc(playerIdParam, votedAnswer);
    }

    [ClientRpc]
    private void SubmitEliminationVoteClientRpc(string playerIdParam, string votedAnswer)
    {
        if (isHost) return;

        OnVoteSubmitted?.Invoke(playerIdParam, votedAnswer);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitEliminationVote(playerIdParam, votedAnswer);
        }
    }

    public void SubmitVotingVote(string votedAnswer)
    {
        if (!IsSpawned) return;

        SubmitVotingVoteServerRpc(playerId, votedAnswer);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitVotingVoteServerRpc(string playerIdParam, string votedAnswer)
    {
        OnVoteSubmitted?.Invoke(playerIdParam, votedAnswer);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitVotingVote(playerIdParam, votedAnswer);
        }

        SubmitVotingVoteClientRpc(playerIdParam, votedAnswer);
    }

    [ClientRpc]
    private void SubmitVotingVoteClientRpc(string playerIdParam, string votedAnswer)
    {
        if (isHost) return;

        OnVoteSubmitted?.Invoke(playerIdParam, votedAnswer);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitVotingVote(playerIdParam, votedAnswer);
        }
    }

    public void SubmitBonusVote(string votedPlayerID)
    {
        if (!IsSpawned) return;

        SubmitBonusVoteServerRpc(playerId, votedPlayerID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitBonusVoteServerRpc(string playerIdParam, string votedPlayerID)
    {
        OnVoteSubmitted?.Invoke(playerIdParam, votedPlayerID);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitBonusVote(playerIdParam, votedPlayerID);
        }

        SubmitBonusVoteClientRpc(playerIdParam, votedPlayerID);
    }

    [ClientRpc]
    private void SubmitBonusVoteClientRpc(string playerIdParam, string votedPlayerID)
    {
        if (isHost) return;

        OnVoteSubmitted?.Invoke(playerIdParam, votedPlayerID);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SubmitBonusVote(playerIdParam, votedPlayerID);
        }
    }

    // === SCORE UPDATES ===

    /// <summary>
    /// DEPRECATED: Use GameManager.SetPlayerScore() instead
    /// This method tried to update scores via RPC but wrote to a transient dictionary copy.
    /// GameManager's NetworkList<NetworkedPlayerData> handles replication automatically.
    /// </summary>
    [System.Obsolete("Use GameManager.SetPlayerScore() - NetworkList handles replication")]
    public void UpdateScore(string playerIdToUpdate, int newScore)
    {
        Debug.LogWarning("[RWMNetworkManager] UpdateScore is deprecated. Use GameManager.SetPlayerScore() instead.");

        if (!isHost || !IsSpawned) return;

        // Forward to proper server-authoritative method
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerScore(playerIdToUpdate, newScore);
        }
    }

    public void BroadcastTransitionToElimination()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[RWMNetworkManager] Only the host server may broadcast elimination transitions.");
            return;
        }

        Debug.Log("[RWMNetworkManager] Broadcasting transition-to-elimination signal.");
        OnTransitionToElimination?.Invoke();

        if (networkManager == null || !networkManager.IsListening)
        {
            return;
        }

        if (networkManager.ConnectedClientsIds.Count <= 1)
        {
            return;
        }

        TransitionToEliminationClientRpc();
    }

    [ClientRpc]
    void TransitionToEliminationClientRpc()
    {
        if (isHost)
        {
            return;
        }

        Debug.Log("[RWMNetworkManager] Received transition-to-elimination signal from host.");
        OnTransitionToElimination?.Invoke();
    }

    // === SCENE TRANSITIONS ===
    // Note: Scene transitions now handled by NetworkManager.SceneManager
    // OnSceneChanged event still available for custom logic

    // === ROOM CODE ACCESS ===

    public string GetRoomCode()
    {
        return roomCode;
    }

    public string CurrentRoomCode => roomCode;

    public bool IsInPreNetworkScene => networkManager == null || !networkManager.IsListening;

    // === CLEANUP ===

    void OnApplicationQuit()
    {
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            networkManager.OnServerStarted -= OnServerStarted;

            if (networkManager.IsListening)
            {
                networkManager.Shutdown();
            }
        }
    }

    private bool EnsureNetworkingComponents()
    {
        if (componentsValidated && networkManager != null && networkObject != null && unityTransport != null)
        {
            return true;
        }

        if (networkObject == null)
        {
            networkObject = GetComponent<NetworkObject>();
        }

        if (networkObject == null)
        {
            networkObject = gameObject.AddComponent<NetworkObject>();
            Debug.LogWarning("[NetworkManager] NetworkObject component was missing and has been added at runtime to the RWMNetworkManager object. Update the LoadingScreen scene so the NetworkObject lives alongside this component to avoid runtime creation.");
        }

        if (networkManagerOverride != null)
        {
            networkManager = networkManagerOverride;
        }

        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        if (networkManager == null)
        {
            networkManager = GetComponent<NetworkManager>();
        }

        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<NetworkManager>();
        }

        if (networkManager == null)
        {
            Debug.LogError("[NetworkManager] NetworkManager component is missing from the scene. Please ensure a dedicated GameObject (for example, 'NetworkManagerSystem') contains the configured NetworkManager component in the LoadingScreen scene.");
            return false;
        }

        if (unityTransportOverride != null)
        {
            unityTransport = unityTransportOverride;
        }

        if (unityTransport == null)
        {
            unityTransport = networkManager.GetComponent<UnityTransport>();
        }

        if (unityTransport == null)
        {
            unityTransport = FindFirstObjectByType<UnityTransport>();
        }

        if (unityTransport == null)
        {
            unityTransport = networkManager.gameObject.AddComponent<UnityTransport>();
            Debug.LogWarning("[NetworkManager] UnityTransport component was missing and has been added at runtime to the GameObject hosting the NetworkManager. Configure the transport on that object in LoadingScreen so remote clients can connect.");
        }

        componentsValidated = true;
        return true;
    }
}
