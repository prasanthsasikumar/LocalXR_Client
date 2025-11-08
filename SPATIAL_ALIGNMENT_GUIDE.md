# Spatial Alignment Setup Guide

## Overview
This system aligns coordinate systems between the VR headset (LocalXR_Client) and MacBook (RemoteXR_Client) so that movements in physical space are accurately represented in both systems.

## The Challenge
- **VR Headset**: Uses its own tracking space with the headset as origin
- **MacBook**: Has a different coordinate system
- **Goal**: When someone moves in VR, their representation moves correctly on the MacBook display (and vice versa)

## Solution Approaches

### Option 1: Auto-Align Based on Scanned Mesh (RECOMMENDED)
Both systems have the same pre-scanned mesh. Use the mesh as a common reference point.

**Setup:**
1. Import the same scanned mesh into both projects
2. Position the mesh at the SAME location in both scenes (e.g., at origin 0,0,0)
3. The system will automatically calculate alignment offsets

**Steps:**
1. **In BOTH projects:**
   - Place your scanned mesh in the scene
   - Create an empty GameObject, name it "AlignmentManager"
   - Add `SpatialAlignmentManager` component
   - Add `PhotonView` component to it
   - Drag your scanned mesh to the "Mesh Reference Point" field
   - Set "Alignment Mode" to "AutoAlign"

2. **Add Calibration Tool (optional):**
   - Create another empty GameObject
   - Add `AlignmentCalibrationTool` component
   - Drag the scanned mesh to "Scanned Mesh" field
   - Drag the AlignmentManager to "Alignment Manager" field

### Option 2: Manual Alignment
Fine-tune alignment using keyboard controls.

**Steps:**
1. Set up AlignmentManager with mode "ManualAlign"
2. Run both systems
3. Use keyboard to adjust:
   - **SHIFT + Arrow Keys**: Move position (X/Z axes)
   - **SHIFT + PgUp/PgDn**: Move position (Y axis)
   - **CTRL + Arrow Keys**: Rotate
   - **CTRL + R**: Reset alignment
   - **CTRL + S**: Save alignment
   - **CTRL + L**: Load saved alignment

### Option 3: Marker-Based Alignment (Most Accurate)
Use physical markers in the real world that exist in both coordinate systems.

**Steps:**
1. Place 3-5 physical markers in your space (e.g., colored cubes)
2. In Unity, create empty GameObjects at the corresponding positions
3. Assign these to the "Calibration Markers" array
4. Click "Align From Markers" button

### Option 4: Shared Origin
If both systems already share the same origin (rare but possible).

**Steps:**
1. Set "Alignment Mode" to "SharedOrigin"
2. No further setup needed

## Setup Instructions

### For VR Headset (LocalXR_Client):

1. **Scene Setup:**
   ```
   Scene Hierarchy:
   ├── LocalClientManager (with LocalClient script)
   ├── AlignmentManager (with SpatialAlignmentManager + PhotonView)
   ├── CalibrationTool (with AlignmentCalibrationTool)
   ├── ScannedMesh (your pre-scanned room mesh)
   └── Main Camera (VR camera rig)
   ```

2. **Configure AlignmentManager:**
   - Mesh Reference Point: Assign your ScannedMesh transform
   - Alignment Mode: AutoAlign (or ManualAlign for testing)
   - Show Debug Info: Check this for testing

3. **Configure CalibrationTool:**
   - Scanned Mesh: Assign your ScannedMesh transform
   - Alignment Manager: Assign the AlignmentManager GameObject
   - Enable Keyboard Adjustment: Check if you want manual control

4. **PhotonView on AlignmentManager:**
   - Must have PhotonView component
   - Observed Components: Add SpatialAlignmentManager
   - This allows sharing alignment data between clients

### For MacBook (RemoteXR_Client):

1. **Copy Files:**
   - Copy `SpatialAlignmentManager.cs`
   - Copy `AlignmentCalibrationTool.cs`

2. **Scene Setup:** (Same as VR project)
   ```
   Scene Hierarchy:
   ├── RemoteClientManager (with RemoteClient script)
   ├── AlignmentManager (with SpatialAlignmentManager + PhotonView)
   ├── CalibrationTool (with AlignmentCalibrationTool)
   ├── ScannedMesh (same mesh, same position as VR project)
   └── Main Camera
   ```

