using UnityEngine;

[RequireComponent(typeof(Camera))]
public class QuarterViewCamera : MonoBehaviour
{
    [Header("따라갈 대상")]
    public Transform target;

    [Header("카메라 위치 오프셋 (플레이어 기준)")]
    public Vector3 offset = new Vector3(0f, 10f, -10f);

    [Header("부드럽게 따라오는 속도 (0~1)")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.125f;

    [Header("카메라가 바라볼 높이 오프셋")]
    public float lookHeight = 1.5f;

    void LateUpdate()
    {
        if (target == null) return;

        // 1) 목표 위치 계산
        Vector3 desiredPosition = target.position + offset;

        // 2) 부드러운 이동
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 3) 대상 바라보기 (플레이어 머리 위치 쪽으로)
        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        transform.LookAt(lookPoint);
    }
}