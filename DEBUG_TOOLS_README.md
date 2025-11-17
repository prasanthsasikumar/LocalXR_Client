# LocalXR_Client デバッグツール使用ガイド

## 概要

LocalXR_Client(Meta Quest)でRemoteXR_Client(デスクトップ)から送信された視線データと表情データの受信状況を確認するための3つのデバッグスクリプトを作成しました。これらのスクリプトはPhotonViewに依存せず、`Debug.Log`とオンスクリーンUIを使用してデータフローを可視化します。

## システム構成

```
RemoteXR_Client (デスクトップ)
  ↓ LSL受信
  ├─ LslGazeReceiver (視線データ)
  └─ LslFaceMeshReceiver (表情データ)
  ↓ Photon送信
  PhotonFaceGazeTransmitter → AlignmentNetworkHub
  ↓ Photon Network (RPC)
  
LocalXR_Client (Meta Quest) ← このツールで監視
  ↓ Photon受信
  AlignmentNetworkHub (RPC受信・イベント)
  ↓
  PhotonFaceGazeReceiver (可視化・利用)
```

## 作成したスクリプト

### 1. **EyeGazeFaceDataDebugger.cs**
   - **目的**: RemoteXR_ClientからPhoton経由で送信された視線・表情データの受信を監視
   - **場所**: `Assets/Script/EyeGazeFaceDataDebugger.cs`
   - **機能**:
     - AlignmentNetworkHub経由で受信した視線データを監視
     - AlignmentNetworkHub経由で受信した表情データを監視
     - プレイヤーごとのデータ受信状況を追跡
     - 受信レート(Hz)の計算
     - データの鮮度チェック(最終受信からの経過時間)
     - 画面左上にリアルタイムステータス表示

### 2. **NetworkDataReceptionDebugger.cs**
   - **目的**: AlignmentNetworkHub経由で受信したネットワークデータを監視
   - **場所**: `Assets/Script/NetworkDataReceptionDebugger.cs`
   - **機能**:
     - RemoteXR_Clientから送信された視線・表情データの受信を監視
     - プレイヤーごとのデータ受信状況を追跡
     - データの鮮度チェック(最終受信時刻)
     - 画面中央にリアルタイムステータス表示

### 3. **DataFlowMonitor.cs**
   - **目的**: データフロー全体を統合監視
   - **場所**: `Assets/Script/DataFlowMonitor.cs`
   - **機能**:
     - RemoteXR_Client(LSL受信) → Photon送信 → LocalXR_Client(Photon受信)の全フローを可視化
     - 各ステージの動作状況を一覧表示
     - システム全体の健全性診断
     - 画面右下にデータフローダイアグラム表示

## セットアップ方法

### 方法1: 個別スクリプトを使用(推奨)

1. **新しいGameObjectを作成**:
   ```
   Hierarchy右クリック → Create Empty
   名前を "DebugTools" に変更
   ```

2. **スクリプトをアタッチ**:
   - `EyeGazeFaceDataDebugger.cs` をアタッチ
   - `NetworkDataReceptionDebugger.cs` をアタッチ
   - `DataFlowMonitor.cs` をアタッチ

3. **Inspector設定**:
   各スクリプトは自動検出機能を持っていますが、手動設定も可能です:
   
   **EyeGazeFaceDataDebugger**:
   - `Target Player Id`: -1(全プレイヤー監視、特定プレイヤーのみの場合はIDを指定)
   - `Show On Screen Debug`: ✓(オンスクリーンUI表示)
   - `Verbose Logging`: ✓(推奨 - 詳細ログ出力)
   - `Log Interval`: 1.0(ログ出力間隔・秒)
   
   **NetworkDataReceptionDebugger**:
   - `Show On Screen Debug`: ✓
   - `Verbose Logging`: ✓(推奨)
   - `Target Player Id`: -1(全プレイヤー監視)
   
   **DataFlowMonitor**:
   - すべて自動検出されます
   - `Show Flow Diagram`: ✓
   - `Show Detailed Info`: ✓

