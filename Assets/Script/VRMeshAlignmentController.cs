using UnityEngine;
using Photon.Pun;

/// <summary>
/// VR Controller integration for mesh alignment
/// Works with Oculus/Meta Quest controllers
/// </summary>
public class VRMeshAlignmentController : MonoBehaviourPunCallbacks
{
    [Header("References")]
    public MeshAlignmentTool alignmentTool;
    public Transform scannedMesh;
    
    [Header("VR Controller Settings")]
    public OVRInput.Controller dominantController = OVRInput.Controller.RTouch;
    public OVRInput.Controller offhandController = OVRInput.Controller.LTouch;
    
    [Header("Movement Settings")]
    public float moveSpeed = 1f;
    public float rotateSpeed = 50f;
    public float scaleSpeed = 0.5f;
    
    [Header("Control Mode")]
    public ControlMode currentMode = ControlMode.Move;
    
    public enum ControlMode
    {
        Move,       // Move mesh with thumbstick
        Rotate,     // Rotate mesh
        Scale       // Scale mesh
    }
    
    private bool isGrabbing = false;
    private Vector3 grabStartPosition;
    private Vector3 meshStartPosition;
    private Quaternion meshStartRotation;
    private bool meshHidden = false;

    void Start()
    {
        if (alignmentTool == null)
        {
            alignmentTool = FindFirstObjectByType<MeshAlignmentTool>();
        }
        
        if (scannedMesh == null && alignmentTool != null)
        {
            scannedMesh = alignmentTool.scannedMesh;
        }
    }

    void Update()
    {
        if (!alignmentTool.alignmentMode || scannedMesh == null) return;
        
        HandleControllerInput();
    }

