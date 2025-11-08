# Mesh Alignment System - Complete Setup Guide

## Overview

This system allows the VR user to manually align the pre-scanned mesh with the real world, then share that alignment with remote users so everyone sees the correct spatial representation.

## The Problem

When the VR application starts:
- The scanned mesh spawns at a default position
- It doesn't match the real-world environment
- The VR user needs to manually position it correctly
- Remote users need to see the same aligned mesh

## The Solution

1. **VR user enters alignment mode**
2. **Manually adjusts mesh to match real world**
3. **Saves the alignment**
4. **Alignment is shared with all remote users**
5. **Everyone now has accurate spatial reference**

---

## Setup Instructions

### LocalXR_Client (VR Headset)

#### Step 1: Add Components to Scene

```
Scene Hierarchy:
├── LocalClientManager
│   └── LocalClient.cs (updated)
│
├── AlignmentSystem
│   ├── SpatialAlignmentManager.cs
│   ├── PhotonView
│   └── MeshAlignmentTool.cs
│       └── PhotonView
│
├── VRControllerAlignment (optional but recommended)
│   └── VRMeshAlignmentController.cs
│
├── ScannedMesh
│   └── (Your pre-scanned room mesh)
│
└── DebugTools
    ├── PhotonDebugUI.cs
    ├── AlignmentCalibrationTool.cs
    └── AlignmentVisualizer.cs
```

#### Step 2: Configure LocalClient

In LocalClient.cs inspector:
- **Is VR Mode**: ✓ Checked
- **VR Camera**: Drag your VR camera/centerEyeAnchor here
- **Move Speed**: 5 (not used in VR mode)
- **Rotation Speed**: 100 (not used in VR mode)

#### Step 3: Configure MeshAlignmentTool

Create GameObject "MeshAlignmentTool", add components:
1. Add `MeshAlignmentTool` script
2. Add `PhotonView` component

**MeshAlignmentTool Settings:**
- **Scanned Mesh**: Drag your scanned mesh transform
- **Alignment Mode**: Unchecked (will toggle with 'M' key)
- **Start In Alignment Mode**: ✓ Checked (starts in alignment mode)
- **Move Speed**: 0.5
- **Rotate Speed**: 30
- **Scale Speed**: 0.1
- **Fine Adjust Multiplier**: 0.1
- **Use VR Controllers**: ✓ Checked
- **Use Keyboard**: ✓ Checked (fallback)
- **Show Grid**: ✓ Checked
- **Auto Save On Exit**: ✓ Checked

**PhotonView Settings:**
- Add `MeshAlignmentTool` to Observed Components
- Synchronization: Reliable Delta Compressed

#### Step 4: Configure VRMeshAlignmentController (Optional)

If using VR controllers for easier alignment:

Create GameObject "VRControllerAlignment", add:
- `VRMeshAlignmentController` script

**Settings:**
- **Alignment Tool**: Drag MeshAlignmentTool GameObject
- **Scanned Mesh**: Drag your scanned mesh
- **Dominant Controller**: RTouch (right hand)
- **Offhand Controller**: LTouch (left hand)
- **Move Speed**: 1
- **Rotate Speed**: 50
- **Scale Speed**: 0.5

#### Step 5: Configure SpatialAlignmentManager

Use existing AlignmentManager GameObject:
- **Mesh Reference Point**: Drag your scanned mesh
- **Alignment Mode**: AutoAlign

---

### RemoteXR_Client (MacBook)

#### Step 1: Copy Files

