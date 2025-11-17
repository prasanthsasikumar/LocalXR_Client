# AlignmentNetworkHub リファクタリング - 技術文書

## 概要

LocalXR_Client プロジェクトのネットワークアラインメントシステムを以下の原則に基づいてリファクタリングしました：

1. **PhotonView の集約** - AlignmentNetworkHub のみが PhotonView を持つ
2. **イベントドリブンアーキテクチャ** - PunRPC から イベント/コールバックへの移行
3. **AlignmentMath の活用** - 座標変換の数学的な厳密性確保

---

## 1. AlignmentNetworkHub の役割

### **目的**
ネットワーク層の唯一の入口・出口として機能し、他のスクリプトがPhotonに直接依存しないようにする。

### **実装**
```csharp
public class AlignmentNetworkHub : MonoBehaviourPunCallbacks
{
    // Singleton インスタンス
    private static AlignmentNetworkHub instance;
    
    // イベント定義（他のスクリプトが購読）
    public static event Action<int, Vector3, Quaternion> OnSpatialAlignmentReceived;
    public static event Action<Vector3, Quaternion, Vector3> OnMeshAlignmentReceived;
    public static event Action<bool> OnMeshAlignmentModeChanged;
}
```

### **提供メソッド**
| メソッド | 用途 | 呼び出し元 |
|---------|------|----------|
| `BroadcastSpatialReference()` | メッシュ原点の送信 | SpatialAlignmentManager |
| `BroadcastMeshAlignment()` | メッシュ変換の同期 | MeshAlignmentTool |
| `BroadcastMeshAlignmentModeChanged()` | アラインメント モード切り替え | MeshAlignmentTool |
| `IsReady` (プロパティ) | ネットワーク準備状態 | 全スクリプト |

---

## 2. 座標変換の改善

### **AlignmentMath の活用**

SpatialAlignmentManager の `TransformFromPlayer()` メソッドが改善されました。

#### **以前の実装（簡略版）**
```csharp
// 単純な位置オフセット計算のみ
return theirPosition + alignment.positionOffset;
```

#### **改善された実装（数学的に厳密）**
```csharp
return AlignmentMath.TransformPositionToLocal(
    remotePosition,                          // リモート座標系の位置
    alignment.meshOrigin,                    // リモート メッシュ原点
    Quaternion.identity,                     // リモート参照回転
    meshReferencePoint.position,             // ローカル メッシュ原点
    meshReferencePoint.rotation,             // ローカル メッシュ回転
    scaleMultiplier);                        // スケール係数
```

### **AlignmentMath で実装される変換式**
```
ローカル位置 = ローカル原点 + 回転オフセット × (リモート位置 - リモート原点) × スケール

数学記号で:
l = O_l + Q_l * Q_r^{-1} * (r - O_r) × s

ここで:
- l: ローカル座標系の位置
- r: リモート座標系の位置
- O_l: ローカル参照原点
- O_r: リモート参照原点
- Q_l: ローカル参照回転
- Q_r: リモート参照回転
- s: スケール係数
```

---

## 3. イベントドリブンアーキテクチャの利点

### **イベントの流れ**

```
[ローカルクライアント]
SpatialAlignmentManager
    ↓ (マッシュ原点を取得)
AlignmentNetworkHub.BroadcastSpatialReference()
    ↓ (Photon RPC)
[リモートクライアント]
AlignmentNetworkHub.ReceiveSpatialReference() [PunRPC]
    ↓ (イベント発行)
OnSpatialAlignmentReceived?.Invoke()
    ↓ (購読者に通知)
SpatialAlignmentManager.HandleRemoteSpatialAlignment()
    ↓ (処理)
playerAlignments[playerId] = new AlignmentData(...)
```

### **メリット**

1. **疎結合（Loose Coupling）**
   - SpatialAlignmentManager が PhotonView に依存しない
   - AlignmentNetworkHub の実装変更が他に影響しない

2. **複数の購読者対応**
   - 複数のスクリプトが同じイベントを購読可能
   - 新しいリスナーの追加が容易

3. **テスト容易性**
   - 各スクリプトを独立してテスト可能
   - モック AlignmentNetworkHub を作成可能

4. **保守性向上**
   - ネットワーク通信ロジックが一箇所に集約
   - 変更の影響範囲が明確

---

## 4. 各スクリプトの責務

### **AlignmentNetworkHub**
```
責務:
- PhotonView の所有・管理
- RPC の送受信
- イベント発行
```

### **SpatialAlignmentManager**
```
責務:
- 座標変換規則の決定
- リモートクライアント データの解釈
- 座標系のアライメント計算
```

### **MeshAlignmentTool**
```
責務:
- メッシュ変換の UI/制御
- 保存/読み込み処理
- ユーザー入力処理
```

### **NetworkedPlayer**
```
責務:
- プレイヤー位置/回転の同期
- 座標変換の適用
- スムーズな補間
```

### **LocalClient**
```
責務:
- クライアント初期化
- Photon 接続管理
- VR/デスク モード切り替え
```

---

## 5. 依存関係図

