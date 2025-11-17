# Eye Gaze / Face Mesh 受信診断ツール - 実装ガイド

## 🎯 実装箇所と戦略

### 実装場所の優先順位

1. **PhotonFaceGazeReceiver.cs** (メイン診断)
   - Face Mesh 受信状況
   - Eye Gaze 受信状況
   - リアルタイムデータ量

2. **RemoteClient.cs / LocalClient.cs** (ネットワーク層)
   - 接続状況
   - プレイヤー検出

3. **GUI / Canvas** (ビジュアル表示)
   - デバッグUI で表示

---

## 🔍 実装1: PhotonFaceGazeReceiver に診断機能を追加

### 現在の状態を確認

```csharp
// 追加するメンバ変数
private int framesSinceFaceMeshReceived = 0;
private int framesSinceGazeReceived = 0;
private int totalFaceMeshPacketsReceived = 0;
private int totalGazePacketsReceived = 0;
private float lastFaceMeshReceiveTime = 0f;
private float lastGazeReceiveTime = 0f;

// 診断プロパティ
public bool IsFaceMeshReceiving => framesSinceFaceMeshReceived < 60;  // 1秒以内
public bool IsGazeReceiving => framesSinceGazeReceived < 60;
public float FaceMeshPacketLoss => CalculatePacketLoss(FaceMeshDataReceived);
```

### 実装手順

```csharp
void Start()
{
    lastFaceMeshReceiveTime = Time.time;
    lastGazeReceiveTime = Time.time;
}

void Update()
{
    // タイマー更新
    framesSinceFaceMeshReceived++;
    framesSinceGazeReceived++;
}

// データ受信時に呼ぶ
void OnFaceMeshDataReceived()
{
    framesSinceFaceMeshReceived = 0;
    lastFaceMeshReceiveTime = Time.time;
    totalFaceMeshPacketsReceived++;
    
    Debug.Log($"[FaceGaze] Face Mesh received! Total: {totalFaceMeshPacketsReceived}");
}

void OnGazeDataReceived()
{
    framesSinceGazeReceived = 0;
    lastGazeReceiveTime = Time.time;
    totalGazePacketsReceived++;
    
    Debug.Log($"[FaceGaze] Gaze received! Total: {totalGazePacketsReceived}");
}
```

---

## 🎨 実装2: デバッグUI の作成

### 推奨: Canvas + GUI で表示

```csharp
void OnGUI()
{
    GUILayout.BeginArea(new Rect(10, 200, 400, 300));
    GUILayout.BeginVertical("box");
    
    GUILayout.Label("=== EYE GAZE / FACE MESH DIAGNOSTICS ===", GUI.skin.box);
    
    GUILayout.Space(5);
    GUILayout.Label($"Face Mesh Receiving: {(IsFaceMeshReceiving ? "✓" : "✗")}");
    GUILayout.Label($"  Packets: {totalFaceMeshPacketsReceived}");
    GUILayout.Label($"  Last received: {(Time.time - lastFaceMeshReceiveTime):F1}s ago");
    
    GUILayout.Space(5);
    GUILayout.Label($"Eye Gaze Receiving: {(IsGazeReceiving ? "✓" : "✗")}");
    GUILayout.Label($"  Packets: {totalGazePacketsReceived}");
    GUILayout.Label($"  Last received: {(Time.time - lastGazeReceiveTime):F1}s ago");
    
    GUILayout.EndVertical();
    GUILayout.EndArea();
}
```

---

## 🌐 実装3: ネットワーク層の診断

### PhotonFaceGazeTransmitter で送信側を確認

```csharp
public class PhotonFaceGazeTransmitter : MonoBehaviourPun
{
    private int faceDataSendCount = 0;
    private int gazeDataSendCount = 0;
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 送信側
            if (transmitFaceMesh && faceMeshReceiver != null)
            {
                // Face Mesh データ送信
                faceDataSendCount++;
                Debug.Log($"[Transmitter] Sending Face Mesh #{faceDataSendCount}");
            }
            
            if (transmitGaze && gazeReceiver != null)
            {
                // Gaze データ送信
                gazeDataSendCount++;
                Debug.Log($"[Transmitter] Sending Gaze #{gazeDataSendCount}");
            }
        }
    }
}
```

