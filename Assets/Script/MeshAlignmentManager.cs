using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Manages spatial mesh alignment for VR controllers.
/// Allows interactive adjustment of mesh position, rotation, and scale using VR controllers.
/// Synchronizes alignment across network via Photon.
/// </summary>
public class MeshAlignmentManager : MonoBehaviourPunCallbacks
{
    [Header("Mesh Reference")]
    public Transform meshToAlign;
    
    [Header("Alignment Controls")]
    public bool alignmentModeEnabled = false;
    
    [Header("VR Controller Settings")]
    [Tooltip("Which controller to use for mesh manipulation (Left or Right)")]
    public XRNode controllerNode = XRNode.RightHand;
    [Tooltip("Speed multiplier for controller-based adjustments")]
    public float vrAdjustSpeed = 1f;
    [Tooltip("Grip button threshold for enabling adjustment mode")]
    public float gripThreshold = 0.5f;
    
    [Header("Adjustment Speeds")]
    public float moveSpeed = 0.5f;
    public float rotateSpeed = 30f;
    public float scaleSpeed = 0.1f;
    
    [Header("Persistence")]
    public string saveKey = "MeshAlignment_";
    [Tooltip("If true, apply saved alignment automatically on Start. Disable to prevent PlayerPrefs from moving the mesh at launch.")]
    public bool autoLoadAlignment = false;
    [Tooltip("If true, Save/Load will use Unity PlayerPrefs. Disable to avoid reading/writing local preferences.(Default: false)")]
    public bool persistToPlayerPrefs = false;
    [Header("Networking")]
    [Tooltip("If true, send mesh transform updates in real time to other Photon clients while adjusting.")]
    public bool broadcastRealtime = false;
    [Tooltip("Minimum time (seconds) between real-time broadcasts.")]
    public float broadcastInterval = 0.1f;
    [Tooltip("Position/scale change threshold (meters) that triggers a broadcast when exceeded.")]
    public float positionThreshold = 0.001f;
    [Tooltip("Rotation change threshold (degrees) that triggers a broadcast when exceeded.")]
    public float rotationThreshold = 0.5f;

    // internal tracking for throttled broadcasts
    float lastBroadcastTime = 0f;
    Vector3 lastBroadcastPos = Vector3.zero;
    Quaternion lastBroadcastRot = Quaternion.identity;
    Vector3 lastBroadcastScale = Vector3.one;
    
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private Vector3 savedScale;

    void Start()
    {
        if (meshToAlign == null)
        {
            Debug.LogWarning("MeshAlignmentManager: No mesh assigned. Searching for mesh in scene...");
            // Try to find a mesh in the scene
            GameObject meshObj = GameObject.Find("ScannedRoom") ?? GameObject.Find("Mesh");
            if (meshObj != null)
                meshToAlign = meshObj.transform;
        }
        
        if (meshToAlign != null && autoLoadAlignment)
            LoadAlignment();
    }

    void Update()
    {
        if (meshToAlign == null)
            return;

        HandleVRControllerInput();
    }

    void HandleVRControllerInput()
    {
        // VR controller input handling for left/right split controls
        float dt = Time.deltaTime;

        // Get both input devices
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!left.isValid && !right.isValid)
        {
            Debug.LogWarning("VR Controllers not found or not valid. Make sure XR is initialized.");
            return;
        }

        // Check grip buttons on both controllers to enable/disable alignment mode
        bool leftGripPressed = false;
        bool rightGripPressed = false;
        if (left.isValid && left.TryGetFeatureValue(CommonUsages.grip, out float lg))
            leftGripPressed = (lg > gripThreshold);
        if (right.isValid && right.TryGetFeatureValue(CommonUsages.grip, out float rg))
            rightGripPressed = (rg > gripThreshold);

        bool wasEnabled = alignmentModeEnabled;
        // Enable alignment if either controller grip is pressed
        alignmentModeEnabled = leftGripPressed || rightGripPressed;

