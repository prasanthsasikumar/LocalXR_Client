using UnityEngine;
using Photon.Pun;

public class NetworkedPlayer : MonoBehaviourPun, IPunObservable
{
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    
    // ★ Cached transformed values (prevent repeated transformations)
    private Vector3 cachedTargetPosition;
    private Quaternion cachedTargetRotation;
    private bool hasValidCache = false;
    
    // ★ Fixed interpolation speeds (no frame-dependent calculations)
    private const float POSITION_INTERPOLATION_SPEED = 0.2f;  // 20% per frame
    private const float ROTATION_INTERPOLATION_SPEED = 0.25f;  // 25% per frame
    
    public Material localPlayerMaterial;
    public Material remotePlayerMaterial;
    
    private SpatialAlignmentManager alignmentManager;

    void Start()
    {
        // Set position and rotation to network values initially
        networkPosition = transform.position;
        networkRotation = transform.rotation;
        
        // Find the alignment manager
        alignmentManager = FindFirstObjectByType<SpatialAlignmentManager>();
        
        // Color the player based on ownership
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (photonView.IsMine)
            {
                // Local player - make it blue
                if (localPlayerMaterial != null)
                    renderer.material = localPlayerMaterial;
                else
                    renderer.material.color = Color.blue;
            }
            else
            {
                // Remote player - make it red
                if (remotePlayerMaterial != null)
                    renderer.material = remotePlayerMaterial;
                else
                    renderer.material.color = Color.red;
            }
        }
        
        // Add a name tag
        CreateNameTag();
    }

    void Update()
    {
        if (!photonView.IsMine)
        {
            // Use cached target values (not recalculating every frame)
            Vector3 targetPosition = networkPosition;
            Quaternion targetRotation = networkRotation;
            
            // ★ Only transform if we haven't cached yet OR values changed
            if (!hasValidCache || 
                networkPosition != cachedTargetPosition || 
                networkRotation != cachedTargetRotation)
            {
                if (alignmentManager != null && alignmentManager.IsAligned())
                {
                    // Transform and cache (only when needed)
                    cachedTargetPosition = alignmentManager.TransformFromPlayer(photonView.Owner.ActorNumber, networkPosition);
                    cachedTargetRotation = alignmentManager.TransformFromPlayer(photonView.Owner.ActorNumber, networkRotation);
                }
                else
                {
                    cachedTargetPosition = networkPosition;
                    cachedTargetRotation = networkRotation;
                }
                hasValidCache = true;
            }
            
            targetPosition = cachedTargetPosition;
            targetRotation = cachedTargetRotation;
            
            // ★ Fixed interpolation speeds (NOT frame-dependent)
            // This provides smooth, predictable motion without frame-rate sensitivity
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                POSITION_INTERPOLATION_SPEED
            );
            
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                ROTATION_INTERPOLATION_SPEED
            );
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send position and rotation to other clients
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // Receive position and rotation from owner
            Vector3 newPosition = (Vector3)stream.ReceiveNext();
            Quaternion newRotation = (Quaternion)stream.ReceiveNext();
            
            // ★ Invalidate cache when new data arrives
            if (newPosition != networkPosition || newRotation != networkRotation)
            {
                networkPosition = newPosition;
                networkRotation = newRotation;
                hasValidCache = false;  // Recalculate transformation on next Update()
            }
        }
    }

    void CreateNameTag()
    {
        // Create a simple text label above the player
        GameObject nameTagObj = new GameObject("NameTag");
        nameTagObj.transform.SetParent(transform);
        nameTagObj.transform.localPosition = new Vector3(0, 0.6f, 0);
        
        TextMesh textMesh = nameTagObj.AddComponent<TextMesh>();
        textMesh.text = photonView.Owner.NickName;
        textMesh.fontSize = 20;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = photonView.IsMine ? Color.cyan : Color.yellow;
        textMesh.characterSize = 0.1f;
    }
}
