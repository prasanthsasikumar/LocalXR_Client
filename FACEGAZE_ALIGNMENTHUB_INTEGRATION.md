# PhotonFaceGazeTransmitter - AlignmentNetworkHub Integration

## Overview
`PhotonFaceGazeTransmitter` has been successfully integrated with `AlignmentNetworkHub` to remove the PhotonView dependency and centralize all networking through the hub.

## Architecture Changes

### Before (❌ Required PhotonView)
```csharp
public class PhotonFaceGazeTransmitter : MonoBehaviourPun, IPunObservable
{
    // Required PhotonView component
    // Used IPunObservable for streaming data
    // Each instance needed its own PhotonView
}
```

### After (✅ Uses AlignmentNetworkHub)
```csharp
public class PhotonFaceGazeTransmitter : MonoBehaviour
{
    // NO PhotonView required
    // Uses AlignmentNetworkHub events
    // Centralized networking
}
```

## Key Integration Features

### 1. AlignmentNetworkHub Extensions
Added new events and RPC methods to handle face/gaze data:

```csharp
// New events in AlignmentNetworkHub
public static event Action<int, Vector3[], bool> OnFaceLandmarksReceived;
public static event Action<int, Vector2, float, bool> OnGazeDataReceived;

// New broadcast methods
public static void BroadcastFaceLandmarks(Vector3[] landmarks)
public static void BroadcastGazeData(Vector2 gazePosition, float pupilSize)
```

### 2. Player-Based Data Management
- Each player has a unique `playerId` for identification
- Received data organized by player ID in Dictionary
- Supports multiple remote players simultaneously

### 3. Event-Driven Communication
- Subscribes to AlignmentNetworkHub events in `Start()`
- Unsubscribes in `OnDestroy()` for proper cleanup
- No direct Photon networking code

## Migration Steps

### Step 1: Update AlignmentNetworkHub
✅ **COMPLETED**: Extended with face/gaze events and RPC methods

### Step 2: Replace PhotonFaceGazeTransmitter
1. **Backup original**: `PhotonFaceGazeTransmitter.cs`
2. **Use new version**: `PhotonFaceGazeTransmitter_Refactored.cs`
3. **Remove PhotonView**: No longer needed on this component

### Step 3: Update Scene Setup
1. Remove PhotonView from `PhotonFaceGazeTransmitter` GameObjects
2. Set `playerId` manually or let it auto-assign
3. Keep LSL receiver assignments the same

## API Comparison

| Feature | Original | Refactored |
|---------|----------|------------|
| **Base Class** | `MonoBehaviourPun` | `MonoBehaviour` |
| **PhotonView** | Required | Not needed |
| **Data Flow** | `IPunObservable` streaming | AlignmentNetworkHub events |
| **Player Detection** | `photonView.IsMine` | `playerId` comparison |
| **Multi-Player** | One sender per PhotonView | Multiple players via Dictionary |

## Data Structures

### FaceGazeData Class
```csharp
public class FaceGazeData
{
    public Vector3[] faceLandmarks;
    public Vector2 gazePosition;
    public float pupilSize;
    public bool hasFaceData;
    public bool hasGazeData;
    public float lastUpdateTime;  // ← NEW: Age tracking
}
```

### Multi-Player Support
```csharp
// Access data by player ID
FaceGazeData playerData = transmitter.GetPlayerData(targetPlayerId);

// Get all active players
int[] activePlayerIds = transmitter.GetActivePlayerIds();

// Check if player has recent data
bool hasRecentData = transmitter.HasRecentDataFrom(playerId, 2f);
```

## Benefits

| Benefit | Impact |
|---------|--------|
| **No PhotonView Required** | Fewer component dependencies |
| **Centralized Networking** | All networking through AlignmentNetworkHub |
| **Multi-Player Support** | Handle multiple remote players easily |
| **Age Tracking** | Know when data was last received |
| **Event-Driven** | Decoupled, clean architecture |
| **Better Debugging** | Clear player identification and status |

## Usage Examples

### Sending Data (Remote Client)
```csharp
// Auto-detected if LSL receivers are present
// Face mesh and gaze data sent automatically via AlignmentNetworkHub
// No code changes needed - works like before
```

### Receiving Data (Local Client)
```csharp
// Get data for specific player
FaceGazeData remotePlayerData = transmitter.GetPlayerData(123);
if (remotePlayerData != null && remotePlayerData.hasFaceData)
{
    Vector3[] landmarks = remotePlayerData.faceLandmarks;
    // Use landmarks...
}

// Get gaze data
if (remotePlayerData.hasGazeData)
{
    Vector2 gazePos = remotePlayerData.gazePosition;
    float pupilSize = remotePlayerData.pupilSize;
    // Use gaze data...
}
```

### Monitor All Players
```csharp
foreach (int playerId in transmitter.GetActivePlayerIds())
{
    FaceGazeData data = transmitter.GetPlayerData(playerId);
    Debug.Log($"Player {playerId}: Face={data.hasFaceData}, Gaze={data.hasGazeData}");
}
```

## Debug Features

### Enhanced Debug UI
- Shows player ID and transmission status
- Lists all received players with data age
- Displays AlignmentNetworkHub connection status
- Real-time face/gaze transmission indicators

### Debug Logs
```
[FaceGazeTransmitter] Auto-assigned playerId: 1234
[FaceGazeTransmitter] Started with playerId=1234, Role: TRANSMITTER
[FaceGazeTransmitter] Sent face landmarks: 20 points
[FaceGazeTransmitter] Received gaze data from Player 5678: (0.3, 0.7)
```

## Testing Checklist

- [ ] AlignmentNetworkHub compiles without errors
- [ ] PhotonFaceGazeTransmitter_Refactored compiles without errors
- [ ] Remove PhotonView from transmitter GameObjects
- [ ] Test face mesh transmission (remote → local)
- [ ] Test gaze data transmission (remote → local) 
- [ ] Verify multi-player support (multiple remote clients)
- [ ] Check debug UI shows all players
- [ ] Confirm NetworkDiagnosticsUI detects transmitters

## Backwards Compatibility

### What's Compatible ✅
- LSL receiver assignments (no change)
- Transmission settings (interval, compression)
- Debug logging functionality
- Face mesh and gaze data formats

### What Changed ❌
- Requires AlignmentNetworkHub in scene
- No longer needs PhotonView component
- Player identification via playerId instead of PhotonView.IsMine
- Event-based instead of IPunObservable streaming

## Integration Complete ✅

Files created/modified:
1. ✅ `AlignmentNetworkHub.cs` - Extended with face/gaze events
2. ✅ `PhotonFaceGazeTransmitter_Refactored.cs` - New AlignmentNetworkHub-based version
3. ✅ This migration guide

Next steps:
1. Replace original PhotonFaceGazeTransmitter with refactored version
2. Remove PhotonView components from existing scene objects
3. Test transmission between remote and local clients