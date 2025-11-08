# Troubleshooting: Not Seeing Remote Player

## Quick Checklist

### 1. Check PhotonView on Prefab
Open `LocalClientCube.prefab` and verify:
- ✅ **PhotonView component** is attached
- ✅ **Observe option** is set to "Unreliable On Change" or "Reliable Delta Compressed"
- ✅ **NetworkedPlayer script** is listed in "Observed Components" list
- ✅ The prefab is in the **Resources** folder (exact path: `Assets/Resources/LocalClientCube.prefab`)

### 2. Check Console Logs
When running BOTH clients, check for these messages:

**LocalClient should show:**
```
LocalClient connected to Master!
LocalClient joined room: MeshVRRoom
Players in room: 1  (or 2 if RemoteClient joined first)
New player joined: RemoteUser_XXXX  (when remote client joins)
```

**RemoteClient should show:**
```
RemoteClient connected to Master!
RemoteClient joined room: MeshVRRoom
Players in room: 2  (if LocalClient is already in)
New player joined: LocalUser_XXXX  (if LocalClient joins after)
```

### 3. Check Scene Hierarchy While Running
When BOTH clients are running, check the Hierarchy:
- You should see **2 cubes** total in each client
- One cube named "LocalPlayer_..." or "RemotePlayer_..." (your own)
- One cube for the other player (automatically instantiated by Photon)

### 4. Verify Photon Settings
**In both projects:**
- Window → Photon Unity Networking → Highlight Server Settings
- Check that **App Id PUN** is the SAME in both projects
- Check that both are using the same **Region** (or set to "Best Region")

### 5. Test Connection
**Quick test to verify Photon is working:**

Add this to your scene and run it in BOTH projects:
```csharp
void OnGUI()
{
    GUILayout.Label("Connected: " + PhotonNetwork.IsConnected);
    GUILayout.Label("In Room: " + PhotonNetwork.InRoom);
    GUILayout.Label("Players: " + PhotonNetwork.CurrentRoom?.PlayerCount);
    GUILayout.Label("Room Name: " + PhotonNetwork.CurrentRoom?.Name);
}
```

You should see:
- Connected: True
- In Room: True
- Players: 2 (when both are running)
- Room Name: MeshVRRoom

## Common Issues and Solutions

### Issue 1: "Only see 1 cube (my own)"
**Cause:** PhotonView not properly configured on prefab
**Solution:** 
1. Select the prefab in Resources folder
2. Add PhotonView component if missing
3. Add NetworkedPlayer to "Observed Components"
4. Set Synchronization to "Unreliable On Change"

### Issue 2: "Players in room: 1" even when both are running
**Cause:** Different Photon App IDs or different room names
**Solution:**
1. Verify SAME App ID in both projects
2. Check that both use "MeshVRRoom" (case-sensitive)
3. Make sure both are connected to the same region

### Issue 3: "Prefab instantiation error"
**Cause:** Prefab not in Resources folder or wrong name
**Solution:**
1. Move prefab to `Assets/Resources/` folder
2. Name must be exactly "LocalClientCube"
3. Both projects need this prefab in Resources

### Issue 4: "See cube but it doesn't move"
**Cause:** NetworkedPlayer not syncing properly
**Solution:**
1. Check PhotonView has NetworkedPlayer in "Observed Components"
2. Verify IPunObservable is implemented (it is in NetworkedPlayer.cs)
3. Check that OnPhotonSerializeView is being called

### Issue 5: "Cubes both appear at same location"
**Cause:** Spawn position not randomized or collision
**Solution:**
- The code already randomizes spawn (-2 to 2 range)
- Add more spacing: Change `Random.Range(-2f, 2f)` to `Random.Range(-5f, 5f)`

## Testing Steps

1. **Start LocalClient project**
   - Wait for "joined room" message
   - You should see YOUR blue cube

2. **Start RemoteClient project** (in another Unity instance or build)
   - Wait for "joined room" message
   - You should see YOUR blue cube

3. **Check LocalClient**
   - You should now see 2 cubes: Your blue one + Remote red one
   - Check Hierarchy for 2 GameObjects

4. **Move in RemoteClient** (WASD)
   - The red cube in LocalClient should move
   
5. **Move in LocalClient** (WASD)
   - The red cube in RemoteClient should move

## Still Not Working?

If you still only see 1 cube, please check:

1. **Scene Hierarchy Count:** How many objects are in the scene?
2. **Console Errors:** Any red errors about instantiation?
3. **PhotonView Owner:** Select the cube, check PhotonView component - what's the Owner ID?
4. **Network Stats:** Window → Photon Unity Networking → PUN Wizard → Show Current Stats

Let me know what you see in the console and hierarchy!
