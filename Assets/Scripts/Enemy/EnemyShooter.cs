using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float detectionRange = 8f;

    private Transform player;
    private SpriteRenderer spriteRenderer;

    private float fireTimer;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        spriteRenderer = GetComponent<SpriteRenderer>();

    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        FollowPlayer();
        if (distance <= detectionRange)
        {
            fireTimer -= Time.deltaTime;

            if (fireTimer <= 0)
            {
                Shoot();
                fireTimer = fireRate;
            }
        }
    }

void FollowPlayer()
{
    float directionX = player.position.x - transform.position.x;

    Vector3 scale = transform.localScale;

    if (directionX > 0)
        scale.x = Mathf.Abs(scale.x);   // face right
    else if (directionX < 0)
        scale.x = -Mathf.Abs(scale.x);    // face left

    transform.localScale = scale;

    Vector2 targetPosition = player.position;
    transform.position = Vector2.MoveTowards(
        transform.position,
        targetPosition,
        moveSpeed * Time.deltaTime
    );
}


    void Shoot()
    {
        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        Vector2 direction = (player.position - firePoint.position).normalized;
        bullet.GetComponent<EnemyBullets>().setDirection(direction);
    }
    private void OnDrawGizmosSelected()
{
    // Detection range
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, detectionRange);

    // Fire point direction
    if (firePoint != null)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.right * 1f);
        Gizmos.DrawSphere(firePoint.position, 0.1f);
    }
}

}
