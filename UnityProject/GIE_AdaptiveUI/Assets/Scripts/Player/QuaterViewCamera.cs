using UnityEngine;

[RequireComponent(typeof(Camera))]
public class QuarterViewCamera : MonoBehaviour
{
    [Header("따라갈 대상")]
    public Transform target;

    [Header("카메라 거리 설정")]
    public float distance = 10f; // Distance from the target
    public float height = 10f;  // Height above the target

    [Header("부드럽게 따라오는 속도 (0~1)")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.125f;

    [Header("카메라가 바라볼 높이 오프셋")]
    public float lookHeight = 1.5f;

    [Header("마우스 회전 감도")]
    public float mouseSensitivity = 100f; // Mouse sensitivity for rotation
    public float verticalAngleMin = -30f; // Min vertical angle (degrees)
    public float verticalAngleMax = 60f;  // Max vertical angle (degrees)

    private float yaw = 0f;   // Horizontal rotation angle
    private float pitch = 30f; // Vertical rotation angle (default 30 for quarter view)

    void Start()
    {
        // Initialize yaw based on target's forward direction
        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1) Handle mouse input for rotation
        if (Input.GetMouseButton(1)) // Right mouse button to rotate
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, verticalAngleMin, verticalAngleMax); // Clamp vertical angle
        }

        // 2) Calculate desired camera position
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = rotation * Vector3.forward;
        Vector3 desiredPosition = target.position - direction * distance + Vector3.up * height;

        // 3) Smoothly move the camera to the desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 4) Look at the target (at the specified look height)
        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        transform.LookAt(lookPoint);
    }
}