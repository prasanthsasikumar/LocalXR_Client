using UnityEngine;
using System.Text;

/// <summary>
/// LocalXR_Clientで視線データと表情データの受信状況をデバッグするスクリプト
/// PhotonViewに依存せず、AlignmentNetworkHub経由でRemoteXR_Clientから送信されたデータを監視
/// 
/// 使用方法:
/// 1. このスクリプトをLocalXR_Clientの任意のGameObjectにアタッチ
/// 2. 実行時にConsoleログとオンスクリーンデバッグUIで受信状況を確認
/// 
/// データフロー:
/// RemoteXR_Client(LSL受信) → Photon送信 → LocalXR_Client(このスクリプトで監視)
/// </summary>
public class EyeGazeFaceDataDebugger : MonoBehaviour
{
    [Header("Network Settings")]
    [Tooltip("監視する特定のプレイヤーID(-1で全プレイヤー)")]
    public int targetPlayerId = -1;

    [Header("Debug Settings")]
    [Tooltip("Consoleログの出力間隔(秒)")]
    [Range(0.1f, 10f)]
    public float logInterval = 1f;
    
    [Tooltip("画面上にデバッグ情報を表示するか")]
    public bool showOnScreenDebug = true;
    
    [Tooltip("詳細なログを出力するか")]
    public bool verboseLogging = false;
    
    [Tooltip("視線データの履歴を保持する数")]
    [Range(10, 1000)]
    public int gazeHistorySize = 100;
    
    [Tooltip("表情データの受信頻度をカウントする")]
    public bool trackReceiveRate = true;

    [Header("Validation Thresholds")]
    [Tooltip("視線データが有効と判断する最小変化量")]
    [Range(0.001f, 0.1f)]
    public float gazeChangeThreshold = 0.01f;
    
    [Tooltip("表情データが有効と判断する最小変化量")]
    [Range(0.001f, 1f)]
    public float faceChangeThreshold = 0.1f;

    // 受信データの履歴
    private class PlayerDataHistory
    {
        public int playerId;
        public int faceLandmarksReceived;
        public int gazeDataReceived;
        public float lastFaceDataTime;
        public float lastGazeDataTime;
        public Vector3[] lastFaceLandmarks;
        public Vector2 lastGazePosition;
        public float lastPupilSize;
        public bool hasFaceData;
        public bool hasGazeData;
        
        public PlayerDataHistory(int id)
        {
            playerId = id;
        }
    }
    
    // 内部状態
    private float logTimer;
    private System.Collections.Generic.Dictionary<int, PlayerDataHistory> playerHistories = 
        new System.Collections.Generic.Dictionary<int, PlayerDataHistory>();
    
    // 統計情報
    private int totalGazeSamples;
    private int totalFaceSamples;
    private float gazeUpdateRate;
    private float faceUpdateRate;
    private float lastRateUpdateTime;
    private int gazeUpdateCount;
    private int faceUpdateCount;
    
    // GUI用の状態
    private GUIStyle headerStyle;
    private GUIStyle normalStyle;
    private GUIStyle warningStyle;
    private GUIStyle successStyle;
    private bool stylesInitialized = false;

    private void Start()
    {
        // AlignmentNetworkHubのイベントに登録
        AlignmentNetworkHub.OnFaceLandmarksReceived += OnFaceLandmarksReceived;
        AlignmentNetworkHub.OnGazeDataReceived += OnGazeDataReceived;
        
        lastRateUpdateTime = Time.time;
        
        Debug.Log("========================================");
        Debug.Log("[EyeGazeFaceDebug] LocalXR_Client デバッグスクリプト開始");
        Debug.Log("[EyeGazeFaceDebug] RemoteXR_Clientからのデータ受信を監視します");
        Debug.Log($"[EyeGazeFaceDebug] AlignmentNetworkHub状態: {(AlignmentNetworkHub.IsReady ? "準備完了" : "未準備")}");
        Debug.Log("========================================");
    }

