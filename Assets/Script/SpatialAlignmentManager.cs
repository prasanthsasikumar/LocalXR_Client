using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

/// <summary>
/// Handles spatial alignment between different coordinate systems
/// Use this to align VR headset coordinates with desktop/laptop coordinates
/// 
/// NOTE: This component no longer directly handles PhotonView or RPC calls.
/// Instead, it subscribes to AlignmentNetworkHub events and uses AlignmentMath
/// for coordinate transformations.
/// </summary>
public class SpatialAlignmentManager : MonoBehaviour
{
    [Header("Alignment Settings")]
    [Tooltip("The shared mesh reference point in the scene")]
    public Transform meshReferencePoint;
    
    [Header("Alignment Mode")]
    public AlignmentMode alignmentMode = AlignmentMode.AutoAlign;
    
    [Header("Manual Alignment (if using Manual mode)")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public float scaleMultiplier = 1f;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public GameObject alignmentMarkerPrefab;
    
    private Dictionary<int, AlignmentData> playerAlignments = new Dictionary<int, AlignmentData>();
    private bool isAligned = false;
    private List<GameObject> debugMarkers = new List<GameObject>();

    public enum AlignmentMode
    {
        AutoAlign,      // Automatically align based on mesh reference
        ManualAlign,    // Use manual offset values
        MarkerBased,    // Use alignment markers to calibrate
        SharedOrigin    // Both systems share the same origin (no alignment needed)
    }

    [System.Serializable]
    public class AlignmentData
    {
        public int playerId;
        public Vector3 positionOffset;
        public Quaternion rotationOffset;
        public float scale;
        public Vector3 meshOrigin; // Where the player's mesh origin is
        
        public AlignmentData(int id)
        {
            playerId = id;
            positionOffset = Vector3.zero;
            rotationOffset = Quaternion.identity;
            scale = 1f;
            meshOrigin = Vector3.zero;
        }
    }

    void Start()
    {
        if (meshReferencePoint == null)
        {
            Debug.LogWarning("Mesh reference point not set! Using scene origin.");
            GameObject refObj = new GameObject("MeshReferencePoint");
            meshReferencePoint = refObj.transform;
        }
        
        // Subscribe to alignment network hub events
        AlignmentNetworkHub.OnSpatialAlignmentReceived += HandleRemoteSpatialAlignment;
    }

    private void OnEnable()
    {
        // Re-subscribe when enabled (in case object was disabled)
        if (AlignmentNetworkHub.Instance != null)
        {
            AlignmentNetworkHub.OnSpatialAlignmentReceived += HandleRemoteSpatialAlignment;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe when disabled to prevent memory leaks
        AlignmentNetworkHub.OnSpatialAlignmentReceived -= HandleRemoteSpatialAlignment;
    }

    void Update()
    {
        // Check if we just joined the room and haven't initiated alignment yet
        if (PhotonNetwork.InRoom && !isAligned && alignmentMode != AlignmentMode.SharedOrigin && !_alignmentInitiated)
        {
            _alignmentInitiated = true;
            StartCoroutine(InitiateAlignment());
        }
    }

    private bool _alignmentInitiated = false;

    System.Collections.IEnumerator InitiateAlignment()
    {
        yield return new WaitForSeconds(1f); // Wait for all players to join
        
        // Send our mesh reference position to other players via AlignmentNetworkHub
        Vector3 myMeshOrigin = meshReferencePoint.position;
        Quaternion myMeshRotation = meshReferencePoint.rotation;
        
        AlignmentNetworkHub.BroadcastSpatialReference(myMeshOrigin, myMeshRotation);
        
        Debug.Log($"<color=cyan>Sent alignment data: Origin at {myMeshOrigin}</color>");
    }

    /// <summary>
    /// Handle incoming spatial alignment data from remote clients
    /// </summary>
    private void HandleRemoteSpatialAlignment(int playerId, Vector3 remoteOrigin, Quaternion remoteRotation)
    {
        if (playerId == Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber)
        {
            // Ignore our own data
            return;
        }
        
        Debug.Log($"<color=green>Received alignment from Player {playerId}: Origin at {remoteOrigin}</color>");
        
        // Calculate alignment using AlignmentMath
        AlignmentData alignment = new AlignmentData(playerId);
        alignment.meshOrigin = remoteOrigin;
        alignment.positionOffset = AlignmentMath.ComputePositionOffset(meshReferencePoint.position, remoteOrigin);
        alignment.rotationOffset = AlignmentMath.ComputeRotationOffset(meshReferencePoint.rotation, remoteRotation);
        
        playerAlignments[playerId] = alignment;
        
        if (showDebugInfo)
        {
            CreateDebugMarker(remoteOrigin, playerId);
        }
        
        isAligned = true;
    }

    /// <summary>
    /// Transform a position from another player's coordinate system to ours
    /// Uses AlignmentMath for mathematically rigorous coordinate transformation
    /// </summary>
    public Vector3 TransformFromPlayer(int playerId, Vector3 theirPosition)
    {
        if (alignmentMode == AlignmentMode.SharedOrigin)
            return theirPosition;

        if (alignmentMode == AlignmentMode.ManualAlign)
            return AlignmentMath.TransformPositionToLocal(
                theirPosition,
                Vector3.zero,  // Remote origin at 0,0,0
                Quaternion.identity,  // Remote rotation identity
                Vector3.zero,  // Local origin at 0,0,0
                Quaternion.identity,  // Local rotation identity
                1f);  // No scale
                
        if (playerAlignments.TryGetValue(playerId, out AlignmentData alignment))
        {
            // Use AlignmentMath for consistent transformation
            return AlignmentMath.TransformPositionToLocal(
                theirPosition,
                alignment.meshOrigin,  // Remote mesh origin
                Quaternion.identity,  // Remote reference rotation (identity)
                meshReferencePoint.position,  // Local mesh origin
                meshReferencePoint.rotation,  // Local mesh rotation
                scaleMultiplier);
        }

        return theirPosition; // No alignment data, return as-is
    }

    /// <summary>
    /// Transform a rotation from another player's coordinate system to ours
    /// Uses AlignmentMath for mathematically rigorous coordinate transformation
    /// </summary>
    public Quaternion TransformFromPlayer(int playerId, Quaternion theirRotation)
    {
        if (alignmentMode == AlignmentMode.SharedOrigin)
            return theirRotation;

        if (alignmentMode == AlignmentMode.ManualAlign)
            return AlignmentMath.TransformRotationToLocal(
                theirRotation,
                Quaternion.identity,  // Remote reference rotation
                Quaternion.identity);  // Local reference rotation

        if (playerAlignments.TryGetValue(playerId, out AlignmentData alignment))
        {
            Quaternion transformedRotation = AlignmentMath.TransformRotationToLocal(
                theirRotation,
                Quaternion.identity,  // Remote reference rotation (fixed: identity)
                meshReferencePoint.rotation);  // Local mesh rotation
            
            // ★ Normalize quaternion to prevent drift
            return transformedRotation.normalized;
        }

        return theirRotation;
    }

    /// <summary>
    /// Get the alignment status
    /// </summary>
    public bool IsAligned()
    {
        return isAligned || alignmentMode == AlignmentMode.SharedOrigin;
    }

    void CreateDebugMarker(Vector3 position, int playerId)
    {
        GameObject marker;
        if (alignmentMarkerPrefab != null)
        {
            marker = Instantiate(alignmentMarkerPrefab, position, Quaternion.identity);
        }
        else
        {
            marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * 0.2f;
            marker.GetComponent<Renderer>().material.color = Color.yellow;
        }
        
        marker.name = $"AlignmentMarker_Player{playerId}";
        debugMarkers.Add(marker);
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(420, 10, 350, 400));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== SPATIAL ALIGNMENT ===", GUI.skin.box);
        GUILayout.Label($"Mode: {alignmentMode}");
        GUILayout.Label($"Aligned: {isAligned}");
        GUILayout.Label($"Mesh Origin: {meshReferencePoint.position}");
        
        if (playerAlignments.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("--- PLAYER ALIGNMENTS ---", GUI.skin.box);
            foreach (var alignment in playerAlignments.Values)
            {
                GUILayout.Label($"Player {alignment.playerId}:");
                GUILayout.Label($"  Offset: {alignment.positionOffset.ToString("F2")}");
                GUILayout.Label($"  Their Origin: {alignment.meshOrigin.ToString("F2")}");
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    /// <summary>
    /// Call this to manually recalibrate alignment
    /// </summary>
    public void RecalibrateAlignment()
    {
        playerAlignments.Clear();
        isAligned = false;
        
        foreach (var marker in debugMarkers)
        {
            if (marker != null)
                Destroy(marker);
        }
        debugMarkers.Clear();
        
        StartCoroutine(InitiateAlignment());
    }

    /// <summary>
    /// Set manual alignment values (useful for testing)
    /// </summary>
    public void SetManualAlignment(Vector3 posOffset, Vector3 rotOffset, float scale = 1f)
    {
        alignmentMode = AlignmentMode.ManualAlign;
        positionOffset = posOffset;
        rotationOffset = rotOffset;
        scaleMultiplier = scale;
        isAligned = true;
    }
}