### LocalClient / RemoteClient で接続確認

```csharp
void Update()
{
    // 接続状況確認
    if (!PhotonNetwork.IsConnected)
    {
        Debug.LogWarning("[Client] Not connected to Photon!");
        return;
    }
    
    // プレイヤー検出
    PhotonView[] allPhotonViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
    int remotePlayerCount = 0;
    
    foreach (PhotonView pv in allPhotonViews)
    {
        if (!pv.IsMine && pv.Owner != null)
        {
            remotePlayerCount++;
            
            // Face Mesh Receiver の確認
            PhotonFaceGazeReceiver receiver = pv.GetComponent<PhotonFaceGazeReceiver>();
            if (receiver != null)
            {
                Debug.Log($"[Client] Player {pv.Owner.NickName} has FaceGazeReceiver");
            }
            else
            {
                Debug.LogWarning($"[Client] Player {pv.Owner.NickName} has NO FaceGazeReceiver!");
            }
        }
    }
    
    Debug.Log($"[Client] Remote players: {remotePlayerCount}");
}
```

---

## 📊 実装4: 完全な診断スクリプト

### 新規: **NetworkDiagnosticsUI.cs** を作成

```csharp
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkDiagnosticsUI : MonoBehaviourPun
{
    private bool showDiagnostics = true;
    private PhotonView[] cachedPhotonViews;
    
    void Start()
    {
        Debug.Log("[NetworkDiagnostics] Started");
    }
    
    void Update()
    {
        // 'D' キーで診断表示 ON/OFF
        if (Input.GetKeyDown(KeyCode.D))
        {
            showDiagnostics = !showDiagnostics;
            Debug.Log($"[NetworkDiagnostics] Toggled: {showDiagnostics}");
        }
        
        // 'R' キーで情報リセット
        if (Input.GetKeyDown(KeyCode.R))
        {
            RefreshDiagnostics();
        }
    }
    
    void RefreshDiagnostics()
    {
        Debug.Log("=== NETWORK DIAGNOSTICS REFRESH ===");
        
        // 接続状況
        Debug.Log($"Connected: {PhotonNetwork.IsConnected}");
        Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
        if (PhotonNetwork.CurrentRoom != null)
        {
            Debug.Log($"Room: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
        }
        
        // プレイヤー検出
        cachedPhotonViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        Debug.Log($"Total PhotonViews: {cachedPhotonViews.Length}");
        
        int ownCount = 0;
        int remoteCount = 0;
        
        foreach (PhotonView pv in cachedPhotonViews)
        {
            if (pv.IsMine)
            {
                ownCount++;
                Debug.Log($"  [OWN] {pv.gameObject.name}");
            }
            else if (pv.Owner != null)
            {
                remoteCount++;
                Debug.Log($"  [REMOTE] {pv.Owner.NickName} - {pv.gameObject.name}");
                
                // Face Mesh Receiver 確認
                PhotonFaceGazeReceiver receiver = pv.GetComponent<PhotonFaceGazeReceiver>();
                if (receiver != null)
                {
                    Debug.Log($"    ✓ Has PhotonFaceGazeReceiver");
                }
                else
                {
                    Debug.LogWarning($"    ✗ Missing PhotonFaceGazeReceiver!");
                }
                
                // Transmitter 確認
                PhotonFaceGazeTransmitter transmitter = pv.GetComponent<PhotonFaceGazeTransmitter>();
                if (transmitter != null)
                {
                    Debug.Log($"    ✓ Has PhotonFaceGazeTransmitter");
                }
            }
        }
        
        Debug.Log($"Own: {ownCount}, Remote: {remoteCount}");
        Debug.Log("=== END DIAGNOSTICS ===");
    }
    
    void OnGUI()
    {
        if (!showDiagnostics) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 500, 500));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== NETWORK DIAGNOSTICS (Press D to toggle) ===", GUI.skin.box);
        
        // 接続状況
        GUILayout.Label("--- CONNECTION STATUS ---", GUI.skin.box);
        GUILayout.Label($"Connected: {(PhotonNetwork.IsConnected ? "✓" : "✗")}");
        GUILayout.Label($"InRoom: {(PhotonNetwork.InRoom ? "✓" : "✗")}");
        
        if (PhotonNetwork.CurrentRoom != null)
        {
            GUILayout.Label($"Room: {PhotonNetwork.CurrentRoom.Name}");
            GUILayout.Label($"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/4");
            GUILayout.Label($"NickName: {PhotonNetwork.NickName}");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("--- PLAYERS IN SCENE ---", GUI.skin.box);
        
        if (cachedPhotonViews == null)
        {
            if (GUILayout.Button("Refresh (R)"))
            {
                RefreshDiagnostics();
            }
        }
        else
        {
            GUILayout.Label($"Total PhotonViews: {cachedPhotonViews.Length}");
            
            foreach (PhotonView pv in cachedPhotonViews)
            {
                string label = pv.IsMine 
                    ? $"[OWN] {pv.gameObject.name}" 
                    : $"[REMOTE] {pv.Owner?.NickName ?? "?"} - {pv.gameObject.name}";
                
                GUILayout.Label(label);
                
                if (!pv.IsMine && pv.Owner != null)
                {
                    PhotonFaceGazeReceiver receiver = pv.GetComponent<PhotonFaceGazeReceiver>();
                    string status = receiver != null ? "✓ Has Receiver" : "✗ No Receiver";
                    GUILayout.Label($"  {status}", GUI.skin.textField);
                }
            }
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Refresh Diagnostics (R)"))
        {
            RefreshDiagnostics();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

---

## 🚀 使用方法

### ステップ1: NetworkDiagnosticsUI.cs をシーンに追加

```
1. Empty GameObject 作成
2. NetworkDiagnosticsUI スクリプトをアタッチ
3. PhotonView をアタッチ
```

### ステップ2: LocalClient と RemoteClient でテスト

```
実行時:
  D キー: 診断UI ON/OFF
  R キー: 診断リセット
  