    private void Update()
    {
        // ログ出力タイマー
        logTimer += Time.deltaTime;
        
        // レート計算(1秒ごと)
        if (trackReceiveRate && Time.time - lastRateUpdateTime >= 1f)
        {
            float deltaTime = Time.time - lastRateUpdateTime;
            gazeUpdateRate = gazeUpdateCount / deltaTime;
            faceUpdateRate = faceUpdateCount / deltaTime;
            
            gazeUpdateCount = 0;
            faceUpdateCount = 0;
            lastRateUpdateTime = Time.time;
        }
        
        // 定期的なログ出力
        if (logTimer >= logInterval)
        {
            LogDebugInfo();
            logTimer = 0f;
        }
    }

    #region AlignmentNetworkHub Event Handlers

    private void OnFaceLandmarksReceived(int senderId, Vector3[] landmarks, bool hasData)
    {
        // 特定のプレイヤーのみ監視する場合
        if (targetPlayerId != -1 && senderId != targetPlayerId)
            return;
        
        // プレイヤー履歴を取得または作成
        if (!playerHistories.ContainsKey(senderId))
        {
            playerHistories[senderId] = new PlayerDataHistory(senderId);
            Debug.Log($"<color=cyan>[EyeGazeFaceDebug] RemoteXR_Client検出: PlayerID={senderId}</color>");
        }
        
        PlayerDataHistory history = playerHistories[senderId];
        history.faceLandmarksReceived++;
        history.lastFaceDataTime = Time.time;
        history.hasFaceData = hasData;
        
        totalFaceSamples++;
        faceUpdateCount++;
        
        if (hasData && landmarks != null)
        {
            history.lastFaceLandmarks = landmarks;
            
            if (history.faceLandmarksReceived == 1)
            {
                Debug.Log($"<color=green>[EyeGazeFaceDebug] ✓ 初回表情データ受信成功!</color>");
                Debug.Log($"  RemoteXR_Client PlayerID: {senderId}");
                Debug.Log($"  ランドマーク数: {landmarks.Length}");
                if (landmarks.Length > 0)
                {
                    Debug.Log($"  サンプル[0]: {landmarks[0]}");
                }
            }
            else if (verboseLogging)
            {
                Debug.Log($"[EyeGazeFaceDebug] 表情データ受信 #{history.faceLandmarksReceived} from Player {senderId}");
            }
        }
    }

    private void OnGazeDataReceived(int senderId, Vector2 gazePosition, float pupilSize, bool hasData)
    {
        // 特定のプレイヤーのみ監視する場合
        if (targetPlayerId != -1 && senderId != targetPlayerId)
            return;
        
        // プレイヤー履歴を取得または作成
        if (!playerHistories.ContainsKey(senderId))
        {
            playerHistories[senderId] = new PlayerDataHistory(senderId);
            Debug.Log($"<color=cyan>[EyeGazeFaceDebug] RemoteXR_Client検出: PlayerID={senderId}</color>");
        }
        
        PlayerDataHistory history = playerHistories[senderId];
        history.gazeDataReceived++;
        history.lastGazeDataTime = Time.time;
        history.hasGazeData = hasData;
        
        totalGazeSamples++;
        gazeUpdateCount++;
        
        if (hasData)
        {
            history.lastGazePosition = gazePosition;
            history.lastPupilSize = pupilSize;
            
            if (history.gazeDataReceived == 1)
            {
                Debug.Log($"<color=green>[EyeGazeFaceDebug] ✓ 初回視線データ受信成功!</color>");
                Debug.Log($"  RemoteXR_Client PlayerID: {senderId}");
                Debug.Log($"  視線位置: ({gazePosition.x:F3}, {gazePosition.y:F3})");
                Debug.Log($"  瞳孔サイズ: {pupilSize:F3}");
            }
            else if (verboseLogging)
            {
                Debug.Log($"[EyeGazeFaceDebug] 視線データ受信 #{history.gazeDataReceived}: ({gazePosition.x:F3}, {gazePosition.y:F3})");
            }
        }
    }

    #endregion

    private void LogDebugInfo()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("[EyeGazeFaceDebug] RemoteXR_Clientからの受信状況");
        sb.AppendLine("========================================");
        sb.AppendLine($"AlignmentNetworkHub状態: {(AlignmentNetworkHub.IsReady ? "✓ 準備完了" : "✗ 未準備")}");
        sb.AppendLine($"検出したRemoteXR_Client数: {playerHistories.Count}");
        sb.AppendLine();
        
