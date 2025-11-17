using UnityEngine;
using System.Text;

/// <summary>
/// LocalXR_Clientのデータフロー全体を監視する統合デバッグスクリプト
/// RemoteXR_Client(LSL受信) → Photon送信 → LocalXR_Client(Photon受信)の全ステップを可視化
/// 
/// 使用方法:
/// 1. このスクリプトをLocalXR_Clientの任意のGameObjectにアタッチ
/// 2. ゲーム実行中に画面右下にデータフローダイアグラムが表示されます
/// 
/// データフロー:
/// RemoteXR_Client:
///   LSL → LslGazeReceiver/LslFaceMeshReceiver → PhotonFaceGazeTransmitter → Photon RPC
/// 
/// LocalXR_Client(このクライアント):
///   Photon RPC → AlignmentNetworkHub → PhotonFaceGazeReceiver → 表示/利用
/// </summary>
public class DataFlowMonitor : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Photon表情・視線受信機(RemoteXR_Clientからデータを受信)")]
    public PhotonFaceGazeReceiver receiver;

    [Header("Display Settings")]
    [Tooltip("画面上にフローダイアグラムを表示")]
    public bool showFlowDiagram = true;
    
    [Tooltip("詳細情報を表示")]
    public bool showDetailedInfo = true;
    
    [Tooltip("更新レートを計算")]
    public bool calculateRates = true;
    
    [Tooltip("フローダイアグラムの位置(X)")]
    public float diagramX = -1; // -1で自動(右寄せ)
    
    [Tooltip("フローダイアグラムの位置(Y)")]
    public float diagramY = 10f;

    [Header("Update Settings")]
    [Tooltip("コンソールログ出力間隔(秒)")]
    [Range(1f, 30f)]
    public float consoleLogInterval = 5f;

    // 内部状態
    private float logTimer;
    private float lastRateCalculationTime;
    
    // レート計算用
    private int gazeNetworkReceiveCount;
    private int faceNetworkReceiveCount;
    private float gazeNetworkReceiveRate;
    private float faceNetworkReceiveRate;
    
    // 最終データ
    private Vector2 lastGazeFromNetwork;
    private Vector3[] lastFaceFromNetwork;
    
    // GUI
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle normalStyle;
    private GUIStyle successStyle;
    private GUIStyle warningStyle;
    private GUIStyle errorStyle;
    private bool stylesInitialized = false;

    private void Start()
    {
        // 自動検出
        if (receiver == null)
        {
            receiver = FindObjectOfType<PhotonFaceGazeReceiver>();
            if (receiver != null)
                Debug.Log("[DataFlowMonitor] PhotonFaceGazeReceiverを自動検出");
        }
        
        // イベント登録
        AlignmentNetworkHub.OnGazeDataReceived += OnNetworkGazeReceived;
        AlignmentNetworkHub.OnFaceLandmarksReceived += OnNetworkFaceReceived;
        
        lastRateCalculationTime = Time.time;
        
        Debug.Log("========================================");
        Debug.Log("[DataFlowMonitor] LocalXR_Client データフロー監視開始");
        Debug.Log("[DataFlowMonitor] RemoteXR_Client → Photon → LocalXR_Client");
        Debug.Log("========================================");
        PrintDataFlowDiagram();
    }

    private void Update()
    {
        // レート計算(1秒ごと)
        if (calculateRates && Time.time - lastRateCalculationTime >= 1f)
        {
            float deltaTime = Time.time - lastRateCalculationTime;
            
            gazeNetworkReceiveRate = gazeNetworkReceiveCount / deltaTime;
            faceNetworkReceiveRate = faceNetworkReceiveCount / deltaTime;
            
            gazeNetworkReceiveCount = 0;
            faceNetworkReceiveCount = 0;
            
            lastRateCalculationTime = Time.time;
        }
        
        // コンソールログ
        logTimer += Time.deltaTime;
        if (logTimer >= consoleLogInterval)
        {
            PrintDataFlowStatus();
            logTimer = 0f;
        }
    }

    private void OnNetworkGazeReceived(int playerId, Vector2 gazePosition, float pupilSize, bool hasData)
    {
        if (hasData)
        {
            lastGazeFromNetwork = gazePosition;
            gazeNetworkReceiveCount++;
        }
    }

    private void OnNetworkFaceReceived(int playerId, Vector3[] landmarks, bool hasData)
    {
        if (hasData && landmarks != null)
        {
            lastFaceFromNetwork = landmarks;
            faceNetworkReceiveCount++;
        }
    }

    private void PrintDataFlowDiagram()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("\n┌─────────────────────────────────────────────────────────┐");
        sb.AppendLine("│          LocalXR_Client データフロー構成               │");
        sb.AppendLine("└─────────────────────────────────────────────────────────┘");
        sb.AppendLine();
        sb.AppendLine("  【RemoteXR_Client側 (デスクトップ)】");
        sb.AppendLine("       ↓");
        sb.AppendLine("  ┌─────────────┐         ┌─────────────────┐");
        sb.AppendLine("  │ LslGazeRx   │         │ LslFaceMeshRx   │");
        sb.AppendLine("  │ (LSL受信)   │         │ (LSL受信)       │");
        sb.AppendLine("  └──────┬──────┘         └────────┬────────┘");
        sb.AppendLine("         │                         │");
        sb.AppendLine("         └────────┬────────────────┘");
        sb.AppendLine("                  ↓");
        sb.AppendLine("       ┌──────────────────────┐");
        sb.AppendLine("       │ PhotonFaceGazeTx     │");
        sb.AppendLine("       │ (AlignmentNetworkHub)│");
        sb.AppendLine("       └───────────┬──────────┘");
        sb.AppendLine("                   │");
        sb.AppendLine("                   │ Photon Network (RPC)");
        sb.AppendLine("                   │");
        sb.AppendLine("                   ↓");
        sb.AppendLine("  【LocalXR_Client側 (Meta Quest)】");
        sb.AppendLine("       ┌──────────────────────┐");
        sb.AppendLine("       │ AlignmentNetworkHub  │");
        sb.AppendLine("       │ (RPC受信・イベント)  │");
        sb.AppendLine("       └───────────┬──────────┘");
        sb.AppendLine("                   ↓");
        sb.AppendLine("       ┌──────────────────────┐");
        sb.AppendLine("       │ PhotonFaceGazeRx     │");
        sb.AppendLine("       │ (可視化・利用)       │");
        sb.AppendLine("       └──────────────────────┘");
        sb.AppendLine();
        
        Debug.Log(sb.ToString());
    }

    private void PrintDataFlowStatus()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("[DataFlowMonitor] データフロー状態");
        sb.AppendLine("========================================");
        
        // RemoteXR_Client側の状態(推定)
        sb.AppendLine("\n【RemoteXR_Client側】");
        sb.AppendLine("  (LSL受信 → Photon送信)");
        sb.AppendLine("  ※RemoteXR_Clientで確認してください");
        
        // Photonネットワーク層
        sb.AppendLine("\n【Photonネットワーク層】");
        sb.AppendLine($"  AlignmentNetworkHub: {(AlignmentNetworkHub.IsReady ? "✓ 準備完了" : "✗ 未準備")}");
        
        // LocalXR_Client受信層
        sb.AppendLine("\n【LocalXR_Client受信層】");
        
        if (calculateRates)
        {
            sb.AppendLine($"  視線データ受信: {gazeNetworkReceiveRate:F1} Hz");
            sb.AppendLine($"    最新データ: {lastGazeFromNetwork}");
            sb.AppendLine($"  表情データ受信: {faceNetworkReceiveRate:F1} Hz");
            if (lastFaceFromNetwork != null)
            {
                sb.AppendLine($"    ランドマーク数: {lastFaceFromNetwork.Length}");
            }
        }
        
        if (receiver != null)
        {
            sb.AppendLine($"  PhotonFaceGazeReceiver: ✓ 検出");
        }
        else
        {
            sb.AppendLine("  PhotonFaceGazeReceiver: ✗ 未検出");
        }
        
        // 総合診断
        sb.AppendLine("\n【総合診断】");
        
        bool networkOk = AlignmentNetworkHub.IsReady;
        bool receivingData = gazeNetworkReceiveRate > 0 || faceNetworkReceiveRate > 0;
        
        if (networkOk && receivingData)
        {
            sb.AppendLine("  ✓ RemoteXR_Clientからデータ受信中");
        }
        else
        {
            if (!networkOk)
                sb.AppendLine("  ⚠ Photonネットワークに問題があります");
            if (!receivingData)
                sb.AppendLine("  ⚠ RemoteXR_Clientからデータを受信していません");
        }
        
        sb.AppendLine("\n========================================");
        
        Debug.Log(sb.ToString());
    }

    private void OnGUI()
    {
        if (!showFlowDiagram)
            return;
        
        InitializeGUIStyles();
        
        float panelWidth = 380f;
        float panelHeight = showDetailedInfo ? 550f : 350f;
        
        float x = diagramX < 0 ? Screen.width - panelWidth - 10f : diagramX;
        float y = diagramY;
        
        GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("データフロー監視", titleStyle);
        GUILayout.Space(10);
        
        // LSL受信層
        DrawLSLReceptionSection();
        
        GUILayout.Space(5);
        DrawSeparator();
        GUILayout.Space(5);
        
        // Photon送信層
        DrawPhotonTransmissionSection();
        
        GUILayout.Space(5);
        DrawSeparator();
        GUILayout.Space(5);
        
        // Photon受信層
        DrawPhotonReceptionSection();
        
        if (showDetailedInfo)
        {
            GUILayout.Space(5);
            DrawSeparator();
            GUILayout.Space(5);
            
            // 詳細情報
            DrawDetailedInfo();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawLSLReceptionSection()
    {
        GUILayout.Label("【RemoteXR_Client側】", headerStyle);
        GUILayout.Label("(LSL受信→Photon送信)", normalStyle);
        GUILayout.Label("※Remote側で確認", normalStyle);
    }

    private void DrawPhotonTransmissionSection()
    {
        GUILayout.Label("【Photonネットワーク】", headerStyle);
        
        bool hubReady = AlignmentNetworkHub.IsReady;
        GUILayout.Label($"NetworkHub: {(hubReady ? "✓" : "✗")}", hubReady ? successStyle : errorStyle);
    }

    private void DrawPhotonReceptionSection()
    {
        GUILayout.Label("【LocalXR_Client受信】", headerStyle);
        
        if (calculateRates)
        {
            GUILayout.Label($"視線: {gazeNetworkReceiveRate:F1} Hz", 
                gazeNetworkReceiveRate > 0 ? successStyle : normalStyle);
            GUILayout.Label($"表情: {faceNetworkReceiveRate:F1} Hz", 
                faceNetworkReceiveRate > 0 ? successStyle : normalStyle);
        }
        
        if (receiver != null)
        {
            GUILayout.Label("Receiver: ✓", successStyle);
        }
        else
        {
            GUILayout.Label("Receiver: ✗", warningStyle);
        }
    }

    private void DrawDetailedInfo()
    {
        GUILayout.Label("【詳細情報】", headerStyle);
        
        if (lastGazeFromNetwork != Vector2.zero)
        {
            GUILayout.Label($"視線: ({lastGazeFromNetwork.x:F2}, {lastGazeFromNetwork.y:F2})", normalStyle);
        }
        else
        {
            GUILayout.Label("視線: 未受信", warningStyle);
        }
        
        if (lastFaceFromNetwork != null)
        {
            GUILayout.Label($"表情: {lastFaceFromNetwork.Length}点", normalStyle);
        }
        else
        {
            GUILayout.Label("表情: 未受信", warningStyle);
        }
    }

    private void DrawSeparator()
    {
        GUILayout.Label("─────────────────────", normalStyle);
    }

    private void InitializeGUIStyles()
    {
        if (stylesInitialized)
            return;
        
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.3f, 0.8f, 1f) }
        };
        
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };
        
        normalStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };
        
        successStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.green }
        };
        
        warningStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.yellow }
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
        AlignmentNetworkHub.OnGazeDataReceived -= OnNetworkGazeReceived;
        AlignmentNetworkHub.OnFaceLandmarksReceived -= OnNetworkFaceReceived;
        
        Debug.Log("[DataFlowMonitor] 監視終了");
    }
}