### 方法2: 既存のGameObjectに追加

既存のLSL関連GameObjectやLocalClientに直接スクリプトをアタッチすることもできます。

## 使用方法

### ステップ1: Unity Editor起動

1. LocalXR_Clientプロジェクトを開く
2. デバッグツールを設定したシーンを開く
3. Playボタンを押す

### ステップ2: Photonデータ受信確認

画面左上(EyeGazeFaceDataDebugger)に以下のように表示されます:

```
LocalXR_Client データ受信デバッグ
(RemoteXR_Client → Photon → LocalXR)

NetworkHub: ✓ 準備完了
RemoteXR_Client検出数: 1

【RemoteXR Player 2】
視線データ:
  状態: ✓ 受信中
  受信: 456回
  最終: 0.1秒前
  位置: (0.51, 0.68)
  瞳孔: 4.23
  レート: 30.2 Hz

表情データ:
  状態: ✓ 受信中
  受信: 234回
  最終: 0.1秒前
  ランドマーク: 68個
  レート: 28.5 Hz
```

### ステップ3: ネットワーク接続確認

画面中央(NetworkDataReceptionDebugger)に以下のように表示されます:

```
ネットワークデータ受信状況
稼働時間: 45.2秒
NetworkHub: ✓ 準備完了
プレイヤー数: 1

【プレイヤー 2】
表情データ:
  受信: 234回
  最終: 0.1秒前
  ランドマーク: 68個
視線データ:
  受信: 456回
  最終: 0.0秒前
  位置: (0.51, 0.68)
  瞳孔: 4.23
```

### ステップ4: データフロー全体確認

画面右下(DataFlowMonitor)に以下のように表示されます:

```
データフロー監視
【RemoteXR_Client側】
(LSL受信→Photon送信)
※Remote側で確認
─────────────────────
【Photonネットワーク】
NetworkHub: ✓
─────────────────────
【LocalXR_Client受信】
視線: 30.2 Hz
表情: 28.5 Hz
Receiver: ✓
```

## コンソールログの見方

### EyeGazeFaceDataDebugger

定期的に以下のようなログが出力されます:

```
========================================
[EyeGazeFaceDebug] RemoteXR_Clientからの受信状況
========================================
AlignmentNetworkHub状態: ✓ 準備完了
検出したRemoteXR_Client数: 1

【RemoteXR_Client PlayerID: 2】

  【視線データ】
    受信状態: ✓ 受信中
    受信回数: 1523
    最終受信: 0.12秒前
    現在位置: (0.512, 0.678)
    瞳孔サイズ: 4.234
    更新レート: 30.2 Hz

  【表情データ】
    受信状態: ✓ 受信中
    受信回数: 1432
    最終受信: 0.08秒前
    ランドマーク数: 68
    サンプル[0]: (0.451, 0.321, 0.892)
    更新レート: 28.5 Hz

【統計】
  総視線サンプル: 1523
  総表情サンプル: 1432
  視線更新レート: 30.2 Hz
  表情更新レート: 28.5 Hz
========================================
```

### NetworkDataReceptionDebugger

RemoteXR_Clientからデータを受信すると:

```
[NetworkDataDebug] ✓ 視線データ受信
  プレイヤーID: 2
  視線位置: (0.512, 0.678)
  瞳孔サイズ: 4.234
  受信回数: 123

[NetworkDataDebug] ✓ 表情データ受信
  プレイヤーID: 2
  ランドマーク数: 68
  受信回数: 234
```

## トラブルシューティング

### 問題1: RemoteXR_Clientを検出できない

**症状**: 
```
RemoteXR_Client検出数: 0
⚠ RemoteXR_Client未検出
```

