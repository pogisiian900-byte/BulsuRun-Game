using UnityEngine;

public class EnemyChase : MonoBehaviour
{
    [Header("Chase Settings")]
    public float startSpeed = 3f;
    public float maxSpeed = 10f;
    public float acceleration = 1f;

    [Header("Max Speed Duration")]
    public float maxSpeedDuration = 4f;

    [Header("Effects")]
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float stopDistance = 0.15f;

    private float currentSpeed;
    private Transform player;
    private Transform assignedStopPoint;
    private bool stopAtAssignedStopPoint;
    private bool destroyAtAssignedStopPoint;
    private bool reachedAssignedStopPoint;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D ownCollider;
    private Collider2D playerCollider;

    private float maxSpeedTimer = 0f;
    private bool atMaxSpeed = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        currentSpeed = startSpeed;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            playerCollider = playerObj.GetComponent<Collider2D>();
        }
        else
        {
            Debug.LogWarning("Player with tag 'Player' not found.");
        }
    }

    void Update()
    {
        if (!atMaxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;

            if (currentSpeed >= maxSpeed)
            {
                currentSpeed = maxSpeed;
                atMaxSpeed = true;
                maxSpeedTimer = maxSpeedDuration;
            }
        }
        else
        {
            maxSpeedTimer -= Time.deltaTime;

            if (maxSpeedTimer <= 0f)
            {
                currentSpeed = startSpeed;
                atMaxSpeed = false;
            }
        }
    }

    void FixedUpdate()
    {
        ChaseTarget();
    }

    public void ConfigureStopPoint(Transform stopPoint, bool shouldStopAtPoint, bool shouldDestroyAtPoint)
    {
        assignedStopPoint = stopPoint;
        stopAtAssignedStopPoint = shouldStopAtPoint;
        destroyAtAssignedStopPoint = shouldDestroyAtPoint;
        reachedAssignedStopPoint = false;
        UpdateSpriteFacing();
    }

    void ChaseTarget()
    {
        Transform target = GetCurrentTarget();
        if (target == null)
            return;

        if (player != null && playerCollider == null)
            playerCollider = player.GetComponent<Collider2D>();

        Vector2 currentPosition = GetCurrentPosition();
        Vector2 targetPosition = target.position;
        Vector2 toTarget = targetPosition - currentPosition;
        float distanceToTarget = toTarget.magnitude;

        if (distanceToTarget <= Mathf.Epsilon)
            return;

        Vector2 direction = toTarget / distanceToTarget;
        float moveDistance = currentSpeed * Time.fixedDeltaTime;

        if (target == player)
            moveDistance = Mathf.Min(moveDistance, GetMaxMoveDistanceWithoutOverlappingPlayer());

        moveDistance = Mathf.Min(moveDistance, distanceToTarget);

        if (moveDistance > 0f)
            SetCurrentPosition(currentPosition + direction * moveDistance);

        ResolvePlayerOverlap();
        UpdateSpriteFacing(direction);

        if (assignedStopPoint != null &&
            !reachedAssignedStopPoint &&
            Vector2.Distance(transform.position, assignedStopPoint.position) <= stopDistance)
        {
            reachedAssignedStopPoint = true;

            if (destroyAtAssignedStopPoint)
            {
                Explode();
                return;
            }

            if (stopAtAssignedStopPoint)
                transform.position = new Vector3(
                    assignedStopPoint.position.x,
                    assignedStopPoint.position.y,
                    transform.position.z
                );
        }
    }

    private Transform GetCurrentTarget()
    {
        if (assignedStopPoint != null && !reachedAssignedStopPoint)
            return assignedStopPoint;

        if (stopAtAssignedStopPoint && reachedAssignedStopPoint)
            return null;

        return player;
    }

    private void UpdateSpriteFacing()
    {
        Transform target = GetCurrentTarget();
        if (target == null)
            return;

        Vector2 direction = target.position - transform.position;
        UpdateSpriteFacing(direction);
    }

    private void UpdateSpriteFacing(Vector2 direction)
    {
        if (spriteRenderer == null || Mathf.Approximately(direction.x, 0f))
            return;

        spriteRenderer.flipX = direction.x < 0f;
    }

    private float GetMaxMoveDistanceWithoutOverlappingPlayer()
    {
        if (ownCollider == null || playerCollider == null)
            return float.PositiveInfinity;

        ColliderDistance2D colliderDistance = ownCollider.Distance(playerCollider);
        return colliderDistance.distance > 0f ? colliderDistance.distance : 0f;
    }

    private void ResolvePlayerOverlap()
    {
        if (ownCollider == null || playerCollider == null)
            return;

        ColliderDistance2D colliderDistance = ownCollider.Distance(playerCollider);
        if (colliderDistance.distance >= 0f)
            return;

        SetCurrentPosition(GetCurrentPosition() + colliderDistance.normal * colliderDistance.distance);
    }

    private Vector2 GetCurrentPosition()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }

    private void SetCurrentPosition(Vector2 newPosition)
    {
        if (rb != null)
            rb.position = newPosition;
        else
            transform.position = newPosition;
    }

  private void OnTriggerEnter2D(Collider2D other)
{
    // Ignore other enemies
    if (other.CompareTag("Enemy") || other.CompareTag("Damage") )
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(other, myCollider);
        return;
    }

    if (assignedStopPoint != null && other.transform == assignedStopPoint)
    {
        reachedAssignedStopPoint = true;

        if (destroyAtAssignedStopPoint)
        {
            Explode();
            return;
        }

        if (stopAtAssignedStopPoint)
        {
            transform.position = new Vector3(
                assignedStopPoint.position.x,
                assignedStopPoint.position.y,
                transform.position.z
            );
            return;
        }
    }

    // Check by layer (EnemyWall)
    if (other.gameObject.layer == LayerMask.NameToLayer("EnemyWall"))
    {
        Explode();
    }
}

    void Explode()
    {
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
