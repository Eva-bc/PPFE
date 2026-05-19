using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles top-down player movement (ZQSD / AZERTY), smooth mouse-based rotation,
/// and drives the Animator with a "Speed" float parameter for Idle/Walk blending.
/// Requires a Rigidbody. Constraints are applied automatically in Awake.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    // Controls how quickly the velocity ramps up and down (higher = more responsive).
    [SerializeField] private float moveSmoothSpeed = 15f;

    [Header("Rotation")]
    // Controls how fast the player rotates toward the mouse (higher = snappier).
    [SerializeField] private float rotationSmoothSpeed = 20f;

    [Header("Animation")]
    // Animator on the Visual child GameObject (set in Inspector).
    [SerializeField] private Animator characterAnimator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Rigidbody rigidBody;
    private Camera mainCamera;
    private PlayerGrabState grabState;

    // Current smoothed velocity, interpolated each frame.
    private Vector3 smoothedVelocity;

    // Target world point the player looks toward, updated from mouse raycast.
    private Vector3 targetLookPoint;

    // Raw input direction read each Update.
    private Vector3 rawMoveDirection;

    private void Awake()
    {
        rigidBody  = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        grabState  = GetComponent<PlayerGrabState>();

        rigidBody.constraints = RigidbodyConstraints.FreezeRotation
                              | RigidbodyConstraints.FreezePositionY;

        targetLookPoint = transform.position + transform.forward;
    }

    // Input is read in Update (frame-rate) for responsiveness.
    private void Update()
    {
        if (grabState != null && grabState.IsGrabbed) return;

        ReadMovementInput();
        ReadMousePosition();
    }

    // Physics and rotation are applied in FixedUpdate for stability.
    private void FixedUpdate()
    {
        if (grabState != null && grabState.IsGrabbed)
        {
            smoothedVelocity = Vector3.zero;
            rigidBody.linearVelocity = Vector3.zero;
            UpdateAnimator(0f);
            return;
        }

        ApplyMovement();
        ApplyRotation();
        UpdateAnimator(smoothedVelocity.magnitude);
    }

    // --- Animation ---

    /// <summary>Pushes the current movement speed to the Animator's Speed parameter.</summary>
    private void UpdateAnimator(float speed)
    {
        if (characterAnimator == null) return;
        characterAnimator.SetFloat(SpeedHash, speed);
    }

    // --- Input Reading ---

    // Touches configurables — correspond aux labels AZERTY (Z, Q, S, D).
    // Unity adresse les touches par position physique QWERTY :
    //   Z (AZERTY) → position W (QWERTY)
    //   Q (AZERTY) → position A (QWERTY)
    private const Key ForwardKey = Key.W; // Z sur AZERTY
    private const Key BackwardKey = Key.S;
    private const Key LeftKey = Key.A; // Q sur AZERTY
    private const Key RightKey = Key.D;

    private void ReadMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard[ForwardKey].isPressed) vertical += 1f;
        if (keyboard[BackwardKey].isPressed) vertical -= 1f;
        if (keyboard[LeftKey].isPressed) horizontal -= 1f;
        if (keyboard[RightKey].isPressed) horizontal += 1f;

        rawMoveDirection = new Vector3(horizontal, 0f, vertical).normalized;
    }

    private void ReadMousePosition()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || mainCamera == null) return;

        Vector2 screenPosition = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(
            new Vector3(screenPosition.x, screenPosition.y, 0f));

        // Cast the ray onto the horizontal plane at the player's height.
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float distance))
            targetLookPoint = ray.GetPoint(distance);
    }

    // --- Physics Application ---

    private void ApplyMovement()
    {
        // Smoothly interpolate toward the target velocity to avoid snapping.
        Vector3 targetVelocity = rawMoveDirection * moveSpeed;
        smoothedVelocity = Vector3.Lerp(
            smoothedVelocity,
            targetVelocity,
            moveSmoothSpeed * Time.fixedDeltaTime);

        rigidBody.MovePosition(rigidBody.position + smoothedVelocity * Time.fixedDeltaTime);
    }

    private void ApplyRotation()
    {
        Vector3 lookDirection = targetLookPoint - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        // Slerp interpolates along the shortest arc between two rotations.
        Quaternion smoothedRotation = Quaternion.Slerp(
            rigidBody.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.fixedDeltaTime);

        rigidBody.MoveRotation(smoothedRotation);
    }
}
