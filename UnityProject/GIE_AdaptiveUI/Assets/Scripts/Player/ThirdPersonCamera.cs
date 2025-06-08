using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("추적할 대상 (플레이어)")]
    public Transform target;

    [Header("회전 속도 및 제한값")]
    public float mouseSensitivity = 4.0f;
    public float minPitch = -20f;
    public float maxPitch = 60f;

    [Header("카메라 거리")]
    public float defaultDistance = 5.0f;
    public float minDistance = 2.0f;
    public float maxDistance = 10.0f;
    public float scrollSensitivity = 2.0f;

    private float yaw;    // 수평 회전 각 (y축)
    private float pitch;  // 수직 회전 각 (x축)
    private float currentDistance;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("[ThirdPersonCamera] Target이 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        currentDistance = defaultDistance;
        Cursor.lockState = CursorLockMode.Locked; // 마우스 커서를 고정할지 여부 (선택 사항)
    }

    void LateUpdate()
    {
        // HandleMouseInput();
        HandleScrollInput();
        UpdateCameraPosition();
    }

    /// <summary>
    /// 마우스 움직임으로 yaw/pitch 값을 조정하고, 제한 범위 안으로 클램핑.
    /// </summary>
    private void HandleMouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    /// <summary>
    /// 스크롤 휠 입력으로 카메라와 대상 간 거리 조정.
    /// </summary>
    private void HandleScrollInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel") * scrollSensitivity;
        currentDistance = Mathf.Clamp(currentDistance - scroll, minDistance, maxDistance);
    }

    /// <summary>
    /// yaw, pitch, currentDistance 값을 기반으로 카메라 위치와 회전 설정.
    /// </summary>
    private void UpdateCameraPosition()
    {
        // 회전 Quaternion 계산
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = rotation * Vector3.back; // 뒤쪽 방향 (0, 0, -1)에 회전 적용

        // 카메라 위치 = 대상 위치 + (방향 * 거리)
        Vector3 desiredPos = target.position + Vector3.up * 1.5f + direction * currentDistance;
        // Vector3.up * 1.5f : 캐릭터 머리 위쪽을 바라보도록 약간 높이 조정 (키 값은 캐릭터 높이에 맞춰 조정)

        // 충돌 체크: 벽 뒤로 카메라가 들어가지 않도록 Raycast 사용 (선택사항)
        if (Physics.Linecast(target.position + Vector3.up * 1.5f, desiredPos, out RaycastHit hit))
        {
            desiredPos = hit.point + hit.normal * 0.2f;  // 벽에 살짝 붙도록 offset
        }

        transform.position = desiredPos;
        transform.rotation = rotation;
    }
}