        if (playerHistories.Count == 0)
        {
            sb.AppendLine("⚠ まだRemoteXR_Clientからデータを受信していません");
            sb.AppendLine();
            sb.AppendLine("確認事項:");
            sb.AppendLine("  1. RemoteXR_Clientが起動しているか");
            sb.AppendLine("  2. Photonルームに接続されているか");
            sb.AppendLine("  3. RemoteXR_ClientでLSLデータを受信し送信しているか");
        }
        else
        {
            foreach (var kvp in playerHistories)
            {
                PlayerDataHistory history = kvp.Value;
                
                sb.AppendLine($"【RemoteXR_Client PlayerID: {history.playerId}】");
                
                // 視線データ
                sb.AppendLine("\n  【視線データ】");
                sb.AppendLine($"    受信状態: {(history.hasGazeData ? "✓ 受信中" : "✗ 未受信")}");
                sb.AppendLine($"    受信回数: {history.gazeDataReceived}");
                
                if (history.hasGazeData)
                {
                    float gazeAge = Time.time - history.lastGazeDataTime;
                    sb.AppendLine($"    最終受信: {gazeAge:F2}秒前");
                    sb.AppendLine($"    現在位置: ({history.lastGazePosition.x:F3}, {history.lastGazePosition.y:F3})");
                    sb.AppendLine($"    瞳孔サイズ: {history.lastPupilSize:F3}");
                    
                    if (trackReceiveRate)
                    {
                        sb.AppendLine($"    更新レート: {gazeUpdateRate:F1} Hz");
                    }
                    
                    if (gazeAge > 5f)
                    {
                        sb.AppendLine($"    <color=yellow>⚠ データが古い ({gazeAge:F1}秒)</color>");
                    }
                }
                
                // 表情データ
                sb.AppendLine("\n  【表情データ】");
                sb.AppendLine($"    受信状態: {(history.hasFaceData ? "✓ 受信中" : "✗ 未受信")}");
                sb.AppendLine($"    受信回数: {history.faceLandmarksReceived}");
                
                if (history.hasFaceData && history.lastFaceLandmarks != null)
                {
                    float faceAge = Time.time - history.lastFaceDataTime;
                    sb.AppendLine($"    最終受信: {faceAge:F2}秒前");
                    sb.AppendLine($"    ランドマーク数: {history.lastFaceLandmarks.Length}");
                    
                    if (history.lastFaceLandmarks.Length > 0)
                    {
                        sb.AppendLine($"    サンプル[0]: {history.lastFaceLandmarks[0]}");
                    }
                    
                    if (trackReceiveRate)
                    {
                        sb.AppendLine($"    更新レート: {faceUpdateRate:F1} Hz");
                    }
                    
                    if (faceAge > 5f)
                    {
                        sb.AppendLine($"    <color=yellow>⚠ データが古い ({faceAge:F1}秒)</color>");
                    }
                }
                
                sb.AppendLine();
            }
            
            // 統計
            sb.AppendLine("【統計】");
            sb.AppendLine($"  総視線サンプル: {totalGazeSamples}");
            sb.AppendLine($"  総表情サンプル: {totalFaceSamples}");
            if (trackReceiveRate)
            {
                sb.AppendLine($"  視線更新レート: {gazeUpdateRate:F1} Hz");
                sb.AppendLine($"  表情更新レート: {faceUpdateRate:F1} Hz");
            }
        }
        
        sb.AppendLine("\n========================================");
        
