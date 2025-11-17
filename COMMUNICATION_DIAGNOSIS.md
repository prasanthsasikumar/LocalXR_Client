# 通信不可問題の診断レポート

## 実行日: 2025年11月17日

---

## 問題の原因分析

添付されたコードを詳細に分析しました。**通信ができない主な理由**を以下にまとめます。

### 🔴 **重大問題 1: AlignmentNetworkHub が登録されていない**

**LocalClient.cs, RemoteClient.cs ともに AlignmentNetworkHub をシーンに配置する処理がない**

#### 現在の状態
```
LocalClient.cs
├─ Start() で Photon 接続開始
├─ OnJoinedRoom() でプレイヤーをインスタンス化
└─ ❌ AlignmentNetworkHub の参照/登録なし

RemoteClient.cs
├─ Start() で Photon 接続開始
├─ OnJoinedRoom() でプレイヤーをインスタンス化
└─ ❌ AlignmentNetworkHub の参照/登録なし
```

#### 問題
- `AlignmentNetworkHub.Instance` が `null` になっている
- `BroadcastSpatialReference()` が呼ばれても `if (instance == null)` で早期終了
- イベント `OnSpatialAlignmentReceived` が発火しない

#### 修正方法
```csharp
// LocalClient.cs または RemoteClient.cs の Start() メソッドに追加
void Start()
{
    // ★ AlignmentNetworkHub が存在しない場合は作成
    if (FindFirstObjectByType<AlignmentNetworkHub>() == null)
    {
        GameObject hubObj = new GameObject("AlignmentNetworkHub");
        AlignmentNetworkHub hub = hubObj.AddComponent<AlignmentNetworkHub>();
        PhotonView photonView = hubObj.AddComponent<PhotonView>();
        // PhotonView の設定...
    }
    
    // 既存のコード...
}
```

---

### 🔴 **重大問題 2: PhotonView の設定が不完全**

**AlignmentNetworkHub の PhotonView が正しく設定されていない可能性**

#### チェックポイント
```csharp
public class AlignmentNetworkHub : MonoBehaviourPunCallbacks
{
    // ❓ PhotonView コンポーネントが自動的に取得されていない
    // ❓ PhotonView.ViewID が割り当てられているか不明
    // ❓ Observed Components の設定が不明
}
```

#### 必要な設定
```csharp
AlignmentNetworkHub GameObject には以下が必須:

1. AlignmentNetworkHub (スクリプト)
2. PhotonView コンポーネント
   ├─ ViewID: 自動割り当て（またはシーンで固定設定）
   ├─ Observed Components: なし（RPC のみ使用）
   └─ Instantiation: 手動（シーンに存在するため）
```

---

### 🔴 **重大問題 3: Photon 接続タイミングの不整合**

**LocalClient と RemoteClient が同時に Photon に接続しない可能性**

#### シナリオ
```
時刻 T1: LocalClient が Photon 接続開始
         ↓
時刻 T2: LocalClient が OnJoinedRoom() → アラインメント開始
         ↓
         ❌ RemoteClient がまだ Photon に接続していない
         ↓
時刻 T3: RemoteClient が Photon 接続開始
         ↓
         ❌ LocalClient からのアラインメント RPC を受信できない（すでに送信済み）
```

#### RpcTarget.AllBuffered の動作
```csharp
public static void BroadcastSpatialReference(Vector3 origin, Quaternion rotation)
{
    instance.photonView.RPC(
        "ReceiveSpatialReference",
        RpcTarget.AllBuffered,  // ← バッファに保存される
        // ...
    );
}
```

- ✅ `RpcTarget.AllBuffered` は RPC をサーバーにバッファ保存
- ✅ 後で接続したクライアントでもバッファから復元される
- ✅ しかし、**バッファが正しく設定されていない可能性**

---

### 🔴 **重大問題 4: イベント購読のタイミング不良**

**SpatialAlignmentManager がイベントを購読する前に RPC が送られている**

