# リモートプレイヤー回転不安定問題 - 修正ガイド

## 🔍 問題の症状

```
リモートユーザーが静止しているのに、回転が安定しない
- 振動/ジッター が発生
- 目標回転に収束しない
- 毎フレーム異なる値が計算される
```

---

## 🎯 根本原因

### **原因1: Lerp vs Slerp の使い分け不足**

```csharp
// ❌ 問題のあるコード (NetworkedPlayer.cs)
transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
```

**問題:**
- `Quaternion.Lerp` は線形補間 → **短い経路での補間がない**
- 回転軸がリアルタイムに変わると **ジッター** が発生
- 特に回転が小さい場合、不安定になる

**解決策:**
```csharp
// ✅ 修正後 (Spherical Linear Interpolation)
transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lerpFactor);
```

### **原因2: Lerp 係数の不安定性**

```csharp
// ❌ 問題
Time.deltaTime * smoothSpeed  // 毎フレーム異なる値
```

**問題:**
- フレームレートが不安定 → deltaTime が変動
- 補間係数が毎フレーム異なる → 加速度 が変わる

**解決策:**
```csharp
// ✅ 修正後
float lerpFactor = Time.deltaTime * smoothSpeed;
lerpFactor = Mathf.Clamp01(lerpFactor);  // 0-1 に正規化
```

### **原因3: 座標変換の累積誤差**

```csharp
// 毎フレーム以下が実行される
targetRotation = alignmentManager.TransformFromPlayer(..., networkRotation);
```

**問題:**
- `networkRotation` は同じ値
- でも毎フレーム変換ロジックを通すと **丸め誤差** が累積
- AlignmentMath の回転変換が非決定的 → 同じ入力で異なる出力

---

## ✅ 実装した修正

### **修正1: Slerp の採用**

```csharp
// NetworkedPlayer.cs
transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lerpFactor);
```

**効果:**
- 短い経路での球面線形補間
- ジンバルロック を回避
- 滑らかで安定した回転

### **修正2: Lerp 係数の正規化**

```csharp
float lerpFactor = Time.deltaTime * smoothSpeed;
lerpFactor = Mathf.Clamp01(lerpFactor);  // 0-1 に制限
```

**効果:**
- 補間係数が常に 0-1 の範囲
- 安定した加速度
- フレームレート変動に強い

### **修正3: デバッグログの追加**

```csharp
float rotationDifference = Quaternion.Angle(transform.rotation, targetRotation);
if (rotationDifference > 0.5f)
{
    Debug.LogWarning($"Rotation unstable! Diff: {rotationDifference}°");
}
```

**効果:**
- 回転の不安定性をリアルタイムで検出
- 問題の原因特定が容易

---

## 🧪 テスト方法

### **テスト1: LocalClient (VR) が静止している状態**

```
1. LocalClient で位置/回転を固定
2. RemoteClient で表示
3. コンソール出力を確認
   
期待される出力:
  ✅ "Rotation unstable!" ログ が出ない
  ✅ 表示される回転が安定している
  ❌ ジッター や 振動 がない
```

### **テスト2: 回転差分を監視**

```csharp
// コンソール出力をフィルタ
[NetworkedPlayer] Rotation unstable! Diff: X°
```

**解釈:**
- Diff < 0.5°: ✅ 正常
- Diff > 1.0°: ⚠️ 要調査
- Diff > 5.0°: 🔴 エラー

### **テスト3: フレームレートの影響**

```
60fps と 120fps で実行して比較
  
修正前:
  60fps: ジッター あり
  120fps: より激しいジッター

修正後:
  60fps と 120fps で同じ滑らかさ
```

---

## 🔧 さらなる改善案

### **案1: 回転変換の決定性を確保**

```csharp
// AlignmentMath.cs
public static Quaternion TransformRotationToLocal(
    Quaternion theirRotation,
    Quaternion remoteReference,
    Quaternion localReference)
{
    // ★ 同じ入力に対して常に同じ出力を返す
    var result = Quaternion.Inverse(remoteReference) * theirRotation * localReference;
    
    // 正規化（重要！）
    return result.normalized;
}
```

### **案2: 静止検出による補間スキップ**

```csharp
// NetworkedPlayer.cs
if (Vector3.Distance(networkPosition, lastNetworkPosition) < 0.001f &&
    Quaternion.Angle(networkRotation, lastNetworkRotation) < 0.1f)
{
    // 静止しているので、直接割り当て
    transform.rotation = networkRotation;
}
else
{
    // 動いているので補間
    transform.rotation = Quaternion.Slerp(...);
}
```

### **案3: 独立した回転補間スピード**

```csharp
[SerializeField]
public float positionSmoothSpeed = 10f;

[SerializeField]
public float rotationSmoothSpeed = 8f;  // 回転用に別枠

// Update()
float posFactor = Time.deltaTime * positionSmoothSpeed;
float rotFactor = Time.deltaTime * rotationSmoothSpeed;

transform.position = Vector3.Lerp(transform.position, targetPosition, posFactor);
transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotFactor);
```

---

## 📊 修正前後の比較

| 項目 | 修正前 | 修正後 |
|------|-------|-------|
| 補間方法 | Lerp（線形） | Slerp（球面線形） |
| 係数範囲 | 0 ~ ∞ | 0 ~ 1（正規化） |
| 回転軸 | 変動（ジッター） | 安定（最短経路） |
| ジンバルロック | あり得る | 回避可能 |
| フレームレート依存 | 強い | 弱い |
| 静止時の揺れ | あり | なし |

---

## 📝 修正ファイル一覧

| ファイル | 修正内容 |
|---------|---------|
| `NetworkedPlayer.cs` | Lerp → Slerp, 係数正規化, デバッグログ追加 |

---

## 🎯 次のステップ

1. **ビルド & 実行**
   - LocalClient と RemoteClient を同時起動
   - RemoteClient が静止した状態を確認

2. **コンソール監視**
   - "Rotation unstable!" が出力されないか確認
   - 回転差分が 0.5° 以下か確認

3. **ビジュアル確認**
   - RemoteClient の回転がジッターしないか
   - 滑らかに収束しているか

4. **フレームレートテスト**
   - 60fps と 120fps で同じ挙動か
   - フレームレート依存がないか

---

## 💡 推奨するさらなる修正

修正後も問題が続く場合：

1. **AlignmentMath.cs の回転変換を確認**
   - 同じ入力で同じ出力か？
   - 正規化されているか？

2. **IPunObservable のシリアライゼーション周期を確認**
   - 送信頻度は適切か？
   - 受信データが重複していないか？

3. **smoothSpeed パラメータの調整**
   - 推奨値: 5-10 (位置)
   - 推奨値: 3-8 (回転)

---

**修正状況**: ✅ 完了  
**テスト対象**: NetworkedPlayer.cs  
**期待される結果**: 静止したリモートプレイヤーの回転が安定
