using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float currentSpeed;
    public float stopDistance = 20f;

    void Update()
    {
        CheckAhead();
        Move();
        Debug.DrawRay(transform.position, transform.forward * stopDistance, Color.red);
    }

    void CheckAhead()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, stopDistance, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("Car"))
            {
                currentSpeed = 0f;
            }
            else if (hit.collider.CompareTag("StopLine"))
            {
                // 신호등 상태 확인 후 결정
                StopLine stopLine = hit.collider.GetComponent<StopLine>();
                if (stopLine != null && stopLine.isBlocking)
                    currentSpeed = 0f;
                else
                    currentSpeed = maxSpeed; // 초록불이면 그냥 통과
            }
        }
        else
        {
            currentSpeed = maxSpeed;
        }
    }

    void Move()
    {
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }
}
