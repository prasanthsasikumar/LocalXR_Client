using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Transmits face mesh and eye gaze data using AlignmentNetworkHub (no PhotonView required)
/// On REMOTE CLIENT: Attach LSL receivers to read and transmit data.
/// On LOCAL CLIENT: Leave LSL receivers empty - this script will only receive data.
/// This version uses AlignmentNetworkHub events instead of IPunObservable
/// </summary>
public class PhotonFaceGazeTransmitter : MonoBehaviour
{
    [Header("LSL Component References (Remote Client Only)")]
    [Tooltip("Reference to the LslFaceMeshReceiver component - ONLY needed on remote client")]
    public LslFaceMeshReceiver faceMeshReceiver;
    
    [Tooltip("Reference to the LslGazeReceiver component - ONLY needed on remote client")]
    public LslGazeReceiver gazeReceiver;

    [Header("Transmission Settings")]
    [Tooltip("Send data every N frames to reduce network traffic (1 = every frame)")]
    [Range(1, 10)]
    public int transmissionInterval = 3;
    
    [Tooltip("Enable face mesh data transmission")]
    public bool transmitFaceMesh = true;
    
    [Tooltip("Enable eye gaze data transmission")]
    public bool transmitGaze = true;

    [Header("Compression Settings")]
    [Tooltip("Number of key facial landmarks to send (instead of all 68)")]
    [Range(10, 68)]
    public int keyLandmarksCount = 20;
    
    [Tooltip("Compress landmark data by reducing precision (fewer decimal places)")]
    public bool compressLandmarks = true;
    
    [Header("Debug")]
    public bool showDebugLogs = false;

    [Header("Identity (Required)")]
    [Tooltip("Unique identifier for this player - must be set manually or via code")]
    public int playerId = -1;  // Must be set to distinguish local vs remote

    // Network state for received data from other players
    private Dictionary<int, FaceGazeData> receivedPlayerData = new Dictionary<int, FaceGazeData>();

    // Transmission counter
    private int frameCounter = 0;
    
    // Local data cache
    private FaceGazeData localData;

    // Key landmark indices to transmit (optimized subset of 68-point model)
    private readonly int[] keyLandmarkIndices = new int[]
    {
        // Face outline & chin
        0, 8, 16,  // Left jaw, chin, right jaw
        
        // Eyebrows
        17, 21, 22, 26,  // Outer right brow, inner right brow, inner left brow, outer left brow
        
        // Nose
        27, 30, 33,  // Nose bridge top, nose tip, nose bottom
        
        // Eyes
        36, 39, 42, 45,  // Right eye outer, right eye inner, left eye inner, left eye outer
        37, 40, 43, 46,  // Right eye top, right eye bottom, left eye top, left eye bottom
        
        // Mouth
        48, 54, 51, 57, 62, 66  // Mouth right, mouth left, upper lip top, lower lip bottom, upper lip inner, lower lip inner
    };

    /// <summary>
    /// Data structure for face and gaze information
    /// </summary>
    [System.Serializable]
    public class FaceGazeData
    {
        public Vector3[] faceLandmarks;
        public Vector2 gazePosition;
        public float pupilSize;
        public bool hasFaceData;
        public bool hasGazeData;
        public float lastUpdateTime;

        public FaceGazeData()
        {
            faceLandmarks = new Vector3[20];  // Default size
            gazePosition = Vector2.zero;
            pupilSize = 0f;
            hasFaceData = false;
            hasGazeData = false;
            lastUpdateTime = 0f;
        }
    }

    private void Awake()
    {
        // Initialize local data
        localData = new FaceGazeData();
        localData.faceLandmarks = new Vector3[keyLandmarksCount];

        // Auto-set playerId if not set
        if (playerId == -1)
        {
            playerId = Photon.Pun.PhotonNetwork.LocalPlayer?.ActorNumber ?? Random.Range(1000, 9999);
            Debug.Log($"[FaceGazeTransmitter] Auto-assigned playerId: {playerId}");
        }
    }

