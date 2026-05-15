using UnityEngine;

public class StopLine : MonoBehaviour
{
    public bool isBlocking = false;

    // CarController의 레이캐스트가 이 콜라이더를 감지
    // isBlocking이 true일 때만 차가 멈추도록 처리
    void OnTriggerStay(Collider other)
    {
        if (!isBlocking) return;
        if (other.TryGetComponent<CarController>(out var car))
        {
            car.currentSpeed = 0f;
            Debug.Log("작동함?");
        }
            
        
    }
}