From LocalXR_Client, copy to RemoteXR_Client:
- `MeshAlignmentTool.cs`
- `VRMeshAlignmentController.cs` (optional, won't be used but needed for compatibility)

#### Step 2: Add to Scene

```
Scene Hierarchy:
├── RemoteClientManager
│   └── RemoteClient.cs
│
├── AlignmentSystem
│   ├── SpatialAlignmentManager.cs
│   ├── PhotonView
│   └── MeshAlignmentTool.cs
│       └── PhotonView
│
├── ScannedMesh (SAME mesh as VR project)
│   └── (Your pre-scanned room mesh)
│
└── DebugTools
    └── PhotonDebugUI.cs
```

#### Step 3: Configure MeshAlignmentTool

**Settings (MacBook):**
- **Scanned Mesh**: Drag your scanned mesh
- **Alignment Mode**: Unchecked
- **Start In Alignment Mode**: ✗ Unchecked (only VR user aligns)
- **Use VR Controllers**: ✗ Unchecked
- **Use Keyboard**: ✗ Unchecked (receives alignment from VR)
- **Show Grid**: Optional

**Important:** The MacBook version receives alignment updates automatically from the VR user.

---

## Usage Workflow

### For VR User (LocalXR_Client):

#### Initial Alignment:

1. **Put on VR headset and start application**
2. **You'll see the scanned mesh floating in space**
3. **Alignment mode is active by default** (shows UI overlay)

#### Option A: Keyboard Controls (Simple)

```
NUMPAD CONTROLS:
├── 8/2/4/6    → Move forward/back/left/right
├── 9/3        → Move up/down
└── CTRL + Numpad → Rotate

ALTERNATIVE:
├── Arrow Keys → Move
├── PageUp/Down → Height
└── CTRL + Arrows → Rotate

SCALE:
└── +/- Keys   → Scale up/down

MODES:
├── F          → Toggle fine adjust (10x slower, more precise)
├── M          → Toggle alignment mode on/off
└── ESC        → Exit alignment mode

SAVE:
├── ENTER      → Save alignment
├── CTRL + R   → Reset to original
└── CTRL + L   → Load saved alignment
```

#### Option B: VR Controller Controls (Recommended)

```
RIGHT CONTROLLER:
├── Thumbstick → Move/rotate (depending on mode)
├── Trigger    → Grab and drag mesh
├── Grip       → Save alignment
├── A/X Button → Cycle control modes
└── B/Y Button → Exit alignment mode

LEFT CONTROLLER:
├── Thumbstick → Height/roll
└── Trigger    → Hold with right trigger to rotate while grabbing

CONTROL MODES:
├── Move Mode   → Thumbstick moves mesh
├── Rotate Mode → Thumbstick rotates mesh
└── Scale Mode  → Thumbstick scales mesh
```

#### Alignment Process:

1. **Walk to a known landmark** (corner, door, table)
2. **Move the mesh** until it matches the real-world position
3. **Fine-tune rotation** to align walls/features
4. **Adjust scale if needed** (usually 1:1)
5. **Press ENTER or GRIP** to save
6. **Press M or B/Y** to exit alignment mode

#### Verification:

- Walk around the space
- Check if virtual mesh overlaps with real walls/furniture
- Adjust as needed
- Save again when perfect

### For Remote User (MacBook):

1. **Start application**
2. **Wait for VR user to join**
3. **Mesh automatically updates** when VR user saves alignment
4. **You'll see a message:** "Received mesh alignment update from VR user"
5. **The mesh moves to the aligned position**
6. **Done!** No manual work needed

---

## Testing the Full System

### Step-by-Step Test:

#### 1. Start VR Application (LocalXR_Client)

**Expected:**
- ✓ Connects to Photon
- ✓ Joins room "MeshVRRoom"
- ✓ Alignment mode UI appears
- ✓ Scanned mesh is visible
- ✓ Blue cube represents your position

#### 2. Align the Mesh (VR)

**Actions:**
- Use keyboard or controllers to move mesh
- Match it to real-world walls/features
- Save with ENTER or Grip button

**Expected:**
- ✓ Mesh moves smoothly
- ✓ UI shows current position/rotation
- ✓ "Mesh alignment SAVED!" message appears

#### 3. Start MacBook Application (RemoteXR_Client)

**Expected:**
- ✓ Connects to Photon
- ✓ Joins same room
- ✓ Sees VR user's blue cube as red cube
- ✓ Mesh is at aligned position

#### 4. Test Movement Sync (Both)

**VR User:** Walk around in physical space

**MacBook User:** Should see red cube move correctly relative to mesh

**MacBook User:** Use WASD to move

**VR User:** Should see red cube move correctly in aligned space

#### 5. Test Re-Alignment

**VR User:** Press M to enter alignment mode again

**Actions:**
- Adjust mesh position
- Save with ENTER

**Expected:**
- ✓ Mesh updates on MacBook automatically
- ✓ Console shows "Received mesh alignment update"

---

## Keyboard Controls Reference Card

### VR User - Mesh Alignment

```
╔═══════════════════════════════════════════╗
║     MESH ALIGNMENT - KEYBOARD CONTROLS    ║
╠═══════════════════════════════════════════╣
║                                           ║
║  MOVEMENT                                 ║
║  ├─ Numpad 8 / ↑  : Move Forward         ║
║  ├─ Numpad 2 / ↓  : Move Backward        ║
║  ├─ Numpad 4 / ←  : Move Left            ║
║  ├─ Numpad 6 / →  : Move Right           ║
║  ├─ Numpad 9 / PgUp : Move Up            ║
║  └─ Numpad 3 / PgDn : Move Down          ║
║                                           ║
║  ROTATION (Hold CTRL)                     ║
║  ├─ CTRL + Numpad 4/6 : Rotate Y (Yaw)   ║
║  ├─ CTRL + Numpad 8/2 : Rotate X (Pitch) ║
║  └─ CTRL + Numpad 7/9 : Rotate Z (Roll)  ║
║                                           ║
║  SCALE                                    ║
║  ├─ + / Numpad +  : Scale Up             ║
║  └─ - / Numpad -  : Scale Down           ║
║                                           ║
║  MODES                                    ║
║  ├─ F     : Toggle Fine Adjust (10x)     ║
║  ├─ M     : Toggle Alignment Mode        ║
║  └─ ESC   : Exit Alignment Mode          ║
║                                           ║
║  SAVE/LOAD                                ║
║  ├─ ENTER    : Save Alignment            ║
║  ├─ CTRL + R : Reset to Original         ║
║  └─ CTRL + L : Load Saved Alignment      ║
║                                           ║
╚═══════════════════════════════════════════╝
```

### VR User - Controller Controls

```
╔═══════════════════════════════════════════╗
║    MESH ALIGNMENT - CONTROLLER CONTROLS   ║
╠═══════════════════════════════════════════╣
║                                           ║
║  RIGHT CONTROLLER (Dominant Hand)         ║
║  ├─ Thumbstick  : Move/Rotate (by mode)  ║
║  ├─ Trigger     : Grab & Drag Mesh       ║
║  ├─ Grip        : Save Alignment         ║
║  ├─ A/X Button  : Change Control Mode    ║
║  └─ B/Y Button  : Exit Alignment Mode    ║
║                                           ║
║  LEFT CONTROLLER (Off Hand)               ║
║  ├─ Thumbstick  : Height / Roll          ║
║  └─ Trigger     : Rotate (with R trigger)║
║                                           ║
║  CONTROL MODES (A/X to cycle)             ║
║  ├─ MOVE   : Thumbstick moves mesh       ║
║  ├─ ROTATE : Thumbstick rotates mesh     ║
║  └─ SCALE  : Thumbstick scales mesh      ║
║                                           ║
╚═══════════════════════════════════════════╝
```

---

## Troubleshooting

### Issue: "Mesh doesn't move in VR"

**Causes:**
- Alignment mode not active
- Wrong mesh assigned

**Solutions:**
1. Check alignment mode UI is visible
2. Press M to toggle alignment mode
3. Verify "Scanned Mesh" is assigned in inspector

### Issue: "MacBook doesn't receive alignment"

**Causes:**
- PhotonView not configured
- Not in same room

**Solutions:**
1. Check PhotonView is on MeshAlignmentTool
2. Verify both in "MeshVRRoom"
3. Check console for "Received mesh alignment" message

### Issue: "Alignment keeps resetting"

**Causes:**
- Not saved properly
- Auto Save On Exit disabled

**Solutions:**
1. Press ENTER explicitly to save
2. Check "Auto Save On Exit" is enabled
3. Wait for "Mesh alignment SAVED!" message

### Issue: "VR controllers don't work"

**Causes:**
- VRMeshAlignmentController not added
- Wrong controller assigned

**Solutions:**
1. Add VRMeshAlignmentController to scene
2. Check controller assignments (RTouch/LTouch)
3. Verify OVR is in project

### Issue: "Movement stops working after alignment"

**Causes:**
- Still in alignment mode
- LocalClient checking alignment mode

**Solutions:**
1. Press M or ESC to exit alignment mode
2. Check alignment mode UI is hidden
3. Restart if needed

---

## Best Practices

### ✅ DO:

1. **Align at application start** before doing anything else
2. **Use physical landmarks** (corners, doors) for accuracy
3. **Walk around to verify** alignment from multiple angles
4. **Save frequently** during adjustment process
5. **Test with remote user** to confirm sync works

### ❌ DON'T:

1. **Don't skip alignment** - everything depends on it
2. **Don't move mesh during collaboration** - only at start
3. **Don't forget to save** - changes lost without saving
4. **Don't use fine adjust for large movements** - too slow
5. **Don't adjust from MacBook** - only VR user should align

---

## Advanced: Persistent Alignment

### Save to File (Optional)

If you want alignment to persist across sessions without PlayerPrefs:

```csharp
// Add to MeshAlignmentTool.cs
public void SaveToFile(string filename)
{
    AlignmentData data = new AlignmentData {
        position = scannedMesh.position,
        rotation = scannedMesh.rotation,
        scale = scannedMesh.localScale
    };
    
    string json = JsonUtility.ToJson(data);
    System.IO.File.WriteAllText(filename, json);
}
```

---

## Performance Notes

- **Alignment mode:** No performance impact
- **Network sync:** One-time RPC when saving (< 100 bytes)
- **VR tracking:** No additional overhead
- **Grid rendering:** Minimal (can disable after alignment)

---

## Success Checklist

- [ ] VR user can enter alignment mode
- [ ] Mesh moves with keyboard/controller input
- [ ] Alignment saves successfully
- [ ] MacBook receives alignment update
- [ ] Both users see same mesh position
- [ ] VR user movement syncs correctly
- [ ] MacBook user movement syncs correctly
- [ ] Alignment persists after restart

---

## Next Steps

Once alignment is working:
1. ✅ You have accurate spatial coordination
2. ✅ Add more collaborative features
3. ✅ Implement shared interactions
4. ✅ Add hand tracking sync
5. ✅ Build for deployment

**You now have a complete remote collaboration system!** 🎉