    private void Start()
    {
        // Subscribe to AlignmentNetworkHub events
        AlignmentNetworkHub.OnFaceLandmarksReceived += OnFaceLandmarksReceived;
        AlignmentNetworkHub.OnGazeDataReceived += OnGazeDataReceived;

        // Auto-find LSL receivers if not assigned
        if (faceMeshReceiver == null)
            faceMeshReceiver = GetComponent<LslFaceMeshReceiver>();
        
        if (gazeReceiver == null)
            gazeReceiver = GetComponent<LslGazeReceiver>();
        
        // Determine role
        bool isTransmitter = faceMeshReceiver != null || gazeReceiver != null;
        
        if (showDebugLogs)
        {
<<<<<<< Updated upstream
            if (faceMeshReceiver == null)
                faceMeshReceiver = GetComponent<LslFaceMeshReceiver>();
            
            if (gazeReceiver == null)
                gazeReceiver = GetComponent<LslGazeReceiver>();
            
            // CRITICAL: Enable LSL receivers only on the owner (Remote client that sends data)
            if (faceMeshReceiver != null)
                faceMeshReceiver.enabled = true;
            if (gazeReceiver != null)
                gazeReceiver.enabled = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"PhotonFaceGazeTransmitter (REMOTE/TRANSMITTER mode). Face: {faceMeshReceiver != null}, Gaze: {gazeReceiver != null}");
            }
        }
        else
        {
            // This is a remote player instance on the local client - we only receive data
            // CRITICAL: Disable LSL receivers on remote instances (Local client that only receives via Photon)
            if (faceMeshReceiver == null)
                faceMeshReceiver = GetComponent<LslFaceMeshReceiver>();
            if (gazeReceiver == null)
                gazeReceiver = GetComponent<LslGazeReceiver>();
            
            if (faceMeshReceiver != null)
            {
                faceMeshReceiver.enabled = false;
                Debug.Log($"[PhotonFaceGazeTransmitter] Disabled LslFaceMeshReceiver on remote player instance (LOCAL/RECEIVER mode)");
            }
            if (gazeReceiver != null)
            {
                gazeReceiver.enabled = false;
                Debug.Log($"[PhotonFaceGazeTransmitter] Disabled LslGazeReceiver on remote player instance (LOCAL/RECEIVER mode)");
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"PhotonFaceGazeTransmitter (LOCAL/RECEIVER mode) for player: {photonView.Owner?.NickName}");
            }
=======
            Debug.Log($"[FaceGazeTransmitter] Started with playerId={playerId}, Role: {(isTransmitter ? "TRANSMITTER" : "RECEIVER")}");
            Debug.Log($"[FaceGazeTransmitter] Face: {faceMeshReceiver != null}, Gaze: {gazeReceiver != null}");
