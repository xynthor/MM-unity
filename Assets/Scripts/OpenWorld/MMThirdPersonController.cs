using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MMThirdPersonController : MonoBehaviour
{
    public Transform visualRoot;
    public Animator animator;
    public Camera playerCamera;
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 12f;
    public float jumpHeight = 1.4f;
    public float gravity = -24f;
    public float cameraDistance = 5.5f;
    public float cameraHeight = 2.3f;
    public float mouseSensitivity = 2.2f;
    public float minPitch = -25f;
    public float maxPitch = 70f;

    CharacterController controller;
    float verticalVelocity;
    float yaw;
    float pitch = 14f;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!playerCamera) playerCamera = Camera.main;
        if (playerCamera)
        {
            yaw = playerCamera.transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f);

        Vector3 camForward = playerCamera ? playerCamera.transform.forward : Vector3.forward;
        Vector3 camRight = playerCamera ? playerCamera.transform.right : Vector3.right;
        camForward.y = 0f; camForward.Normalize();
        camRight.y = 0f; camRight.Normalize();
        Vector3 move = camForward * input.z + camRight * input.x;

        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool run = input.magnitude > 0.01f;
        float speed = sprint ? sprintSpeed : (run ? runSpeed : walkSpeed);
        if (Input.GetKey(KeyCode.LeftControl)) speed = walkSpeed;

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(move.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        if (grounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move.normalized * speed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        if (animator && animator.runtimeAnimatorController && animator.isActiveAndEnabled)
        {
            float normalized = move.magnitude * (speed / sprintSpeed);
            animator.SetFloat(SpeedHash, normalized, 0.12f, Time.deltaTime);
            animator.SetBool(GroundedHash, grounded);
        }
    }

    void LateUpdate()
    {
        if (!playerCamera) return;
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Vector3 focus = transform.position + Vector3.up * cameraHeight;
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desired = focus - orbit * Vector3.forward * cameraDistance;

        if (Physics.Linecast(focus, desired, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            desired = hit.point + hit.normal * 0.25f;

        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, desired, 14f * Time.deltaTime);
        playerCamera.transform.rotation = Quaternion.LookRotation(focus - playerCamera.transform.position, Vector3.up);
    }
}
