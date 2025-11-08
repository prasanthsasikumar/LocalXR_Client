using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LocalClient : MonoBehaviourPunCallbacks
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;
    
    private GameObject localPlayerRepresentation;
    private Vector3 localPosition;
    private Quaternion localRotation;
    
    [Header("VR Setup")]
    public bool isVRMode = true;
    public Transform vrCamera; // Assign your VR camera/head transform
    
    private MeshAlignmentTool meshAlignmentTool;

    void Start()
    {
        // Find VR camera if not assigned
        if (isVRMode && vrCamera == null)
        {
            TryResolveVRCamera();
        }
        
        // Initialize position
        if (isVRMode && vrCamera != null)
        {
            localPosition = vrCamera.position;
            localRotation = vrCamera.rotation;
        }
        else
        {
            localPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            localRotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
        }
        
        // Find mesh alignment tool
        meshAlignmentTool = FindFirstObjectByType<MeshAlignmentTool>();
        
        // Auto-connect to Photon
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.NickName = "LocalUser_" + Random.Range(1000, 9999);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("LocalClient connected to Master!");
        // Join or create a room automatically
        PhotonNetwork.JoinOrCreateRoom("MeshVRRoom", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("LocalClient joined room: " + PhotonNetwork.CurrentRoom.Name);
        Debug.Log("Players in room: " + PhotonNetwork.CurrentRoom.PlayerCount);

        // Instantiate player representation
        Vector3 spawnPos = localPosition;
        localPlayerRepresentation = PhotonNetwork.Instantiate("LocalClientCube", spawnPos, Quaternion.identity);
        localPlayerRepresentation.name = "LocalPlayer_" + PhotonNetwork.NickName;

        // Ensure initial alignment to VR headset immediately after spawn
        if (isVRMode && vrCamera != null)
        {
            localPlayerRepresentation.transform.SetPositionAndRotation(vrCamera.position, vrCamera.rotation);
        }

        // Prevent physics from pulling the local representation down
        var rb = localPlayerRepresentation.GetComponent<Rigidbody>();
        var pv = localPlayerRepresentation.GetComponent<PhotonView>();
        if (rb != null && pv != null && pv.IsMine)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        // Always track VR headset if available, even in alignment mode
        if (isVRMode)
        {
            if (vrCamera == null)
            {
                TryResolveVRCamera();
            }
            if (vrCamera != null)
            {
                localPosition = vrCamera.position;
                localRotation = vrCamera.rotation;
            }
        }
        
        // Handle WASD only when not in VR and not aligning the mesh
        if (!isVRMode && (meshAlignmentTool == null || !meshAlignmentTool.alignmentMode))
        {
            HandleMovement();
        }
        
        // Update the networked player representation (only if we own it)
        if (localPlayerRepresentation != null)
        {
            PhotonView pv = localPlayerRepresentation.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                localPlayerRepresentation.transform.position = localPosition;
                localPlayerRepresentation.transform.rotation = localRotation;
            }
        }
    }

    // Attempts to find the VR camera/head transform in common setups
    private void TryResolveVRCamera()
    {
        // 1) Unity MainCamera tag
        if (Camera.main != null)
        {
            vrCamera = Camera.main.transform;
            if (vrCamera != null) return;
        }

        // 2) Common Oculus/OVR name
        var centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            vrCamera = centerEye.transform;
            if (vrCamera != null) return;
        }

        // 3) Any enabled Camera in scene
        var anyCam = Object.FindFirstObjectByType<Camera>();
        if (anyCam != null)
        {
            vrCamera = anyCam.transform;
        }
    }

    void HandleMovement()
    {
        float moveHorizontal = 0f;
        float moveVertical = 0f;
        float rotate = 0f;

        // WASD controls
        if (Input.GetKey(KeyCode.W))
            moveVertical = 1f;
        if (Input.GetKey(KeyCode.S))
            moveVertical = -1f;
        if (Input.GetKey(KeyCode.A))
            moveHorizontal = -1f;
        if (Input.GetKey(KeyCode.D))
            moveHorizontal = 1f;
        
        // Q/E for rotation
        if (Input.GetKey(KeyCode.Q))
            rotate = -1f;
        if (Input.GetKey(KeyCode.E))
            rotate = 1f;

        // Apply movement relative to current rotation
        Vector3 forward = localRotation * Vector3.forward;
        Vector3 right = localRotation * Vector3.right;
        
        Vector3 movement = (forward * moveVertical + right * moveHorizontal) * moveSpeed * Time.deltaTime;
        localPosition += movement;
        
        // Apply rotation
        if (rotate != 0)
        {
            localRotation *= Quaternion.Euler(0, rotate * rotationSpeed * Time.deltaTime, 0);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("New player joined: " + newPlayer.NickName);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player left: " + otherPlayer.NickName);
    }
}
