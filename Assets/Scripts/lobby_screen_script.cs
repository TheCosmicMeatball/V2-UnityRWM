using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

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
    public Button createRoomButton;
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
    [Tooltip("Optional: Host IP address input (leave null to use room code field if it contains an IP)")]
    public TMP_InputField hostIpInput;
    public Button joinButton; // Button on JoinForm to submit name/icon
    public Transform scrollingPlayerIconContainer;
    public GameObject mobileIconSelectionButtonPrefab;
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
    private IconSelectionButton currentlySelectedIconButton;
    private bool hostRoomCreated = false;
    private Coroutine iconSelectionBuildCoroutine;
    private readonly Dictionary<string, PlayerDisplayState> displayedPlayerStates = new Dictionary<string, PlayerDisplayState>();
    private Coroutine errorCoroutine;
    private bool awaitingNetworkConnection = false;
    private bool hasConnectedToHost = false;
    private bool networkCallbacksRegistered = false;

    private class IconSelectionButton
    {
        public GameObject Root;
        public Button Button;
        public Image IconImage;
        public GameObject SelectionHighlight;
    }
    
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
            // Always visible on desktop; interactable depends on player count
            startTestButton.gameObject.SetActive(true);
            startTestButton.interactable = false;
        }

        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
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

        // If desktop, ensure host is running so room code and joins work
        EnsureHostStartedIfDesktop();

        // Initialize networking flow based on device type
        // Host must explicitly choose to create a room; nothing happens automatically here.

        // Play landing page music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLandingPageMusic();
        }
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

        if (createRoomButton != null)
        {
            createRoomButton.gameObject.SetActive(!isMobile);
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

    void OnHostRoomCreated(string createdRoomCode)
    {
        roomCode = createdRoomCode;
        if (string.IsNullOrEmpty(roomCode))
        {
            // Ignore empty early event (services join code may be assigned shortly after)
            return;
        }

        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = roomCode;
        }

        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[LobbyScreen] Host room created: {roomCode}");

        // Unsubscribe after handling
        if (RWMNetworkManager.Instance != null)
        {
            RWMNetworkManager.Instance.OnRoomCreated -= OnHostRoomCreated;
        }
        hostRoomCreated = true;

        if (startTestButton != null)
        {
            startTestButton.gameObject.SetActive(true);
        }

        if (createRoomButton != null)
        {
            createRoomButton.interactable = false;
        }
    }

    void EnsureHostStartedIfDesktop()
    {
        if (isMobile)
        {
            return;
        }

        // Desktop/host: automatically start the server when entering the lobby
        CoreSystemsBootstrapper.EnsureInitialized();

        var net = RWMNetworkManager.Instance;
        if (net == null)
        {
            Debug.LogWarning("[LobbyScreen] RWMNetworkManager not yet available. Will retry to start host.");
            StartCoroutine(WaitAndStartHost());
            return;
        }

        // Subscribe to room-created so we can display the code
        net.OnRoomCreated -= OnHostRoomCreated;
        net.OnRoomCreated += OnHostRoomCreated;

        // If already listening, we consider host started
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (!hostRoomCreated && !string.IsNullOrEmpty(net.CurrentRoomCode))
            {
                OnHostRoomCreated(net.CurrentRoomCode);
            }
            return;
        }

        try
        {
            // Prefer Relay host if available; StartHost() will already try Relay first per RWMNetworkManager
            net.StartHost();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyScreen] Failed to auto-start host: {ex.Message}");
        }
    }

    System.Collections.IEnumerator WaitAndStartHost()
    {
        float start = Time.realtimeSinceStartup;
        const float timeout = 5f;
        while (RWMNetworkManager.Instance == null && (Time.realtimeSinceStartup - start) < timeout)
        {
            yield return null;
        }

        var net = RWMNetworkManager.Instance;
        if (net == null)
        {
            Debug.LogError("[LobbyScreen] NetworkManager did not become available in time to start host.");
            yield break;
        }

        net.OnRoomCreated -= OnHostRoomCreated;
        net.OnRoomCreated += OnHostRoomCreated;
        net.StartHost();
    }


    async void OnCreateRoomButtonClicked()
    {
        MobileHaptics.LightImpact();

        if (createRoomButton != null)
        {
            createRoomButton.interactable = false;
        }

        if (RWMNetworkManager.Instance == null)
        {
            Debug.LogError("[LobbyScreen] RWMNetworkManager not available. Cannot create network room.");
            GenerateLocalRoomCode();
            if (createRoomButton != null)
            {
                createRoomButton.interactable = true;
            }
            return;
        }

        RWMNetworkManager.Instance.OnRoomCreated -= OnHostRoomCreated;
        RWMNetworkManager.Instance.OnRoomCreated += OnHostRoomCreated;

        try
        {
            await RWMNetworkManager.Instance.StartHostAsync();

            if (!hostRoomCreated)
            {
                string code = RWMNetworkManager.Instance.CurrentRoomCode;
                if (!string.IsNullOrEmpty(code))
                {
                    OnHostRoomCreated(code);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyScreen] Failed to start host: {ex.Message}");
            RWMNetworkManager.Instance.OnRoomCreated -= OnHostRoomCreated;
            GenerateLocalRoomCode();
            if (createRoomButton != null)
            {
                createRoomButton.interactable = true;
            }
        }
    }

    void GenerateLocalRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Text.StringBuilder code = new System.Text.StringBuilder();

        for (int i = 0; i < 5; i++)
        {
            code.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
        }

        roomCode = code.ToString();

        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = roomCode;
        }

        hostRoomCreated = true;

        if (startTestButton != null)
        {
            startTestButton.gameObject.SetActive(true);
        }

        if (createRoomButton != null)
        {
            createRoomButton.interactable = false;
        }

        Debug.LogWarning("Networking not available - using locally generated room code");
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
            GameManager.Instance.BeginMatch();
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
        currentlySelectedIconButton = null;

        if (selectedIcon != null)
        {
            selectedIcon.sprite = null;
            selectedIcon.enabled = false;
        }

        // Ensure room code input supports services join codes
        if (roomCodeInput != null)
        {
            if (roomCodeInput.characterLimit > 0 && roomCodeInput.characterLimit < 6)
            {
                roomCodeInput.characterLimit = 6;
            }
            if (!string.IsNullOrEmpty(roomCodeInput.text))
            {
                roomCodeInput.text = roomCodeInput.text.Trim().ToUpper();
            }
            if (ENABLE_DEBUG_LOGS)
            {
                Debug.Log($"[LobbyScreen] RoomCodeInput limit={roomCodeInput.characterLimit} text='{roomCodeInput.text}' platform={Application.platform} url={Application.absoluteURL}");
            }
        }

        // Defer icon build by one frame to allow layout to size on mobile
        StartCoroutine(BuildIconsAfterLayout());
        UpdateJoinButtonState();
    }

    System.Collections.IEnumerator BuildIconsAfterLayout()
    {
        // Wait at least one frame so the JoinForm hierarchy activates and layouts run
        yield return null;

        Canvas.ForceUpdateCanvases();

        RectTransform contentRect = scrollingPlayerIconContainer as RectTransform;
        ScrollRect scrollRect = null;
        RectTransform viewportRect = null;

        if (contentRect != null)
        {
            scrollRect = contentRect.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                viewportRect = scrollRect.viewport;
            }
        }

        // Give layout a few frames to stabilize on mobile if sizes are zero
        for (int i = 0; i < 5; i++)
        {
            if (viewportRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);
            }

            if (contentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }

            if (contentRect == null || (contentRect.rect.width > 0f && contentRect.rect.height > 0f))
            {
                break;
            }

            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        if (ENABLE_DEBUG_LOGS)
        {
            string contentSize = contentRect != null ? contentRect.rect.size.ToString() : "null";
            string viewportSize = viewportRect != null ? viewportRect.rect.size.ToString() : "null";
            Debug.Log($"[LobbyScreen] BuildIconsAfterLayout - contentRect={contentSize} viewport={viewportSize}");
        }

        RequestIconSelectionListRebuild();

        if (viewportRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);
        }
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
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

    void OnIconSelectionButtonClicked(IconSelectionButton button, string iconName)
    {
        if (button == null)
        {
            return;
        }

        if (currentlySelectedIconButton != null && currentlySelectedIconButton != button)
        {
            SetButtonSelected(currentlySelectedIconButton, false);
        }

        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[LobbyScreen] Icon button clicked for '{iconName}'");

        currentlySelectedIconButton = button;
        SetButtonSelected(currentlySelectedIconButton, true);

        OnPlayerIconSelected(iconName);
    }

    void RequestIconSelectionListRebuild()
    {
        if (iconSelectionBuildCoroutine != null)
        {
            StopCoroutine(iconSelectionBuildCoroutine);
            iconSelectionBuildCoroutine = null;
        }

        if (PlayerManager.Instance != null)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log("[LobbyScreen] PlayerManager ready - building icon list immediately");

            BuildIconSelectionList();
            return;
        }

        if (ENABLE_DEBUG_LOGS)
            Debug.Log("[LobbyScreen] PlayerManager not ready - waiting before building icon selection list");

        CoreSystemsBootstrapper.EnsureInitialized();
        iconSelectionBuildCoroutine = StartCoroutine(WaitForPlayerManagerAndBuildIcons());
    }

    System.Collections.IEnumerator WaitForPlayerManagerAndBuildIcons()
    {
        const float timeoutSeconds = 5f;
        float startTime = Time.realtimeSinceStartup;

        while (PlayerManager.Instance == null && (Time.realtimeSinceStartup - startTime) < timeoutSeconds)
        {
            yield return null;
        }

        iconSelectionBuildCoroutine = null;

        if (PlayerManager.Instance == null)
        {
            Debug.LogError("[LobbyScreen] PlayerManager failed to initialize in time - icon selection cannot be shown");
            ShowErrorMessage("Icon library failed to load. Please restart the app and try again.", false);
            yield break;
        }

        BuildIconSelectionList();
    }

    void BuildIconSelectionList()
    {
        if (scrollingPlayerIconContainer == null || mobileIconSelectionButtonPrefab == null)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.LogWarning("[LobbyScreen] Cannot build icon list - container or prefab missing");

            return;
        }

        if (PlayerManager.Instance == null)
        {
            RequestIconSelectionListRebuild();
            return;
        }

        currentlySelectedIconButton = null;

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
            if (ENABLE_DEBUG_LOGS)
                Debug.LogWarning("[LobbyScreen] PlayerManager returned null icon list");

            UpdateJoinButtonState();
            return;
        }

        IconSelectionButton firstButton = null;
        string firstIconName = null;
        IconSelectionButton matchedSelectionButton = null;
        string desiredSelection = selectedPlayerIconName;
        bool anyIcons = false;
        int builtCount = 0;

        // Determine a sane button size based on viewport height
        float buttonSize = 180f;
        RectTransform containerRect = scrollingPlayerIconContainer as RectTransform;
        ScrollRect parentScroll = containerRect != null ? containerRect.GetComponentInParent<ScrollRect>() : null;
        RectTransform vp = parentScroll != null ? parentScroll.viewport : null;
        if (vp != null)
        {
            float vh = vp.rect.height;
            if (vh > 0f)
            {
                buttonSize = Mathf.Clamp(vh - 20f, 80f, 280f);
            }
        }

        if (ENABLE_DEBUG_LOGS)
            Debug.Log($"[LobbyScreen] Building icons with buttonSize={buttonSize}");

        foreach (string iconName in iconNames)
        {
            GameObject instance = Instantiate(mobileIconSelectionButtonPrefab, scrollingPlayerIconContainer);
            if (instance == null)
            {
                continue;
            }

            IconSelectionButton binding = BindIconSelectionButton(instance);
            if (binding == null)
            {
                if (ENABLE_DEBUG_LOGS)
                    Debug.LogWarning($"[LobbyScreen] Unable to bind icon button for '{iconName}'");

                Destroy(instance);
                continue;
            }

            Sprite iconSprite = PlayerManager.Instance.GetPlayerIcon(iconName);
            if (iconSprite == null && ENABLE_DEBUG_LOGS)
            {
                Debug.LogWarning($"[LobbyScreen] Sprite not found for icon '{iconName}'");
            }

            ApplyIconSprite(binding, iconSprite);

            // Ensure base visuals are enabled and visible
            Image rootImage = instance.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.enabled = true;
                if (rootImage.color.a <= 0f)
                {
                    var c = rootImage.color; c.a = 1f; rootImage.color = c;
                }
            }

            // Ensure layout provides a preferred size so HorizontalLayoutGroup sizes children correctly
            var layoutEl = instance.GetComponent<LayoutElement>();
            if (layoutEl == null)
            {
                layoutEl = instance.AddComponent<LayoutElement>();
            }
            layoutEl.preferredWidth = buttonSize;
            layoutEl.preferredHeight = buttonSize;
            layoutEl.minWidth = Mathf.Min(100f, buttonSize);
            layoutEl.minHeight = Mathf.Min(100f, buttonSize);
            layoutEl.flexibleWidth = 0f;
            layoutEl.flexibleHeight = 0f;
            SetButtonSelected(binding, false);

            IconSelectionButton capturedBinding = binding;
            string capturedIconName = iconName;

            if (capturedBinding.Button != null)
            {
                capturedBinding.Button.onClick.AddListener(() => OnIconSelectionButtonClicked(capturedBinding, capturedIconName));
            }
            else if (ENABLE_DEBUG_LOGS)
            {
                Debug.LogWarning("[LobbyScreen] Icon selection prefab is missing a Button component.");
            }

            if (firstButton == null)
            {
                firstButton = binding;
                firstIconName = iconName;
            }

            if (!string.IsNullOrEmpty(desiredSelection) && iconName == desiredSelection)
            {
                matchedSelectionButton = binding;
            }

            anyIcons = true;
            builtCount++;
        }

        UpdateIconContentLayoutMetrics();

        // Ensure final layout updates after building buttons
        RectTransform contentRectPost = scrollingPlayerIconContainer as RectTransform;
        if (contentRectPost != null)
        {
            ScrollRect srPost = contentRectPost.GetComponentInParent<ScrollRect>();
            if (srPost != null && srPost.viewport != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(srPost.viewport);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectPost);

            // Reset scroll position to start so first icons are in view
            contentRectPost.anchoredPosition = Vector2.zero;

            // Add a faint background on the ScrollRect root to ensure contrast (debug aid)
            if (srPost != null)
            {
                var srImage = srPost.GetComponent<Image>();
                if (srImage != null && srImage.color.a == 0f)
                {
                    var bg = srImage.color; bg.a = 0.08f; srImage.color = bg;
                }
            }
        }

        if (ENABLE_DEBUG_LOGS)
        {
            RectTransform dbgRect = scrollingPlayerIconContainer as RectTransform;
            string dbgSize = dbgRect != null ? dbgRect.rect.size.ToString() : "null";

            string firstInfo = "none";
            if (dbgRect != null && dbgRect.childCount > 0)
            {
                var first = dbgRect.GetChild(0) as RectTransform;
                if (first != null)
                {
                    firstInfo = $"anchored={first.anchoredPosition} size={first.rect.size} localPos={first.localPosition}";
                }
            }

            Debug.Log($"[LobbyScreen] Icon selection build complete. Built {builtCount} buttons. Container child count: {scrollingPlayerIconContainer.childCount} size={dbgSize} first={firstInfo}");
        }

        if (!anyIcons)
        {
            selectedPlayerIconName = string.Empty;
            if (selectedIcon != null)
            {
                selectedIcon.sprite = null;
                selectedIcon.enabled = false;
            }
            UpdateJoinButtonState();
            return;
        }

        if (matchedSelectionButton != null)
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[LobbyScreen] Restoring previously selected icon '{desiredSelection}'");

            OnIconSelectionButtonClicked(matchedSelectionButton, desiredSelection);
        }
        else if (string.IsNullOrEmpty(selectedPlayerIconName) && firstButton != null && !string.IsNullOrEmpty(firstIconName))
        {
            if (ENABLE_DEBUG_LOGS)
                Debug.Log($"[LobbyScreen] Auto-selecting first icon '{firstIconName}'");

            OnIconSelectionButtonClicked(firstButton, firstIconName);
        }
        else
        {
            UpdateJoinButtonState();
        }
    }

    IconSelectionButton BindIconSelectionButton(GameObject instance)
    {
        if (instance == null)
        {
            return null;
        }

        IconSelectionButton binding = new IconSelectionButton
        {
            Root = instance,
            Button = instance.GetComponent<Button>()
        };

        if (binding.Button == null)
        {
            binding.Button = instance.GetComponentInChildren<Button>(true);
        }

        Image iconImage = null;
        Transform iconTransform = instance.transform.Find("Icon");
        if (iconTransform != null)
        {
            iconImage = iconTransform.GetComponent<Image>();
        }

        if (iconImage == null)
        {
            Image[] images = instance.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image candidate = images[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.gameObject == instance)
                {
                    continue;
                }

                if (string.Equals(candidate.gameObject.name, "SelectionHighlight", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                iconImage = candidate;
                break;
            }
        }

        binding.IconImage = iconImage;
        if (binding.IconImage == null && ENABLE_DEBUG_LOGS)
        {
            Debug.LogWarning("[LobbyScreen] Icon selection prefab is missing an Image component for the icon artwork.");
        }

        binding.SelectionHighlight = FindChildGameObject(instance.transform, "SelectionHighlight");
        if (binding.SelectionHighlight != null)
        {
            binding.SelectionHighlight.SetActive(false);
        }
        else if (ENABLE_DEBUG_LOGS)
        {
            Debug.LogWarning("[LobbyScreen] Icon selection prefab is missing a 'SelectionHighlight' child GameObject.");
        }

        return binding;
    }

    void ApplyIconSprite(IconSelectionButton binding, Sprite sprite)
    {
        if (binding == null)
        {
            return;
        }

        if (binding.IconImage != null)
        {
            binding.IconImage.sprite = sprite;
            binding.IconImage.enabled = sprite != null;
        }
    }

    void SetButtonSelected(IconSelectionButton binding, bool selected)
    {
        if (binding == null)
        {
            return;
        }

        if (binding.SelectionHighlight != null)
        {
            binding.SelectionHighlight.SetActive(selected);
        }
    }

    GameObject FindChildGameObject(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                return child.gameObject;
            }

            GameObject nested = FindChildGameObject(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
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

            // Guard against zero-sized children on first layout pass
            if (preferredWidth <= 0f)
            {
                preferredWidth = 64f; // sensible minimum to ensure visibility
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

        // Force rebuild of content and viewport, if present
        ScrollRect sr = contentRect.GetComponentInParent<ScrollRect>();
        if (sr != null && sr.viewport != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(sr.viewport);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }
    
    public void OnJoinButtonClicked()
    {
        MobileHaptics.MediumImpact();

        if (ENABLE_DEBUG_LOGS)
        {
            string rc = roomCodeInput != null ? roomCodeInput.text : "<null>";
            string nm = nameInput != null ? nameInput.text : "<null>";
            Debug.Log($"[LobbyScreen] Join clicked. name='{nm}' roomCode='{rc}' selectedIcon='{selectedPlayerIconName}'");
        }

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

        // WebGL is supported when using Multiplayer Services (Relay) path

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

        // Determine host IP. Prefer explicit field; else treat room code as IP if it looks like one; else default to localhost
        string hostIP = "127.0.0.1";
        if (hostIpInput != null && !string.IsNullOrWhiteSpace(hostIpInput.text))
        {
            hostIP = hostIpInput.text.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(enteredRoomCode) && enteredRoomCode.Contains("."))
        {
            hostIP = enteredRoomCode.Trim();
        }
        pendingHostIP = hostIP;

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
    private string pendingHostIP;

    void OnPlayerJoinedRoom(string joinedRoomCode)
    {
        // Unsubscribe
        RWMNetworkManager.Instance.OnRoomJoined -= OnPlayerJoinedRoom;

        // Ensure NetworkBehaviour is spawned before attempting RPC registration
        StartCoroutine(RegisterPlayerWhenNetworkReady());
    }

    System.Collections.IEnumerator RegisterPlayerWhenNetworkReady()
    {
        var net = RWMNetworkManager.Instance;
        float start = Time.realtimeSinceStartup;
        const float timeout = 5f;

        while (net != null && !net.IsSpawned && (Time.realtimeSinceStartup - start) < timeout)
        {
            yield return null;
        }

        if (net == null)
        {
            ShowErrorMessage("Network system became unavailable while joining.", false);
            yield break;
        }

        if (!net.IsSpawned)
        {
            Debug.LogWarning("[LobbyScreen] Network object not spawned after timeout; attempting registration anyway.");
        }

        // Register the player - PlayerAuthSystem will handle sending RPC to server
        if (PlayerAuthSystem.Instance != null)
        {
            PlayerAuthSystem.Instance.RegisterPlayer(pendingPlayerName, selectedPlayerIconName);
        }
        else
        {
            // Fallback: directly call network manager if auth system unavailable
            Debug.LogWarning("[LobbyScreen] PlayerAuthSystem unavailable, using fallback registration");
            net.AddPlayer(pendingPlayerName, selectedPlayerIconName);
        }

        UpdateWaitingScreenUI(pendingPlayerName);
        Debug.Log("Player successfully joined and registration dispatched: " + pendingPlayerName);
    }

    void ConnectToHostIfNeeded()
    {
        if (ENABLE_DEBUG_LOGS)
        {
            Debug.Log($"[LobbyScreen] ConnectToHostIfNeeded: roomCode='{roomCode}' isHost={(RWMNetworkManager.Instance!=null && RWMNetworkManager.Instance.isHost)} isConnected={(RWMNetworkManager.Instance!=null && RWMNetworkManager.Instance.isConnected)}");
        }

        RegisterNetworkCallbacks();

        if (RWMNetworkManager.Instance == null)
        {
            Debug.LogWarning("[LobbyScreen] RWMNetworkManager.Instance is null in ConnectToHostIfNeeded");
            return;
        }

        if (string.IsNullOrEmpty(roomCode))
        {
            Debug.LogWarning("[LobbyScreen] roomCode empty in ConnectToHostIfNeeded");
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

        // For services join, hostIP is ignored. For UDP fallback, use localhost.
        string hostIP = "127.0.0.1";

        if (ENABLE_DEBUG_LOGS)
        {
            Debug.Log($"[LobbyScreen] Calling JoinGame with roomCode='{roomCode}' hostIP='{hostIP}'");
        }

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

        // Update start button interactable based on player count (desktop/host)
        UpdateStartButtonInteractable();

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

        if (createRoomButton != null)
        {
            createRoomButton.onClick.RemoveListener(OnCreateRoomButtonClicked);
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

        if (RWMNetworkManager.Instance != null)
        {
            RWMNetworkManager.Instance.OnRoomCreated -= OnHostRoomCreated;
        }

        if (nameInput != null)
        {
            nameInput.onValueChanged.RemoveListener(OnNameInputChanged);
        }

        if (iconSelectionBuildCoroutine != null)
        {
            StopCoroutine(iconSelectionBuildCoroutine);
            iconSelectionBuildCoroutine = null;
        }
    }

    void UpdateStartButtonInteractable()
    {
        if (startTestButton == null)
        {
            return;
        }

        if (isMobile)
        {
            startTestButton.gameObject.SetActive(false);
            return;
        }

        startTestButton.gameObject.SetActive(true);

        int count = 0;
        if (GameManager.Instance != null)
        {
            var players = GameManager.Instance.GetAllPlayers();
            count = players != null ? players.Count : 0;
        }

        startTestButton.interactable = count >= 2;
    }
}
