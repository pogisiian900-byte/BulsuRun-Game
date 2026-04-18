using UnityEngine;

public class ExploderEnemy : MonoBehaviour
{
    private const float PlayerSearchRetryInterval = 0.5f;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float detectionRange = 6f;

    [Header("Chase Timer (Fuse)")]
    [SerializeField] private float chaseDuration = 2.5f;

    [Header("Explosion")]
    [SerializeField] private float explosionRange = 1.5f;
    [SerializeField] private GameObject explosionPrefab;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private bool hasExploded;
    private bool hasDetectedPlayer;
    private bool hasLoggedMissingPlayer;
    private float chaseTimer;
    private float nextPlayerSearchTime;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        if (rb != null)
        {
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        TryFindPlayer();

        if (anim != null)
        {
            anim.SetBool("isChasing", false);
        }
    }

    private void FixedUpdate()
    {
        if (hasExploded)
        {
            return;
        }

        if (player == null && !TryFindPlayer())
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (!hasDetectedPlayer && distance <= detectionRange)
        {
            hasDetectedPlayer = true;
            chaseTimer = chaseDuration;

            if (anim != null)
            {
                anim.SetBool("isChasing", true);
            }
        }

        if (hasDetectedPlayer)
        {
            ChasePlayer();
            chaseTimer -= Time.fixedDeltaTime;

            if (distance <= explosionRange || chaseTimer <= 0f)
            {
                Explode();
            }
        }
        else if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void ChasePlayer()
    {
        if (rb == null || player == null)
        {
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);

        if (direction.x != 0f && sr != null)
        {
            sr.flipX = direction.x < 0f;
        }
    }

    private void Explode()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;

        if (anim != null)
        {
            anim.SetBool("isChasing", false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRange);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player"))
            {
                continue;
            }

            hit.GetComponent<PlayerHealth>()?.TakeHeartDamage(1, transform.position);
            break;
        }

        Destroy(gameObject);
    }

    private bool TryFindPlayer()
    {
        if (Time.time < nextPlayerSearchTime)
        {
            return player != null;
        }

        nextPlayerSearchTime = Time.time + PlayerSearchRetryInterval;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>(true);
            if (playerMovement != null)
            {
                playerObject = playerMovement.gameObject;
            }
        }

        if (playerObject == null)
        {
            if (!hasLoggedMissingPlayer)
            {
                Debug.LogWarning("ExploderEnemy is waiting for the player to spawn.");
                hasLoggedMissingPlayer = true;
            }

            player = null;
            return false;
        }

        hasLoggedMissingPlayer = false;
        player = playerObject.transform;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}