        if (alignmentModeEnabled && !wasEnabled)
            Debug.Log("<color=green>VR Mesh alignment mode: ON (Grip pressed)</color>");
        else if (!alignmentModeEnabled && wasEnabled)
            Debug.Log("<color=yellow>VR Mesh alignment mode: OFF (Grip released)</color>");

        if (!alignmentModeEnabled)
            return;

        // --- Right controller: Movement on XZ plane (Y is height) ---
        if (right.isValid)
        {
            if (right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightAxis))
            {
                if (rightAxis.magnitude > 0.05f)
                {
                    // Move relative to player's facing direction (camera forward/right)
                    Transform head = Camera.main != null ? Camera.main.transform : null;
                    Vector3 camForward = head != null ? head.forward : Vector3.forward;
                    Vector3 camRight = head != null ? head.right : Vector3.right;
                    camForward.y = 0f;
                    camRight.y = 0f;
                    camForward.Normalize();
                    camRight.Normalize();

                    Vector3 movement = camRight * rightAxis.x + camForward * rightAxis.y;
                    meshToAlign.position += movement * moveSpeed * vrAdjustSpeed * dt;
                }
            }

            // Vertical movement (height) using trigger on right controller
            if (right.TryGetFeatureValue(CommonUsages.trigger, out float rightTrigger))
            {
                if (rightTrigger > 0.1f)
                {
                    meshToAlign.position += Vector3.up * rightTrigger * moveSpeed * vrAdjustSpeed * dt;
                }
            }

