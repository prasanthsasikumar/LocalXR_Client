using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// AlignmentNetworkHubを通じて受信した視線・表情データをデバッグするスクリプト
/// PhotonViewに依存せず、AlignmentNetworkHubのイベントをリッスンしてデータ受信を監視
/// 
/// 使用方法:
/// 1. このスクリプトをLocalXR_Clientの任意のGameObjectにアタッチ
/// 2. 自動的にAlignmentNetworkHubのイベントに登録されます
/// 3. RemoteXR_Clientからのデータ受信状況を確認できます
/// </summary>
public class NetworkDataReceptionDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Consoleログの出力間隔(秒)")]
    [Range(0.1f, 10f)]
    public float logInterval = 2f;
    
    [Tooltip("画面上にデバッグ情報を表示するか")]
    public bool showOnScreenDebug = true;
    
    [Tooltip("詳細なログを出力するか")]
    public bool verboseLogging = true;
    
    [Tooltip("監視する特定のプレイヤーID(-1で全プレイヤー)")]
    public int targetPlayerId = -1;

    [Header("Display Settings")]
    [Tooltip("画面上のデバッグパネルのX位置")]
    public float panelX = 470f;
    
    [Tooltip("画面上のデバッグパネルのY位置")]
    public float panelY = 10f;

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
            faceLandmarksReceived = 0;
            gazeDataReceived = 0;
            lastFaceDataTime = 0f;
            lastGazeDataTime = 0f;
            lastFaceLandmarks = null;
            lastGazePosition = Vector2.zero;
            lastPupilSize = 0f;
            hasFaceData = false;
            hasGazeData = false;
        }
        
        public float GetFaceDataAge()
        {
            if (!hasFaceData) return -1f;
            return Time.time - lastFaceDataTime;
        }
        
        public float GetGazeDataAge()
        {
            if (!hasGazeData) return -1f;
            return Time.time - lastGazeDataTime;
        }
    }

    private Dictionary<int, PlayerDataHistory> playerHistories = new Dictionary<int, PlayerDataHistory>();
    private float logTimer;
    private float startTime;
    
    // GUI用の状態
    private GUIStyle headerStyle;
    private GUIStyle normalStyle;
    private GUIStyle warningStyle;
    private GUIStyle successStyle;
    private GUIStyle errorStyle;
    private bool stylesInitialized = false;

    private void Start()
    {
        startTime = Time.time;
        
        // AlignmentNetworkHubのイベントに登録
        AlignmentNetworkHub.OnFaceLandmarksReceived += OnFaceLandmarksReceived;
        AlignmentNetworkHub.OnGazeDataReceived += OnGazeDataReceived;
        
        Debug.Log("========================================");
        Debug.Log("[NetworkDataDebug] ネットワークデータ受信デバッグ開始");
        Debug.Log($"[NetworkDataDebug] AlignmentNetworkHub接続状態: {AlignmentNetworkHub.IsReady}");
        Debug.Log($"[NetworkDataDebug] 監視対象プレイヤーID: {(targetPlayerId == -1 ? "全プレイヤー" : targetPlayerId.ToString())}");
        Debug.Log("========================================");
    }

    private void Update()
    {
        logTimer += Time.deltaTime;
        
        if (logTimer >= logInterval)
        {
            LogNetworkStatus();
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
            Debug.Log($"<color=cyan>[NetworkDataDebug] 新しいプレイヤーを検出: ID={senderId}</color>");
        }
        
        PlayerDataHistory history = playerHistories[senderId];
        history.faceLandmarksReceived++;
        history.lastFaceDataTime = Time.time;
        history.hasFaceData = hasData;
        
        if (hasData && landmarks != null)
        {
            history.lastFaceLandmarks = landmarks;
            
            if (verboseLogging)
            {
                Debug.Log($"<color=green>[NetworkDataDebug] ✓ 表情データ受信</color>");
                Debug.Log($"  プレイヤーID: {senderId}");
                Debug.Log($"  ランドマーク数: {landmarks.Length}");
                Debug.Log($"  受信回数: {history.faceLandmarksReceived}");
                
                // サンプルポイントを表示
                if (landmarks.Length > 0)
                {
                    Debug.Log($"  サンプル[0]: {landmarks[0]}");
                    if (landmarks.Length > 30)
                    {
                        Debug.Log($"  サンプル[30]: {landmarks[30]}");
                    }
                }
            }
        }
        else
        {
            if (verboseLogging)
            {
                Debug.LogWarning($"[NetworkDataDebug] ⚠ 表情データなし (プレイヤー{senderId})");
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
            Debug.Log($"<color=cyan>[NetworkDataDebug] 新しいプレイヤーを検出: ID={senderId}</color>");
        }
        
        PlayerDataHistory history = playerHistories[senderId];
        history.gazeDataReceived++;
        history.lastGazeDataTime = Time.time;
        history.hasGazeData = hasData;
        
        if (hasData)
        {
            history.lastGazePosition = gazePosition;
            history.lastPupilSize = pupilSize;
            
            if (verboseLogging)
            {
                Debug.Log($"<color=green>[NetworkDataDebug] ✓ 視線データ受信</color>");
                Debug.Log($"  プレイヤーID: {senderId}");
                Debug.Log($"  視線位置: ({gazePosition.x:F3}, {gazePosition.y:F3})");
                Debug.Log($"  瞳孔サイズ: {pupilSize:F3}");
                Debug.Log($"  受信回数: {history.gazeDataReceived}");
            }
        }
        else
        {
            if (verboseLogging)
            {
                Debug.LogWarning($"[NetworkDataDebug] ⚠ 視線データなし (プレイヤー{senderId})");
            }
        }
    }

    #endregion

    private void LogNetworkStatus()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("[NetworkDataDebug] ネットワーク受信状況");
        sb.AppendLine("========================================");
        sb.AppendLine($"稼働時間: {Time.time - startTime:F1}秒");
        sb.AppendLine($"AlignmentNetworkHub状態: {(AlignmentNetworkHub.IsReady ? "✓ 準備完了" : "✗ 未準備")}");
        sb.AppendLine($"追跡中のプレイヤー数: {playerHistories.Count}");
        sb.AppendLine();
        
        if (playerHistories.Count == 0)
        {
            sb.AppendLine("⚠ まだリモートプレイヤーからのデータを受信していません");
            sb.AppendLine();
            sb.AppendLine("確認事項:");
            sb.AppendLine("  1. RemoteXR_Clientが起動しているか");
            sb.AppendLine("  2. Photonルームに接続されているか");
            sb.AppendLine("  3. RemoteXR_ClientでLSLデータが送信されているか");
        }
        else
        {
            foreach (var kvp in playerHistories)
            {
                PlayerDataHistory history = kvp.Value;
                
                sb.AppendLine($"【プレイヤー {history.playerId}】");
                sb.AppendLine($"  表情データ:");
                sb.AppendLine($"    受信回数: {history.faceLandmarksReceived}");
                
                if (history.hasFaceData)
                {
                    float faceAge = history.GetFaceDataAge();
                    sb.AppendLine($"    最終受信: {faceAge:F2}秒前");
                    
                    if (history.lastFaceLandmarks != null)
                    {
                        sb.AppendLine($"    ランドマーク数: {history.lastFaceLandmarks.Length}");
                        
                        if (history.lastFaceLandmarks.Length > 0)
                        {
                            sb.AppendLine($"    サンプル[0]: {history.lastFaceLandmarks[0]}");
                        }
                    }
                    
                    // 警告: データが古い
                    if (faceAge > 5f)
                    {
                        sb.AppendLine($"    <color=yellow>⚠ データが古い可能性があります ({faceAge:F1}秒)</color>");
                    }
                }
                else
                {
                    sb.AppendLine($"    <color=red>✗ データなし</color>");
                }
                
                sb.AppendLine($"  視線データ:");
                sb.AppendLine($"    受信回数: {history.gazeDataReceived}");
                
                if (history.hasGazeData)
                {
                    float gazeAge = history.GetGazeDataAge();
                    sb.AppendLine($"    最終受信: {gazeAge:F2}秒前");
                    sb.AppendLine($"    視線位置: ({history.lastGazePosition.x:F3}, {history.lastGazePosition.y:F3})");
                    sb.AppendLine($"    瞳孔サイズ: {history.lastPupilSize:F3}");
                    
                    // 警告: データが古い
                    if (gazeAge > 5f)
                    {
                        sb.AppendLine($"    <color=yellow>⚠ データが古い可能性があります ({gazeAge:F1}秒)</color>");
                    }
                }
                else
                {
                    sb.AppendLine($"    <color=red>✗ データなし</color>");
                }
                
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("========================================");
        
        Debug.Log(sb.ToString());
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug)
            return;
        
        InitializeGUIStyles();
        
        float panelWidth = 500f;
        float panelHeight = 500f;
        
        GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, panelHeight));
        GUILayout.BeginVertical("box");
        
        // ヘッダー
        GUILayout.Label("ネットワークデータ受信状況", headerStyle);
        GUILayout.Space(5);
        
        // ステータス
        GUILayout.Label($"稼働時間: {Time.time - startTime:F1}秒", normalStyle);
        
        bool hubReady = AlignmentNetworkHub.IsReady;
        GUILayout.Label($"NetworkHub: {(hubReady ? "✓ 準備完了" : "✗ 未準備")}", 
            hubReady ? successStyle : errorStyle);
        
        GUILayout.Label($"プレイヤー数: {playerHistories.Count}", normalStyle);
        GUILayout.Space(10);
        
        if (playerHistories.Count == 0)
        {
            GUILayout.Label("⚠ リモートプレイヤー未検出", warningStyle);
            GUILayout.Space(5);
            GUILayout.Label("確認事項:", normalStyle);
            GUILayout.Label("• RemoteXR_Clientが起動中か", normalStyle);
            GUILayout.Label("• Photonルームに接続中か", normalStyle);
            GUILayout.Label("• LSLデータが送信中か", normalStyle);
        }
        else
        {
            foreach (var kvp in playerHistories)
            {
                DrawPlayerDataPanel(kvp.Value);
                GUILayout.Space(10);
            }
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawPlayerDataPanel(PlayerDataHistory history)
    {
        GUILayout.Label($"【プレイヤー {history.playerId}】", headerStyle);
        
        // 表情データ
        GUILayout.Label("表情データ:", normalStyle);
        GUILayout.Label($"  受信: {history.faceLandmarksReceived}回", normalStyle);
        
        if (history.hasFaceData)
        {
            float faceAge = history.GetFaceDataAge();
            GUIStyle ageStyle = faceAge > 5f ? warningStyle : successStyle;
            GUILayout.Label($"  最終: {faceAge:F1}秒前", ageStyle);
            
            if (history.lastFaceLandmarks != null && history.lastFaceLandmarks.Length > 0)
            {
                GUILayout.Label($"  ランドマーク: {history.lastFaceLandmarks.Length}個", normalStyle);
            }
        }
        else
        {
            GUILayout.Label("  ✗ データなし", errorStyle);
        }
        
        // 視線データ
        GUILayout.Label("視線データ:", normalStyle);
        GUILayout.Label($"  受信: {history.gazeDataReceived}回", normalStyle);
        
        if (history.hasGazeData)
        {
            float gazeAge = history.GetGazeDataAge();
            GUIStyle ageStyle = gazeAge > 5f ? warningStyle : successStyle;
            GUILayout.Label($"  最終: {gazeAge:F1}秒前", ageStyle);
            GUILayout.Label($"  位置: ({history.lastGazePosition.x:F2}, {history.lastGazePosition.y:F2})", normalStyle);
            GUILayout.Label($"  瞳孔: {history.lastPupilSize:F2}", normalStyle);
        }
        else
        {
            GUILayout.Label("  ✗ データなし", errorStyle);
        }
    }

    private void InitializeGUIStyles()
    {
        if (stylesInitialized)
            return;
        
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };
        
        normalStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };
        
        warningStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.yellow }
        };
        
        successStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.green }
        };
        
        errorStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.red }
        };
        
        stylesInitialized = true;
    }

    private void OnDestroy()
    {
        // イベント登録解除
        AlignmentNetworkHub.OnFaceLandmarksReceived -= OnFaceLandmarksReceived;
        AlignmentNetworkHub.OnGazeDataReceived -= OnGazeDataReceived;
        
        Debug.Log("========================================");
        Debug.Log("[NetworkDataDebug] 最終統計");
        
        foreach (var kvp in playerHistories)
        {
            PlayerDataHistory history = kvp.Value;
            Debug.Log($"プレイヤー {history.playerId}:");
            Debug.Log($"  表情データ: {history.faceLandmarksReceived}回受信");
            Debug.Log($"  視線データ: {history.gazeDataReceived}回受信");
        }
        
        Debug.Log("========================================");
    }
}
