# Unity Netcode Setup Instructions

## Overview
This game has been converted from NativeWebSocket to **Unity Netcode for GameObjects**. The Node.js WebSocket server is no longer required.

## Architecture
- **Desktop**: Acts as Host (Server + Client)
- **Mobile Devices**: Connect as Clients to the desktop host
- **Network Transport**: Unity Transport Package (UTP) over UDP port 7777

---

## Unity Editor Setup

### 1. Install Unity Netcode Package
The package has already been added to `Packages/manifest.json`:
```json
"com.unity.netcode.gameobjects": "1.8.1"
```

Unity should automatically download and install the package when you open the project.

### 2. Setup NetworkManager GameObject

In each scene that requires networking (especially LobbyScreen), you need to configure the NetworkManager:

#### A. Create or Configure NetworkManager GameObject:
1. In the Unity Hierarchy, find or create a GameObject named "NetworkManager"
2. Add the `RWMNetworkManager` component to it
3. **Important**: The `RWMNetworkManager` script must be attached to a GameObject that also has Unity's `NetworkManager` and `UnityTransport` components (these are added automatically by the script)

#### B. Configure the RWMNetworkManager:
- **Port**: Default is 7777 (can be changed if needed)
- Make sure the GameObject is set to persist across scenes (DontDestroyOnLoad is handled in code)

#### C. Mark as NetworkObject:
1. Add the `NetworkObject` component to the NetworkManager GameObject
2. This allows the RWMNetworkManager to send RPCs and handle network events

### 3. Scene Setup Checklist

For **every scene** that uses networking:
- [ ] NetworkManager GameObject exists with RWMNetworkManager component
- [ ] NetworkObject component is attached and configured
- [ ] GameManager persists across scenes (DontDestroyOnLoad)
- [ ] All networked GameObjects have NetworkObject components if they need to sync state

---

## How the Connection System Works

### Desktop (Host) Flow:
1. Desktop starts in LobbyScreen
2. `SetupDesktopHost()` is called in LobbyScreen
3. `RWMNetworkManager.StartHost()` starts Unity's NetworkManager as Host
4. A 5-character room code is generated (e.g., "XJ4K9")
5. Room code is displayed on screen for mobile players to enter
6. Host listens on UDP port 7777 for client connections

### Mobile (Client) Flow:
1. Mobile device opens LobbyScreen and sees the join form
2. Player enters their name, selects icon, and enters the room code
3. `RWMNetworkManager.JoinGame(roomCode, hostIP)` is called
4. Client connects to host's IP address on port 7777
5. Once connected, player data is sent via `AddPlayerServerRpc()`
6. Player appears in the lobby on all connected devices

---

## Network Communication

All game events are now synchronized using Unity Netcode RPCs (Remote Procedure Calls):

### Server RPCs (Client → Host):
- `AddPlayerServerRpc()` - Register a new player
- `SubmitAnswerServerRpc()` - Submit answer to question
- `SubmitEliminationVoteServerRpc()` - Submit elimination vote
- `SubmitVotingVoteServerRpc()` - Submit voting vote
- `SubmitBonusVoteServerRpc()` - Submit bonus vote

### Client RPCs (Host → All Clients):
- `AddPlayerClientRpc()` - Broadcast new player to all clients
- `SyncGameStateClientRpc()` - Sync game state changes
- `SyncTimerClientRpc()` - Sync timer countdown
- `UpdateScoreClientRpc()` - Sync score updates
- `ChangeSceneClientRpc()` - Trigger scene changes on clients

---

## Networking on Local Network (LAN)

### For Testing on Same Machine:
- Host IP: `127.0.0.1` (localhost)
- This is the default and works for testing desktop + mobile simulator

### For Testing on Local Network:
1. **Find Host's Local IP Address:**
   - Windows: Open Command Prompt and run `ipconfig`
   - Look for "IPv4 Address" under your active network adapter (e.g., `192.168.1.100`)

2. **Update Mobile Client Connection:**
   - In `lobby_screen_script.cs`, find the `ConnectToHostIfNeeded()` method
   - Change `string hostIP = "127.0.0.1";` to your host's local IP
   - Example: `string hostIP = "192.168.1.100";`

3. **Ensure Firewall Allows Connections:**
   - Windows Firewall may block incoming connections on port 7777
   - Add an inbound rule to allow UDP traffic on port 7777
   - Or temporarily disable firewall for testing (not recommended for production)

### For Production (Internet Play):
You will need one of the following:
- **Port Forwarding**: Configure your router to forward port 7777 to the host machine
- **Unity Relay Service**: Use Unity's managed relay service (requires Unity Gaming Services)
- **Third-party Solutions**: Photon, Mirror, or other relay services

---

## Testing the Conversion

### 1. Test Host Connection:
- Open the project in Unity Editor
- Press Play and navigate to LobbyScreen
- Verify a room code appears on the desktop display
- Check the Console for "[NetworkManager] Host started with room code: XXXXX"

### 2. Test Client Connection (Localhost):
- Build the project for mobile or run a second Unity Editor instance
- On the client, enter a player name and icon
- Enter the room code displayed on the host
- Click Join
- The player should appear in both the host and client lobbies

### 3. Test Game Flow:
- Add at least 2 players (minimum to start)
- Select game mode (8 or 12 questions)
- Click "Start Test" on the host
- Verify all clients transition to the first round
- Test answer submission, voting, and scoring
- Verify all game state stays synchronized

---

## Troubleshooting

### "Cannot send message - not connected"
- The RWMNetworkManager component is not properly spawned as a NetworkObject
- Make sure the NetworkManager GameObject has both NetworkObject and RWMNetworkManager components
- Verify the GameObject is set to DontDestroyOnLoad

### Clients can't connect to host:
- Verify host IP address is correct
- Check firewall settings on host machine
- Ensure both devices are on the same network
- Try pinging the host from the client device

### Players not appearing in lobby:
- Check that AddPlayerServerRpc() is being called
- Verify GameManager is persisting across scenes
- Look for errors in the Unity Console

### Scene transitions not working:
- Ensure ChangeSceneClientRpc() is being called by the host
- Verify all scenes are added to Build Settings
- Check that GameManager.LoadScene() is using the correct scene names

---

## Key Differences from WebSocket System

| Feature | Old (WebSocket) | New (Unity Netcode) |
|---------|----------------|---------------------|
| **Server** | Separate Node.js server | Unity Host (integrated) |
| **Transport** | TCP (WebSocket) | UDP (Unity Transport) |
| **Messages** | JSON strings | Serialized RPCs |
| **Connection** | WebSocket URL | IP address + port |
| **Room Codes** | Server-managed | Client-side only (for UX) |
| **State Sync** | Manual JSON parsing | Automatic via RPCs |
| **Dependencies** | Node.js + npm | Unity packages only |

---

## Next Steps

1. **Open Unity and verify package installation** (Window > Package Manager > check for "Netcode for GameObjects")
2. **Configure NetworkManager GameObject** in LobbyScreen and other networked scenes
3. **Test host/client connection** with two Unity Editor instances or build to device
4. **Update mobile client connection code** with actual host IP for LAN testing
5. **Consider Unity Relay** for production deployment (internet play without port forwarding)

---

## Additional Resources

- [Unity Netcode Documentation](https://docs-multiplayer.unity3d.com/)
- [Unity Transport Package](https://docs-multiplayer.unity3d.com/transport/current/about/)
- [Unity Relay Service](https://unity.com/products/relay) - For internet play without port forwarding
- [Netcode for GameObjects GitHub](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
