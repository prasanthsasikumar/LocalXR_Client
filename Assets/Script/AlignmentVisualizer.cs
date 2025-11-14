using UnityEngine;

/// <summary>
/// Visualizes the alignment between coordinate systems
/// Draws gizmos and debug lines to show alignment status
/// </summary>
public class AlignmentVisualizer : MonoBehaviour
{
    [Header("References")]
    public SpatialAlignmentManager alignmentManager;
    public Transform scannedMesh;
    
    [Header("Visualization Settings")]
    public bool showAxes = true;
    public bool showAlignmentLines = true;
    public bool showGrid = true;
    public float axisLength = 2f;
    public float gridSize = 10f;
    public float gridSpacing = 1f;
    
    [Header("Colors")]
    public Color localAxisColor = Color.blue;
    public Color remoteAxisColor = Color.red;
    public Color alignmentLineColor = Color.yellow;
    public Color gridColor = new Color(1, 1, 1, 0.3f);
    
    private void OnDrawGizmos()
    {
        if (scannedMesh != null && showAxes)
        {
            DrawCoordinateAxes(scannedMesh.position, scannedMesh.rotation, localAxisColor, axisLength);
        }
        
        if (showGrid)
        {
            DrawAlignmentGrid();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (alignmentManager != null && showAlignmentLines)
        {
            DrawAlignmentInfo();
        }
    }
    
    void DrawCoordinateAxes(Vector3 position, Quaternion rotation, Color color, float length)
    {
        // X axis - Red
        Gizmos.color = Color.red;
        Gizmos.DrawRay(position, rotation * Vector3.right * length);
        
        // Y axis - Green
        Gizmos.color = Color.green;
        Gizmos.DrawRay(position, rotation * Vector3.up * length);
        
        // Z axis - Blue
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(position, rotation * Vector3.forward * length);
        
        // Origin sphere
        Gizmos.color = color;
        Gizmos.DrawWireSphere(position, 0.1f);
    }
    
    void DrawAlignmentGrid()
    {
        Gizmos.color = gridColor;
        
        Vector3 center = scannedMesh != null ? scannedMesh.position : Vector3.zero;
        
        // Draw grid lines
        for (float x = -gridSize; x <= gridSize; x += gridSpacing)
        {
            Gizmos.DrawLine(
                center + new Vector3(x, 0, -gridSize),
                center + new Vector3(x, 0, gridSize)
            );
        }
        
        for (float z = -gridSize; z <= gridSize; z += gridSpacing)
        {
            Gizmos.DrawLine(
                center + new Vector3(-gridSize, 0, z),
                center + new Vector3(gridSize, 0, z)
            );
        }
    }
    
    void DrawAlignmentInfo()
    {
        // This will draw lines showing alignment between local and remote coordinate systems
        // Requires runtime information, so only draws if manager has alignment data
    }
    
    void OnGUI()
    {
        if (scannedMesh == null) return;
        
        // Show mesh reference info
        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== MESH REFERENCE ===", GUI.skin.box);
        GUILayout.Label($"Position: {scannedMesh.position.ToString("F2")}");
        GUILayout.Label($"Rotation: {scannedMesh.rotation.eulerAngles.ToString("F1")}");
        GUILayout.Label($"Scale: {scannedMesh.localScale.ToString("F2")}");
        
        if (alignmentManager != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Alignment Status:");
            GUILayout.Label(alignmentManager.IsAligned() ? "✓ ALIGNED" : "✗ NOT ALIGNED");
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
