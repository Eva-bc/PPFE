using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Projects a cone of light toward the mouse cursor.
/// Ghosts inside the cone take damage over time while left mouse button is held.
/// The cone direction is driven by the mouse position, not the player's mesh rotation.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Cone Settings")]
    [SerializeField] private float range = 8f;
    [SerializeField][Range(10f, 120f)] private float coneAngle = 45f;

    [Header("Damage")]
    [SerializeField] private float damagePerSecond = 20f;
    [SerializeField] private LayerMask ghostLayerMask;

    [Header("Gizmo")]
    [SerializeField] private int gizmoArcSegments = 20;

    private const int MaxOverlapResults = 16;
    private readonly Collider[] overlapResults = new Collider[MaxOverlapResults];

    // Direction from the player toward the mouse cursor, updated every frame.
    private Vector3 aimDirection;

    // Whether the flashlight is currently active (readable by other systems).
    public bool IsActive { get; private set; }
    // Aim direction readable by visual systems.
    public Vector3 AimDirection => aimDirection;


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

        if (IsActive)
            DamageGhostsInCone();
    }

    // --- Input ---

    private void ReadInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        IsActive = mouse.leftButton.isPressed;
    }

    /// <summary>
    /// Casts a ray from the camera through the mouse position onto the ground plane
    /// to determine the world-space aim direction of the flashlight.
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
                ghost.TakeDamage(damagePerSecond * Time.deltaTime);

        }
    }

    /// <summary>
    /// Returns true if the world position falls within the flashlight cone.
    /// Uses the mouse aim direction rather than the player's mesh forward.
    /// </summary>
    private bool IsInsideCone(Vector3 targetPosition)
    {
        Vector3 directionToTarget = targetPosition - transform.position;
        directionToTarget.y = 0f;

        float angle = Vector3.Angle(aimDirection, directionToTarget);
        return angle <= coneAngle * 0.5f;
    }

    // --- Gizmo ---

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Vector3 direction = Application.isPlaying ? aimDirection : transform.forward;
        float halfAngle = coneAngle * 0.5f;

        // Left and right boundary rays.
        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);
        Vector3 leftEdge = leftRot * direction * range;
        Vector3 rightEdge = rightRot * direction * range;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, leftEdge);
        Gizmos.DrawRay(origin, rightEdge);

        // Arc connecting the two boundary rays.
        Vector3 previousPoint = origin + leftEdge;
        for (int i = 1; i <= gizmoArcSegments; i++)
        {
            float t = (float)i / gizmoArcSegments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 arcPoint = origin + Quaternion.AngleAxis(angle, Vector3.up) * direction * range;

            Gizmos.DrawLine(previousPoint, arcPoint);
            previousPoint = arcPoint;
        }

        // Filled transparent disc to show the cone area.
        Gizmos.color = new Color(1f, 1f, 0f, 0.08f);
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
