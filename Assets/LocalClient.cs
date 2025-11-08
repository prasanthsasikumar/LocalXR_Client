using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LocalClient : MonoBehaviourPunCallbacks
{
    public GameObject cubePrefab; // assign a simple Cube prefab in Inspector
    private GameObject localCube;

    void Start()
    {
        // Auto-connect to Photon
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        // Join or create a room automatically
        PhotonNetwork.JoinOrCreateRoom("MeshVRRoom", new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("LocalClient joined room!");

        // Instantiate your cube to represent this camera
        localCube = PhotonNetwork.Instantiate(cubePrefab.name, Camera.main.transform.position, Camera.main.transform.rotation, 0);
        localCube.name = "LocalClientCube";
    }

    void Update()
    {
        if (localCube != null)
        {
            // Move the cube with the camera
            localCube.transform.position = Camera.main.transform.position;
            localCube.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
