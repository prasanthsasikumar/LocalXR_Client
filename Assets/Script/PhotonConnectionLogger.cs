using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Simple on-screen Photon connection HUD and console logger.
/// Add this to any active GameObject in your scene to see:
/// - Connect / join / leave / player events (Console + HUD)
/// - Current region, room, player list, and ping
/// Press F1 (default) to toggle the HUD during Play.
/// Optional: enable AutoConnect to connect/join if nothing else is doing it.
/// </summary>
public class PhotonConnectionLogger : MonoBehaviourPunCallbacks
{
    [Header("HUD")]
    public bool showHud = true;
    public KeyCode toggleKey = KeyCode.F1;
    public int maxEventsShown = 15;

    [Header("Console Logging")] 
    public bool logToConsole = true;

    [Header("Optional Auto Connect")]
    [Tooltip("If true and not connected, this component will ConnectUsingSettings and JoinOrCreate the room.")]
    public bool autoConnectIfNotConnected = false;
    [Tooltip("Room name to join when AutoConnect is enabled.")]
    public string roomName = "MeshVRRoom";
    [Tooltip("Max players when creating the room with AutoConnect.")]
    public byte maxPlayers = 4;

    private readonly Queue<string> _events = new Queue<string>();
    private Vector2 _scroll;
    private bool _manualConnectRequested = false;

    private void Awake()
    {
        LogEvent("[Logger] Awake");
    }

    private void Start()
    {
        LogEvent($"[Logger] Start | showHud={showHud} | autoConnect={autoConnectIfNotConnected} | connected={PhotonNetwork.IsConnected} | inRoom={PhotonNetwork.InRoom}");
        LogEvent("[Logger] PhotonConnectionLogger active (press F1 to toggle HUD)");

        if (autoConnectIfNotConnected && !PhotonNetwork.IsConnected)
        {
            LogEvent("[Logger] AutoConnect: Connecting using settings...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showHud = !showHud;
        }
    }

    private void LogEvent(string msg)
    {
        if (logToConsole)
        {
            Debug.Log(msg);
        }
        _events.Enqueue($"{System.DateTime.Now:HH:mm:ss}  {msg}");
        while (_events.Count > maxEventsShown)
        {
            _events.Dequeue();
        }
    }

    public override void OnConnectedToMaster()
    {
        LogEvent($"[Photon] ConnectedToMaster | Region={PhotonNetwork.CloudRegion} | Nick={PhotonNetwork.NickName}");
        if ((autoConnectIfNotConnected || _manualConnectRequested) && !PhotonNetwork.InRoom)
        {
            LogEvent($"[Logger] AutoConnect: Joining/Creating room '{roomName}'...");
            PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = maxPlayers }, TypedLobby.Default);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        LogEvent($"[Photon] Disconnected | Cause={cause}");
    }

    public override void OnJoinedLobby()
    {
        LogEvent("[Photon] JoinedLobby");
    }

    public override void OnCreatedRoom()
    {
        LogEvent($"[Photon] CreatedRoom '{PhotonNetwork.CurrentRoom?.Name}'");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        LogEvent($"[Photon] CreateRoomFailed | Code={returnCode} | {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        LogEvent($"[Photon] JoinRoomFailed | Code={returnCode} | {message}");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        LogEvent($"[Photon] JoinRandomFailed | Code={returnCode} | {message}");
    }

    public override void OnJoinedRoom()
    {
        LogEvent($"[Photon] JoinedRoom '{PhotonNetwork.CurrentRoom.Name}' | Players={PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
    }

    public override void OnLeftRoom()
    {
        LogEvent("[Photon] LeftRoom");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        LogEvent($"[Photon] PlayerEntered | {newPlayer.NickName} (ID {newPlayer.ActorNumber})");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        LogEvent($"[Photon] PlayerLeft | {otherPlayer.NickName} (ID {otherPlayer.ActorNumber})");
    }

    private void OnGUI()
    {
        if (!showHud) return;

        const int width = 420;
        const int height = 280;
        GUILayout.BeginArea(new Rect(10, 10, width, height));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b>Photon Connection</b>");

        GUILayout.Label($"Connected: {PhotonNetwork.IsConnected}");
        GUILayout.Label($"In Room: {PhotonNetwork.InRoom}");
        GUILayout.Label($"Server: {PhotonNetwork.Server}");
        GUILayout.Label($"Region: {PhotonNetwork.CloudRegion}");
        GUILayout.Label($"Nick: {PhotonNetwork.NickName}");
        GUILayout.Label($"Ping: {PhotonNetwork.GetPing()} ms");

        if (!PhotonNetwork.IsConnected)
        {
            GUILayout.Space(4);
            GUILayout.Label("Status: Not connected");
            if (GUILayout.Button("Connect Using Settings"))
            {
                LogEvent("[Logger] Manual ConnectUsingSettings() requested");
                _manualConnectRequested = true;
                PhotonNetwork.ConnectUsingSettings();
            }
        }
        else if (!PhotonNetwork.InRoom)
        {
            GUILayout.Space(4);
            GUILayout.Label("Status: Connected to Master, not in room");
            if (GUILayout.Button($"Join/Create Room '{roomName}'"))
            {
                LogEvent($"[Logger] Manual JoinOrCreateRoom('{roomName}') requested");
                _manualConnectRequested = true;
                PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = maxPlayers }, TypedLobby.Default);
            }
        }
        
        if (PhotonNetwork.InRoom)
        {
            GUILayout.Label($"Room: {PhotonNetwork.CurrentRoom.Name}");
            GUILayout.Label($"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            GUILayout.Space(4);
            GUILayout.Label("Players:");
            foreach (var p in PhotonNetwork.PlayerList)
            {
                string me = p.IsLocal ? " (YOU)" : string.Empty;
                GUILayout.Label($" • {p.NickName} [ID {p.ActorNumber}]{me}");
            }
        }
        else
        {
            GUILayout.Label("Room: -");
        }

        GUILayout.Space(6);
        GUILayout.Label("Recent Events:");
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(100));
        foreach (var e in _events)
        {
            GUILayout.Label(e);
        }
        GUILayout.EndScrollView();

        GUILayout.Label("Toggle HUD: F1");
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
