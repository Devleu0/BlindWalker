using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TactilePavingPro : EditorWindow
{
    private GameObject dotPrefab;
    private GameObject linePrefab;
    private bool isPaintMode = false;
    private bool isLineMode = false; // 라인 모드 활성화 여부
    private int selectedIndex = 0; 
    private float gridSize = 0.3f;
    private float currentYRotation = 0f;

    private bool lockYHeight = false;
    private float fixedYValue = 0f;
    private float yOffset = 0.01f;

    private Vector3? lineStartPos = null; // 라인 시작점
    private GameObject previewObject;
    private List<GameObject> linePreviews = new List<GameObject>();
    private string containerName = "TactilePaving_Container";

    [MenuItem("Tools/Tactile Paving Pro Tool")]
    public static void ShowWindow() => GetWindow<TactilePavingPro>("Paving Pro");

    private void OnGUI()
    {
        GUILayout.Label("Paving Asset Settings", EditorStyles.boldLabel);
        dotPrefab = (GameObject)EditorGUILayout.ObjectField("Dot (Warning) Prefab", dotPrefab, typeof(GameObject), false);
        linePrefab = (GameObject)EditorGUILayout.ObjectField("Line (Directional) Prefab", linePrefab, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        gridSize = EditorGUILayout.FloatField("Grid Size (m)", gridSize);
        containerName = EditorGUILayout.TextField("Container Name", containerName);
        
        EditorGUILayout.Space();
        GUILayout.Label("Placement Mode", EditorStyles.boldLabel);
        isLineMode = EditorGUILayout.Toggle("Enable Line Mode", isLineMode);
        
        EditorGUILayout.Space();
        GUILayout.Label("Height Control (Y-Axis)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        lockYHeight = EditorGUILayout.Toggle("Lock Y Height", lockYHeight);
        fixedYValue = EditorGUILayout.FloatField("Value", fixedYValue);
        EditorGUILayout.EndHorizontal();
        yOffset = EditorGUILayout.Slider("Surface Offset", yOffset, 0f, 0.05f);

        EditorGUILayout.Space();
        selectedIndex = GUILayout.Toolbar(selectedIndex, new string[] { "Dot Tile", "Line Tile" });

        string modeDesc = isLineMode ? "라인 모드: 클릭(시작) -> 클릭(끝)" : "브러시 모드: 클릭 또는 드래그";
        EditorGUILayout.HelpBox($"{modeDesc}\n- R: 90도 회전\n- Ctrl + 클릭: 높이 샘플링\n- ESC: 라인 시작점 취소", MessageType.Info);

        GUI.color = isPaintMode ? Color.red : Color.green;
        if (GUILayout.Button(isPaintMode ? "PAINTER ON" : "PAINTER OFF"))
        {
            isPaintMode = !isPaintMode;
            ResetLineMode();
        }
        GUI.color = Color.white;
    }

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; ClearAllPreviews(); }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPaintMode) return;
        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { ResetLineMode(); Repaint(); }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R) { currentYRotation = (currentYRotation + 90f) % 360f; e.Use(); }

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (e.control && e.type == EventType.MouseDown) { fixedYValue = hit.point.y; lockYHeight = true; Repaint(); return; }

            float finalY = lockYHeight ? fixedYValue : hit.point.y;
            finalY += yOffset;

            Vector3 snappedPos = new Vector3(
                Mathf.Round(hit.point.x / gridSize) * gridSize,
                finalY,
                Mathf.Round(hit.point.z / gridSize) * gridSize
            );

            if (isLineMode && lineStartPos.HasValue)
            {
                // 일직선 고정 로직 (X축 또는 Z축 중 이동거리가 더 먼 쪽으로 고정)
                Vector3 diff = snappedPos - lineStartPos.Value;
                if (Mathf.Abs(diff.x) > Mathf.Abs(diff.z)) snappedPos.z = lineStartPos.Value.z;
                else snappedPos.x = lineStartPos.Value.x;
                
                UpdateLinePreview(lineStartPos.Value, snappedPos, lockYHeight ? Vector3.up : hit.normal);
            }
            else
            {
                UpdateSinglePreview(snappedPos, lockYHeight ? Vector3.up : hit.normal);
            }

            if (e.button == 0 && e.type == EventType.MouseDown)
            {
                if (isLineMode)
                {
                    if (!lineStartPos.HasValue) lineStartPos = snappedPos;
                    else { PlaceLine(lineStartPos.Value, snappedPos, lockYHeight ? Vector3.up : hit.normal); ResetLineMode(); }
                }
                else if (!e.shift) PlaceTile(snappedPos, lockYHeight ? Vector3.up : hit.normal);
                else DeleteTile(snappedPos);
                
                e.Use();
            }
        }
        else ClearAllPreviews();
        
        sceneView.Repaint();
    }

    private void UpdateLinePreview(Vector3 start, Vector3 end, Vector3 normal)
    {
        ClearAllPreviews();
        GameObject prefab = selectedIndex == 0 ? dotPrefab : linePrefab;
        if (prefab == null) return;

        List<Vector3> points = GetLinePoints(start, end);
        foreach (var p in points)
        {
            GameObject go = Instantiate(prefab);
            go.transform.position = p;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, currentYRotation, 0);
            go.name = "Preview_Tile";
            go.hideFlags = HideFlags.HideAndDontSave;
            SetPreviewTransparency(go, 0.4f);
            linePreviews.Add(go);
        }
    }

    private void UpdateSinglePreview(Vector3 pos, Vector3 normal)
    {
        ClearAllPreviews();
        GameObject prefab = selectedIndex == 0 ? dotPrefab : linePrefab;
        if (prefab == null) return;

        previewObject = Instantiate(prefab);
        previewObject.transform.position = pos;
        previewObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, currentYRotation, 0);
        previewObject.name = "Preview_Tile";
        previewObject.hideFlags = HideFlags.HideAndDontSave;
        SetPreviewTransparency(previewObject, 0.5f);
    }

    private void PlaceLine(Vector3 start, Vector3 end, Vector3 normal)
    {
        List<Vector3> points = GetLinePoints(start, end);
        foreach (var p in points) PlaceTile(p, normal);
    }

    private List<Vector3> GetLinePoints(Vector3 start, Vector3 end)
    {
        List<Vector3> pts = new List<Vector3>();
        float dist = Vector3.Distance(start, end);
        int count = Mathf.RoundToInt(dist / gridSize);
        Vector3 dir = (end - start).normalized;

        for (int i = 0; i <= count; i++) pts.Add(start + (dir * i * gridSize));
        return pts;
    }

    private void PlaceTile(Vector3 pos, Vector3 normal)
    {
        GameObject targetPrefab = selectedIndex == 0 ? dotPrefab : linePrefab;
        if (targetPrefab == null || IsAlreadyOccupied(pos)) return;

        GameObject container = GameObject.Find(containerName) ?? new GameObject(containerName);
        GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
        newTile.transform.position = pos;
        newTile.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0, currentYRotation, 0);
        newTile.transform.SetParent(container.transform);
        Undo.RegisterCreatedObjectUndo(newTile, "Place Tile");
    }

    private void DeleteTile(Vector3 pos)
    {
        Collider[] colliders = Physics.OverlapSphere(pos, 0.05f);
        foreach (var col in colliders) if (col.gameObject.name != "Preview_Tile") Undo.DestroyObjectImmediate(col.gameObject);
    }

    private bool IsAlreadyOccupied(Vector3 pos)
    {
        Collider[] colliders = Physics.OverlapSphere(pos, 0.05f);
        foreach (var col in colliders) if (col.gameObject.name != "Preview_Tile") return true;
        return false;
    }

    private void ResetLineMode() { lineStartPos = null; ClearAllPreviews(); }
    private void ClearAllPreviews()
    {
        if (previewObject != null) DestroyImmediate(previewObject);
        foreach (var p in linePreviews) if (p != null) DestroyImmediate(p);
        linePreviews.Clear();
    }

    private void SetPreviewTransparency(GameObject obj, float alpha)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
            foreach (Material m in r.sharedMaterials) { m.SetFloat("_Mode", 3); Color c = m.color; c.a = alpha; m.color = c; }
    }
}