        Debug.Log(sb.ToString());
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug)
            return;
        
        InitializeGUIStyles();
        
        // 画面左上にデバッグパネルを表示
        float panelWidth = 450f;
        float panelHeight = 500f;
        float margin = 10f;
        
        GUILayout.BeginArea(new Rect(margin, margin, panelWidth, panelHeight));
        GUILayout.BeginVertical("box");
        
        // ヘッダー
        GUILayout.Label("LocalXR_Client データ受信デバッグ", headerStyle);
        GUILayout.Label("(RemoteXR_Client → Photon → LocalXR)", normalStyle);
        GUILayout.Space(10);
        
        // ネットワーク状態
        bool hubReady = AlignmentNetworkHub.IsReady;
        GUILayout.Label($"NetworkHub: {(hubReady ? "✓ 準備完了" : "✗ 未準備")}", 
            hubReady ? successStyle : warningStyle);
        
        GUILayout.Label($"RemoteXR_Client検出数: {playerHistories.Count}", normalStyle);
        GUILayout.Space(10);
        
        if (playerHistories.Count == 0)
        {
            GUILayout.Label("⚠ RemoteXR_Client未検出", warningStyle);
            GUILayout.Space(5);
            GUILayout.Label("確認事項:", normalStyle);
            GUILayout.Label("• RemoteXR_Clientが起動中か", normalStyle);
            GUILayout.Label("• Photonルームに接続中か", normalStyle);
            GUILayout.Label("• LSLデータを送信中か", normalStyle);
        }
        else
        {
            foreach (var kvp in playerHistories)
            {
                DrawPlayerDataPanel(kvp.Value);
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawPlayerDataPanel(PlayerDataHistory history)
    {
        GUILayout.Label($"【RemoteXR Player {history.playerId}】", headerStyle);
        
        // 視線データ
        GUILayout.Label("視線データ:", normalStyle);
        GUILayout.Label($"  状態: {(history.hasGazeData ? "✓ 受信中" : "✗ 未受信")}", 
            history.hasGazeData ? successStyle : warningStyle);
        GUILayout.Label($"  受信: {history.gazeDataReceived}回", normalStyle);
        
        if (history.hasGazeData)
        {
            float gazeAge = Time.time - history.lastGazeDataTime;
            GUIStyle ageStyle = gazeAge > 5f ? warningStyle : successStyle;
            GUILayout.Label($"  最終: {gazeAge:F1}秒前", ageStyle);
            GUILayout.Label($"  位置: ({history.lastGazePosition.x:F2}, {history.lastGazePosition.y:F2})", normalStyle);
            GUILayout.Label($"  瞳孔: {history.lastPupilSize:F2}", normalStyle);
            
            if (trackReceiveRate)
            {
                GUILayout.Label($"  レート: {gazeUpdateRate:F1} Hz", normalStyle);
            }
        }
        
        GUILayout.Space(5);
        
        // 表情データ
        GUILayout.Label("表情データ:", normalStyle);
        GUILayout.Label($"  状態: {(history.hasFaceData ? "✓ 受信中" : "✗ 未受信")}", 
            history.hasFaceData ? successStyle : warningStyle);
        GUILayout.Label($"  受信: {history.faceLandmarksReceived}回", normalStyle);
        
        if (history.hasFaceData && history.lastFaceLandmarks != null)
        {
            float faceAge = Time.time - history.lastFaceDataTime;
            GUIStyle ageStyle = faceAge > 5f ? warningStyle : successStyle;
            GUILayout.Label($"  最終: {faceAge:F1}秒前", ageStyle);
            GUILayout.Label($"  ランドマーク: {history.lastFaceLandmarks.Length}個", normalStyle);
            
            if (trackReceiveRate)
            {
                GUILayout.Label($"  レート: {faceUpdateRate:F1} Hz", normalStyle);
            }
        }
        
        GUILayout.Space(10);
    }

    private void InitializeGUIStyles()
    {
        if (stylesInitialized)
            return;
        
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };
        
        normalStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white }
        };
        
        warningStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.yellow }
        };
        
        successStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.green }
        };
        
        stylesInitialized = true;
    }

    private void OnDestroy()
    {
        // イベント登録解除
        AlignmentNetworkHub.OnFaceLandmarksReceived -= OnFaceLandmarksReceived;
        AlignmentNetworkHub.OnGazeDataReceived -= OnGazeDataReceived;
        
        Debug.Log("========================================");
        Debug.Log("[EyeGazeFaceDebug] 最終統計");
        
        foreach (var kvp in playerHistories)
        {
            PlayerDataHistory history = kvp.Value;
            Debug.Log($"RemoteXR_Client PlayerID {history.playerId}:");
            Debug.Log($"  視線データ: {history.gazeDataReceived}回受信");
            Debug.Log($"  表情データ: {history.faceLandmarksReceived}回受信");
        }
        
        Debug.Log($"総視線サンプル: {totalGazeSamples}");
        Debug.Log($"総表情サンプル: {totalFaceSamples}");
        Debug.Log("========================================");
    }
}
