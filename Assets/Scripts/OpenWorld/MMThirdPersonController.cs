using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MMThirdPersonController : MonoBehaviour
{
    public Transform visualRoot;
    public Animator animator;
    public Camera playerCamera;

    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;
    public float sprintSpeed = 8f;
    public float acceleration = 22f;
    public float deceleration = 30f;
    public float airControl = 0.45f;
    public float rotationSpeed = 12f;
    public float rotationSmoothTime = 0.085f;

    [Header("Turn Anticipation")]
    public float turnAnticipationAngle = 65f;
    public float turnAnticipationMaxSpeed = 0.35f;
    public float turnAnticipationCooldown = 0.55f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 1.4f;
    public float gravity = -24f;
    public float groundedForce = -3.5f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Camera")]
    public float cameraDistance = 5.5f;
    public float cameraHeight = 2.3f;
    public float mouseSensitivity = 2.2f;
    public float minPitch = -25f;
    public float maxPitch = 70f;
    public float minCameraDistance = 2.4f;
    public float maxCameraDistance = 8.0f;
    public float cameraZoomSpeed = 2.2f;
    public float cameraSmoothTime = 0.055f;
    public float cameraCollisionRadius = 0.28f;
    public float cameraCollisionPadding = 0.18f;
    public float cameraLookAhead = 0.12f;

    CharacterController controller;
    Vector3 planarVelocity;
    Vector3 cameraVelocity;
    float verticalVelocity;
    float yaw;
    float pitch = 14f;
    float turnVelocity;
    float lastGroundedTime = -100f;
    float lastJumpPressedTime = -100f;
    float targetCameraDistance;
    float nextTurnAnticipationTime;

#if UNITY_EDITOR
    bool editorTestOverride;
    Vector2 editorTestMove;
    bool editorTestSprint;
    bool editorTestWalk;
    bool editorTestJumpPressed;
#endif

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int MoveXHash = Animator.StringToHash("MoveX");
    static readonly int MoveYHash = Animator.StringToHash("MoveY");
    static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int TurnLeftHash = Animator.StringToHash("TurnLeft");
    static readonly int TurnRightHash = Animator.StringToHash("TurnRight");

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!playerCamera) playerCamera = Camera.main;
        targetCameraDistance = Mathf.Clamp(cameraDistance, minCameraDistance, maxCameraDistance);
        EnsureFootIK();

        if (playerCamera)
        {
            yaw = playerCamera.transform.eulerAngles.y;
            float cameraPitch = playerCamera.transform.eulerAngles.x;
            pitch = cameraPitch > 180f ? cameraPitch - 360f : cameraPitch;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        LockCursor(true);
    }

    void Update()
    {
        HandleCursor();

        bool groundedBeforeMove = controller.isGrounded;
        if (groundedBeforeMove)
        {
            lastGroundedTime = Time.time;
            if (verticalVelocity < 0f) verticalVelocity = groundedForce;
        }

        bool jumpPressed = Input.GetButtonDown("Jump");
#if UNITY_EDITOR
        if (editorTestOverride) jumpPressed = editorTestJumpPressed;
#endif
        if (jumpPressed)
        {
            lastJumpPressedTime = Time.time;
#if UNITY_EDITOR
            editorTestJumpPressed = false;
#endif
        }

        Vector2 input = Vector2.ClampMagnitude(
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            1f);
#if UNITY_EDITOR
        if (editorTestOverride)
            input = Vector2.ClampMagnitude(editorTestMove, 1f);
#endif

        Vector3 camForward = playerCamera ? playerCamera.transform.forward : Vector3.forward;
        Vector3 camRight = playerCamera ? playerCamera.transform.right : Vector3.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward = camForward.sqrMagnitude > 0.0001f ? camForward.normalized : Vector3.forward;
        camRight = camRight.sqrMagnitude > 0.0001f ? camRight.normalized : Vector3.right;

        Vector3 desiredDirection = camForward * input.y + camRight * input.x;
        if (desiredDirection.sqrMagnitude > 1f) desiredDirection.Normalize();

        TryTurnAnticipation(desiredDirection, input.magnitude, groundedBeforeMove);

        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool walk = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#if UNITY_EDITOR
        if (editorTestOverride)
        {
            sprint = editorTestSprint;
            walk = editorTestWalk;
        }
#endif
        float maxSpeed = walk ? walkSpeed : (sprint ? sprintSpeed : runSpeed);
        Vector3 desiredVelocity = desiredDirection * (maxSpeed * input.magnitude);

        float control = groundedBeforeMove ? 1f : airControl;
        float rate = desiredVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration;
        planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, rate * control * Time.deltaTime);

        if (desiredDirection.sqrMagnitude > 0.001f)
        {
            float targetYaw = Mathf.Atan2(desiredDirection.x, desiredDirection.z) * Mathf.Rad2Deg;
            float smoothYaw = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetYaw,
                ref turnVelocity,
                rotationSmoothTime,
                Mathf.Max(90f, rotationSpeed * 60f),
                Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
        }

        bool canCoyoteJump = Time.time - lastGroundedTime <= coyoteTime;
        bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
        if (canCoyoteJump && hasBufferedJump)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpPressedTime = -100f;
            lastGroundedTime = -100f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        bool groundedAfterMove = controller.isGrounded;
        if (groundedAfterMove && verticalVelocity < groundedForce)
            verticalVelocity = groundedForce;

        UpdateAnimator(groundedAfterMove);
    }

    void LateUpdate()
    {
        if (!playerCamera) return;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
            targetCameraDistance = Mathf.Clamp(
                targetCameraDistance - scroll * cameraZoomSpeed,
                minCameraDistance,
                maxCameraDistance);

        Vector3 lookAhead = planarVelocity * cameraLookAhead;
        lookAhead.y = 0f;
        Vector3 focus = transform.position + Vector3.up * cameraHeight + lookAhead;

        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desired = focus - orbit * Vector3.forward * targetCameraDistance;
        Vector3 castVector = desired - focus;
        float castDistance = castVector.magnitude;

        if (castDistance > 0.001f)
        {
            Vector3 castDirection = castVector / castDistance;
            RaycastHit[] hits = Physics.SphereCastAll(
                focus,
                cameraCollisionRadius,
                castDirection,
                castDistance,
                ~0,
                QueryTriggerInteraction.Ignore);

            float nearest = castDistance;
            foreach (RaycastHit hit in hits)
            {
                if (!hit.collider) continue;
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                nearest = Mathf.Min(nearest, hit.distance);
            }

            if (nearest < castDistance)
                desired = focus + castDirection * Mathf.Max(
                    minCameraDistance * 0.35f,
                    nearest - cameraCollisionPadding);
        }

        playerCamera.transform.position = Vector3.SmoothDamp(
            playerCamera.transform.position,
            desired,
            ref cameraVelocity,
            cameraSmoothTime);

        Vector3 look = focus - playerCamera.transform.position;
        if (look.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(look, Vector3.up);
            float blend = 1f - Mathf.Exp(-22f * Time.deltaTime);
            playerCamera.transform.rotation = Quaternion.Slerp(
                playerCamera.transform.rotation,
                targetRotation,
                blend);
        }
    }

    void TryTurnAnticipation(Vector3 desiredDirection, float inputAmount, bool grounded)
    {
        if (!grounded ||
            inputAmount < 0.20f ||
            desiredDirection.sqrMagnitude < 0.001f ||
            planarVelocity.magnitude > turnAnticipationMaxSpeed ||
            Time.time < nextTurnAnticipationTime ||
            !animator ||
            !animator.runtimeAnimatorController ||
            !animator.isActiveAndEnabled)
            return;

        float angle = Vector3.SignedAngle(transform.forward, desiredDirection, Vector3.up);
        if (Mathf.Abs(angle) < turnAnticipationAngle)
            return;

        animator.ResetTrigger(TurnLeftHash);
        animator.ResetTrigger(TurnRightHash);
        animator.SetTrigger(angle < 0f ? TurnLeftHash : TurnRightHash);
        nextTurnAnticipationTime = Time.time + turnAnticipationCooldown;
    }

    void UpdateAnimator(bool grounded)
    {
        if (!animator || !animator.runtimeAnimatorController || !animator.isActiveAndEnabled)
            return;

        float normalizedSpeed = sprintSpeed > 0.001f
            ? Mathf.Clamp01(planarVelocity.magnitude / sprintSpeed)
            : 0f;

        Vector3 localVelocity = transform.InverseTransformDirection(planarVelocity);
        float planarSpeed = planarVelocity.magnitude;
        float moveX = planarSpeed > 0.05f ? localVelocity.x / planarSpeed : 0f;
        float moveY = planarSpeed > 0.05f ? localVelocity.z / planarSpeed : 0f;

        animator.SetFloat(SpeedHash, normalizedSpeed, 0.10f, Time.deltaTime);
        animator.SetFloat(MoveXHash, moveX, 0.08f, Time.deltaTime);
        animator.SetFloat(MoveYHash, moveY, 0.08f, Time.deltaTime);
        animator.SetFloat(VerticalSpeedHash, verticalVelocity, 0.06f, Time.deltaTime);
        animator.SetBool(GroundedHash, grounded);
    }

    void EnsureFootIK()
    {
        if (!animator || !animator.isHuman)
            return;

        var footIK = animator.GetComponent<MMHeroFootIK>();
        if (!footIK)
            footIK = animator.gameObject.AddComponent<MMHeroFootIK>();

        footIK.animator = animator;
        footIK.characterRoot = transform;
        footIK.characterController = controller;
    }

#if UNITY_EDITOR
    public void EditorSetTestInput(Vector2 move, bool sprint = false, bool walk = false, bool jump = false)
    {
        editorTestOverride = true;
        editorTestMove = move;
        editorTestSprint = sprint;
        editorTestWalk = walk;
        editorTestJumpPressed |= jump;
    }

    public void EditorClearTestInput()
    {
        editorTestOverride = false;
        editorTestMove = Vector2.zero;
        editorTestSprint = false;
        editorTestWalk = false;
        editorTestJumpPressed = false;
    }

    public Vector3 EditorPlanarVelocity => planarVelocity;
    public float EditorVerticalVelocity => verticalVelocity;
#endif

    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(false);
        else if (Input.GetMouseButtonDown(0))
            LockCursor(true);
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
