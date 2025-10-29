# WebSocket NetworkManager Setup Guide

## What Was Done

### ✅ Completed
1. **Replaced Unity Netcode with WebSocket implementation** in `network_manager_script.cs`
2. **Added NativeWebSocket package** to `Packages/manifest.json`
3. **Created NetworkMessage structure** for all game events

### ⏳ To Do (10 minutes)

## Step 1: Install NativeWebSocket Package (Automatic - Unity will do this)

The package has been added to `manifest.json`. Unity will automatically download it when you open the project.

**If it fails:**
- Open Unity Package Manager
- Click "+" → "Add package from git URL"
- Enter: `https://github.com/endel/NativeWebSocket.git#upm`

## Step 2: Create Manager GameObjects in LoadingScreen (10 minutes)

Open the **LoadingScreen** scene in Unity and create the following:

### A) Create NetworkManager GameObject

1. **Create empty GameObject**: Right-click in Hierarchy → Create Empty
2. **Name it**: "NetworkManager"
3. **Add script**: Add Component → Scripts → RWMNetworkManager
4. **Configure**:
   - Server URL: `ws://localhost:3000` (change to your server URL for production)
   - The script will handle DontDestroyOnLoad automatically

### B) Verify GameManager Exists

1. Check if **GameManager** GameObject exists in LoadingScreen
2. If not, create it:
   - Right-click → Create Empty → Name: "GameManager"
   - Add Component → Scripts → GameManager

### C) Verify PlayerManager Exists

1. Check if **PlayerManager** GameObject exists in LoadingScreen
2. If not, create it:
   - Right-click → Create Empty → Name: "PlayerManager"
   - Add Component → Scripts → PlayerManager

## Step 3: Update Code References (Need to remove Unity Netcode references)

The following files still reference Unity Netcode and need updates:

### Files to Fix:
1. `lobby_screen_script.cs` - line 281, 283
2. `core_systems_bootstrapper.cs`
3. `game_manager_script.cs`
4. `player_auth_system.cs`

**Find all:** `NetworkManager.Singleton`
**Replace with:** Check `RWMNetworkManager.Instance.isConnected` instead

## Step 4: Server Setup (If you don't have it yet)

You need a Node.js WebSocket server. Here's a minimal example:

```javascript
// server.js
const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 3000 });

const rooms = {}; // { roomCode: { host: clientId, players: [], gameState: {} } }

wss.on('connection', (ws) => {
    console.log('Client connected');

    ws.on('message', (message) => {
        const data = JSON.parse(message);
        console.log('Received:', data.type);

        switch(data.type) {
            case 'create_room':
                rooms[data.roomCode] = {
                    host: data.playerId,
                    players: [],
                    gameState: {}
                };
                ws.roomCode = data.roomCode;
                ws.send(JSON.stringify({ type: 'room_created', roomCode: data.roomCode }));
                break;

            case 'join_room':
                if (rooms[data.roomCode]) {
                    ws.roomCode = data.roomCode;
                    ws.send(JSON.stringify({ type: 'room_joined', roomCode: data.roomCode }));
                    // Broadcast to all in room
                    broadcast(data.roomCode, { type: 'player_joined', playerId: data.playerId });
                } else {
                    ws.send(JSON.stringify({ type: 'error', error: 'Room not found' }));
                }
                break;

            case 'add_player':
            case 'submit_answer':
            case 'elimination_vote':
            case 'voting_vote':
            case 'bonus_vote':
            case 'score_update':
            case 'scene_change':
            case 'timer_sync':
            case 'round_sync':
                // Broadcast to all clients in the room
                broadcast(data.roomCode, data);
                break;
        }
    });

    ws.on('close', () => {
        console.log('Client disconnected');
        if (ws.roomCode && rooms[ws.roomCode]) {
            broadcast(ws.roomCode, { type: 'player_left', playerId: ws.playerId });
        }
    });
});

function broadcast(roomCode, message) {
    wss.clients.forEach(client => {
        if (client.roomCode === roomCode && client.readyState === WebSocket.OPEN) {
            client.send(JSON.stringify(message));
        }
    });
}

console.log('WebSocket server running on ws://localhost:3000');
```

**To run:**
```bash
npm install ws
node server.js
```

## Step 5: Update LobbyScreen Logic

In `lobby_screen_script.cs`, the `SetupDesktopHost()` method needs to:

1. Connect to WebSocket server first
2. Wait for connection
3. Then call StartHost()

**New flow:**
```csharp
void SetupDesktopHost()
{
    if (RWMNetworkManager.Instance != null)
    {
        if (!RWMNetworkManager.Instance.isConnected)
        {
            RWMNetworkManager.Instance.Connect();
            // Wait for connection, then call StartHost in OnRoomCreated event
            RWMNetworkManager.Instance.OnRoomCreated += OnHostRoomCreated;
        }
        else
        {
            RWMNetworkManager.Instance.StartHost();
        }
    }
}

void OnHostRoomCreated(string code)
{
    roomCode = code;
    if (roomCodeDisplay != null)
    {
        roomCodeDisplay.text = roomCode;
    }
}
```

## Testing

### Desktop Host:
1. Start Node.js server: `node server.js`
2. Run Desktop build
3. Should see room code on LobbyScreen
4. Check console logs for "[NetworkManager] WebSocket connected!"

### Mobile Client:
1. Open WebGL build on phone
2. Enter room code
3. Should join and see player list update

## Network Events Available

The NetworkManager provides these events you can subscribe to:

- `OnRoomCreated` - Host's room was created
- `OnRoomJoined` - Client joined a room
- `OnPlayerJoined` / `OnPlayerLeft` - Player connection changes
- `OnPlayerAdded` - New player with name/icon
- `OnGameStateChanged` - Game state sync
- `OnAnswerSubmitted` - Player submitted answer
- `OnVoteSubmitted` - Player voted
- `OnScoreUpdated` - Score changed
- `OnSceneChanged` - Scene transition
- `OnTimerSync` - Timer sync from host
- `OnConnectionError` - Connection failed

## Troubleshooting

### "NativeWebSocket namespace not found"
- Close Unity
- Delete `Library` folder
- Reopen Unity to force package refresh

### "WebSocket connected but no messages"
- Check server is running
- Check server URL matches in Unity Inspector
- Check browser/Unity console for errors

### Desktop can't connect
- Make sure server URL is correct
- Try `ws://localhost:3000` for local testing
- For LAN testing, use your computer's IP: `ws://192.168.1.X:3000`

### Mobile WebGL can't connect
- WebGL requires `wss://` (secure WebSocket) for HTTPS pages
- Use ngrok or similar to create secure tunnel: `ngrok http 3000`
- Update serverUrl to the ngrok URL

## Next Steps

1. Remove Unity Netcode package from `manifest.json` (line 13)
2. Update all code files to remove `NetworkManager.Singleton` references
3. Test Desktop → Mobile connection
4. Deploy server to production (Heroku, AWS, etc.)