    void HandleControllerInput()
    {
        // Toggle alignment mode with Y/B button
        if (OVRInput.GetDown(OVRInput.Button.Two, dominantController))
        {
            alignmentTool.ToggleAlignmentMode();
        }
        
        // Toggle mesh visibility with A button (Button.One)
        if (OVRInput.GetDown(OVRInput.Button.One, dominantController))
        {
            ToggleMeshVisibility();
        }
        // Optionally keep mode cycling on left controller X button
        if (OVRInput.GetDown(OVRInput.Button.One, offhandController))
        {
            CycleControlMode();
        }
        
        // Save alignment with grip button
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, dominantController))
        {
            alignmentTool.SaveAlignment();
            // Haptic feedback
            OVRInput.SetControllerVibration(0.5f, 0.5f, dominantController);
        }
        
    // Get thumbstick input
    // Oculus convention: PrimaryThumbstick = Left, SecondaryThumbstick = Right (independent of controller enum)
    // We rely on axis separation to avoid mirrored values.
    Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick); // Dominant / right hand XZ translate
    Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);    // Offhand / left hand Y translate + yaw rotate request
        
        switch (currentMode)
        {
            case ControlMode.Move:
                HandleMovement(rightStick, leftStick);
                break;
            case ControlMode.Rotate:
                HandleRotation(rightStick, leftStick);
                break;
            case ControlMode.Scale:
                HandleScaling(rightStick);
                break;
        }
        
        // Grab and drag with trigger
        HandleGrabMode();
    }

    void HandleMovement(Vector2 rightStick, Vector2 leftStick)
    {
        // Right stick still moves X/Z
        Vector3 movement = Vector3.zero;
        if (Mathf.Abs(rightStick.x) > 0.1f || Mathf.Abs(rightStick.y) > 0.1f)
        {
            movement.x = rightStick.x;
            movement.z = rightStick.y;
        }

        // Left stick: Up/Down moves Y; Left/Right rotates around Y instead of translating
        float vertical = Mathf.Abs(leftStick.y) > 0.1f ? leftStick.y : 0f;
        movement.y = vertical;

        if (movement != Vector3.zero)
        {
            scannedMesh.position += movement * moveSpeed * Time.deltaTime;
        }

        // Yaw rotation from left stick X
        if (Mathf.Abs(leftStick.x) > 0.1f)
        {
            float yaw = leftStick.x * rotateSpeed * Time.deltaTime;
            scannedMesh.Rotate(0f, yaw, 0f, Space.World);
        }
    }

    void HandleRotation(Vector2 rightStick, Vector2 leftStick)
    {
        // Preserve original rotate semantics for right stick (pitch + yaw)
        if (rightStick.magnitude > 0.1f)
        {
            Vector3 rotation = new Vector3(-rightStick.y, rightStick.x, 0);
            scannedMesh.Rotate(rotation * rotateSpeed * Time.deltaTime, Space.World);
        }

        // Left stick X still performs yaw rotation (to stay consistent with move mode behavior); Y ignored here
        if (Mathf.Abs(leftStick.x) > 0.1f)
        {
            float yaw = leftStick.x * rotateSpeed * Time.deltaTime;
            scannedMesh.Rotate(0, yaw, 0, Space.World);
        }
    }

    void HandleScaling(Vector2 thumbstick)
    {
        if (Mathf.Abs(thumbstick.y) > 0.1f)
        {
            float scaleChange = thumbstick.y * scaleSpeed * Time.deltaTime;
            scannedMesh.localScale += Vector3.one * scaleChange;
            
            // Prevent negative scale
            scannedMesh.localScale = Vector3.Max(scannedMesh.localScale, Vector3.one * 0.01f);
        }
    }

    void HandleGrabMode()
    {
        // Press trigger to grab and drag
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, dominantController))
        {
            isGrabbing = true;
            grabStartPosition = OVRInput.GetLocalControllerPosition(dominantController);
            meshStartPosition = scannedMesh.position;
            meshStartRotation = scannedMesh.rotation;
            
            // Haptic feedback
            OVRInput.SetControllerVibration(0.3f, 0.3f, dominantController);
        }
        
        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, dominantController))
        {
            isGrabbing = false;
            OVRInput.SetControllerVibration(0, 0, dominantController);
        }
        
        if (isGrabbing)
        {
            Vector3 currentControllerPos = OVRInput.GetLocalControllerPosition(dominantController);
            Vector3 delta = currentControllerPos - grabStartPosition;
            
            scannedMesh.position = meshStartPosition + delta;
            
            // If both triggers are pressed, also rotate
            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, offhandController))
            {
                Quaternion controllerRotation = OVRInput.GetLocalControllerRotation(dominantController);
                scannedMesh.rotation = controllerRotation;
            }
        }
    }

    void CycleControlMode()
    {
        currentMode = (ControlMode)(((int)currentMode + 1) % 3);
        Debug.Log($"<color=cyan>Control Mode: {currentMode}</color>");
        
        // Haptic feedback pattern based on mode
        float frequency = 0f;
        float amplitude = 0f;
        
        switch (currentMode)
        {
            case ControlMode.Move:
                frequency = 0.3f;
                amplitude = 0.3f;
                break;
            case ControlMode.Rotate:
                frequency = 0.5f;
                amplitude = 0.5f;
                break;
            case ControlMode.Scale:
                frequency = 0.7f;
                amplitude = 0.7f;
                break;
        }
        
        OVRInput.SetControllerVibration(frequency, amplitude, dominantController);
        
        // Stop vibration after short delay
        Invoke(nameof(StopVibration), 0.2f);
    }

    void StopVibration()
    {
        OVRInput.SetControllerVibration(0, 0, dominantController);
    }

    void ToggleMeshVisibility()
    {
        if (scannedMesh == null) return;

        meshHidden = !meshHidden;
        // Toggle all renderers under the scanned mesh
        var renderers = scannedMesh.GetComponentsInChildren<Renderer>(includeInactive: true);
        foreach (var r in renderers)
        {
            r.enabled = !meshHidden;
        }

        // Optionally disable colliders too
        var colliders = scannedMesh.GetComponentsInChildren<Collider>(includeInactive: true);
        foreach (var c in colliders)
        {
            c.enabled = !meshHidden;
        }

        // Light haptic to confirm
        OVRInput.SetControllerVibration(0.2f, 0.4f, dominantController);
        Invoke(nameof(StopVibration), 0.1f);
    }

    void OnGUI()
    {
        if (!alignmentTool.alignmentMode) return;
        
        // VR-specific UI overlay
        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 250));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== VR MESH ALIGNMENT ===", GUI.skin.box);
        GUILayout.Label($"<b>Mode:</b> {currentMode}");
        
        GUILayout.Space(10);
        GUILayout.Label("<b>Controls:</b>");
        
        switch (currentMode)
        {
            case ControlMode.Move:
                GUILayout.Label("Right Stick: Move X/Z");
                GUILayout.Label("Left Stick Up/Down: Move Y");
                GUILayout.Label("Left Stick Left/Right: Yaw Rotate");
                break;
            case ControlMode.Rotate:
                GUILayout.Label("Right Stick: Pitch/Yaw");
                GUILayout.Label("Left Stick Left/Right: Additional Yaw");
                break;
            case ControlMode.Scale:
                GUILayout.Label("Right Stick: Scale");
                break;
        }
        
        GUILayout.Space(10);
        GUILayout.Label("<b>Buttons:</b>");
    GUILayout.Label("A (Right): Hide/Show Mesh");
    GUILayout.Label("X (Left): Change Mode");
        GUILayout.Label("Grip: Save");
        GUILayout.Label("Trigger: Grab & Move");
        GUILayout.Label("B/Y: Exit");

    GUILayout.Space(6);
    GUILayout.Label($"Mesh Visible: {!meshHidden}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