**対処法**:
1. RemoteXR_Clientが起動しているか確認
2. 両方が同じPhotonルームに接続されているか確認
3. RemoteXR_ClientでLSLサーバーが起動しているか確認(`lsl_server.py`)
4. ファイアウォール設定を確認

### 問題2: データを受信できない

**症状**:
```
視線データ:
  状態: ✗ 未受信
```

**対処法**:
1. RemoteXR_Client側でLSLデータを正常に受信しているか確認
2. RemoteXR_Client側のPhotosFaceGazeTransmitterが動作しているか確認
3. `Verbose Logging` を有効にして詳細ログを確認

### 問題3: ネットワークデータを受信できない

**症状**:
```
NetworkHub: ✗ 未準備
プレイヤー数: 0
```

**対処法**:
1. Photonルームに接続されているか確認
2. RemoteXR_Clientが同じPhotonルームにいるか確認
3. `AlignmentNetworkHub` が正しくセットアップされているか確認
4. PhotonViewがアタッチされているか確認

### 問題4: データが古い

**症状**:
```
⚠ データが古い可能性があります (12.3秒)
```

**対処法**:
1. RemoteXR_Clientがまだデータを送信しているか確認
2. ネットワーク接続の安定性を確認
3. Photon送信レートを確認

## パフォーマンスへの影響

- **軽量**: これらのデバッグスクリプトはパフォーマンスへの影響が最小限です
- **ログ頻度**: `Log Interval` を調整してログ出力頻度を制御できます
- **オンスクリーンUI**: `Show On Screen Debug` をオフにすればGUI描画をスキップできます
- **詳細ログ**: `Verbose Logging` をオフにすれば詳細ログを抑制できます

## 本番環境での使用

デバッグが完了したら:

1. **デバッグスクリプトを無効化**:
   - Inspector で各スクリプトのチェックを外す
   - または GameObjectごと無効化

2. **スクリプトを削除**:
   - デバッグ完了後は完全に削除してもOK

## FAQ

**Q: スクリプトが自動検出できない**
A: Inspector で手動設定してください。Hierarchy内のLSL関連オブジェクトを探してドラッグ&ドロップ。

**Q: 複数のプレイヤーのデータを同時監視できる?**
A: はい。`NetworkDataReceptionDebugger` がすべてのプレイヤーを自動追跡します。

**Q: RemoteXR_Client側でも使える?**
A: はい。同じスクリプトをRemoteXR_Clientでも使用可能です。

**Q: Meta Questで動作する?**
A: はい。オンスクリーンUIはVR HMD内でも表示されます。

## 開発者向け情報

### イベントフロー

```
【RemoteXR_Client側】
LSL → LslGazeReceiver/LslFaceMeshReceiver
                      ↓
               PhotonFaceGazeTransmitter
                      ↓
              AlignmentNetworkHub (RPC送信)
                      ↓
            Photon Network (RPC)
                      ↓
【LocalXR_Client側】
              AlignmentNetworkHub (RPC受信・イベント発火)
                      ↓
            EyeGazeFaceDataDebugger (監視)
            NetworkDataReceptionDebugger (監視)
            DataFlowMonitor (監視)
                      ↓
               PhotonFaceGazeReceiver (表示・利用)
```

### 拡張方法

新しい監視項目を追加する場合:

1. 対応するレシーバークラスに public アクセサメソッドを追加
2. デバッガースクリプトで定期的にポーリング
3. GUI表示とログ出力を追加

### カスタマイズ

各スクリプトのInspectorパラメータで以下をカスタマイズ可能:
- ログ出力間隔
- 画面表示位置
- 詳細度
- データ検証閾値

## サポート

問題が解決しない場合は、以下の情報を含めて報告してください:
- Unity のバージョン
- コンソールログ全文
- 各デバッグスクリプトのスクリーンショット
- RemoteXR_Client の状態

---

**作成日**: 2025年11月17日
**対象プロジェクト**: LocalXR_Client
**依存関係**: LSL, Photon PUN 2, AlignmentNetworkHub
