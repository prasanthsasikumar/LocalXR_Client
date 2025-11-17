# Eye Gaze / Face Mesh 受信診断 - クイックスタート

## 📋 実装完了内容

### ✅ 作成されたスクリプト

**NetworkDiagnosticsUI.cs** - ネットワーク診断UI

**機能:**
- 接続状態の確認
- プレイヤー検出
- PhotonFaceGazeReceiver の有無確認
- リアルタイム更新

---

## 🚀 セットアップ手順

### ステップ1: シーンに診断UIを追加

**LocalXR_Client 側:**

```
1. Hierarchy で右クリック
2. 新規 Empty GameObject 作成
3. "NetworkDiagnostics" に名前変更
4. NetworkDiagnosticsUI スクリプトをアタッチ
5. PhotonView コンポーネントをアタッチ
```

**RemoteXR_Client 側:**

```
同じように追加
```

### ステップ2: 実行と確認

```
1. LocalClient と RemoteClient を同時起動
2. 両方ウィンドウを並べて配置
3. 'D' キーを押す (診断UI 表示)
4. 接続状況を確認
```

---

## 📊 診断UI の読み方

### 接続状況セクション

```
=== CONNECTION STATUS ===
Status: ✓✓ In Room          ← OK（ルーム参加済み）
Connected: True             ← OK（接続中）
InRoom: True                ← OK（ルーム内）
Room: MeshVRRoom            ← ルーム名
Players: 2/4                ← 参加人数
LocalPlayer: LocalUser_1234 ← あなたのニックネーム
```

**期待値:**
```
Status: ✓✓ In Room (最高)
Status: ✓ Connected (接続中)
Status: ✗ Not Connected (エラー)
```

### プレイヤーセクション

```
=== PLAYERS IN SCENE ===
Total PhotonViews: 2

[OWN] LocalClientCube        ← 自分のプレイヤー

[REMOTE] RemoteUser_5678
  GameObject: RemotePlayer_RemoteUser_5678
  ✓ PhotonFaceGazeReceiver found   ← OK
  ✓ PhotonFaceGazeTransmitter found ← OK

Own players: 1
Remote players: 1
With FaceGazeReceiver: 1
```

**期待値:**
```
✓ PhotonFaceGazeReceiver found (最高)
✗ NO PhotonFaceGazeReceiver! (要修正)
```

### 診断セクション

```
=== DIAGNOSTICS ===
✓ Everything looks OK!              ← 完璧
```

or

```
✗ Not connected to room             ← 接続失敗
✗ No remote players found           ← プレイヤー未検出
✗ Remote players missing FaceGazeReceiver ← コンポーネント不足
```

---

## 🔍 トラブルシューティング

### 症状1: "Not connected to room"

**原因:**
- Photon 接続失敗
- ルーム参加失敗

**確認:**
```
1. Photon App ID が設定されているか？
2. ネットワーク接続は正常か？
3. Photon サーバーは稼働しているか？

コンソール確認:
  [LocalClient] Starting Photon connection...
  [LocalClient] connected to Master!
  [LocalClient] joined room: MeshVRRoom
```

### 症状2: "No remote players found"

**原因:**
- リモートプレイヤーが参加していない
- プレイヤーインスタンシエーションが失敗

**確認:**
```
1. 両クライアントが同じルームに参加しているか？
2. OnJoinedRoom() で player instantiation が実行されているか？

コンソール確認:
  [LocalClient] joined room: MeshVRRoom
  [RemoteClient] joined room: MeshVRRoom
  
両方のログが出ているか確認
```

### 症状3: "NO PhotonFaceGazeReceiver!"

**原因:**
- LocalClient.SetupRemotePlayerVisualization() が実行されていない
- PhotonFaceGazeReceiver が自動追加されていない

**確認:**
```
1. LocalClient.cs の OnJoinedRoom() を確認
   → SetupRemotePlayerVisualization() が呼ばれているか？

2. LocalClient.cs の SetupRemotePlayerVisualization() を確認
   → AddComponent<PhotonFaceGazeReceiver>() があるか？

コンソール確認:
  [LocalClient] Added PhotonFaceGazeReceiver to remote player: RemoteUser_XXX
```

**修正方法:**
```csharp
// LocalClient.cs の OnJoinedRoom() に以下を追加
private System.Collections.IEnumerator SetupRemotePlayerVisualization()
{
    yield return new WaitForSeconds(0.5f);
    
    PhotonView[] allViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
    
    foreach (PhotonView view in allViews)
    {
        if (view.IsMine) continue;
        
        if (view.Owner != null)
        {
            Debug.Log($"[LocalClient] Setting up receiver for {view.Owner.NickName}");
            
            PhotonFaceGazeReceiver receiver = view.GetComponent<PhotonFaceGazeReceiver>();
            if (receiver == null)
            {
                receiver = view.gameObject.AddComponent<PhotonFaceGazeReceiver>();
                receiver.showDebugInfo = true;
                Debug.Log($"[LocalClient] ✓ Added PhotonFaceGazeReceiver");
            }
        }
    }
}
```

---

## 💡 次のステップ

### データ受信確認（Face Mesh）

```csharp
// PhotonFaceGazeReceiver.cs に追加
void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
{
    if (!stream.IsWriting)
    {
        // Receiving data
        Debug.Log("[FaceGazeReceiver] Received Face Mesh data!");
        faceMeshCount++;
    }
}

// OnGUI で表示
GUILayout.Label($"Face Mesh packets: {faceMeshCount}");
```

### データ送信確認（Eye Gaze）

```csharp
// PhotonFaceGazeTransmitter.cs に追加
void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
{
    if (stream.IsWriting)
    {
        // Sending data
        Debug.Log("[FaceGazeTransmitter] Sending Gaze data!");
        gazeCount++;
    }
}

// OnGUI で表示
GUILayout.Label($"Gaze packets sent: {gazeCount}");
```

---

## 🎯 チェックリスト

- [ ] NetworkDiagnosticsUI をシーンに追加
- [ ] 両クライアントで D キー押下
- [ ] "✓✓ In Room" が表示される
- [ ] Remote players が検出される
- [ ] "✓ PhotonFaceGazeReceiver found" が表示される
- [ ] "✓ Everything looks OK!" が表示される

すべてチェックできたら、Eye Gaze / Face Mesh 通信準備完了！

---

## 📝 操作キー

```
D キー: 診断UI ON/OFF
R キー: 診断情報リフレッシュ

コンソール:
  'R' キーを押すと詳細なコンソール出力も表示
```

---

**実装状況**: ✅ 完了
**テスト対象**: LocalXR_Client, RemoteXR_Client
**期待結果**: "✓ Everything looks OK!" の表示

