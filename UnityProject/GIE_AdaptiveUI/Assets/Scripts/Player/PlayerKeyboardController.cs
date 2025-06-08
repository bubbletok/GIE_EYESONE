using Unity.VisualScripting;
using UnityEngine;

public class PlayerKeyboardController : MonoBehaviour
{
    private CharacterController controller;
    // private Animator animator;

    [Header("이동 속도")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;

    [Header("중력 관련")]
    public float gravity = -9.81f;
    private Vector3 velocity;

    private readonly int hashMoveSpeed = Animator.StringToHash("MoveSpeed");

    void Awake()
    {
        controller = gameObject.AddComponent<CharacterController>();
        // animator = GetComponent<Animator>() ?? null;
    }

    void Update()
    {
        if (controller == null) return;

        HandleMovement();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");  // A/D, ←/→
        float v = Input.GetAxis("Vertical");    // W/S, ↑/↓

        Vector3 moveDir = new Vector3(h, 0, v).normalized;
        if (moveDir.magnitude >= 0.1f)
        {
            // 카메라 방향에 따라 이동 방향을 보정하려면 카메라의 forward/right를 사용 가능
            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0;
            Vector3 right = Camera.main.transform.right;
            right.y = 0;

            Vector3 desiredDir = (forward * v + right * h).normalized;
            float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

            controller.Move(desiredDir * targetSpeed * Time.deltaTime);

            // 회전
            Quaternion targetRotation = Quaternion.LookRotation(desiredDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

            // 애니메이터 속도 설정 (0~1 범위)
            // float normalizedSpeed = targetSpeed == runSpeed ? 1f : 0.5f;
            // animator?.SetFloat(hashMoveSpeed, normalizedSpeed, 0.1f, Time.deltaTime);
        }
        else
        {
            // animator?.SetFloat(hashMoveSpeed, 0f, 0.1f, Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;  // 짧게 땅에 붙도록 제어

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