### **変更前**
```
┌─────────────────────┐
│LocalClient          │
│(MonoBehaviourPun)   │
├─────────────────────┤
│+ photonView         │
│+ RPC() calls        │
└──────────────┬──────┘
               │
               ├──→ MeshAlignmentTool
               │    (MonoBehaviourPun)
               │    ├─ photonView ─┐
               │    └─ RPC calls   │
               │                   │
               ├──→ SpatialAlignmentManager
               │    (MonoBehaviourPun)
               │    ├─ photonView ─┤
               │    └─ RPC calls   │
               │                   ↓
               │                [Photon]
               │                   ↑
               └──→ NetworkedPlayer
                    (MonoBehaviourPun)
                    └─ photonView
```

### **変更後**
```
┌─────────────────────────────────────┐
│LocalClient                          │
│(MonoBehaviourPunCallbacks)          │
├─────────────────────────────────────┤
│- SpatialAlignmentManager 参照        │
│- MeshAlignmentTool 参照              │
└─────────────────────────────────────┘
         ↓         ↓         ↓
    [API Call] [API Call] [Subscribe]
         ↓         ↓         ↓
┌─────────────────────────────────────┐
│AlignmentNetworkHub (Singleton)      │
│(MonoBehaviourPunCallbacks)          │
├─────────────────────────────────────┤
│+ photonView [唯一のネットワーク層] │
│+ OnSpatialAlignmentReceived         │
│+ OnMeshAlignmentReceived            │
│+ OnMeshAlignmentModeChanged         │
│+ BroadcastSpatialReference()        │
│+ BroadcastMeshAlignment()           │
│+ BroadcastMeshAlignmentModeChanged()│
└─────────────┬───────────────────────┘
              │ RPC
              ↓
          [Photon]
              ↑
              │ RPC
┌─────────────────────────────────────┐
│SpatialAlignmentManager              │
│(MonoBehaviour)                      │
├─────────────────────────────────────┤
│- Subscribe to OnSpatialAlignmentReceived │
│- HandleRemoteSpatialAlignment()    │
│- TransformFromPlayer() [AlignmentMath] │
└─────────────────────────────────────┘
              ↑
              │ Subscribe
┌─────────────────────────────────────┐
│MeshAlignmentTool                    │
│(MonoBehaviour)                      │
├─────────────────────────────────────┤
│- Subscribe to OnMeshAlignmentReceived │
│- HandleRemoteMeshAlignment()       │
└─────────────────────────────────────┘
              ↑
              │ Subscribe
┌─────────────────────────────────────┐
│NetworkedPlayer                      │
│(MonoBehaviourPun + IPunObservable) │
├─────────────────────────────────────┤
│- SpatialAlignmentManager参照        │
│- TransformFromPlayer()使用         │
└─────────────────────────────────────┘
```

---

## 6. 実装チェックリスト

### **AlignmentNetworkHub**
- [x] Singleton パターンの実装
- [x] PhotonView コンポーネント所有
- [x] ReceiveSpatialReference() PunRPC メソッド
- [x] ReceiveMeshAlignment() PunRPC メソッド
- [x] ReceiveMeshAlignmentModeChanged() PunRPC メソッド
- [x] イベント定義と発行

### **SpatialAlignmentManager**
- [x] MonoBehaviourPun への変更
- [x] AlignmentNetworkHub イベント購読
- [x] HandleRemoteSpatialAlignment() 実装
- [x] AlignmentMath 活用の TransformFromPlayer()
- [x] OnEnable/OnDisable での購読管理

### **MeshAlignmentTool**
- [x] MonoBehaviourPun への変更
- [x] AlignmentNetworkHub イベント購読
- [x] HandleRemoteMeshAlignment() 実装
- [x] HandleRemoteMeshAlignmentModeChanged() 実装
- [x] SaveAlignment() の AlignmentNetworkHub.BroadcastMeshAlignment() 呼び出し
- [x] OnDestroy での購読解除

### **その他**
- [x] NetworkedPlayer は変更なし（既に AlignmentManager 参照）
- [x] LocalClient は変更なし（MonoBehaviourPunCallbacks 必要）
- [x] コンパイルエラー なし

---

## 7. テストシナリオ

### **シナリオ 1: 座標変換の正確性**
```
1. LocalClient でメッシュを (0, 0, 0) に配置
2. RemoteClient でメッシュを (5, 0, 0) に配置
3. LocalClient でプレイヤーを (3, 0, 0) に移動
4. RemoteClient 画面でプレイヤーが (-2, 0, 0) に表示されることを確認
```

### **シナリオ 2: メッシュ同期**
```
1. VR クライアント(LocalClient) でメッシュをアラインメント モード
2. メッシュを移動・回転・スケール変更
3. ボタンで "Save"
4. MacBook クライアント(RemoteClient) でメッシュが同じ位置に更新されることを確認
```

### **シナリオ 3: イベント通知**
```
1. SpatialAlignmentManager が OnSpatialAlignmentReceived イベント受信
2. MeshAlignmentTool が OnMeshAlignmentReceived イベント受信
3. 各リスナーが正しく処理されることを確認
```

---

## 参考資料

- `AlignmentMath.cs`: 座標変換の数学実装
- `AlignmentNetworkHub.cs`: ネットワーク層の集約
- `SpatialAlignmentManager.cs`: 座標系管理
- `MeshAlignmentTool.cs`: メッシュ操作
- `NetworkedPlayer.cs`: プレイヤー同期

---

**作成日**: 2025年11月17日
**対象**: LocalXR_Client (feature-connection)
**ステータス**: リファクタリング完了 ✅
