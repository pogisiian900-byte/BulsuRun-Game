using UnityEngine;

public class HomingRocket : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float rotateSpeed = 400f;

    [Header("Homing")]
    [SerializeField] private float homingTime = 1.5f;

    [Header("Launch")]
    [SerializeField] private float launchStraightTime = 0.5f;

    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;
[SerializeField] private float spriteRotationOffset = -90f;
    private Transform player;
    private float timer;

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (player == null)
            FindPlayer();

        // Keep the initial launch direction instead of forcing world-up.
        if (timer < launchStraightTime)
        {
            transform.position += transform.up * speed * Time.deltaTime;
            return;
        }

        // Homing time is measured after the straight launch phase.
        if (player != null && timer < launchStraightTime + homingTime)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.AngleAxis(angle + spriteRotationOffset, Vector3.forward);

            if (rotateSpeed <= 0f)
            {
                transform.rotation = targetRot;
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    rotateSpeed * Time.deltaTime
                );
            }
        }

        transform.position += transform.up * speed * Time.deltaTime;
    }

    private void FindPlayer()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            player = taggedPlayer.transform;
            return;
        }

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
            player = playerHealth.transform;
    }

    void Explode()
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider2D other)
    {
        if (other == null)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null || other.CompareTag("Player"))
        {
            Explode();
            return;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        int wallLayer = LayerMask.NameToLayer("Wall");
        int otherLayer = other.gameObject.layer;

        if (other.CompareTag("Ground") ||
            (groundLayer >= 0 && otherLayer == groundLayer) ||
            (wallLayer >= 0 && otherLayer == wallLayer))
        {
            Explode();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 start = transform.position;
        Vector3 end = transform.position + transform.up * 2f;

        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(start, 0.1f);
    }
}
