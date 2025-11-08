# PhotonView Configuration Guide

## Understanding PhotonView Components

PhotonView has two main ways to synchronize data:

### 1. Continuous Synchronization (IPunObservable)
- Used when data needs to sync **every frame** (position, rotation, etc.)
- Requires component to implement `IPunObservable` interface
- Listed in "Observed Components"
- Example: `NetworkedPlayer.cs` continuously syncs position

### 2. RPC (Remote Procedure Calls)
- Used for **one-time events** (button clicks, state changes, etc.)
- No need for `IPunObservable`
- "Observed Components" can be **EMPTY**
- Example: `MeshAlignmentTool.cs` sends alignment when user saves

---

## MeshAlignmentTool Configuration

### ✅ Correct Setup:

**GameObject with MeshAlignmentTool:**
1. Add `MeshAlignmentTool` component
2. Add `PhotonView` component

**PhotonView Inspector:**
```
PhotonView Component:
├── View ID: (auto-assigned)
├── Observed Components: [EMPTY] ← This is CORRECT!
├── Synchronization: Off ← This is CORRECT!
└── Ownership: Fixed
```

### Why Observed Components is Empty:

The `MeshAlignmentTool` doesn't need continuous synchronization because:
- The mesh only moves when VR user adjusts it locally
- When user saves, it sends **one RPC** with the new position
- Remote users receive the RPC and update their mesh
- No need to sync every frame

### The RPC Method:

```csharp
// In MeshAlignmentTool.cs
[PunRPC]
void ReceiveMeshAlignment(float px, float py, float pz, ...)
{
    // Remote users receive this one-time update
    scannedMesh.position = new Vector3(px, py, pz);
    // ...
}
```

---

## Other Components' PhotonView Setup

### NetworkedPlayer (Continuous Sync):

**PhotonView Inspector:**
```
PhotonView Component:
├── Observed Components: [NetworkedPlayer] ← REQUIRED!
├── Synchronization: Unreliable On Change
└── Ownership: Takeover
```

This needs continuous sync because players move every frame.

### SpatialAlignmentManager (RPC Only):

**PhotonView Inspector:**
```
PhotonView Component:
├── Observed Components: [EMPTY] ← Correct for RPC-only
├── Synchronization: Off
└── Ownership: Fixed
```

Only sends alignment data once when joining room.

---

## Quick Reference

| Component | Observed Components | Why? |
|-----------|-------------------|------|
| **MeshAlignmentTool** | Empty | RPC only - sends mesh position when saved |
| **NetworkedPlayer** | NetworkedPlayer | Continuous - syncs player position every frame |
| **SpatialAlignmentManager** | Empty | RPC only - sends alignment once at room join |
| **LocalClient** | N/A | No PhotonView needed - doesn't sync directly |
| **RemoteClient** | N/A | No PhotonView needed - doesn't sync directly |

---

## Common Confusion

### ❓ "Why is Observed Components greyed out?"

**Answer:** Because `MeshAlignmentTool` doesn't implement `IPunObservable`. This is **intentional and correct**! It uses RPC instead.

### ❓ "Should I make it implement IPunObservable?"

**Answer:** No! That would sync the mesh position every frame to all clients, which is:
- Unnecessary (mesh only changes when user adjusts)
- Wasteful (network bandwidth)
- Wrong approach (remote users shouldn't control the mesh)

### ❓ "How does it work without continuous sync?"

**Answer:**
1. VR user moves mesh locally (no network traffic)
2. VR user presses ENTER to save
3. `SaveAlignment()` calls RPC to send position
4. Remote users receive RPC and update their mesh
5. Done! Only one network message sent

---

## Verification Checklist

### MeshAlignmentTool GameObject:

- [ ] Has `MeshAlignmentTool` component
- [ ] Has `PhotonView` component
- [ ] PhotonView "Observed Components" is **EMPTY** ✓
- [ ] PhotonView "Synchronization" is **Off** ✓
- [ ] "Scanned Mesh" field is assigned
- [ ] "Start In Alignment Mode" is checked (VR) or unchecked (MacBook)

### Testing:

- [ ] VR user can move mesh
- [ ] VR user saves alignment (ENTER)
- [ ] Console shows "Mesh alignment SAVED!"
- [ ] MacBook console shows "Received mesh alignment update"
- [ ] MacBook's mesh moves to match VR user's alignment
- [ ] No errors about PhotonView

---

## If You See Errors:

### "PhotonView with ID X has no observed components"

**This is just a warning, not an error!** It's safe to ignore.

If you want to suppress it, you can:
1. Set Synchronization to "Off" (should already be)
2. Or add a dummy component (not recommended)
3. Or just ignore it - it doesn't affect functionality

### "Could not find PhotonView"

**Solution:** Make sure PhotonView is on the **same GameObject** as MeshAlignmentTool.

### "RPC method not found"

**Solution:** 
- Check method has `[PunRPC]` attribute
- Method is in the component attached to GameObject with PhotonView
- Rebuild/recompile scripts

---

## Summary

✅ **MeshAlignmentTool with PhotonView:**
- Observed Components: EMPTY (correct!)
- Uses RPC for one-time updates
- No continuous synchronization needed
- Greyed out = expected behavior

✅ **NetworkedPlayer with PhotonView:**
- Observed Components: NetworkedPlayer (required!)
- Uses IPunObservable for continuous sync
- Syncs player position every frame

Both are correct for their use cases! Different components need different sync strategies.
