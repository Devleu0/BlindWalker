using System.Collections; // 코루틴(IEnumerator) 사용
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))] // 실수로 컴포넌트가 누락 방지
public class NPCController : MonoBehaviour
{
    // 1. 상태(State) 및 열거형 정의
    public enum NPCState { Idle, Walking, Event }
    
    [Header("Current State")]
    public NPCState currentState = NPCState.Walking;

    // 2. 변수 선언부 (Variables)
    [Header("Path Settings")]
    [Tooltip("NPC가 순회할 웨이포인트(목적지) 배열입니다.")]
    public Transform[] waypoints; 
    private int currentWaypointIndex = 0;
    
    [Header("Components")]
    private NavMeshAgent agent;
    private Animator animator;

    // 3. 유니티 생명주기 메서드
    private void Awake()
    {
        // 필요한 컴포넌트 캐싱
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // 웨이포인트들의 부모가 NPC로 되어있다면, 부모 관계를 끊어 월드에 고정
        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
        {
            // 웨이포인트를 묶어둔 'Waypoints' 부모 오브젝트를 찾아서 독립
            Transform waypointsRoot = waypoints[0].parent;
            if (waypointsRoot != null && waypointsRoot.IsChildOf(this.transform))
            {
                waypointsRoot.SetParent(null); // 부모를 해제하여 월드 좌표계로 분리!
            }
        }
    }

    private void Start()
    {
        // 웨이포인트가 하나라도 등록되어 있다면 즉시 순회 시작
        if (waypoints != null && waypoints.Length > 0)
        {
            SetDestinationToWaypoint();
        }
    }

    private void Update()
    {
        // 걷기 상태일 때만 목적지 도착 여부 확인 및 걷기 애니메이션 반영
        if (currentState == NPCState.Walking)
        {
            HandleMovement();
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
        else 
        {
            // 이벤트(또는 대기) 중일 때는 애니메이션 간섭을 막기 위해 Speed를 0으로 강제
            animator.SetFloat("Speed", 0f);
        }
    }

    // 4. 이동 및 경로 제어 메서드 (Movement Logic)
    
    /// <summary>
    /// 목적지 도착 여부를 확인하고 다음 목적지로 이동을 지시 
    /// </summary>
    private void HandleMovement()
    {
        // 경로 계산 중이 아니고, 목적지까지의 남은 거리가 0.5f 미만이면 도착으로 간주
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }
    }

    /// <summary>
    /// 배열의 다음 웨이포인트 인덱스를 계산 
    /// </summary>
    private void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        // 배열의 끝에 도달하면 다시 0번(처음)으로 순환
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        SetDestinationToWaypoint();
    }

    /// <summary>
    /// 현재 인덱스의 웨이포인트로 NavMeshAgent의 목적지를 설정 
    /// </summary>
    private void SetDestinationToWaypoint()
    {
        agent.isStopped = false;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    /// <summary>
    /// 일시정지된 이동을 다시 재개 (주로 이벤트 종료 후 호출됨)
    /// </summary>
    public void ResumeMovement()
    {
        currentState = NPCState.Walking;
        SetDestinationToWaypoint();
    }

    // 5. 이벤트 및 애니메이션 제어 메서드

    /// <summary>
    /// 지정된 애니메이션을 재생하고 일정 시간(duration) 후 자동으로 걷기 상태로 복귀
    /// </summary>
    /// <param name="triggerName">애니메이터의 트리거 파라미터 이름</param>
    /// <param name="duration">애니메이션 재생 후 대기할 시간(초)</param>
    public void StartEventWithDuration(string triggerName, float duration)
    {
        StopAllCoroutines(); // 혹시 실행 중인 복귀 타이머가 있다면 겹치지 않게 취소
        StartCoroutine(EventRoutine(triggerName, duration));
    }

    /// <summary>
    /// 애니메이션 대기 및 복귀 처리를 담당하는 코루틴
    /// </summary>
    private IEnumerator EventRoutine(string triggerName, float duration)
    {
        // 1. 상태 변경 및 물리적 이동 완벽히 정지
        currentState = NPCState.Event;
        agent.isStopped = true;
        agent.velocity = Vector3.zero; // 관성으로 인한 미끄러짐 방지

        // 2. 애니메이션 실행
        animator.SetTrigger(triggerName);

        // 3. 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);

        // 4. 애니메이션 트리거 초기화(잔상 발동 방지) 및 다시 걷기
        animator.ResetTrigger(triggerName);
        ResumeMovement();
    }

    /// <summary>
    /// (참고용) 자동으로 복귀하지 않고 영구적으로 이벤트를 실행할 때 사용
    public void TriggerEventAnimation(string triggerName)
    {
        currentState = NPCState.Event;
        agent.isStopped = true; 
        agent.velocity = Vector3.zero;
        
        animator.SetTrigger(triggerName);
        
        // 주의: 이 메서드를 사용했다면 외부 스크립트에서 
        // 직접 ResumeMovement()를 호출해 주어야 NPC가 다시 움직 
    }

    // 6. 충돌 및 물리 감지 (Physics & Triggers)

    /// <summary>
    /// Is Trigger가 체크된 다른 Collider 영역에 진입했을 때 호출됩니다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 이미 이벤트 상태라면 다른 충돌은 무시 (연속 발동 방지)
        if (currentState == NPCState.Event) return; 

        // 장애물 구역에 닿았을 때
        if (other.CompareTag("Obs")) 
        {
            StartEventWithDuration("Stumble", 2.0f); 
        }
        // 다른 NPC와 만났을 때
        else if (other.CompareTag("NPC")) 
        {
            StartEventWithDuration("Greet", 1.5f); 
        }
    }


    // 7. 에디터 시각화 (Gizmos)
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            // 1. 구슬 색상 구분 (시작점: 녹색, 나머지: 시안색)
            Gizmos.color = (i == 0) ? Color.green : Color.cyan;
            Gizmos.DrawSphere(waypoints[i].position, 0.3f);

            // 2. 웨이포인트 인덱스(번호) 표시
            GUIStyle labelStyle = new GUIStyle();
            labelStyle.normal.textColor = (i == 0) ? Color.green : Color.white; // 시작점 텍스트는 녹색
            labelStyle.fontSize = 14;
            labelStyle.fontStyle = FontStyle.Bold;
            
            // 구슬에 가려지지 않게 Y축으로 살짝 올려서 텍스트 렌더링
            Vector3 labelPos = waypoints[i].position + Vector3.up * 0.6f;
            UnityEditor.Handles.Label(labelPos, $" WP {i+1}", labelStyle);

            // 3. 선 긋기 (진행 선과 복귀 선을 구분)
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                // 일반 진행 선 (노란색, 더 잘 보이게 두꺼운 선 사용)
                UnityEditor.Handles.color = Color.yellow;
                UnityEditor.Handles.DrawAAPolyLine(3f, waypoints[i].position, waypoints[i + 1].position);
            }
            else if (i == waypoints.Length - 1 && waypoints.Length > 1 && waypoints[0] != null)
            {
                // 마지막에서 처음으로 돌아오는 루프 선 (반투명한 흰색 점선으로 구분)
                UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.4f);
                UnityEditor.Handles.DrawDottedLine(waypoints[i].position, waypoints[0].position, 4f);
            }
        }
    }
#endif
}