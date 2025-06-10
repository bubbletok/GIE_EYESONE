using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerKeyboardController : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;

    [Header("이동 속도")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;

    [Header("중력 및 점프 관련")]
    public float gravity = -9.81f;
    public float jumpForce = 5.0f; // 점프 힘
    private Vector3 velocity;

    private Vector3 currentMovement;
    private float currentSpeed = 0;
    private bool isJumping = false;
    public bool isFalling = false;

    void Awake()
    {
        controller = gameObject.GetOrAddComponent<CharacterController>();
        animator = GetComponent<Animator>() ?? null;
    }

    private void UpdateAnimationParameters()
    {
        if (animator == null) return;

        animator.SetFloat("MoveSpeed", currentSpeed);

        isJumping = !controller.isGrounded && velocity.y > 0;
        animator.SetBool("IsJumping", isJumping);

        isFalling = !controller.isGrounded && velocity.y < 0;
        animator.SetBool("IsFalling", isFalling);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Experiment_Main");
        }
        if (Input.GetKeyDown(KeyCode.R) || transform.position.y < -100f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void FixedUpdate()
    {
        if (controller == null) return;
        HandleInput();
        ApplyGravity();
        UpdateAnimationParameters();
    }

    // void LateUpdate()
    // {
    //     UpdateAnimationParameters();
    // }

    private void HandleInput()
    {
        float h = Input.GetAxis("Horizontal");  // A/D, ←/→
        float v = Input.GetAxis("Vertical");    // W/S, ↑/↓

        // Vector3 moveDir = new Vector3(h, 0, v).normalized;

        // 카메라 방향에 따라 이동 방향을 보정
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        Vector3 right = Camera.main.transform.right;
        right.y = 0;

        Vector3 desiredDir = (forward * v + right * h).normalized;
        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        currentMovement = desiredDir * targetSpeed;
        currentSpeed = currentMovement.magnitude;

        controller.Move(currentMovement * Time.deltaTime);
        // 회전
        if (currentMovement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentMovement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }

        if (Input.GetKey(KeyCode.Space) && !isJumping && !isFalling)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity); // 점프 속도 계산
            isJumping = true;
        }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;  // 짧게 땅에 붙도록 제어

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("FallingTrap"))
        {
            Debug.Log("Player has stepped on a falling trap!");
            Rigidbody rb = hit.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true; // 트랩이 중력을 사용하도록 설정
            }
        }
        if (hit.gameObject.CompareTag("FinalTarget"))
        {
            SceneManager.LoadScene("Experiment_End");
        }
    }
}