            // Secondary axis on right can also move up/down if used
            if (right.TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 rightSecondAxis))
            {
                if (Mathf.Abs(rightSecondAxis.y) > 0.1f)
                    meshToAlign.position += Vector3.up * rightSecondAxis.y * moveSpeed * vrAdjustSpeed * dt;

                // Use secondary thumbstick X on right to rotate around Y if needed
                if (Mathf.Abs(rightSecondAxis.x) > 0.1f)
                    meshToAlign.Rotate(Vector3.up, rightSecondAxis.x * rotateSpeed * vrAdjustSpeed * dt, Space.World);
            }
        }

        // --- Left controller: Rotation around Y axis ---
        if (left.isValid)
        {
            if (left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftAxis))
            {
                // Only X axis of left thumbstick controls yaw (rotation around Y)
                if (Mathf.Abs(leftAxis.x) > 0.1f)
                {
                    meshToAlign.Rotate(Vector3.up, leftAxis.x * rotateSpeed * vrAdjustSpeed * dt, Space.World);
                }
            }
        }
        
        // === SAVE/LOAD ===
        // Use menu button on either controller to save
        bool menuPressed = false;
        if ((right.isValid && right.TryGetFeatureValue(CommonUsages.menuButton, out menuPressed) && menuPressed) ||
            (left.isValid && left.TryGetFeatureValue(CommonUsages.menuButton, out menuPressed) && menuPressed))
        {
            SaveAlignment();
            BroadcastAlignment();
            Debug.Log("<color=green>VR: Alignment saved and broadcasted!</color>");
        }

        // Realtime broadcasting while actively aligning
        if (broadcastRealtime && alignmentModeEnabled)
        {
            float now = Time.time;
            Vector3 curPos = meshToAlign.position;
            Quaternion curRot = meshToAlign.rotation;
            Vector3 curScale = meshToAlign.localScale;

            bool timeOk = (now - lastBroadcastTime) >= broadcastInterval;
            bool posChanged = Vector3.Distance(curPos, lastBroadcastPos) > positionThreshold;
            bool rotChanged = Quaternion.Angle(curRot, lastBroadcastRot) > rotationThreshold;
            bool scaleChanged = Vector3.Distance(curScale, lastBroadcastScale) > positionThreshold;

            if (timeOk && (posChanged || rotChanged || scaleChanged))
            {
                if (PhotonNetwork.InRoom)
                {
                    // Send alignment via RaiseEvent (same as BroadcastAlignment method)
                    const byte alignmentEventCode = 101;
                    object[] content = new object[] {
                        curPos.x, curPos.y, curPos.z,
                        curRot.x, curRot.y, curRot.z, curRot.w,
                        curScale.x, curScale.y, curScale.z
                    };

                    var options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    PhotonNetwork.RaiseEvent(alignmentEventCode, content, options, SendOptions.SendReliable);

                    lastBroadcastTime = now;
                    lastBroadcastPos = curPos;
                    lastBroadcastRot = curRot;
                    lastBroadcastScale = curScale;
                    
                    Debug.Log("<color=cyan>VR: Broadcasting realtime mesh alignment update...</color>");
                }
            }
        }
    }

    public void SaveAlignment()
    {
        if (meshToAlign == null)
            return;

        savedPosition = meshToAlign.position;
        savedRotation = meshToAlign.rotation;
        savedScale = meshToAlign.localScale;

        if (persistToPlayerPrefs)
        {
            PlayerPrefs.SetFloat(saveKey + "PosX", savedPosition.x);
            PlayerPrefs.SetFloat(saveKey + "PosY", savedPosition.y);
            PlayerPrefs.SetFloat(saveKey + "PosZ", savedPosition.z);
            
            PlayerPrefs.SetFloat(saveKey + "RotX", savedRotation.x);
            PlayerPrefs.SetFloat(saveKey + "RotY", savedRotation.y);
            PlayerPrefs.SetFloat(saveKey + "RotZ", savedRotation.z);
            PlayerPrefs.SetFloat(saveKey + "RotW", savedRotation.w);
            
            PlayerPrefs.SetFloat(saveKey + "ScaleX", savedScale.x);
            PlayerPrefs.SetFloat(saveKey + "ScaleY", savedScale.y);
            PlayerPrefs.SetFloat(saveKey + "ScaleZ", savedScale.z);
            
            PlayerPrefs.Save();
        }
        
        Debug.Log($"<color=green>✓ Mesh alignment saved!</color>\nPos: {savedPosition}\nRot: {savedRotation.eulerAngles}\nScale: {savedScale}");
    }

    public void LoadAlignment()
    {
        if (meshToAlign == null)
            return;
        if (!persistToPlayerPrefs)
        {
            Debug.Log("PlayerPrefs persistence is disabled; LoadAlignment skipped.");
            return;
        }

        if (PlayerPrefs.HasKey(saveKey + "PosX"))
        {
            savedPosition = new Vector3(
                PlayerPrefs.GetFloat(saveKey + "PosX"),
                PlayerPrefs.GetFloat(saveKey + "PosY"),
                PlayerPrefs.GetFloat(saveKey + "PosZ")
            );
            
            savedRotation = new Quaternion(
                PlayerPrefs.GetFloat(saveKey + "RotX"),
                PlayerPrefs.GetFloat(saveKey + "RotY"),
                PlayerPrefs.GetFloat(saveKey + "RotZ"),
                PlayerPrefs.GetFloat(saveKey + "RotW")
            );
            
            savedScale = new Vector3(
                PlayerPrefs.GetFloat(saveKey + "ScaleX"),
                PlayerPrefs.GetFloat(saveKey + "ScaleY"),
                PlayerPrefs.GetFloat(saveKey + "ScaleZ")
            );
            
            meshToAlign.position = savedPosition;
            meshToAlign.rotation = savedRotation;
            meshToAlign.localScale = savedScale;
            
            Debug.Log($"<color=green>✓ Mesh alignment loaded!</color>");
        }
    }

    /// <summary>
    /// Deletes saved PlayerPrefs keys for this mesh alignment (if persistence is enabled).
    /// </summary>
    public void ClearSavedAlignment()
    {
        //Because if using PlayerPrefs, implementing calibration become complex 
        if (!persistToPlayerPrefs)
        {
            Debug.Log("PlayerPrefs persistence is disabled; nothing to clear.");
            return;
        }

        PlayerPrefs.DeleteKey(saveKey + "PosX");
        PlayerPrefs.DeleteKey(saveKey + "PosY");
        PlayerPrefs.DeleteKey(saveKey + "PosZ");

        PlayerPrefs.DeleteKey(saveKey + "RotX");
        PlayerPrefs.DeleteKey(saveKey + "RotY");
        PlayerPrefs.DeleteKey(saveKey + "RotZ");
        PlayerPrefs.DeleteKey(saveKey + "RotW");

        PlayerPrefs.DeleteKey(saveKey + "ScaleX");
        PlayerPrefs.DeleteKey(saveKey + "ScaleY");
        PlayerPrefs.DeleteKey(saveKey + "ScaleZ");

        PlayerPrefs.Save();
        Debug.Log("Cleared saved mesh alignment PlayerPrefs.");
    }

    void BroadcastAlignment()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("Cannot broadcast alignment: Not in a Photon room.");
            return;
        }
        
        Debug.Log("<color=cyan>Broadcasting mesh alignment to other users...</color>");
        
        // Send alignment via Photon RaiseEvent so any client can receive it
        // without requiring a matching PhotonView instance
        const byte alignmentEventCode = 101;
        object[] content = new object[] {
            savedPosition.x, savedPosition.y, savedPosition.z,
            savedRotation.x, savedRotation.y, savedRotation.z, savedRotation.w,
            savedScale.x, savedScale.y, savedScale.z
        };

        var options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(alignmentEventCode, content, options, SendOptions.SendReliable);
        
        Debug.Log($"<color=green>Sent alignment event: Pos({savedPosition}), Rot({savedRotation.eulerAngles}), Scale({savedScale})</color>");
    }

    // Note: ReceiveAlignment is now handled by NetworkedDataReceiver via Photon events
    // (removed local RPC handler to avoid confusion)

    void OnGUI()
    {
        if (!alignmentModeEnabled || meshToAlign == null)
            return;

        GUILayout.BeginArea(new Rect(10, Screen.height - 280, 400, 270));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== VR MESH ALIGNMENT MODE ===", GUI.skin.box);
        GUILayout.Space(5);
        
        GUILayout.Label("VR Controller Mode Active");
        GUILayout.Space(3);
        GUILayout.Label("GRIP: Hold to enable alignment");
        GUILayout.Label("THUMBSTICK: Move mesh (XZ plane)");
        GUILayout.Label("TRIGGER: Move mesh up");
        GUILayout.Label("2ND THUMBSTICK Y: Move up/down");
        GUILayout.Label("2ND THUMBSTICK X: Rotate Y-axis");
        GUILayout.Label("A/X BUTTON + STICK: Rotate pitch/yaw");
        GUILayout.Label("B/Y BUTTON + STICK: Rotate roll");
        GUILayout.Label("STICK EXTREMES: Scale");
        GUILayout.Label("MENU BUTTON: Save alignment");
        
        GUILayout.Space(10);
        GUILayout.Label($"Position: {meshToAlign.position.ToString("F2")}");
        GUILayout.Label($"Rotation: {meshToAlign.rotation.eulerAngles.ToString("F1")}");
        GUILayout.Label($"Scale: {meshToAlign.localScale.ToString("F2")}");
        GUILayout.Space(10);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("SAVE"))
        {
            SaveAlignment();
            BroadcastAlignment();
        }
        if (GUILayout.Button("LOAD"))
            LoadAlignment();
        if (GUILayout.Button("EXIT"))
        {
            alignmentModeEnabled = false;
            Debug.Log("[MeshAlignment] EXIT pressed from GUI. Forcing alignmentModeEnabled=false");
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