#### 現在の流れ
```
LocalClient.OnJoinedRoom()
    ↓
photonView.RPC("ReceiveSpatialReference", RpcTarget.AllBuffered, ...)  (T1)
    ↓
    ↓ [遅延 → RemoteClient が接続]
    ↓
RemoteClient.OnJoinedRoom()
    ↓
SpatialAlignmentManager.Start()  (T2 >> T1)
    ↓
AlignmentNetworkHub.OnSpatialAlignmentReceived += HandleRemoteSpatialAlignment  (T2 >> T1)
    ↓
❌ バッファされた RPC は既に実行済み、新たな購読者には通知されない可能性
```

#### 問題の詳細
```csharp
// SpatialAlignmentManager.cs の Start() で購読開始
void Start()
{
    // ...
    AlignmentNetworkHub.OnSpatialAlignmentReceived += HandleRemoteSpatialAlignment;
    // この時点では、過去の RPC は実行済み
}
```

---

### 🔴 **重大問題 5: Photon RPC ターゲットの設定誤り**

#### BroadcastMeshAlignment の問題
```csharp
public static void BroadcastMeshAlignment(Vector3 position, Quaternion rotation, Vector3 scale)
{
    instance.photonView.RPC(
        "ReceiveMeshAlignment",
        RpcTarget.Others,  // ← ⚠️ "Others" (自分以外)
        // ...
    );
}
```

- ✅ `RpcTarget.AllBuffered`: バッファに保存、全クライアントで実行
- ❌ `RpcTarget.Others`: バッファに保存されない、現在接続中のクライアントのみ

**LocalClient でメッシュ保存時に RemoteClient がまだ接続していない場合、同期されない**

#### 修正案
```csharp
public static void BroadcastMeshAlignment(Vector3 position, Quaternion rotation, Vector3 scale)
{
    instance.photonView.RPC(
        "ReceiveMeshAlignment",
        RpcTarget.AllBuffered,  // ← "AllBuffered" に変更
        // ...
    );
}
```

---

## 通信不可の根本原因トップ 3

### **1番目: AlignmentNetworkHub がシーンに存在しない**
- ❌ `FindFirstObjectByType<AlignmentNetworkHub>()` が `null` を返す
- ❌ すべての RPC が失敗する

### **2番目: PhotonView が正しく初期化されていない**
- ❌ PhotonView.ViewID が未設定
- ❌ RPC が送信できない

### **3番目: RpcTarget の設定が不適切**
- ❌ `RpcTarget.Others` で後接続クライアントが同期されない
- ❌ バッファが保存されない

---

## チェックリスト

### **シーン設定の確認**

```
シーン内に以下が存在するか確認:

□ GameObject "AlignmentNetworkHub"
  ├─ AlignmentNetworkHub.cs スクリプト
  ├─ PhotonView コンポーネント
  │  ├─ ViewID: 1 (または別の値)
  │  └─ Observed Components: (空でOK)
  └─ DontDestroyOnLoad: 設定済み

□ GameObject "LocalClientManager" または同等
  ├─ LocalClient.cs スクリプト
  ├─ SpatialAlignmentManager.cs スクリプト
  ├─ MeshAlignmentTool.cs スクリプト
  └─ その他のアラインメント関連スクリプト

□ Photon Network Instantiate Prefab
  ├─ "LocalClientCube" prefab
  ├─ NetworkedPlayer.cs
  ├─ PhotonView コンポーネント
  └─ Observed Components: [NetworkedPlayer]
```

### **Photon 設定の確認**

```
□ PhotonNetwork.AppID が両プロジェクトで同じ
□ 両プロジェクトが同じ Photon Server に接続
□ 両プロジェクトが同じルーム "MeshVRRoom" に接続
□ PhotonNetwork.InRoom が true を返している
```

---

## 推奨される修正手順

### **ステップ 1: AlignmentNetworkHub の自動作成**

