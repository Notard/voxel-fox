using UnityEngine;
using UnityEngine.InputSystem;

// 여우 이동: 카메라 기준 방향 입력, 이동 방향으로 회전, 중력, 점프.
// 입력은 InputSystem_Actions의 Player/Move, Player/Jump (WASD·방향키·게임패드, Space).
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 3f;
    [Tooltip("회전 속도 (도/초)")]
    [SerializeField] float turnSpeed = 720f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] CameraRig cameraRig;
    [SerializeField] InputActionAsset actions;

    [Tooltip("끄면 키보드 대신 MoveInput / RequestJump로 조종한다 (테스트용)")]
    public bool readDeviceInput = true;
    public Vector2 MoveInput { get; set; }

    public float MoveSpeed => moveSpeed;
    public float JumpHeight => jumpHeight;
    public bool IsGrounded => Controller.isGrounded;
    public Vector3 Velocity => velocity;

    CharacterController controller;
    CharacterController Controller => controller ? controller : controller = GetComponent<CharacterController>();

    InputAction moveAction, jumpAction;
    Vector3 velocity;
    bool jumpQueued;

    void Awake()
    {
        if (cameraRig == null) cameraRig = FindAnyObjectByType<CameraRig>();
        if (actions != null)
        {
            moveAction = actions.FindAction("Player/Move", true);
            jumpAction = actions.FindAction("Player/Jump", true);
        }
    }

    void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
    }

    public void RequestJump() => jumpQueued = true;

    void Update()
    {
        if (readDeviceInput && moveAction != null)
        {
            MoveInput = moveAction.ReadValue<Vector2>();
            if (jumpAction.WasPressedThisFrame()) jumpQueued = true;
        }

        var input = Vector2.ClampMagnitude(MoveInput, 1f);
        var direction = cameraRig != null ? cameraRig.InputToWorld(input) : new Vector3(input.x, 0, input.y);

        bool grounded = Controller.isGrounded;
        if (grounded && velocity.y < 0) velocity.y = -2f; // 바닥에 붙여 두어 isGrounded가 깜빡이지 않게
        if (jumpQueued && grounded) velocity.y = Mathf.Sqrt(2f * -gravity * jumpHeight);
        jumpQueued = false; // 공중에서 누른 점프는 버린다

        velocity.x = direction.x * moveSpeed;
        velocity.z = direction.z * moveSpeed;
        velocity.y += gravity * Time.deltaTime;
        Controller.Move(velocity * Time.deltaTime);

        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
    }

    // CharacterController는 켜진 상태에서 위치를 직접 바꾸면 무시하므로 잠시 끈다.
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        Controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        Controller.enabled = true;
        velocity = Vector3.zero;
        jumpQueued = false;
    }
}
