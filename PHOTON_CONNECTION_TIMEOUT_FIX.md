# Photon接続タイムアウトエラー修正ガイド

## エラーメッセージ
```
Connection lost. OnStatusChanged to TimeoutDisconnect
Client state was: ConnectingToNameServer
SocketErrorCode: 0 AppOutOfFocus WinSock
```

---

## 🔍 エラーの意味

| コンポーネント | 説明 |
|---|---|
| `TimeoutDisconnect` | ネームサーバーへの接続がタイムアウト |
| `ConnectingToNameServer` | Photon ネームサーバーに接続中だった |
| `AppOutOfFocus` | アプリケーションがバックグラウンド状態 |
| `WinSock` | UDP ソケット通信エラー |

---

## 🛠️ 実装された修正

### 修正1: 接続前に設定確認
```csharp
// RemoteClient.cs / LocalClient.cs の Start()
if (!PhotonNetwork.IsConnected)
{
    Debug.Log("[Client] App ID configured: " + 
        (PhotonNetwork.PhotonServerSettings?.AppSettings?.AppIdRealtime != null));
    PhotonNetwork.ConnectUsingSettings();
}
```

**効果**: App ID 設定を可視化し、接続前に確認できます

### 修正2: 接続失敗時の自動再試行
```csharp
public override void OnDisconnected(DisconnectCause cause)
{
    Debug.LogError($"[Client] Disconnected! Cause: {cause}");
    
    if (cause == DisconnectCause.TimeoutDisconnect)
    {
        Debug.LogWarning("[Client] Timeout detected. Retrying in 3 seconds...");
        Invoke(nameof(RetryConnection), 3f);
    }
}

void RetryConnection()
{
    if (!PhotonNetwork.IsConnected)
    {
        Debug.Log("[Client] Retrying Photon connection...");
        PhotonNetwork.ConnectUsingSettings();
    }
}
```

**効果**: タイムアウト時に自動で3秒後に再接続を試みます

### 修正3: 定期的な接続状態監視
```csharp
// Update() メソッド
private float lastConnectionCheckTime = 0f;
private const float CONNECTION_CHECK_INTERVAL = 5f;

if (Time.time - lastConnectionCheckTime > CONNECTION_CHECK_INTERVAL)
{
    lastConnectionCheckTime = Time.time;
    
    if (!PhotonNetwork.IsConnected)
    {
        Debug.LogWarning("[Client] Connection lost! Status: " + 
            PhotonNetwork.NetworkClientState);
    }
}
```

**効果**: 5秒ごとに接続状態を確認、問題の早期検出

---

## ✅ ステップバイステップ 確認リスト

### 1️⃣ Photon App ID の確認

**Unity Editor で確認:**
1. `Window` → `Photon PUN 2` → `Highlight Server Settings`
2. `PhotonServerSettings` を確認
3. `AppSettings` → `AppIdRealtime` にコピー&ペーストされた App ID があるか確認

**コンソール出力:**
```
[LocalClient] App ID configured: True
[RemoteClient] App ID configured: True
```

### 2️⃣ インターネット接続の確認

```bash
# Mac/Linux ターミナル
ping 8.8.8.8              # Google DNS への接続確認

# ファイアウォール確認
# Settings → Network & Internet → Firewall
```

### 3️⃣ Photon ネームサーバーの疎通確認

```bash
# Photon ネームサーバーへの接続確認
nslookup ns.photonengine.com    # Mac/Linux
```

**期待される出力:**
```
Name: ns.photonengine.com
Addresses: 34.197.223.227 (など)
```

### 4️⃣ UDP 通信の確認

ファイアウォール設定:
- **Windows Firewall**: UDP 5055-5058 を許可
- **Mac Firewall**: Terminal → System Preferences で確認
- **VPN**: VPN を一時的に無効化してテスト

### 5️⃣ アプリケーションフォーカスの確認

```csharp
// Debug で確認
Debug.Log("App in Focus: " + Application.isFocused);
```

コンソール出力で `false` の場合、アプリがバックグラウンドにあります。

---

## 📊 期待される正常なシーケンス

```
[LocalClient Start]
  ↓
[LocalClient] App ID configured: True
[LocalClient] Starting Photon connection...
[LocalClient] Connecting with nickname: LocalUser_XXXX
  ↓
[LocalClient] connected to Master!
  ↓
[LocalClient] joined room: MeshVRRoom
[LocalClient] Players in room: 1
  ↓
✅ 通信成功

=== RemoteClient Start (同時実行) ===
[RemoteClient] App ID configured: True
[RemoteClient] Starting Photon connection...
[RemoteClient] Connecting with nickname: RemoteUser_YYYY
  ↓
[RemoteClient] connected to Master!
  ↓
[RemoteClient] joined room: MeshVRRoom
[RemoteClient] Players in room: 2
  ↓
✅ 両クライアント通信成功
```

