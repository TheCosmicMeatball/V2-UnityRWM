using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LobbyScreen : MonoBehaviour
{
    // === DEBUG FLAG - SET TO FALSE TO REMOVE ALL DEBUG LOGS ===
    private const bool ENABLE_DEBUG_LOGS = true;
    [Header("Desktop UI Elements")]
    public GameObject desktopDisplay;
    public TextMeshProUGUI roomCodeDisplay; // "Data" in JoinWait
    public Transform playerIconContainer;
    public GameObject playerIconLobbyPrefab;
    public Button startTestButton;
    public Button eightQButton;
    public Button twelveQButton;
    public TextMeshProUGUI headlineText;
    public TextMeshProUGUI dataHeaders;
    public Image desktopBackground;
    
    [Header("Mobile UI Elements - Join Form")]
    public GameObject mobileDisplay;
    public GameObject joinScreen;
    public Button joinGameButton; // Button on JoinScreen to show JoinForm
    public GameObject joinForm;
    public TMP_InputField nameInput;
    public TMP_InputField roomCodeInput;
    public Button joinButton; // Button on JoinForm to submit name/icon
    public Transform scrollingPlayerIconContainer;
    public MobileIconSelectionButton mobileIconSelectionButtonPrefab;
    public Image selectedIcon;
    public Transform playerIconSelectionContainer;
    public Image namebar;
    public TextMeshProUGUI playerNameDisplay;
    public Image joinScreenBackground;
    public Image joinFormBackground;

    [Header("Mobile UI Elements - Join Wait")]
    public GameObject joinWait;
    public Image joinWaitBackground;
    public TextMeshProUGUI waitingHeadlineText;
    public Image waitPlayerIcon;
    public TextMeshProUGUI waitData;
    public TextMeshProUGUI waitDataHeaders;

    [Header("Error Display")]
    public TextMeshProUGUI errorMessageText; // Connect in Unity - shows validation errors
    public TextMeshProUGUI submitMessageText; // Connect in Unity - shows submit confirmation messages
    public float errorDisplayDuration = 3f; // How long to show error messages

    [Header("State")]
    private string selectedPlayerIconName = "";
    private string roomCode = "";
    private bool isMobile = false;
    private List<GameObject> spawnedPlayerIcons = new List<GameObject>();
    private MobileIconSelectionButton currentlySelectedMobileIconButton;
    private readonly Dictionary<string, PlayerDisplayState> displayedPlayerStates = new Dictionary<string, PlayerDisplayState>();
    private Coroutine errorCoroutine;
    private bool awaitingNetworkConnection = false;
    private bool hasConnectedToHost = false;
    private bool networkCallbacksRegistered = false;
    
    void Start()
    {
        isMobile = DeviceDetector.Instance != null && DeviceDetector.Instance.IsMobile();

        if (ENABLE_DEBUG_LOGS)
        {
            Debug.Log($"[LobbyScreen] Start - isMobile: {isMobile}");
            Debug.Log($"[LobbyScreen] Screen size: {Screen.width}x{Screen.height}");
        }

        // Set game state to Lobby (server-only)
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.currentGameState.Value = GameManager.GameState.Lobby;
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[LobbyScreen] Set currentGameState to Lobby");
        }

        // Setup desktop buttons
        if (startTestButton != null)
        {
            startTestButton.onClick.AddListener(OnStartGameClicked);
        }

        if (eightQButton != null)
        {
            eightQButton.onClick.AddListener(() => OnGameModeSelected(GameManager.GameMode.EightQuestions));
        }

        if (twelveQButton != null)
        {
            twelveQButton.onClick.AddListener(() => OnGameModeSelected(GameManager.GameMode.TwelveQuestions));
        }

        // Setup mobile join game button (shows the form)
        if (joinGameButton != null)
        {
            joinGameButton.onClick.AddListener(OnJoinGameButtonClicked);
        }

        // Setup mobile join button (submits the form)
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }

        if (nameInput != null)
        {
            nameInput.onValueChanged.AddListener(OnNameInputChanged);
        }

        UpdateJoinButtonState();

        RegisterNetworkCallbacks();

        // Show appropriate display
        ShowAppropriateDisplay();

        // Initialize networking flow based on device type
        if (!isMobile)
        {
            SetupDesktopHost();

            // Add desktop host as a player
            AddDesktopHostPlayer();
        }

        // Play landing page music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLandingPageMusic();
        }
    }

    void AddDesktopHostPlayer()
    {
        // Create host player ID
        string hostPlayerID = PlayerAuthSystem.Instance != null ?
            PlayerAuthSystem.Instance.GetLocalPlayerID() :
            "host_" + SystemInfo.deviceUniqueIdentifier;

        // Use a default icon for the host
        string hostIconName = PlayerManager.Instance != null ?
            PlayerManager.Instance.GetRandomIconName() :
            "player icon (1)";

        // Desktop host is NOT a player - it's just a display screen
        // Players join via mobile devices only
        Debug.Log("Desktop is host - not added as a player");
    }
    
    void Update()
    {
        // Update player list
        UpdatePlayerList();
    }
    
    void ShowAppropriateDisplay()
    {
        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[LobbyScreen] ShowAppropriateDisplay - isMobile: {isMobile}");

        if (desktopDisplay != null)
        {
            desktopDisplay.SetActive(!isMobile);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[LobbyScreen] desktopDisplay set to: {!isMobile}");
        }

        if (mobileDisplay != null)
        {
            mobileDisplay.SetActive(isMobile);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[LobbyScreen] mobileDisplay set to: {isMobile}");
        }

        // Mobile starts with joinScreen visible, then shows JoinForm when button clicked
        if (isMobile)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[LobbyScreen] Mobile detected - showing JoinScreen");

            // Show JoinScreen (initial screen with "Join Game" button)
            if (joinScreen != null)
            {
                joinScreen.SetActive(true);
                if (ENABLE_DEBUG_LOGS)
                    Debug.Log($"[LobbyScreen] joinScreen set to: true");
            }

            // Hide JoinForm initially
            if (joinForm != null)
            {
                joinForm.SetActive(false);
                if (ENABLE_DEBUG_LOGS)
                    Debug.Log($"[LobbyScreen] joinForm set to: false");
            }

            // Hide JoinWait initially
            if (joinWait != null)
            {
                joinWait.SetActive(false);
                if (ENABLE_DEBUG_LOGS)
                    Debug.Log($"[LobbyScreen] joinWait set to: false");
            }
        }
    }

    void RegisterNetworkCallbacks()
    {
        if (!isMobile || networkCallbacksRegistered)
        {
            return;
        }

        if (RWMNetworkManager.Instance == null)
        {
            return;
        }

        RWMNetworkManager.Instance.OnRoomJoined += HandleRoomJoined;
        RWMNetworkManager.Instance.OnConnectionError += HandleConnectionError;
        networkCallbacksRegistered = true;
    }

    void UnregisterNetworkCallbacks()
    {
        if (!networkCallbacksRegistered)
        {
            return;
        }

        if (RWMNetworkManager.Instance != null)
        {
            RWMNetworkManager.Instance.OnRoomJoined -= HandleRoomJoined;
            RWMNetworkManager.Instance.OnConnectionError -= HandleConnectionError;
        }

        networkCallbacksRegistered = false;
    }

    void HandleRoomJoined(string joinedRoomCode)
    {
        if (!isMobile)
        {
            return;
        }

        awaitingNetworkConnection = false;
        hasConnectedToHost = true;

        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[LobbyScreen] Successfully joined room: {joinedRoomCode}");
    }

    void HandleConnectionError()
    {
        if (!isMobile)
        {
            return;
        }

        bool wasConnected = hasConnectedToHost;
        hasConnectedToHost = false;

        if (awaitingNetworkConnection)
        {
            awaitingNetworkConnection = false;
            ShowJoinForm();
            ShowErrorMessage("Unable to connect to the host. Please check the room code and try again.", false);
        }
        else if (wasConnected)
        {
            ShowJoinForm();
            ShowErrorMessage("Connection to the host was lost. Please try joining again.", false);
        }
    }

    void SetupDesktopHost()
    {
        if (RWMNetworkManager.Instance != null)
        {
            // Register callback for when room is created
            RWMNetworkManager.Instance.OnRoomCreated += OnHostRoomCreated;

            // Start host immediately
            RWMNetworkManager.Instance.StartHost();
            roomCode = RWMNetworkManager.Instance.GetRoomCode();

            if (roomCodeDisplay != null)
            {
                roomCodeDisplay.text = roomCode;
            }
        }
        else
        {
            // Fallback: generate a local code so the UI isn't empty when networking is unavailable
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            System.Text.StringBuilder code = new System.Text.StringBuilder();

            for (int i = 0; i < 5; i++)
            {
                code.Append(chars[Random.Range(0, chars.Length)]);
            }

            roomCode = code.ToString();

            if (roomCodeDisplay != null)
            {
                roomCodeDisplay.text = roomCode;
            }

            Debug.LogWarning("Networking not available - using locally generated room code");
        }
    }

    void OnHostRoomCreated(string createdRoomCode)
    {
        roomCode = createdRoomCode;
        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = roomCode;
        }

        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[LobbyScreen] Host room created: {roomCode}");

        // Unsubscribe after handling
        RWMNetworkManager.Instance.OnRoomCreated -= OnHostRoomCreated;
    }


    void OnGameModeSelected(GameManager.GameMode mode)
    {
        MobileHaptics.SelectionChanged();

        // Only server can set game mode
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.gameMode.Value = mode;
            Debug.Log("Game mode selected: " + mode);
        }
        else
        {
            Debug.LogWarning("[LobbyScreen] Non-server tried to set game mode");
        }

        // Update UI to show selected mode
        if (eightQButton != null && twelveQButton != null)
        {
            // Visual feedback for selected button (you can add color changes, etc.)
            if (mode == GameManager.GameMode.EightQuestions)
            {
                Debug.Log("8 Questions selected");
            }
            else
            {
                Debug.Log("12 Questions selected");
            }
        }
    }
    
    public void OnEightQuestionsClicked()
    {
        OnGameModeSelected(GameManager.GameMode.EightQuestions);
    }

    public void OnTwelveQuestionsClicked()
    {
        OnGameModeSelected(GameManager.GameMode.TwelveQuestions);
    }

    public void OnStartGameClicked()
    {
        MobileHaptics.MediumImpact();

        // Check if we have enough players (minimum 2)
        if (GameManager.Instance.GetAllPlayers().Count < 2)
        {
            Debug.LogWarning("Need at least 2 players to start!");
            return;
        }

        Debug.Log("Start button clicked - currentGameState: " + GameManager.Instance.currentGameState.Value + ", currentRound: " + GameManager.Instance.currentRound.Value);

        // Advance to Round Art (which will load Round 1)
        if (GameManager.Instance != null && GameManager.Instance.IsServer)
        {
            GameManager.Instance.AdvanceToNextScreen();
        }
        else
        {
            Debug.LogWarning("[LobbyScreen] Only the host can start the game.");
        }
    }

    // === MOBILE JOIN FORM ===

    public void OnJoinGameButtonClicked()
    {
        MobileHaptics.LightImpact();

        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[LobbyScreen] OnJoinGameButtonClicked - transitioning from JoinScreen to JoinForm");

        // User clicked "Join Game" button - show the form
        ShowJoinForm();
    }

    void ShowJoinForm()
    {
        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[LobbyScreen] ShowJoinForm called");

        awaitingNetworkConnection = false;
        hasConnectedToHost = false;

        // Hide JoinScreen
        if (joinScreen != null)
        {
            joinScreen.SetActive(false);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[LobbyScreen] joinScreen hidden");
        }

        // Show JoinForm
        if (joinForm != null)
        {
            joinForm.SetActive(true);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[LobbyScreen] joinForm shown");
        }

        // Hide JoinWait
        if (joinWait != null)
        {
            joinWait.SetActive(false);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[LobbyScreen] joinWait hidden");
        }

        selectedPlayerIconName = string.Empty;
        currentlySelectedMobileIconButton = null;

        if (selectedIcon != null)
        {
            selectedIcon.sprite = null;
            selectedIcon.enabled = false;
        }

        BuildIconSelectionList();
        UpdateJoinButtonState();
    }

    void ShowJoinWait()
    {
        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[LobbyScreen] ShowJoinWait called");

        // Hide JoinScreen
        if (joinScreen != null)
        {
            joinScreen.SetActive(false);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[LobbyScreen] joinScreen hidden");
        }

        // Hide JoinForm
        if (joinForm != null)
        {
            joinForm.SetActive(false);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[LobbyScreen] joinForm hidden");
        }

        // Show JoinWait
        if (joinWait != null)
        {
            joinWait.SetActive(true);
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[LobbyScreen] joinWait shown");
        }
    }
    
    public void OnPlayerIconSelected(string iconName)
    {
        MobileHaptics.SelectionChanged();

        // Check if icon is available (not already selected by another player)
        if (PlayerManager.Instance != null && !PlayerManager.Instance.IsIconAvailable(iconName))
        {
            Debug.LogWarning($"Icon {iconName} is already selected by another player!");
            ShowErrorMessage("This icon is already taken. Please choose another one.");
            return;
        }

        selectedPlayerIconName = iconName;

        // Update selected icon display
        if (selectedIcon != null && PlayerManager.Instance != null)
        {
            selectedIcon.sprite = PlayerManager.Instance.GetPlayerIcon(iconName);
            selectedIcon.enabled = selectedIcon.sprite != null;
        }

        Debug.Log("Player icon selected: " + iconName);

        UpdateJoinButtonState();
    }

    void OnIconSelectionButtonClicked(MobileIconSelectionButton button, string iconName)
    {
        if (currentlySelectedMobileIconButton != null && currentlySelectedMobileIconButton != button)
        {
            currentlySelectedMobileIconButton.SetSelected(false);
        }

        currentlySelectedMobileIconButton = button;
        if (currentlySelectedMobileIconButton != null)
        {
            currentlySelectedMobileIconButton.SetSelected(true);
        }

        OnPlayerIconSelected(iconName);
    }

    void BuildIconSelectionList()
    {
        if (scrollingPlayerIconContainer == null || mobileIconSelectionButtonPrefab == null || PlayerManager.Instance == null)
        {
            return;
        }

        currentlySelectedMobileIconButton = null;

        for (int i = scrollingPlayerIconContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = scrollingPlayerIconContainer.GetChild(i);
            if (child == null)
            {
                continue;
            }

            child.SetParent(null);
            Destroy(child.gameObject);
        }

        IEnumerable<string> iconNames = PlayerManager.Instance.GetAvailableIconNames();
        if (iconNames == null)
        {
            return;
        }

        foreach (string iconName in iconNames)
        {
            MobileIconSelectionButton button = Instantiate(mobileIconSelectionButtonPrefab, scrollingPlayerIconContainer);
            if (button == null)
            {
                continue;
            }

            Sprite iconSprite = PlayerManager.Instance.GetPlayerIcon(iconName);
            button.SetIcon(iconSprite);
            button.SetSelected(false);

            string capturedIconName = iconName;
            button.Button.onClick.AddListener(() => OnIconSelectionButtonClicked(button, capturedIconName));
        }

        UpdateIconContentLayoutMetrics();
    }

    void OnNameInputChanged(string value)
    {
        UpdateJoinButtonState();
    }

    void UpdateJoinButtonState()
    {
        if (joinButton == null)
        {
            return;
        }

        bool hasName = nameInput == null || !string.IsNullOrWhiteSpace(nameInput.text);
        bool hasIcon = !string.IsNullOrEmpty(selectedPlayerIconName);
        joinButton.interactable = hasName && hasIcon;
    }

    void UpdateIconContentLayoutMetrics()
    {
        if (scrollingPlayerIconContainer == null)
        {
            return;
        }

        RectTransform contentRect = scrollingPlayerIconContainer as RectTransform;
        if (contentRect == null)
        {
            return;
        }

        HorizontalLayoutGroup layoutGroup = contentRect.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            return;
        }

        int childCount = contentRect.childCount;
        float totalWidth = layoutGroup.padding.left + layoutGroup.padding.right;
        float spacing = layoutGroup.spacing;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform childRect = contentRect.GetChild(i) as RectTransform;
            if (childRect == null)
            {
                continue;
            }

            float preferredWidth = 0f;

            LayoutElement layoutElement = childRect.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                if (layoutElement.preferredWidth > 0)
                {
                    preferredWidth = layoutElement.preferredWidth;
                }
                else if (layoutElement.minWidth > 0)
                {
                    preferredWidth = layoutElement.minWidth;
                }
            }

            if (preferredWidth <= 0f)
            {
                float rectWidth = childRect.rect.width;
                if (rectWidth > 0f)
                {
                    preferredWidth = rectWidth;
                }
            }

            if (preferredWidth <= 0f)
            {
                preferredWidth = Mathf.Abs(childRect.sizeDelta.x);
            }

            totalWidth += Mathf.Max(0f, preferredWidth);
            if (i < childCount - 1)
            {
                totalWidth += spacing;
            }
        }

        if (childCount == 0)
        {
            totalWidth = 0f;
        }

        contentRect.sizeDelta = new Vector2(totalWidth, contentRect.sizeDelta.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }
    
    public void OnJoinButtonClicked()
    {
        MobileHaptics.MediumImpact();

        if (nameInput == null || string.IsNullOrEmpty(nameInput.text))
        {
            Debug.LogWarning("Please enter a name!");
            ShowErrorMessage("Please enter a name!");
            return;
        }

        if (string.IsNullOrEmpty(selectedPlayerIconName))
        {
            Debug.LogWarning("Please select an icon!");
            ShowErrorMessage("Please select an icon!");
            return;
        }

        string enteredRoomCode = roomCodeInput != null ? roomCodeInput.text.Trim().ToUpper() : "";

        if (isMobile && string.IsNullOrEmpty(enteredRoomCode))
        {
            Debug.LogWarning("Please enter the room code!");
            ShowErrorMessage("Please enter the room code!");
            return;
        }

        // Validate name using ContentFilterManager
        if (ContentFilterManager.Instance != null)
        {
            ValidationResult validation = ContentFilterManager.Instance.ValidatePlayerName(nameInput.text);

            if (!validation.isValid)
            {
                Debug.LogWarning("Name validation failed: " + validation.errorMessage);
                ShowErrorMessage(validation.errorMessage);
                return;
            }

            // Use sanitized name
            string sanitizedName = validation.sanitizedText;

            CompleteJoinFlow(sanitizedName, enteredRoomCode);
        }
        else
        {
            // Fallback if ContentFilterManager not available
            CompleteJoinFlow(nameInput.text.Trim(), enteredRoomCode);
        }
    }

    void CompleteJoinFlow(string playerName, string enteredRoomCode)
    {
        // Validate network prerequisites BEFORE proceeding
        if (RWMNetworkManager.Instance == null)
        {
            ShowErrorMessage("Network system is not available. Please restart the app and try again.", false);
            return;
        }

        if (!string.IsNullOrEmpty(enteredRoomCode))
        {
            roomCode = enteredRoomCode;
        }

        if (string.IsNullOrEmpty(roomCode))
        {
            ShowErrorMessage("Room code is required to join a game.", false);
            return;
        }

        // Store player info for registration after connection
        string playerID = System.Guid.NewGuid().ToString();
        if (PlayerAuthSystem.Instance != null)
        {
            playerID = PlayerAuthSystem.Instance.GetLocalPlayerID();
        }

        // Store for use after connection
        pendingPlayerName = playerName;
        pendingPlayerID = playerID;

        // Subscribe to connection events
        RWMNetworkManager.Instance.OnRoomJoined += OnPlayerJoinedRoom;

        // Attempt connection
        ConnectToHostIfNeeded();

        // Show waiting screen (connection in progress)
        UpdateWaitingScreenUI(playerName);
        ShowJoinWait();
    }

    private string pendingPlayerName;
    private string pendingPlayerID;

    void OnPlayerJoinedRoom(string joinedRoomCode)
    {
        // Unsubscribe
        RWMNetworkManager.Instance.OnRoomJoined -= OnPlayerJoinedRoom;

        // Register the player - PlayerAuthSystem will handle sending RPC to server
        if (PlayerAuthSystem.Instance != null)
        {
            PlayerAuthSystem.Instance.RegisterPlayer(pendingPlayerName, selectedPlayerIconName);
        }
        else
        {
            // Fallback: directly call network manager if auth system unavailable
            Debug.LogWarning("[LobbyScreen] PlayerAuthSystem unavailable, using fallback registration");
            if (RWMNetworkManager.Instance != null)
            {
                RWMNetworkManager.Instance.AddPlayer(pendingPlayerName, selectedPlayerIconName);
            }
        }

        UpdateWaitingScreenUI(pendingPlayerName);
        Debug.Log("Player successfully joined and registered: " + pendingPlayerName);
    }

    void ConnectToHostIfNeeded()
    {
        RegisterNetworkCallbacks();

        if (RWMNetworkManager.Instance == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(roomCode))
        {
            return;
        }

        // Check if already connected
        if (RWMNetworkManager.Instance.isHost)
        {
            return;
        }

        if (RWMNetworkManager.Instance.isConnected && !string.IsNullOrEmpty(RWMNetworkManager.Instance.roomCode))
        {
            awaitingNetworkConnection = false;
            hasConnectedToHost = true;
            return;
        }

        // NOTE: For local testing, connect to localhost
        // For networked play, you'll need to provide the host's IP address
        // TODO: Add UI for entering host IP address for mobile clients
        string hostIP = "127.0.0.1"; // Localhost for testing

        bool started = RWMNetworkManager.Instance.JoinGame(roomCode, hostIP);

        if (!started)
        {
            ShowJoinForm();
            ShowErrorMessage("Unable to start the network client. Please try again.", false);
            return;
        }

        awaitingNetworkConnection = true;
        hasConnectedToHost = false;
    }

    void UpdateWaitingScreenUI(string playerName)
    {
        if (playerNameDisplay != null)
        {
            playerNameDisplay.text = playerName;
        }

        if (waitPlayerIcon != null && PlayerManager.Instance != null)
        {
            waitPlayerIcon.sprite = PlayerManager.Instance.GetPlayerIcon(selectedPlayerIconName);
        }
    }

    void ShowErrorMessage(string message, bool logAsError = true)
    {
        MobileHaptics.Failure();

        if (logAsError)
        {
            Debug.LogError("INPUT ERROR: " + message);
        }
        else
        {
            Debug.LogWarning("INPUT WARNING: " + message);
        }

        // Display error message in UI
        if (errorMessageText != null)
        {
            errorMessageText.text = message;
            errorMessageText.gameObject.SetActive(true);

            // Clear previous coroutine if exists
            if (errorCoroutine != null)
            {
                StopCoroutine(errorCoroutine);
            }

            // Auto-hide after duration
            errorCoroutine = StartCoroutine(HideErrorMessageAfterDelay());
        }

        // Play error sound if AudioManager exists
        if (AudioManager.Instance != null)
        {
            // Could add: AudioManager.Instance.PlayErrorSFX();
        }
    }

    System.Collections.IEnumerator HideErrorMessageAfterDelay()
    {
        yield return new WaitForSeconds(errorDisplayDuration);

        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }
    }
    
    // === PLAYER LIST UPDATES ===

    private struct PlayerDisplayState
    {
        public string Name;
        public string IconName;
    }

    void UpdatePlayerList()
    {
        if (playerIconContainer == null) return;
        if (GameManager.Instance == null) return;

        List<PlayerData> players = GameManager.Instance.GetAllPlayers();
        List<PlayerData> nonHostPlayers = new List<PlayerData>();

        foreach (PlayerData player in players)
        {
            if (!player.isHost)
            {
                nonHostPlayers.Add(player);
            }
        }

        bool requiresRefresh = false;

        if (nonHostPlayers.Count != displayedPlayerStates.Count)
        {
            requiresRefresh = true;
        }
        else
        {
            foreach (PlayerData player in nonHostPlayers)
            {
                if (!displayedPlayerStates.TryGetValue(player.playerID, out PlayerDisplayState state) ||
                    state.Name != player.playerName ||
                    state.IconName != player.iconName)
                {
                    requiresRefresh = true;
                    break;
                }
            }
        }

        if (!requiresRefresh)
        {
            return;
        }

        HashSet<string> previousPlayerIds = new HashSet<string>(displayedPlayerStates.Keys);

        // Clear existing icons
        foreach (GameObject icon in spawnedPlayerIcons)
        {
            if (icon != null)
            {
                Destroy(icon);
            }
        }
        spawnedPlayerIcons.Clear();
        displayedPlayerStates.Clear();

        int nonHostPlayerCount = 0;

        // Spawn new icons for each player (excluding host)
        foreach (PlayerData player in nonHostPlayers)
        {
            nonHostPlayerCount++;

            if (playerIconLobbyPrefab != null)
            {
                GameObject iconObj = Instantiate(playerIconLobbyPrefab, playerIconContainer);

                // Set player name
                Transform nameTransform = FindPlayerNameTransform(iconObj);
                if (nameTransform != null)
                {
                    TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = player.playerName;
                    }
                }

                // Set player icon
                Transform iconTransform = FindPlayerIconTransform(iconObj);
                if (iconTransform != null && PlayerManager.Instance != null)
                {
                    Image iconImage = iconTransform.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        iconImage.sprite = PlayerManager.Instance.GetPlayerIcon(player.iconName);
                    }
                }

                spawnedPlayerIcons.Add(iconObj);
                displayedPlayerStates[player.playerID] = new PlayerDisplayState
                {
                    Name = player.playerName,
                    IconName = player.iconName
                };

                // Animate bounce scale-up for newly added players
                if (!previousPlayerIds.Contains(player.playerID))
                {
                    StartCoroutine(BounceScaleUp(iconObj.transform));
                }
            }
        }

        // Update player count on mobile wait screen (excluding host)
        if (isMobile && waitData != null)
        {
            string displayRoomCode = !string.IsNullOrEmpty(roomCode) ? roomCode :
                (RWMNetworkManager.Instance != null ? RWMNetworkManager.Instance.GetRoomCode() : "");

            waitData.text = nonHostPlayerCount + " Players\n" +
                           (GameManager.Instance.gameMode.Value == GameManager.GameMode.EightQuestions ? "8" : "12") + " Questions\n" +
                           displayRoomCode;
        }

        if (!isMobile && roomCodeDisplay != null && string.IsNullOrEmpty(roomCode))
        {
            string hostRoomCode = RWMNetworkManager.Instance != null ? RWMNetworkManager.Instance.GetRoomCode() : "";
            if (!string.IsNullOrEmpty(hostRoomCode))
            {
                roomCode = hostRoomCode;
                roomCodeDisplay.text = roomCode;
            }
        }
    }

    System.Collections.IEnumerator BounceScaleUp(Transform transform)
    {
        if (transform == null) yield break;

        float duration = 0.6f;
        float elapsed = 0f;

        // Start from scale 0
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        Vector3 overshootScale = targetScale * 1.2f; // Overshoot by 20%

        transform.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Elastic ease out with bounce
            float easedT;
            if (t < 0.5f)
            {
                // First half: scale up to overshoot
                easedT = 1f - Mathf.Pow(1f - (t * 2f), 3f);
                transform.localScale = Vector3.Lerp(startScale, overshootScale, easedT);
            }
            else
            {
                // Second half: bounce back to target
                easedT = 1f - Mathf.Pow(1f - ((t - 0.5f) * 2f), 2f);
                transform.localScale = Vector3.Lerp(overshootScale, targetScale, easedT);
            }

            yield return null;
        }

        transform.localScale = targetScale;
    }

    Transform FindPlayerIconTransform(GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }

        // Direct child search
        Transform icon = obj.transform.Find("PlayerIcon");
        if (icon != null)
        {
            return icon;
        }

        // Could add additional fallback logic here if needed
        return null;
    }

    Transform FindPlayerNameTransform(GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }

        // Direct child search
        Transform nameTransform = obj.transform.Find("PlayerName");
        if (nameTransform != null)
        {
            return nameTransform;
        }

        // Could add additional fallback logic here if needed
        return null;
    }

    void OnDestroy()
    {
        UnregisterNetworkCallbacks();

        // Clean up listeners
        if (startTestButton != null)
        {
            startTestButton.onClick.RemoveListener(OnStartGameClicked);
        }
        
        if (eightQButton != null)
        {
            eightQButton.onClick.RemoveAllListeners();
        }
        
        if (twelveQButton != null)
        {
            twelveQButton.onClick.RemoveAllListeners();
        }
        
        if (joinGameButton != null)
        {
            joinGameButton.onClick.RemoveListener(OnJoinGameButtonClicked);
        }

        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(OnJoinButtonClicked);
        }

        if (nameInput != null)
        {
            nameInput.onValueChanged.RemoveListener(OnNameInputChanged);
        }
    }
}
