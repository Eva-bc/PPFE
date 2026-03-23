using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Projects a cone of light toward the mouse cursor.
/// Normal mode: left click held. UV mode: double-click, limited duration and cooldown.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Cone Settings")]
    [SerializeField] private float range = 8f;
    [SerializeField][Range(10f, 120f)] private float coneAngle = 45f;

    [Header("Damage")]
    [SerializeField] private float damagePerSecond = 20f;
    [SerializeField] private LayerMask ghostLayerMask;

    [Header("UV Mode")]
    [SerializeField] private float uvDuration = 2.5f;
    [SerializeField] private float uvCooldown = 5f;

    [Header("Gizmo")]
    [SerializeField] private int gizmoArcSegments = 20;

    private const int MaxOverlapResults = 16;
    private readonly Collider[] overlapResults = new Collider[MaxOverlapResults];

    // Direction from the player toward the mouse cursor, updated every frame.
    private Vector3 aimDirection;

    public bool IsActive { get; private set; }
    public bool IsUVActive { get; private set; }
    public Vector3 AimDirection => aimDirection;

    // Double-click detection.
    private const float DoubleClickThreshold = 0.25f;
    private float lastClickTime = -1f;

    // UV timers.
    private float uvRemainingTime;
    private float uvCooldownRemaining;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        aimDirection = transform.forward;
    }

    private void Update()
    {
        ReadMouseAim();
        ReadInput();
        UpdateUVTimer();

        if (IsActive || IsUVActive)
            DamageGhostsInCone();
    }

    // --- Input ---

    private void ReadInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Normal flashlight: held click (only when UV is not active).
        IsActive = mouse.leftButton.isPressed && !IsUVActive;

        // Double-click detection for UV mode.
        if (mouse.leftButton.wasPressedThisFrame)
        {
            float timeSinceLastClick = Time.time - lastClickTime;

            if (timeSinceLastClick <= DoubleClickThreshold)
                TryActivateUV();

            lastClickTime = Time.time;
        }
    }

    private void TryActivateUV()
    {
        if (IsUVActive || uvCooldownRemaining > 0f)
        {
            Debug.Log("[Flashlight] UV on cooldown.");
            return;
        }

        IsUVActive = true;
        uvRemainingTime = uvDuration;
        uvCooldownRemaining = 0f;

        Debug.Log("[Flashlight] UV mode activated.");
    }

    private void UpdateUVTimer()
    {
        if (IsUVActive)
        {
            uvRemainingTime -= Time.deltaTime;
            if (uvRemainingTime <= 0f)
            {
                IsUVActive = false;
                uvCooldownRemaining = uvCooldown;
                Debug.Log("[Flashlight] UV mode ended. Cooldown started.");
            }
        }
        else if (uvCooldownRemaining > 0f)
        {
            uvCooldownRemaining -= Time.deltaTime;
        }
    }

    // --- Detection & Damage ---

    private void DamageGhostsInCone()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, range, overlapResults, ghostLayerMask);

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapResults[i];
            if (!IsInsideCone(hit.transform.position)) continue;

            if (hit.TryGetComponent(out Ghost ghost))
                ghost.TakeDamage(damagePerSecond * Time.deltaTime, isUV: IsUVActive);
        }
    }

    /// <summary>
    /// Returns true if the world position falls within the flashlight cone.
    /// </summary>
    private bool IsInsideCone(Vector3 targetPosition)
    {
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0f;

        float angle = Vector3.Angle(aimDirection, directionToTarget);
        return angle <= coneAngle * 0.5f;
    }

    /// <summary>
    /// Casts a ray from the camera through the mouse position onto the ground plane.
    /// </summary>
    private void ReadMouseAim()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || mainCamera == null) return;

        Vector2 screenPosition = mouse.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));

        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            Vector3 direction = worldPoint - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
                aimDirection = direction.normalized;
        }
    }

    // --- Gizmo ---

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Vector3 direction = Application.isPlaying ? aimDirection : transform.forward;
        float halfAngle = coneAngle * 0.5f;

        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);
        Vector3 leftEdge = leftRot * direction * range;
        Vector3 rightEdge = rightRot * direction * range;

        Gizmos.color = IsUVActive ? Color.magenta : Color.yellow;
        Gizmos.DrawRay(origin, leftEdge);
        Gizmos.DrawRay(origin, rightEdge);

        Vector3 previousPoint = origin + leftEdge;
        for (int i = 1; i <= gizmoArcSegments; i++)
        {
            float t = (float)i / gizmoArcSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 arcPoint = origin + Quaternion.AngleAxis(angle, Vector3.up) * direction * range;

            Gizmos.DrawLine(previousPoint, arcPoint);
            previousPoint = arcPoint;
        }

        Gizmos.color = IsUVActive
            ? new Color(0.8f, 0f, 1f, 0.08f)
            : new Color(1f, 1f, 0f, 0.08f);

        for (int i = 1; i <= gizmoArcSegments; i++)
        {
            float t = (float)i / gizmoArcSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 arcPoint = origin + Quaternion.AngleAxis(angle, Vector3.up) * direction * range;

            Gizmos.DrawLine(origin, arcPoint);
        }
    }
#endif
}

