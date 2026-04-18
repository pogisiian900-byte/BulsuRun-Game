using UnityEngine;

public class FitBossSideShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leftFirePoint;
    [SerializeField] private Transform rightFirePoint;
    [SerializeField] private DodgeBallProjectile basketballPrefab;

    [Header("Fire Settings")]
     public float fireCooldown = 0.9f;
    [SerializeField] private float spawnForwardOffset = 0.8f;
    private float baseFireCooldown;
    private float timer;

    [Header("Launch Override (optional)")]
    [SerializeField] private bool overrideLaunch = false;
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float upForce = 5f;
    [SerializeField] private float randomForceOffset = 2f;
    [SerializeField] private bool rollOnly = false;
    [SerializeField] private float rollForce = 12f;

    [Header("Gizmo Settings")]
    [SerializeField] private float gizmoLength = 2f;

    public void SetMode(float fireCooldown, bool allowElectric = false)
    {
        this.fireCooldown = fireCooldown;
    }

    private void Awake()
    {
        baseFireCooldown = Mathf.Max(0.01f, fireCooldown);
        fireCooldown = baseFireCooldown;
    }

    private void OnEnable()
    {
        timer = fireCooldown;
    }

    public void SetFireRateMultiplier(float multiplier)
    {
        if (baseFireCooldown <= 0f)
            baseFireCooldown = Mathf.Max(0.01f, fireCooldown);

        multiplier = Mathf.Max(0.01f, multiplier);
        fireCooldown = baseFireCooldown / multiplier;
        timer = Mathf.Min(timer, fireCooldown);
    }

    private void Update()
    {
        if (basketballPrefab == null || leftFirePoint == null || rightFirePoint == null)
            return;

        timer += Time.deltaTime;

        if (timer >= fireCooldown)
        {
            timer = 0f;
            FireCannons();
        }
    }

    private void FireCannons()
    {
        SpawnBall(leftFirePoint.position, Vector2.right);
        SpawnBall(rightFirePoint.position, Vector2.left);
    }

    private void SpawnBall(Vector2 spawnPos, Vector2 dir)
    {
        Vector2 finalSpawnPos = spawnPos + dir.normalized * spawnForwardOffset;
        var proj = Instantiate(basketballPrefab, finalSpawnPos, Quaternion.identity);

        if (overrideLaunch)
        {
            ApplyCustomLaunch(proj, dir);
        }
        else
        {
            proj.Launch(dir);
        }
    }

    private void ApplyCustomLaunch(DodgeBallProjectile proj, Vector2 dir)
    {
        var rb = proj.GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            proj.Launch(dir);
            return;
        }

        float rand = Random.Range(-randomForceOffset, randomForceOffset);

        if (rollOnly)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(dir.normalized * (rollForce + rand), ForceMode2D.Impulse);
        }
        else
        {
            Vector2 force = new Vector2(dir.normalized.x * (throwForce + rand), upForce);
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    // =============================
    // GIZMOS (FOR CANNONS)
    // =============================

    private void OnDrawGizmos()
    {
        DrawFirePointGizmo(leftFirePoint, Vector2.right, Color.red);
        DrawFirePointGizmo(rightFirePoint, Vector2.left, Color.blue);
    }

    private void DrawFirePointGizmo(Transform firePoint, Vector2 dir, Color color)
    {
        if (firePoint == null) return;

        Gizmos.color = color;

        Vector3 start = firePoint.position + (Vector3)(dir.normalized * spawnForwardOffset);

        // draw spawn point
        Gizmos.DrawSphere(start, 0.15f);

        // draw shoot direction
        Vector3 end = start + (Vector3)(dir.normalized * gizmoLength);
        Gizmos.DrawLine(start, end);

        // arrow head
        Vector3 right = Quaternion.Euler(0,0,30) * -dir;
        Vector3 left = Quaternion.Euler(0,0,-30) * -dir;

        Gizmos.DrawLine(end, end + right * 0.4f);
        Gizmos.DrawLine(end, end + left * 0.4f);
    }
}
