using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    public enum NPCState { Idle, Walking, Event }
    public NPCState currentState = NPCState.Walking;

    [Header("Path Settings")]
    public Transform[] waypoints; // 이동할 지점들
    private int currentWaypointIndex = 0;
    
    [Header("Components")]
    private NavMeshAgent agent;
    private Animator animator;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (waypoints.Length > 0)
        {
            SetDestinationToWaypoint();
        }
    }

    void Update()
    {
        if (currentState == NPCState.Walking)
        {
            HandleMovement();
        }
        
        // 애니메이션 파라미터 업데이트 (속도에 따라 걷기/대기 전환)
        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    private void HandleMovement()
    {
        // 목적지에 거의 도착했는지 확인
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }
    }

    private void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        SetDestinationToWaypoint();
    }

    private void SetDestinationToWaypoint()
    {
        agent.isStopped = false;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
    }

    // --- 이벤트 애니메이션 제어 메소드 ---

    /// <summary>
    /// 외부 이벤트 발생 시 호출하는 메소드
    /// </summary>
    /// <param name="triggerName">Animator에 설정된 Trigger 이름</param>
    public void TriggerEventAnimation(string triggerName)
    {
        currentState = NPCState.Event;
        agent.isStopped = true; // 이동 중지
        
        animator.SetTrigger(triggerName);
        
        // 참고: 애니메이션이 끝난 후 다시 Walking 상태로 복귀하는 로직은 
        // Animation Event나 코루틴을 사용하여 구현할 수 있습니다.
    }

    public void ResumeMovement()
    {
        currentState = NPCState.Walking;
        SetDestinationToWaypoint();
    }
}