using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MMFirstPersonController : MonoBehaviour
{
    public Transform cameraRoot;
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.4f;
    public float gravity = -25f;
    public float mouseSensitivity = 2f;

    CharacterController controller;
    float verticalVelocity;
    float pitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraRoot == null && Camera.main != null)
            cameraRoot = Camera.main.transform;
        LockCursor(true);
    }

    void Update()
    {
        HandleCursor();
        HandleLook();
        HandleMove();
    }

    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(Cursor.lockState != CursorLockMode.Locked);
        if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            LockCursor(true);
    }

    void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked || cameraRoot == null) return;
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up * mx);
        pitch = Mathf.Clamp(pitch - my, -89f, 89f);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMove()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 planar = (transform.right * x + transform.forward * z).normalized;
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 motion = planar * speed + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
