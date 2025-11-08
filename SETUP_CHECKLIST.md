# Quick Setup Checklist for Spatial Alignment

## Files to Copy to RemoteXR_Client:
- [ ] `SpatialAlignmentManager.cs`
- [ ] `AlignmentCalibrationTool.cs`
- [ ] `NetworkedPlayer.cs` (updated version)
- [ ] `RemoteClient.cs`
- [ ] `PhotonDebugUI.cs`
- [ ] The `LocalClientCube.prefab` from Resources folder

## LocalXR_Client (VR Headset) Setup:

### 1. Scene GameObjects:
- [ ] GameObject with `LocalClient.cs`
- [ ] GameObject with `SpatialAlignmentManager.cs` + `PhotonView`
- [ ] GameObject with `AlignmentCalibrationTool.cs`
- [ ] GameObject with `PhotonDebugUI.cs`
- [ ] Your scanned mesh in the scene

### 2. AlignmentManager Configuration:
- [ ] Mesh Reference Point: Drag your scanned mesh here
- [ ] Alignment Mode: Set to `AutoAlign`
- [ ] Show Debug Info: ✓ Checked
- [ ] PhotonView: Make sure it's added and SpatialAlignmentManager is observed

### 3. CalibrationTool Configuration:
- [ ] Scanned Mesh: Drag your scanned mesh here
- [ ] Alignment Manager: Drag the AlignmentManager GameObject
- [ ] Enable Keyboard Adjustment: ✓ Checked (for testing)

### 4. Mesh Position:
- [ ] Position: (0, 0, 0) or write down: _______________
- [ ] Rotation: (0, 0, 0) or write down: _______________
- [ ] Scale: (1, 1, 1) or write down: _______________

## RemoteXR_Client (MacBook) Setup:

### 1. Scene GameObjects:
- [ ] GameObject with `RemoteClient.cs`
- [ ] GameObject with `SpatialAlignmentManager.cs` + `PhotonView`
- [ ] GameObject with `AlignmentCalibrationTool.cs`
- [ ] GameObject with `PhotonDebugUI.cs`
- [ ] Your scanned mesh in the scene (SAME as VR project)

### 2. AlignmentManager Configuration:
- [ ] Mesh Reference Point: Drag your scanned mesh here
- [ ] Alignment Mode: Set to `AutoAlign`
- [ ] Show Debug Info: ✓ Checked

### 3. CalibrationTool Configuration:
- [ ] Scanned Mesh: Drag your scanned mesh here
- [ ] Alignment Manager: Drag the AlignmentManager GameObject
- [ ] Enable Keyboard Adjustment: ✓ Checked

### 4. Mesh Position (MUST MATCH VR PROJECT):
- [ ] Position: (0, 0, 0) or SAME as VR: _______________
- [ ] Rotation: (0, 0, 0) or SAME as VR: _______________
- [ ] Scale: (1, 1, 1) or SAME as VR: _______________

## Prefab Configuration (BOTH projects):

### LocalClientCube.prefab:
- [ ] `PhotonView` component
- [ ] `NetworkedPlayer` component
- [ ] PhotonView Observed Components includes `NetworkedPlayer`
- [ ] PhotonView Synchronization: "Unreliable On Change"
- [ ] Located in `Assets/Resources/` folder

## Testing Checklist:

### Pre-Test:
- [ ] Both projects have the SAME Photon App ID
- [ ] Both meshes are at the SAME position
- [ ] All scripts are compiled without errors

### Test Run 1 - Connection:
- [ ] Start LocalXR_Client (VR)
- [ ] Check console: "LocalClient connected to Master!"
- [ ] Check console: "LocalClient joined room: MeshVRRoom"
- [ ] Start RemoteXR_Client (MacBook)
- [ ] Check console: "RemoteClient connected to Master!"
- [ ] Check console: "RemoteClient joined room: MeshVRRoom"
- [ ] Both show: "Players in room: 2"

### Test Run 2 - Visual Sync:
- [ ] VR sees: 1 blue cube (self) + 1 red cube (MacBook user)
- [ ] MacBook sees: 1 blue cube (self) + 1 red cube (VR user)
- [ ] Debug UI shows "Total Objects: 2" on both

### Test Run 3 - Movement Sync:
- [ ] Move in VR (walk around) → Red cube moves on MacBook
- [ ] Use WASD on MacBook → Red cube moves in VR
- [ ] Movements look natural and aligned to the mesh

### Test Run 4 - Alignment Check:
- [ ] Both debug UIs show "Aligned: True"
- [ ] Check "Player Alignments" section
- [ ] Note the offset values: _______________
- [ ] If offset is large (>5 meters), recheck mesh positions

### Test Run 5 - Fine Tuning:
- [ ] If alignment is off, use manual adjustment
- [ ] SHIFT + Arrows to adjust position
- [ ] CTRL + Arrows to adjust rotation
- [ ] Press CTRL + S to save when correct
- [ ] Restart both and press CTRL + L to load saved alignment

## Troubleshooting Quick Fixes:

| Problem | Quick Fix |
|---------|-----------|
| Not seeing remote player | Check "Players in room" - should be 2 |
| Movement is backwards | CTRL + Arrow keys, rotate 180° |
| Position is way off | Check mesh positions match exactly |
| No alignment happening | Check PhotonView on AlignmentManager |
| Alignment keeps resetting | Save with CTRL + S after adjusting |

## Success Criteria:

✅ Both clients connect and join the same room
✅ Both see each other's player representations
✅ Movement syncs in real-time
✅ Positions are aligned relative to the scanned mesh
✅ Walking in VR = correct movement on MacBook screen
✅ WASD on MacBook = correct movement in VR view

## Performance Check:

- [ ] Frame rate is smooth on both systems
- [ ] No lag when moving
- [ ] Debug UI doesn't cause performance issues
- [ ] Network synchronization is smooth

## Production Ready:

Once everything works:
- [ ] Set "Show Debug Info" to false in both projects
- [ ] Disable "Enable Keyboard Adjustment" if not needed
- [ ] Remove or disable PhotonDebugUI for release builds
- [ ] Document your final alignment values
- [ ] Test on final hardware (actual VR headset + MacBook)

---

**Notes:**
Write down any alignment values that work well:
- Position Offset: _______________
- Rotation Offset: _______________
- Scale Multiplier: _______________

**Date Tested:** _______________
**Tested By:** _______________
**Results:** _______________
