using UnityEngine;
using Photon.Pun;

/// <summary>
/// Receives and visualizes face mesh and eye gaze data from remote clients via AlignmentNetworkHub.
/// Attach this script to a GameObject that represents a remote player.
/// Uses AlignmentNetworkHub events instead of direct PhotonView dependency.
/// </summary>
public class PhotonFaceGazeReceiver : MonoBehaviour
{
    [Header("Player Identification")]
    [Tooltip("Remote player ID to receive data from")]
    public int targetPlayerId = -1;

    [Header("Face Mesh Visualization")]
    [Tooltip("Prefab or GameObject to instantiate for each landmark")]
    public GameObject landmarkPrefab;
    
    [Tooltip("Parent transform for landmark visualizations")]
    public Transform landmarkParent;
    
    [Tooltip("Scale multiplier for landmark positions")]
    public float landmarkScale = 1f;
    
    [Tooltip("Offset for landmark visualization")]
    public Vector3 landmarkOffset = Vector3.zero;

    [Header("Gaze Visualization")]
    [Tooltip("GameObject to represent the gaze point (e.g., a sphere)")]
    public GameObject gazeIndicator;
    
    [Tooltip("Reference to the camera for gaze raycasting")]
    public Camera targetCamera;
    
    [Tooltip("Max distance for gaze ray")]
    public float gazeRayDistance = 10f;
    
    [Tooltip("Show gaze ray in Scene view")]
    public bool showGazeRay = true;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Internal state
    private GameObject[] landmarkObjects;
    private PhotonView photonView;
    private bool isInitialized = false;
    
    // Received data cache (from AlignmentNetworkHub events)
    private Vector3[] receivedFaceLandmarks;
    private Vector2 receivedGazePosition;
    private float receivedPupilSize;
    private bool hasFaceData = false;
    private bool hasGazeData = false;
    private float lastFaceDataTime = 0f;
    private float lastGazeDataTime = 0f;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        
        // Auto-assign target player ID from PhotonView if available
        if (targetPlayerId == -1 && photonView != null && photonView.Owner != null)
        {
            targetPlayerId = photonView.Owner.ActorNumber;
        }
        
        // Setup camera reference
        if (targetCamera == null)
            targetCamera = Camera.main;
        
