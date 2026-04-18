using UnityEngine;

public class TriggeredFly : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private float flySpeed = 8f;
    [SerializeField] private Vector2 flyDirection = Vector2.right;
    [SerializeField] private float destroyAfter = 5f;

    [Header("Gizmo")]
    [SerializeField] private float gizmoLength = 3f;
    [SerializeField] private Color gizmoColor = Color.cyan;

    private bool isFlying;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isFlying)
        {
            rb.linearVelocity = flyDirection.normalized * flySpeed;
        }
    }

    public void StartFlying()
    {
        isFlying = true;
        Destroy(gameObject, destroyAfter);
    }

    // 👇 GIZMO DRAWING
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        Vector3 start = transform.position;
        Vector3 dir = (Vector3)flyDirection.normalized * gizmoLength;
        Vector3 end = start + dir;

        // Main line
        Gizmos.DrawLine(start, end);

        // Arrow head
        Vector3 right = Quaternion.Euler(0, 0, 25) * -dir * 0.3f;
        Vector3 left = Quaternion.Euler(0, 0, -25) * -dir * 0.3f;

        Gizmos.DrawLine(end, end + right);
        Gizmos.DrawLine(end, end + left);
    }
}