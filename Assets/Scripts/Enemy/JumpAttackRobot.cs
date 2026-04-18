using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class JumpAttackRobot : MonoBehaviour
{
    private const float PlayerSearchRetryInterval = 0.5f;
    private static readonly int WalkingStateHash = Animator.StringToHash("Walking");
    private static readonly int JumpStateHash = Animator.StringToHash("Jump");

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private int startingDirection = -1;
    [SerializeField] private LayerMask turnAroundLayers;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.6f);

    [Header("Jump Attack")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float jumpHorizontalSpeed = 6f;
    [SerializeField] private float jumpVerticalSpeed = 7f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float minimumAirTime = 0.15f;
    [SerializeField] private int contactDamageHearts = 1;
    [SerializeField] private bool destroyOnPlayerHit = true;
    [SerializeField] private bool destroyOnMissedJump = true;

    [Header("Effects")]
    [SerializeField] private GameObject destroyEffectPrefab;
    [SerializeField] private float destroyEffectMaxDistance = 12f;

    [Header("Optimization")]
    [SerializeField] private float simulationRadius = 14f;
    [SerializeField] private float wakeUpBuffer = 2f;
    [SerializeField] private bool disableAnimatorWhenDormant = true;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D solidCollider;
    private Collider2D[] cachedColliders;
    private Transform player;
    private int direction;
    private bool isJumpAttacking;
    private bool isDestroyed;
    private bool isGrounded;
    private bool isDormant;
    private bool hasLoggedMissingPlayer;
    private bool hasWalkingState;
    private bool hasJumpState;
    private float attackCooldownTimer;
    private float jumpAttackStartTime;
    private float nextPlayerSearchTime;
    private float detectionRadiusSqr;
    private float simulationRadiusSqr;
    private float wakeUpRadiusSqr;
    private float destroyEffectMaxDistanceSqr;
    private int currentAnimationStateHash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.freezeRotation = true;

        cachedColliders = GetComponents<Collider2D>();
        foreach (Collider2D collider in cachedColliders)
        {
            if (!collider.isTrigger)
            {
                solidCollider = collider;
                break;
            }
        }

        if (animator != null)
        {
            hasWalkingState = animator.HasState(0, WalkingStateHash);
            hasJumpState = animator.HasState(0, JumpStateHash);
        }

        CacheDistanceThresholds();

        direction = startingDirection >= 0 ? 1 : -1;
        UpdateFacing();
    }

    private void Start()
    {
        TryFindPlayer();
    }

    private void FixedUpdate()
    {
        if (isDestroyed)
        {
            return;
        }

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.fixedDeltaTime;
        }

        if (player == null)
        {
            TryFindPlayer();
        }

        if (UpdateDormantState())
        {
            return;
        }

        isGrounded = CheckGrounded();

        if (isJumpAttacking)
        {
            if (HasLandedAfterJump())
            {
                isJumpAttacking = false;

                if (destroyOnMissedJump)
                {
                    SelfDestruct();
                    return;
                }
            }

            UpdateAnimationState();

            return;
        }

        rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);
        UpdateAnimationState();

        if (player == null || attackCooldownTimer > 0f || !isGrounded)
        {
            return;
        }

        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.sqrMagnitude <= detectionRadiusSqr)
        {
            StartJumpAttack();
        }
    }

    private void StartJumpAttack()
    {
        if (player == null)
        {
            return;
        }

        isJumpAttacking = true;
        attackCooldownTimer = attackCooldown;
        jumpAttackStartTime = Time.time;

        direction = player.position.x >= transform.position.x ? 1 : -1;
        UpdateFacing();

        rb.linearVelocity = new Vector2(direction * jumpHorizontalSpeed, jumpVerticalSpeed);
    }

    private bool HasLandedAfterJump()
    {
        if (Time.time < jumpAttackStartTime + minimumAirTime)
        {
            return false;
        }

        return isGrounded && rb.linearVelocity.y <= 0.05f;
    }

    private bool CheckGrounded()
    {
        if (solidCollider != null)
        {
            return solidCollider.IsTouchingLayers(groundLayer);
        }

        Vector2 checkPosition = groundCheck != null
            ? groundCheck.position
            : (Vector2)transform.position + groundCheckOffset;

        return Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer) != null;
    }

    private void UpdateAnimationState()
    {
        if (animator == null)
        {
            return;
        }

        int targetStateHash = (isJumpAttacking || !isGrounded) ? JumpStateHash : WalkingStateHash;
        if (currentAnimationStateHash == targetStateHash)
        {
            return;
        }

        bool targetStateExists = targetStateHash == JumpStateHash ? hasJumpState : hasWalkingState;
        if (targetStateExists)
        {
            animator.Play(targetStateHash);
            currentAnimationStateHash = targetStateHash;
        }
    }

    private bool UpdateDormantState()
    {
        if (player == null)
        {
            SetDormant(true);
            return true;
        }

        Vector2 toPlayer = player.position - transform.position;
        float distanceSqr = toPlayer.sqrMagnitude;

        if (isDormant)
        {
            if (distanceSqr > wakeUpRadiusSqr)
            {
                return true;
            }

            SetDormant(false);
            return false;
        }

        if (distanceSqr > simulationRadiusSqr)
        {
            SetDormant(true);
            return true;
        }

        return false;
    }

    private void SetDormant(bool shouldBeDormant)
    {
        if (isDormant == shouldBeDormant)
        {
            return;
        }

        isDormant = shouldBeDormant;

        if (rb != null)
        {
            if (shouldBeDormant)
            {
                rb.linearVelocity = Vector2.zero;
                rb.Sleep();
            }
            else
            {
                rb.WakeUp();
            }
        }

        if (shouldBeDormant)
        {
            isJumpAttacking = false;
            currentAnimationStateHash = 0;
        }

        if (animator != null && disableAnimatorWhenDormant)
        {
            animator.enabled = !shouldBeDormant;

            if (!shouldBeDormant)
            {
                currentAnimationStateHash = 0;
                UpdateAnimationState();
            }
        }
    }

    private void TurnAround()
    {
        direction *= -1;
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (direction >= 0 ? 1 : -1);
        transform.localScale = scale;
    }

    private bool TryDamagePlayer(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            return false;
        }

        playerHealth.TakeHeartDamage(Mathf.Max(1, contactDamageHearts), transform.position);
        return true;
    }

    private void SelfDestruct()
    {
        if (isDestroyed)
        {
            return;
        }

        isDestroyed = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        foreach (Collider2D collider2D in cachedColliders)
        {
            collider2D.enabled = false;
        }

        if (destroyEffectPrefab != null && ShouldSpawnDestroyEffect())
        {
            Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
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
                hasLoggedMissingPlayer = true;
            }

            player = null;
            return false;
        }

        hasLoggedMissingPlayer = false;
        player = playerObject.transform;
        return true;
    }

    private bool ShouldSpawnDestroyEffect()
    {
        if (player == null || destroyEffectMaxDistance <= 0f)
        {
            return true;
        }

        Vector2 toPlayer = player.position - transform.position;
        return toPlayer.sqrMagnitude <= destroyEffectMaxDistanceSqr;
    }

    private void CacheDistanceThresholds()
    {
        detectionRadius = Mathf.Max(0f, detectionRadius);
        simulationRadius = Mathf.Max(detectionRadius, simulationRadius);
        wakeUpBuffer = Mathf.Clamp(wakeUpBuffer, 0f, simulationRadius);
        destroyEffectMaxDistance = Mathf.Max(0f, destroyEffectMaxDistance);

        detectionRadiusSqr = detectionRadius * detectionRadius;
        simulationRadiusSqr = simulationRadius * simulationRadius;

        float wakeUpRadius = Mathf.Max(0f, simulationRadius - wakeUpBuffer);
        wakeUpRadiusSqr = wakeUpRadius * wakeUpRadius;
        destroyEffectMaxDistanceSqr = destroyEffectMaxDistance * destroyEffectMaxDistance;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed)
        {
            return;
        }

        if (TryDamagePlayer(other))
        {
            if (destroyOnPlayerHit)
            {
                SelfDestruct();
            }

            return;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed)
        {
            return;
        }

        if (TryDamagePlayer(collision.collider))
        {
            if (destroyOnPlayerHit)
            {
                SelfDestruct();
            }

            return;
        }

        int hitLayer = collision.gameObject.layer;
        if (!isJumpAttacking && ((1 << hitLayer) & turnAroundLayers) != 0)
        {
            TurnAround();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Vector2 checkPosition = groundCheck != null
            ? groundCheck.position
            : (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
    }

    private void OnValidate()
    {
        CacheDistanceThresholds();
    }
}
