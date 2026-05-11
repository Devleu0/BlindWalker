using System.Collections;
using UnityEngine;

public class TrafficLight : MonoBehaviour
{
    public float greenDuration = 5f;
    public float redDuration = 5f;
    public bool isGreen = true;

    public StopLine[] controlledStopLines; // 이 신호등이 관리하는 정지선들

    void Start() => StartCoroutine(CycleLight());

    IEnumerator CycleLight()
    {
        while (true)
        {
            isGreen = true;
            SetStopLines(false); // 정지선 비활성화 (차 통과 가능)
            Debug.Log("초록불");
            yield return new WaitForSeconds(greenDuration);

            isGreen = false;
            SetStopLines(true); // 정지선 활성화 (차 정지)
            Debug.Log("빨간불");
            yield return new WaitForSeconds(redDuration);
        }
    }

    void SetStopLines(bool active)
    {
        foreach (var line in controlledStopLines)
            line.isBlocking = active;
    }
}