>>>>>>> Stashed changes
        }
    }

    private void Update()
    {
        frameCounter++;
        
        // Throttle transmission based on interval
        if (frameCounter % transmissionInterval != 0)
            return;

        // Try to send data if we have LSL receivers
        TryTransmitData();
    }

    private void TryTransmitData()
    {
        bool sentFace = false;
        bool sentGaze = false;

        // Send face mesh data
        if (transmitFaceMesh && faceMeshReceiver != null && faceMeshReceiver.IsConnected)
        {
            Vector3[] landmarks = ExtractKeyLandmarks();
            if (landmarks != null)
            {
                AlignmentNetworkHub.BroadcastFaceLandmarks(landmarks);
                sentFace = true;
                
                // Update local cache
                localData.faceLandmarks = landmarks;
                localData.hasFaceData = true;
                localData.lastUpdateTime = Time.time;

                if (showDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[FaceGazeTransmitter] Sent face landmarks: {landmarks.Length} points");
                }
            }
        }

        // Send gaze data
        if (transmitGaze && gazeReceiver != null && gazeReceiver.IsConnected)
        {
            Vector2 gazePos = gazeReceiver.GetGazePosition();
            float pupilSize = gazeReceiver.GetPupilSize();
            
            AlignmentNetworkHub.BroadcastGazeData(gazePos, pupilSize);
            sentGaze = true;
            
            // Update local cache
            localData.gazePosition = gazePos;
            localData.pupilSize = pupilSize;
            localData.hasGazeData = true;
            localData.lastUpdateTime = Time.time;

            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[FaceGazeTransmitter] Sent gaze data: {gazePos}, pupil={pupilSize}");
            }
        }

        // Log diagnostics if enabled but not sending
        if (showDebugLogs && !sentFace && !sentGaze && Time.frameCount % 180 == 0)
        {
            if (!transmitFaceMesh) Debug.Log("[FaceGazeTransmitter] Face transmission disabled");
            if (transmitFaceMesh && faceMeshReceiver == null) Debug.Log("[FaceGazeTransmitter] Face receiver missing");
            if (transmitFaceMesh && faceMeshReceiver != null && !faceMeshReceiver.IsConnected) Debug.Log("[FaceGazeTransmitter] Face LSL not connected");

            if (!transmitGaze) Debug.Log("[FaceGazeTransmitter] Gaze transmission disabled");
            if (transmitGaze && gazeReceiver == null) Debug.Log("[FaceGazeTransmitter] Gaze receiver missing");
            if (transmitGaze && gazeReceiver != null && !gazeReceiver.IsConnected) Debug.Log("[FaceGazeTransmitter] Gaze LSL not connected");
        }
    }

    private Vector3[] ExtractKeyLandmarks()
    {
        if (faceMeshReceiver == null || !faceMeshReceiver.IsConnected)
            return null;

        Vector3[] landmarks = new Vector3[keyLandmarksCount];
        
        for (int i = 0; i < keyLandmarksCount && i < keyLandmarkIndices.Length; i++)
        {
            int landmarkIndex = keyLandmarkIndices[i];
            Vector3 landmark = faceMeshReceiver.GetLandmark(landmarkIndex);
            
            if (compressLandmarks)
            {
                // Compress by reducing precision to 3 decimal places
                landmark.x = Mathf.Round(landmark.x * 1000f) / 1000f;
                landmark.y = Mathf.Round(landmark.y * 1000f) / 1000f;
                landmark.z = Mathf.Round(landmark.z * 1000f) / 1000f;
            }
            
            landmarks[i] = landmark;
        }
        
        return landmarks;
    }

    #region AlignmentNetworkHub Event Handlers

    private void OnFaceLandmarksReceived(int senderId, Vector3[] landmarks, bool hasData)
    {
        if (senderId == playerId) return; // Ignore our own data

        // Ensure player data exists
        if (!receivedPlayerData.ContainsKey(senderId))
        {
            receivedPlayerData[senderId] = new FaceGazeData();
        }

        // Update face data
        FaceGazeData playerData = receivedPlayerData[senderId];
        playerData.hasFaceData = hasData;
        playerData.lastUpdateTime = Time.time;

        if (hasData && landmarks != null)
        {
            // Ensure array is right size
            if (playerData.faceLandmarks.Length != landmarks.Length)
            {
                playerData.faceLandmarks = new Vector3[landmarks.Length];
            }
            
            System.Array.Copy(landmarks, playerData.faceLandmarks, landmarks.Length);

            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[FaceGazeTransmitter] Received face landmarks from Player {senderId}: {landmarks.Length} points");
            }
        }
    }

    private void OnGazeDataReceived(int senderId, Vector2 gazePosition, float pupilSize, bool hasData)
    {
        if (senderId == playerId) return; // Ignore our own data

        // Ensure player data exists
        if (!receivedPlayerData.ContainsKey(senderId))
        {
            receivedPlayerData[senderId] = new FaceGazeData();
        }

        // Update gaze data
        FaceGazeData playerData = receivedPlayerData[senderId];
        playerData.hasGazeData = hasData;
        playerData.gazePosition = gazePosition;
        playerData.pupilSize = pupilSize;
        playerData.lastUpdateTime = Time.time;

        if (showDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[FaceGazeTransmitter] Received gaze data from Player {senderId}: {gazePosition}");
        }
    }

    #endregion

    #region Public API for accessing received data

    /// <summary>
    /// Get received face/gaze data for a specific player
    /// </summary>
    public FaceGazeData GetPlayerData(int targetPlayerId)
    {
        return receivedPlayerData.ContainsKey(targetPlayerId) ? receivedPlayerData[targetPlayerId] : null;
    }

    /// <summary>
    /// Get all received player data
    /// </summary>
    public Dictionary<int, FaceGazeData> GetAllPlayerData()
    {
        return new Dictionary<int, FaceGazeData>(receivedPlayerData);
    }

    /// <summary>
    /// Get our own local data (if we're transmitting)
    /// </summary>
    public FaceGazeData GetLocalData()
    {
        return localData;
    }

    /// <summary>
    /// Get list of all players we've received data from
    /// </summary>
    public int[] GetActivePlayerIds()
    {
        List<int> activeIds = new List<int>();
        foreach (var kvp in receivedPlayerData)
        {
            if (Time.time - kvp.Value.lastUpdateTime < 5f) // Active within last 5 seconds
            {
                activeIds.Add(kvp.Key);
            }
        }
        return activeIds.ToArray();
    }

    /// <summary>
    /// Check if we have recent data from a specific player
    /// </summary>
    public bool HasRecentDataFrom(int targetPlayerId, float maxAge = 2f)
    {
        if (!receivedPlayerData.ContainsKey(targetPlayerId)) return false;
        return Time.time - receivedPlayerData[targetPlayerId].lastUpdateTime < maxAge;
    }

    /// <summary>
    /// Get the original landmark index from our key landmark index
    /// </summary>
    public int GetOriginalLandmarkIndex(int keyIndex)
    {
        if (keyIndex >= 0 && keyIndex < keyLandmarkIndices.Length)
            return keyLandmarkIndices[keyIndex];
        return -1;
    }

    #endregion

    private void OnGUI()
    {
        if (!showDebugLogs)
            return;

        // Display status in the corner of the screen
        GUILayout.BeginArea(new Rect(10, 150, 400, 300));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"<b>Face/Gaze Transmitter (via AlignmentNetworkHub)</b>");
        GUILayout.Label($"Player ID: {playerId}");
        
        // Show transmission status
        bool canTransmitFace = faceMeshReceiver != null && faceMeshReceiver.IsConnected;
        bool canTransmitGaze = gazeReceiver != null && gazeReceiver.IsConnected;
        
        GUILayout.Label($"<b>Transmission:</b>");
        GUILayout.Label($"  Face: {(canTransmitFace ? "✓ Active" : "✗ Inactive")}");
        GUILayout.Label($"  Gaze: {(canTransmitGaze ? "✓ Active" : "✗ Inactive")}");
        
        // Show received data
        GUILayout.Label($"<b>Receiving from {receivedPlayerData.Count} players:</b>");
        foreach (var kvp in receivedPlayerData)
        {
            int pid = kvp.Key;
            FaceGazeData data = kvp.Value;
            float age = Time.time - data.lastUpdateTime;
            
            GUILayout.Label($"  Player {pid}: Face={data.hasFaceData} Gaze={data.hasGazeData} (age: {age:F1}s)");
        }
        
        // AlignmentNetworkHub status
        GUILayout.Label($"<b>Network Hub Ready:</b> {AlignmentNetworkHub.IsReady}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        AlignmentNetworkHub.OnFaceLandmarksReceived -= OnFaceLandmarksReceived;
        AlignmentNetworkHub.OnGazeDataReceived -= OnGazeDataReceived;
    }
}