```csharp
// LocalClient.cs の Start() に追加
void Start()
{
    // AlignmentNetworkHub が存在しない場合は作成
    AlignmentNetworkHub hub = FindFirstObjectByType<AlignmentNetworkHub>();
    if (hub == null)
    {
        Debug.LogWarning("AlignmentNetworkHub not found in scene! Creating one...");
        GameObject hubObj = new GameObject("AlignmentNetworkHub");
        hub = hubObj.AddComponent<AlignmentNetworkHub>();
        PhotonView pv = hubObj.AddComponent<PhotonView>();
        // NOTE: ViewID は Photon が自動割り当てするため、特に設定不要
    }
    
    // 既存のコード...
    if (isVRMode && vrCamera == null)
    {
        TryResolveVRCamera();
    }
    // ...
}
```

### **ステップ 2: RpcTarget の統一**

```csharp
// AlignmentNetworkHub.cs の BroadcastMeshAlignment を修正
public static void BroadcastMeshAlignment(Vector3 position, Quaternion rotation, Vector3 scale)
{
    if (instance == null || !PhotonNetwork.InRoom)
    {
        Debug.LogWarning("AlignmentNetworkHub: Not connected to broadcast mesh alignment");
        return;
    }

    instance.photonView.RPC(
        "ReceiveMeshAlignment",
        RpcTarget.AllBuffered,  // ← "Others" から "AllBuffered" に変更
        position.x, position.y, position.z,
        rotation.x, rotation.y, rotation.z, rotation.w,
        scale.x, scale.y, scale.z
    );
}
```

### **ステップ 3: デバッグログの追加**

```csharp
// AlignmentNetworkHub.cs の ReceiveSpatialReference に追加
[PunRPC]
void ReceiveSpatialReference(int playerId, float px, float py, float pz, float rx, float ry, float rz, float rw)
{
    Vector3 origin = new Vector3(px, py, pz);
    Quaternion rotation = new Quaternion(rx, ry, rz, rw);
    
    Debug.Log($"<color=cyan>[AlignmentHub] Received spatial reference from Player {playerId}: {origin}</color>");
    Debug.Log($"[AlignmentHub] OnSpatialAlignmentReceived subscribers: {OnSpatialAlignmentReceived?.GetInvocationList().Length ?? 0}");
    
    OnSpatialAlignmentReceived?.Invoke(playerId, origin, rotation);
}
```

---

## 診断用スクリプト

以下のスクリプトをシーンに追加して、通信状態をリアルタイム監視できます：

```csharp
using UnityEngine;
using Photon.Pun;

public class NetworkDiagnostics : MonoBehaviour
{
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== NETWORK DIAGNOSTICS ===", GUI.skin.box);
        GUILayout.Label($"PhotonNetwork.Connected: {PhotonNetwork.Connected}");
        GUILayout.Label($"PhotonNetwork.InRoom: {PhotonNetwork.InRoom}");
        GUILayout.Label($"Room: {(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "None")}");
        GUILayout.Label($"Players: {PhotonNetwork.PlayerList.Length}");
        
        AlignmentNetworkHub hub = FindFirstObjectByType<AlignmentNetworkHub>();
        GUILayout.Label($"AlignmentNetworkHub: {(hub != null ? "✓ Found" : "✗ NOT FOUND")}");
        
        if (hub != null)
        {
            PhotonView pv = hub.GetComponent<PhotonView>();
            GUILayout.Label($"  PhotonView ViewID: {(pv != null ? pv.ViewID.ToString() : "None")}");
            GUILayout.Label($"  IsReady: {AlignmentNetworkHub.IsReady}");
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

---

## まとめ

| 問題 | 影響 | 対応 |
|------|------|------|
| AlignmentNetworkHub が未作成 | すべての通信失敗 | シーンに追加または自動作成 |
| PhotonView 未設定 | RPC 送信失敗 | Inspector で設定 |
| RpcTarget.Others | 後接続クライアント未同期 | RpcTarget.AllBuffered に変更 |
| イベント購読タイミング | バッファ RPC 未受信 | 初期化順序を調整 |
| Photon 接続失敗 | 根本的に通信不可 | Photon AppID と接続設定を確認 |

---

**推奨アクション**: 上記の「ステップ 1 → ステップ 2 → ステップ 3」を順に実施し、診断スクリプトで各ステップ後に状態を確認してください。
