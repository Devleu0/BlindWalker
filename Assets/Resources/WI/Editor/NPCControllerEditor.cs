using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// 이 스크립트가 NPCController의 인스펙터를 덮어씌우도록 설정
[CustomEditor(typeof(NPCController))]
public class NPCControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기존 인스펙터 UI를 그대로 그려줌
        base.OnInspectorGUI();

        NPCController npc = (NPCController)target;

        GUILayout.Space(15);
        GUILayout.Label("=== Waypoint Helper ===", EditorStyles.boldLabel);

        // 버튼 클릭 시 실행될 로직
        if (GUILayout.Button("현재 NPC 위치에 웨이포인트 추가", GUILayout.Height(30)))
        {
            AddWaypoint(npc);
        }

        if (GUILayout.Button("모든 웨이포인트 삭제", GUILayout.Height(20)))
        {
            ClearWaypoints(npc);
        }
    }

    private void AddWaypoint(NPCController npc)
    {
        // 1. 하이어라키 정리를 위해 "Waypoints"라는 부모 오브젝트 찾기 또는 생성
        Transform waypointsRoot = npc.transform.Find("Waypoints");
        if (waypointsRoot == null)
        {
            GameObject rootObj = new GameObject("Waypoints");
            Debug.Log("Waypoints 부모 오브젝트가 없어 새로 생성했습니다.");
            rootObj.transform.SetParent(npc.transform);
            rootObj.transform.localPosition = Vector3.zero;
            waypointsRoot = rootObj.transform;
            
            // Undo 기록 (Ctrl+Z 지원)
            Undo.RegisterCreatedObjectUndo(rootObj, "Create Waypoints Root");
        }

        // 2. 새 웨이포인트 빈 오브젝트 생성
        int newIndex = npc.waypoints != null ? npc.waypoints.Length + 1 : 1;
        GameObject newPoint = new GameObject($"Waypoint_{newIndex}");
        
        // 3. 부모 설정 및 위치를 현재 NPC 위치로 초기화
        newPoint.transform.SetParent(waypointsRoot);
        newPoint.transform.position = npc.transform.position;
        
        // Undo 기록
        Undo.RegisterCreatedObjectUndo(newPoint, "Add Waypoint");

        // 4. NPCController의 waypoints 배열에 추가
        Undo.RecordObject(npc, "Update Waypoints Array");
        
        List<Transform> currentWaypoints = new List<Transform>();
        if (npc.waypoints != null)
        {
            currentWaypoints.AddRange(npc.waypoints);
        }
        currentWaypoints.Add(newPoint.transform);
        npc.waypoints = currentWaypoints.ToArray();

        // 생성한 웨이포인트를 씬 뷰에서 바로 이동시킬 수 있도록 선택(Select) 처리
        Selection.activeGameObject = newPoint;
        
        // 변경사항 저장 플래그
        EditorUtility.SetDirty(npc);
    }

    private void ClearWaypoints(NPCController npc)
    {
        if (EditorUtility.DisplayDialog("경고", "정말로 모든 웨이포인트를 삭제하시겠습니까?", "네", "아니오"))
        {
            Undo.RecordObject(npc, "Clear Waypoints");
            
            Transform waypointsRoot = npc.transform.Find("Waypoints");
            if (waypointsRoot != null)
            {
                Undo.DestroyObjectImmediate(waypointsRoot.gameObject);
            }
            
            npc.waypoints = new Transform[0];
            EditorUtility.SetDirty(npc);
        }
    }
}