3. **CRITICAL: Mesh Positioning**
   - The scanned mesh MUST be at the SAME position in both projects
   - Recommended: Place at (0, 0, 0) in both
   - Same rotation: (0, 0, 0) in both
   - Same scale: (1, 1, 1) in both

## Testing Alignment

### Step 1: Visual Check
1. Run both systems
2. Check the debug UI on both screens
3. Look for "Spatial Alignment" section
4. Both should show "Aligned: True"

### Step 2: Movement Test
1. In VR, walk forward 1 meter
2. On MacBook, the VR user's cube should move forward proportionally
3. On MacBook, use WASD to move
4. In VR, the MacBook user's cube should move correctly

### Step 3: Mesh Reference Check
1. Both systems should show the same "Mesh Origin" position
2. If they differ, that's your alignment offset
3. Check debug UI: "Player Alignments" section shows the offset

## Common Alignment Issues

### Issue 1: "Movement is backwards or inverted"
**Cause:** Coordinate system handedness difference or rotation offset
**Solution:**
- Add 180° rotation offset
- In AlignmentCalibrationTool, use CTRL + Arrows to rotate
- Press CTRL + S to save when correct

### Issue 2: "Movement is scaled wrong (too fast/slow)"
**Cause:** Different units or scale between systems
**Solution:**
- In SpatialAlignmentManager, adjust "Scale Multiplier"
- Start with 1.0 and adjust up or down
- Values: 0.5 = half speed, 2.0 = double speed

### Issue 3: "Positions are offset by several meters"
**Cause:** Mesh not at same position in both scenes
**Solution:**
- Check mesh position in both projects
- Use Transform Inspector to verify exact positions
- Adjust "Position Offset" in manual mode to compensate

### Issue 4: "Alignment drifts over time"
**Cause:** One system's tracking is drifting
**Solution:**
- Press CTRL + R to recalibrate
- Use VR guardian system to prevent drift
- Consider adding periodic re-alignment

## Advanced: Creating Physical Alignment Markers

For the most accurate alignment:

1. **Create Physical Markers:**
   - Use 3-5 distinct objects (colored cubes, QR codes, etc.)
   - Place them at known positions in your room
   - Measure their positions precisely

2. **In VR Project:**
   - Create empty GameObjects at marker positions
   - Get positions from VR tracking while standing at markers
   - Assign to CalibrationMarkers array

3. **In MacBook Project:**
   - Create GameObjects at corresponding mesh positions
   - These should be at the same positions in the scanned mesh
   - Assign to CalibrationMarkers array

4. **Calibrate:**
   - Run both systems
   - Click "Align From Markers" in both
   - System calculates transformation between marker sets

## Debugging Tips

### Enable All Debug Info:
```csharp
// In scene
PhotonDebugUI - Shows network status
SpatialAlignmentManager - Shows alignment data
AlignmentCalibrationTool - Shows calibration controls
```

### Check Console for:
- "Sent alignment data: Origin at..."
- "Received alignment from Player X..."
- Any errors about missing references

### Visual Debugging:
- Alignment markers (yellow spheres) show where other players' origins are
- Blue cube = Your position
- Red cube = Other player's position
- The red cube should move naturally when the other player moves

## Best Practices

1. **Always start with the mesh at (0,0,0)** in both projects
2. **Test alignment before adding gameplay** - get this working first
3. **Save successful alignments** - Use CTRL+S after finding good values
4. **Document your alignment values** - Write down the offsets that work
5. **Consider room-scale differences** - VR might have guardian boundaries

## Performance Notes

- Alignment is calculated once when joining the room
- Updates are sent through Photon's network sync (no extra overhead)
- Interpolation is smooth and efficient
- No performance impact on VR or desktop systems

## Next Steps

Once alignment is working:
1. Add avatar models instead of cubes
2. Sync hand positions (VR controllers)
3. Add head orientation sync
4. Consider adding spatial anchors for persistence
5. Add room-scale boundaries visualization