コンソール出力:
  [NetworkDiagnostics] Connected: True
  [NetworkDiagnostics] InRoom: True
  [NetworkDiagnostics] Players: 2/4
  [NetworkDiagnostics] Total PhotonViews: 2
  [REMOTE] RemoteUser_1234 - LocalClientCube
    ✓ Has PhotonFaceGazeReceiver
```

### ステップ3: 受信確認

```
期待される出力:
  ✓ Has PhotonFaceGazeReceiver (コンポーネント存在)
  ✓ Sending Face Mesh #1 (データ送信)
  ✓ Face Mesh Receiving: ✓ (データ受信)
  ✓ Packets: 123 (累積パケット数)

問題がある場合:
  ✗ Missing PhotonFaceGazeReceiver! (コンポーネント不足)
  ✗ Not connected to Photon! (接続失敗)
  ✗ Remote players: 0 (プレイヤー未検出)
```

---

## 📝 トラブルシューティング

### 症状1: "Missing PhotonFaceGazeReceiver!"

```
原因: LocalClient.cs の SetupRemotePlayerVisualization() で
      コンポーネントが追加されていない

対策:
1. LocalClient.cs を確認
2. OnJoinedRoom() で SetupRemotePlayerVisualization() が呼ばれているか
3. AddComponent<PhotonFaceGazeReceiver>() が実行されているか
```

### 症状2: "Not connected to Photon!"

```
原因: Photon 接続が確立されていない

対策:
1. PhotonNetwork.ConnectUsingSettings() が呼ばれたか
2. App ID が正しく設定されているか
3. Photon ネットワークが稼働しているか
```

### 症状3: "Remote players: 0"

```
原因: リモートプレイヤーが参加していない

対策:
1. 両クライアントが同じルームに参加しているか確認
2. PhotonNetwork.JoinOrCreateRoom() が成功したか
3. OnJoinedRoom() が呼ばれたか
```

---

## ✅ チェックリスト

- [ ] NetworkDiagnosticsUI.cs を作成
- [ ] シーンに追加、PhotonView アタッチ
- [ ] LocalClient と RemoteClient を起動
- [ ] D キーで診断UI を表示
- [ ] 接続状況を確認
- [ ] プレイヤー検出を確認
- [ ] PhotonFaceGazeReceiver の有無を確認
- [ ] データ送受信ログを確認

