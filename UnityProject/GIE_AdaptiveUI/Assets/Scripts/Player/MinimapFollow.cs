using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    [Tooltip("미니맵 카메라가 따라갈 대상")]
    public Transform target;
    [Tooltip("카메라 높이 (Y 값)")]
    public float height = 50f;
    [Tooltip("카메라와 타겟 사이의 오프셋 (XZ 평면)")]
    public Vector3 offset = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // 타겟 위치 + XZ 오프셋
        Vector3 newPos = target.position + new Vector3(offset.x, height, offset.z);

        transform.position = newPos;

        // (선택) 플레이어 방향으로 맵 회전
        transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
    }
}