---

## 🔴 トラブルシューティング

### 症状1: "App ID configured: False"

**原因**: Photon App ID が設定されていない

**対応**:
1. `Window` → `Photon PUN 2` → `Highlight Server Settings`
2. Dashboard: https://dashboard.photonengine.com にログイン
3. App ID をコピーして `PhotonServerSettings` に貼り付け

### 症状2: タイムアウトが頻繁に発生

**原因**: 
- インターネット接続が不安定
- Photon ネームサーバーが過負荷

**対応**:
```csharp
// リトライ間隔を延長
Invoke(nameof(RetryConnection), 5f);  // 3秒 → 5秒に変更

// リトライ回数制限
private int retryCount = 0;
private const int MAX_RETRIES = 5;

void RetryConnection()
{
    if (retryCount < MAX_RETRIES && !PhotonNetwork.IsConnected)
    {
        retryCount++;
        Debug.Log($"Retry attempt {retryCount}/{MAX_RETRIES}");
        PhotonNetwork.ConnectUsingSettings();
    }
    else
    {
        Debug.LogError("Max retries reached!");
    }
}
```

### 症状3: "AppOutOfFocus" が原因のタイムアウト

**原因**: アプリケーションがバックグラウンドにある

**対応**:
```csharp
// Start() メソッド
void Start()
{
    // フォーカス喪失時の処理設定
    Application.wantsToQuit += OnApplicationQuit;
    
    // 継続接続の設定
    DontDestroyOnLoad(gameObject);
}

bool OnApplicationQuit()
{
    Debug.Log("App quitting. Disconnecting from Photon...");
    PhotonNetwork.Disconnect();
    return true;
}
```

### 症状4: "WinSock" エラー

**原因**: Windows Socket エラー (Windows のみ)

**対応**:
```powershell
# Windows PowerShell (管理者実行)
ipconfig /all                  # ネットワーク設定確認
netsh winsock reset catalog   # Winsock リセット
```

---

## 📈 本番環境での推奨設定

```csharp
public class PhotonConnectionManager : MonoBehaviourPunCallbacks
{
    private int retryCount = 0;
    private const int MAX_RETRIES = 3;
    private const float RETRY_DELAY = 5f;
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Photon Disconnected: {cause}");
        
        switch (cause)
        {
            case DisconnectCause.TimeoutDisconnect:
                if (retryCount < MAX_RETRIES)
                {
                    retryCount++;
                    Debug.LogWarning($"Retry {retryCount}/{MAX_RETRIES} in {RETRY_DELAY}s");
                    Invoke(nameof(RetryConnection), RETRY_DELAY);
                }
                else
                {
                    Debug.LogError("Connection failed after max retries");
                    ShowConnectionErrorUI();
                }
                break;
                
            case DisconnectCause.DisconnectByServerLogicProperties:
            case DisconnectCause.InvalidAuthentication:
                Debug.LogError("Auth failed - check App ID");
                ShowAuthErrorUI();
                break;
                
            default:
                Debug.LogError($"Unexpected disconnect: {cause}");
                break;
        }
    }
    
    void RetryConnection()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }
    
    void ShowConnectionErrorUI()
    {
        // UI表示ロジック
    }
    
    void ShowAuthErrorUI()
    {
        // UI表示ロジック
    }
}
```

---

## 🎯 修正ファイル一覧

| ファイル | 修正内容 |
|---------|---------|
| `LocalClient.cs` | OnDisconnected, RetryConnection, 接続状態監視 |
| `RemoteClient.cs` | OnDisconnected, RetryConnection, 接続状態監視 |

---

## 📝 次のステップ

- [ ] Photon App ID が正しく設定されているか確認
- [ ] インターネット接続が安定しているか確認
- [ ] ファイアウォール設定で UDP が許可されているか確認
- [ ] LocalClient と RemoteClient を同時起動してコンソール出力を確認
- [ ] "connected to Master!" ログが両方に出現するか確認

---

**修正状況**: ✅ 完了  
**テスト対象**: LocalClient.cs, RemoteClient.cs  
**期待される結果**: 自動再接続により、タイムアウトから回復