        // Initialize received data arrays
        receivedFaceLandmarks = new Vector3[20]; // Default size, will resize as needed
    }

    private void Start()
    {
        // Only visualize for remote players (not our own)
        if (photonView != null && photonView.IsMine)
        {
            if (showDebugInfo)
                Debug.Log("[FaceGazeRx] Disabled - this is our own player");
            enabled = false;
            return;
        }

        // Subscribe to AlignmentNetworkHub events
        AlignmentNetworkHub.OnFaceLandmarksReceived += OnFaceLandmarksReceived;
        AlignmentNetworkHub.OnGazeDataReceived += OnGazeDataReceived;

        InitializeLandmarkVisualization();
        InitializeGazeVisualization();
        
        isInitialized = true;
        
        string ownerName = photonView?.Owner?.NickName ?? "Unknown";
        Debug.Log($"[FaceGazeRx] Initialized for remote player '{ownerName}' | PlayerID={targetPlayerId} | ViewID={photonView?.ViewID}");
    }

    private void InitializeLandmarkVisualization()
    {
        if (landmarkPrefab == null)
        {
            Debug.LogWarning("PhotonFaceGazeReceiver: No landmark prefab assigned. Creating default spheres.");
            landmarkPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            landmarkPrefab.transform.localScale = Vector3.one * 0.01f;
            landmarkPrefab.GetComponent<Renderer>().material.color = Color.yellow;
        }

        // Create parent if needed
        if (landmarkParent == null)
        {
            GameObject parentObj = new GameObject("RemoteFaceLandmarks");
            parentObj.transform.SetParent(transform);
            parentObj.transform.localPosition = landmarkOffset;
            landmarkParent = parentObj.transform;
        }

        // Create landmark visualization objects (default 20 landmarks)
        int landmarkCount = 20;
        landmarkObjects = new GameObject[landmarkCount];
        
        for (int i = 0; i < landmarkCount; i++)
        {
            GameObject landmark = Instantiate(landmarkPrefab, landmarkParent);
            landmark.name = $"Landmark_{i}";
            landmark.SetActive(false); // Hide until data is received
            landmarkObjects[i] = landmark;
        }
    }

    private void InitializeGazeVisualization()
    {
        if (gazeIndicator == null)
        {
            // Create a simple sphere for gaze indication
            gazeIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gazeIndicator.name = "RemoteGazeIndicator";
            gazeIndicator.transform.SetParent(transform);
            gazeIndicator.transform.localScale = Vector3.one * 0.05f;
            
            Renderer renderer = gazeIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0f, 0f, 0.7f);
            }
            
            gazeIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        // Periodic diagnostic: check if we're receiving any data
        if (showDebugInfo && Time.frameCount % 180 == 0)
        {
            string ownerName = photonView?.Owner?.NickName ?? "Unknown";
            float faceAge = Time.time - lastFaceDataTime;
            float gazeAge = Time.time - lastGazeDataTime;
            Debug.Log($"[FaceGazeRx] Status | Player={ownerName} | PlayerID={targetPlayerId} | Face={hasFaceData}(age:{faceAge:F1}s) | Gaze={hasGazeData}(age:{gazeAge:F1}s)");
        }

        UpdateFaceMeshVisualization();
        UpdateGazeVisualization();
    }

    #region AlignmentNetworkHub Event Handlers

    private void OnFaceLandmarksReceived(int senderId, Vector3[] landmarks, bool hasData)
    {
        // Only process data from our target player
        if (senderId != targetPlayerId)
            return;

        hasFaceData = hasData;
        lastFaceDataTime = Time.time;

        if (hasData && landmarks != null)
        {
            // Resize array if needed
            if (receivedFaceLandmarks.Length != landmarks.Length)
            {
                receivedFaceLandmarks = new Vector3[landmarks.Length];
                
                // Resize landmark objects array if needed
                if (landmarkObjects == null || landmarkObjects.Length != landmarks.Length)
                {
                    ResizeLandmarkObjects(landmarks.Length);
                }
            }

            System.Array.Copy(landmarks, receivedFaceLandmarks, landmarks.Length);

            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[FaceGazeRx] Received {landmarks.Length} face landmarks from Player {senderId}");
            }
        }
    }

    private void OnGazeDataReceived(int senderId, Vector2 gazePosition, float pupilSize, bool hasData)
    {
        // Only process data from our target player
        if (senderId != targetPlayerId)
            return;

        hasGazeData = hasData;
        lastGazeDataTime = Time.time;

        if (hasData)
        {
            receivedGazePosition = gazePosition;
            receivedPupilSize = pupilSize;

            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[FaceGazeRx] Received gaze data from Player {senderId}: {gazePosition}");
            }
        }
    }

    private void ResizeLandmarkObjects(int newSize)
    {
        // Clean up old objects
        if (landmarkObjects != null)
        {
            foreach (var obj in landmarkObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
        }

        // Create new objects
        landmarkObjects = new GameObject[newSize];
        for (int i = 0; i < newSize; i++)
        {
            GameObject landmark = Instantiate(landmarkPrefab, landmarkParent);
            landmark.name = $"Landmark_{i}";
            landmark.SetActive(false);
            landmarkObjects[i] = landmark;
        }

        Debug.Log($"[FaceGazeRx] Resized landmark objects to {newSize}");
    }

    #endregion

    private void UpdateFaceMeshVisualization()
    {
        if (!hasFaceData || landmarkObjects == null)
        {
            if (showDebugInfo && Time.frameCount % 300 == 0 && landmarkObjects != null)
            {
                Debug.Log($"[FaceGazeRx] No face data from Player {targetPlayerId} yet.");
            }
            return;
        }

        for (int i = 0; i < landmarkObjects.Length && i < receivedFaceLandmarks.Length; i++)
        {
            if (landmarkObjects[i] != null)
            {
                Vector3 position = receivedFaceLandmarks[i];
                
                // Check if landmark data is valid
                if (position != Vector3.zero)
                {
                    // Apply scale and offset
                    Vector3 worldPos = landmarkParent.TransformPoint(position * landmarkScale);
                    landmarkObjects[i].transform.position = worldPos;
                    landmarkObjects[i].SetActive(true);
                }
                else
                {
                    landmarkObjects[i].SetActive(false);
                }
            }
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[FaceGazeRx] Visualizing {receivedFaceLandmarks.Length} landmarks from Player {targetPlayerId}");
        }
    }

    private void UpdateGazeVisualization()
    {
        if (!hasGazeData || gazeIndicator == null || targetCamera == null)
        {
            if (gazeIndicator != null)
                gazeIndicator.SetActive(false);
            
            if (showDebugInfo && Time.frameCount % 300 == 0 && gazeIndicator != null && targetCamera != null)
            {
                Debug.Log($"[FaceGazeRx] No gaze data from Player {targetPlayerId} yet.");
            }
            return;
        }

        // Validate gaze position
        if (float.IsNaN(receivedGazePosition.x) || float.IsNaN(receivedGazePosition.y) || 
            receivedGazePosition.x < 0 || receivedGazePosition.x > 1 || 
            receivedGazePosition.y < 0 || receivedGazePosition.y > 1)
        {
            gazeIndicator.SetActive(false);
            return;
        }

        // Convert normalized screen coordinates to world position via raycast
        Vector3 screenPos = new Vector3(
            receivedGazePosition.x * Screen.width,
            receivedGazePosition.y * Screen.height,
            0f
        );

        Ray ray = targetCamera.ScreenPointToRay(screenPos);
        
        if (Physics.Raycast(ray, out RaycastHit hit, gazeRayDistance))
        {
            // Hit something - place indicator at hit point
            gazeIndicator.transform.position = hit.point;
            gazeIndicator.SetActive(true);

            if (showGazeRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red);
            }
        }
        else
        {
            // Didn't hit anything - place at max distance
            gazeIndicator.transform.position = ray.origin + ray.direction * gazeRayDistance;
            gazeIndicator.SetActive(true);

            if (showGazeRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * gazeRayDistance, Color.yellow);
            }
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[FaceGazeRx] Visualizing gaze at {receivedGazePosition} from Player {targetPlayerId}");
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo)
            return;

        GUILayout.BeginArea(new Rect(420, 150, 400, 250));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"<b>Face/Gaze Receiver (AlignmentNetworkHub)</b>");
        GUILayout.Label($"Remote Player: {photonView?.Owner?.NickName ?? "Unknown"}");
        GUILayout.Label($"Target Player ID: {targetPlayerId}");
        GUILayout.Label($"Has Face Data: {hasFaceData}");
        GUILayout.Label($"Has Gaze Data: {hasGazeData}");
        
        if (hasFaceData)
        {
            float faceAge = Time.time - lastFaceDataTime;
            GUILayout.Label($"Face Data Age: {faceAge:F1}s");
            GUILayout.Label($"Landmarks: {receivedFaceLandmarks.Length}");
        }
        
        if (hasGazeData)
        {
            float gazeAge = Time.time - lastGazeDataTime;
            GUILayout.Label($"Gaze Pos: ({receivedGazePosition.x:F3}, {receivedGazePosition.y:F3})");
            GUILayout.Label($"Gaze Data Age: {gazeAge:F1}s");
            GUILayout.Label($"Pupil Size: {receivedPupilSize:F3}");
        }
        
        GUILayout.Label($"Hub Ready: {AlignmentNetworkHub.IsReady}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        // Unsubscribe from AlignmentNetworkHub events
        AlignmentNetworkHub.OnFaceLandmarksReceived -= OnFaceLandmarksReceived;
        AlignmentNetworkHub.OnGazeDataReceived -= OnGazeDataReceived;

        // Clean up landmark objects
        if (landmarkObjects != null)
        {
            foreach (var landmark in landmarkObjects)
            {
                if (landmark != null)
                    Destroy(landmark);
            }
        }

        // Clean up gaze indicator
        if (gazeIndicator != null)
            Destroy(gazeIndicator);
    }
}
