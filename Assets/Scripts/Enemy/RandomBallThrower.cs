using UnityEngine;

public class RandomBallThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spawnPoint;

    [Header("Ball Prefabs (Different Types)")]
    [SerializeField] private GameObject[] ballPrefabs;

    [Header("Fire Settings")]
    [SerializeField] private float fireCooldown = 1.5f;

    [Header("Default Throw (Arc) Settings")]
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float upForce = 5f;
    [SerializeField] private float randomForceOffset = 2f;

    [Header("Direction (USED ALWAYS)")]
    [SerializeField] private Vector2 shootDirection = Vector2.left;

    [Header("Optional")]
    [SerializeField] private float destroyAfter = 8f;

    [Header("Gizmo")]
    [SerializeField] private float gizmoLength = 2f;

    private float timer;

    private void Awake()
    {
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= fireCooldown)
        {
            timer = 0f;
            ShootRandom();
        }
    }

    private void ShootRandom()
    {
        if (ballPrefabs == null || ballPrefabs.Length == 0)
        {
            Debug.LogWarning("RandomBallThrower: No ball prefabs assigned!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("RandomBallThrower: No spawnPoint assigned!");
            return;
        }

        // ✅ Spawn a random ball
        int index = Random.Range(0, ballPrefabs.Length);
        GameObject obj = Instantiate(ballPrefabs[index], spawnPoint.position, Quaternion.identity);

        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("RandomBallThrower: Spawned ball has no Rigidbody2D!");
            return;
        }

        // default values from thrower
        float f = throwForce;
        float u = upForce;
        float off = randomForceOffset;

        // ✅ per-ball overrides (if component exists)
        BallLaunchSettings settings = obj.GetComponent<BallLaunchSettings>();
        if (settings != null && settings.useCustomLaunch)
        {
            off = settings.randomForceOffset;

            if (settings.rollOnly)
            {
                f = settings.rollForce;
                u = 0f;
            }
            else
            {
                f = settings.throwForce;
                u = settings.upForce;
            }
        }

        // add variation
        f += Random.Range(-off, off);
        u += Random.Range(-off, off);

        // shoot
        Vector2 dir = shootDirection.normalized;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dir * f + Vector2.up * u, ForceMode2D.Impulse);

        // cleanup
        if (destroyAfter > 0f)
            Destroy(obj, destroyAfter);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        Gizmos.color = Color.red;

        Vector3 start = spawnPoint.position;
        Vector3 dir = new Vector3(shootDirection.x, shootDirection.y, 0f).normalized;
        Vector3 end = start + dir * gizmoLength;

        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(start, 0.15f);
    }
}