using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Network diagnostics UI for debugging Eye Gaze and Face Mesh reception
/// Uses AlignmentNetworkHub events instead of PhotonView
/// Press D to toggle diagnostics UI
/// Press R to refresh network information
/// </summary>
public class NetworkDiagnosticsUI : MonoBehaviour
{
    private bool showDiagnostics = true;
    private float lastRefreshTime = 0f;
    
    // Cached data for display
    private int connectionStatus = 0;  // 0=disconnected, 1=connected, 2=in room
    private int remotePlayerCount = 0;
    private string diagnosticMessage = "";
    
    // Event counters
    private int spatialAlignmentEventCount = 0;
    private int meshAlignmentEventCount = 0;

    void Start()
    {
        Debug.Log("[NetworkDiagnostics] Started diagnostics UI (no PhotonView required)");
        
        // Subscribe to AlignmentNetworkHub events
        AlignmentNetworkHub.OnSpatialAlignmentReceived += OnSpatialAlignmentReceived;
        AlignmentNetworkHub.OnMeshAlignmentReceived += OnMeshAlignmentReceived;
        AlignmentNetworkHub.OnMeshAlignmentModeChanged += OnMeshAlignmentModeChanged;
        
        RefreshDiagnostics();
    }

    void Update()
    {
        // D key: Toggle diagnostics UI
        if (Input.GetKeyDown(KeyCode.D))
        {
            showDiagnostics = !showDiagnostics;
            Debug.Log($"[NetworkDiagnostics] Diagnostics UI: {(showDiagnostics ? "ON" : "OFF")}");
        }

        // R key: Refresh diagnostics
        if (Input.GetKeyDown(KeyCode.R))
        {
            RefreshDiagnostics();
            Debug.Log("[NetworkDiagnostics] Refreshed");
        }
        
        // Auto-refresh every 2 seconds for real-time updates
        if (Time.time - lastRefreshTime > 2f)
        {
            RefreshDiagnostics();
            lastRefreshTime = Time.time;
        }
    }

    #region Event Callbacks

    private void OnSpatialAlignmentReceived(int playerId, Vector3 origin, Quaternion rotation)
    {
        spatialAlignmentEventCount++;
        Debug.Log($"[NetworkDiagnostics] Spatial Alignment event #{spatialAlignmentEventCount} from Player {playerId}");
    }

    private void OnMeshAlignmentReceived(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        meshAlignmentEventCount++;
        Debug.Log($"[NetworkDiagnostics] Mesh Alignment event #{meshAlignmentEventCount}");
    }

    private void OnMeshAlignmentModeChanged(bool isEnabled)
    {
        Debug.Log($"[NetworkDiagnostics] Mesh Alignment mode: {(isEnabled ? "ENABLED" : "DISABLED")}");
    }

    #endregion

