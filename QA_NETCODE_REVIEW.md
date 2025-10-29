# Netcode Implementation Review (Follow-up)

## ✅ Items Verified
- `RWMNetworkManager` now requires and caches the `NetworkManager`, `NetworkObject`, and `UnityTransport` components instead of creating new ones at runtime, and it spawns itself on `OnServerStarted` so the RPC guards on `IsSpawned` now succeed.
- `GameManager` has a `NetworkObject`, subscribes to `NetworkManager.OnServerStarted`, and attempts to spawn itself under host authority, resolving the original issue where its `NetworkVariables` never replicated.

## ⚠️ Outstanding Issues / Best-Practice Gaps
1. **Fallback `NetworkManager` is created without configuration**  
   `CoreSystemsBootstrapper` still manufactures a bare `NetworkManager`/`UnityTransport` pair when the scene copy is missing. That runtime-created singleton has none of the configured prefabs, connection approval settings, lobby handlers, etc., so any session that relies on the fallback will fail to spawn gameplay objects. Best practice is to load a pre-configured prefab (via `NetworkManager.Singleton.NetworkConfig.NetworkPrefabs`) or, preferably, log a fatal error and halt boot so the author-supplied setup is fixed rather than silently replaced.

2. **Scene flow bypasses Netcode's scene system**  
   `GameManager.LoadScene` still uses `SceneManager.LoadScene` locally and then fires a `ClientRpc` to ask everyone else to do the same. This skips Netcode's built-in `NetworkSceneManager`, so late joiners won't be synchronized (they miss the historical scene events), host migration is impossible, and additive network scene support is lost. Switching to `NetworkManager.Singleton.SceneManager.LoadScene` (or additive equivalents) keeps scene state authoritative and automatically handles clients that connect mid-game.

3. **Key gameplay data stays server-only**  
   `GameManager.currentQuestion` and several other round payloads remain plain fields. Client UIs (for example `QuestionScreenScript.DisplayQuestion`) call into `GameManager.Instance.GetCurrentQuestion()`, but those fields never synchronize—only the host ever assigns them. Either move the data into `NetworkVariables`/`NetworkLists` or send structured RPC payloads so that clients receive the current question, answer set, and metadata before rendering screens.

4. **Host transport binds to loopback**  
   `RWMNetworkManager.StartHost` hardcodes `unityTransport.SetConnectionData("127.0.0.1", port)`. This prevents remote devices from connecting because the server only listens on the loopback interface. Use `UnityTransport.SetConnectionData(ServerAddress, port, listenAddress: "0.0.0.0")` (or expose the listen address) so LAN/Internet clients can discover the host.

## 📌 Recommendations
- Replace the fallback singleton creation with a reference to a preconfigured bootstrap prefab, or throw an explicit error prompting designers to include the real networking rig in their scenes.
- Adopt `NetworkSceneManager` for all scene loads (`LoadScene`, `ChangeScene`) so scene transitions are replicated, trackable, and recoverable for late joins.
- Promote question/answer round payloads to netcode-friendly containers (`NetworkVariable<FixedString>` / `NetworkList<FixedString>`), or push them through server-to-client RPCs, before client UI scripts depend on them.
- Expose transport configuration (listen address, port, relay allocation if applicable) instead of hard-coding loopback so mobile/remote clients can connect.
