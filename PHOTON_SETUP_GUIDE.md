# Photon Multiplayer Setup Guide

## Overview
This setup allows LocalXR_Client and RemoteXR_Client to see each other's player representations and sync movement in real-time using Photon.

## Files Created/Updated

### LocalXR_Client Project:
1. **LocalClient.cs** - Updated with WASD controls and network synchronization
2. **NetworkedPlayer.cs** - NEW - Handles position/rotation syncing over network
3. **RemoteClient.cs** - Code for the RemoteXR_Client project

### What's Different:
- **Local players appear BLUE**
- **Remote players appear RED**
- Each player has a name tag showing their nickname
- WASD keys for movement (forward/backward/left/right)
- Q/E keys for rotation (left/right)

## Setup Instructions

### For LocalXR_Client Project:

1. **Update the LocalClientCube Prefab:**
   - Open `Assets/Resources/LocalClientCube.prefab`
   - Add the `NetworkedPlayer` component to it
   - Add a `PhotonView` component if not already present
   - In PhotonView settings:
     - Set "Observed Components" to include `NetworkedPlayer`
     - Set "Synchronization" to "Unreliable On Change"
     - Observation Option: "Unreliable On Change"
   - Save the prefab

2. **Scene Setup:**
   - Create an empty GameObject in your scene
   - Add the `LocalClient` component to it
   - Remove any references to the old `cubePrefab` field (no longer needed)

3. **Photon Settings:**
   - Make sure your Photon AppId is configured in:
     - Window → Photon Unity Networking → PUN Wizard
   - Both projects must use the SAME Photon AppId

### For RemoteXR_Client Project:

1. **Copy Files:**
   - Copy `RemoteClient.cs` to your RemoteXR_Client project's Assets folder
   - Copy `NetworkedPlayer.cs` to your RemoteXR_Client project's Assets folder

2. **Copy Prefab:**
   - Copy the entire `Assets/Resources/LocalClientCube.prefab` to the RemoteXR_Client project
   - Place it in `Assets/Resources/` folder (create if doesn't exist)
   - Make sure it has the same setup:
     - PhotonView component
     - NetworkedPlayer component
     - Both components properly configured

3. **Scene Setup:**
   - Create an empty GameObject in your scene
   - Add the `RemoteClient` component to it

4. **Photon Settings:**
   - Configure with the SAME Photon AppId as LocalXR_Client
   - Window → Photon Unity Networking → PUN Wizard

## Testing

1. **Build and Run:**
   - Run LocalXR_Client in Unity Editor or build it
   - Run RemoteXR_Client in Unity Editor or build it (on different machine or as standalone build)

2. **Expected Behavior:**
   - Both clients should auto-connect and join the same room "MeshVRRoom"
   - Each player will see their own cube (BLUE) and the other player's cube (RED)
   - Name tags will appear above each cube showing the player nickname
   - Moving with WASD in one client will be visible in the other client
   - Rotating with Q/E will sync across clients

3. **Controls:**
   - **W** - Move forward
   - **S** - Move backward
   - **A** - Strafe left
   - **D** - Strafe right
   - **Q** - Rotate left
   - **E** - Rotate right

## Troubleshooting

### Players not seeing each other:
- Check that both projects use the same Photon AppId
- Check Console logs for "joined room" messages
- Verify both are connecting to the same region

### Movement not syncing:
- Ensure PhotonView component is on the prefab
- Verify NetworkedPlayer is listed in PhotonView's "Observed Components"
- Check that prefab is in Resources folder with exact name "LocalClientCube"

### Prefab instantiation fails:
- Prefab MUST be in a "Resources" folder
- Prefab MUST have PhotonView component
- Use exact name in PhotonNetwork.Instantiate() call

## Network Synchronization Details

- **Update Rate:** 10 times per second (Photon default)
- **Interpolation:** Smooth lerping for remote players
- **Ownership:** Each client owns their own player representation
- **Room:** Both clients join "MeshVRRoom" automatically

## Next Steps

To enhance this setup, you can:
1. Add vertical movement (jump/crouch)
2. Add avatars instead of simple cubes
3. Sync additional data (animations, states, etc.)
4. Add voice chat using Photon Voice
5. Implement room lists and matchmaking
6. Add spawn points and game logic

## Important Notes

- Both projects need Photon Unity Networking (PUN 2) installed
- The prefab must be identical in both projects
- Make sure network synchronization is smooth by testing on different networks
- Consider bandwidth optimization for VR/XR applications