    void RefreshDiagnostics()
    {
        diagnosticMessage = "";
        
        // Connection status
        diagnosticMessage += "=== CONNECTION STATUS ===\n";
        
        if (!PhotonNetwork.IsConnected)
        {
            diagnosticMessage += "Status: ✗ Not Connected\n";
            connectionStatus = 0;
        }
        else if (!PhotonNetwork.InRoom)
        {
            diagnosticMessage += "Status: ✓ Connected (Waiting for room)\n";
            connectionStatus = 1;
        }
        else
        {
            diagnosticMessage += "Status: ✓✓ In Room\n";
            connectionStatus = 2;
        }

        diagnosticMessage += $"Connected: {PhotonNetwork.IsConnected}\n";
        diagnosticMessage += $"InRoom: {PhotonNetwork.InRoom}\n";

        if (PhotonNetwork.CurrentRoom != null)
        {
            diagnosticMessage += $"Room: {PhotonNetwork.CurrentRoom.Name}\n";
            diagnosticMessage += $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/4\n";
        }

        diagnosticMessage += $"LocalPlayer: {PhotonNetwork.NickName}\n";

        // AlignmentNetworkHub status
        diagnosticMessage += "\n=== ALIGNMENT NETWORK HUB ===\n";
        diagnosticMessage += $"Hub Ready: {AlignmentNetworkHub.IsReady}\n";
        
        // Event counters
        diagnosticMessage += "\n=== NETWORK EVENTS ===\n";
        diagnosticMessage += $"Spatial Alignment events: {spatialAlignmentEventCount}\n";
        diagnosticMessage += $"Mesh Alignment events: {meshAlignmentEventCount}\n";
        
        // NetworkedPlayer detection in scene
        diagnosticMessage += "\n=== PLAYERS IN SCENE ===\n";
        
        NetworkedPlayer[] networkedPlayers = FindObjectsByType<NetworkedPlayer>(FindObjectsSortMode.None);
        diagnosticMessage += $"Total NetworkedPlayers: {networkedPlayers.Length}\n";

        remotePlayerCount = 0;
        int ownCount = 0;
        int faceGazeReceiverCount = 0;
        int faceGazeTransmitterCount = 0;

        foreach (NetworkedPlayer player in networkedPlayers)
        {
            // Determine if it's own or remote (both have PhotonView internally)
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null)
            {
                if (pv.IsMine)
                {
                    ownCount++;
                    diagnosticMessage += $"\n[OWN] {player.gameObject.name}\n";
                    
                    // Check for PhotonFaceGazeTransmitter on own player
                    PhotonFaceGazeTransmitter transmitter = player.GetComponent<PhotonFaceGazeTransmitter>();
                    if (transmitter != null)
                    {
                        faceGazeTransmitterCount++;
                        diagnosticMessage += $"  ✓ PhotonFaceGazeTransmitter found\n";
                    }
                    else
                    {
                        diagnosticMessage += $"  - No transmitter\n";
                    }
                }
                else if (pv.Owner != null)
                {
                    remotePlayerCount++;
                    diagnosticMessage += $"\n[REMOTE] {pv.Owner.NickName}\n";
                    diagnosticMessage += $"  GameObject: {player.gameObject.name}\n";

                    // Check for PhotonFaceGazeReceiver on remote player
                    PhotonFaceGazeReceiver receiver = player.GetComponent<PhotonFaceGazeReceiver>();
                    if (receiver != null)
                    {
                        faceGazeReceiverCount++;
                        diagnosticMessage += $"  ✓ PhotonFaceGazeReceiver found\n";
                    }
                    else
                    {
                        diagnosticMessage += $"  ✗ NO PhotonFaceGazeReceiver!\n";
                    }
                }
            }
        }

        diagnosticMessage += $"\nOwn players: {ownCount}\n";
        diagnosticMessage += $"Remote players: {remotePlayerCount}\n";
        diagnosticMessage += $"With FaceGazeReceiver: {faceGazeReceiverCount}\n";
        diagnosticMessage += $"With FaceGazeTransmitter: {faceGazeTransmitterCount}\n";

        // Summary
        diagnosticMessage += "\n=== DIAGNOSTICS ===\n";
        if (connectionStatus == 2 && remotePlayerCount > 0 && faceGazeReceiverCount > 0)
        {
            diagnosticMessage += "✓ Everything looks OK!\n";
        }
        else
        {
            if (connectionStatus < 2)
                diagnosticMessage += "✗ Not connected to room\n";
            if (remotePlayerCount == 0)
                diagnosticMessage += "✗ No remote players found\n";
            if (faceGazeReceiverCount == 0 && remotePlayerCount > 0)
                diagnosticMessage += "✗ Remote players missing FaceGazeReceiver\n";
        }
        
        // Add network event monitoring hint
        diagnosticMessage += "\nNetwork event counters help verify\n";
        diagnosticMessage += "that AlignmentNetworkHub is receiving\n";
        diagnosticMessage += "and broadcasting data correctly.\n";
    }


    void OnGUI()
    {
        if (!showDiagnostics) return;

        GUILayout.BeginArea(new Rect(10, 10, 600, 700));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== NETWORK DIAGNOSTICS UI ===", GUI.skin.box);
        GUILayout.Label("(Press D to toggle, R to refresh)", GUI.skin.label);

        GUILayout.Space(5);

        // Display diagnostic message
        GUILayout.TextArea(diagnosticMessage, GUI.skin.textArea, GUILayout.Height(600));

        GUILayout.Space(5);

        if (GUILayout.Button("Refresh Now (R)", GUILayout.Height(30)))
        {
            RefreshDiagnostics();
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        // Unsubscribe from AlignmentNetworkHub events
        AlignmentNetworkHub.OnSpatialAlignmentReceived -= OnSpatialAlignmentReceived;
        AlignmentNetworkHub.OnMeshAlignmentReceived -= OnMeshAlignmentReceived;
        AlignmentNetworkHub.OnMeshAlignmentModeChanged -= OnMeshAlignmentModeChanged;
    }
}
