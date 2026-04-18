using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPun
{
    private const string IdleAnimation = "Player_Idle";
    private const string RunAnimation = "Player_Running";
    private const string JumpAnimation = "Player_Jump";
    private const string FallAnimation = "Player_Fall";
    private const string GirlIdleAnimation = "Player_Girl_Idle";
    private const string GirlRunAnimation = "Player Girl Running";
    private const string GirlJumpAnimation = "Player_Girl_Jump";
    private const string GirlFallAnimation = "Player_Girl_Falling";
    private const int BaseAnimationLayer = 0;

    [Header("Movement")]
    [SerializeField] private float speed = 7f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 70f;
    [SerializeField] private float velocityPower = 0.9f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float jumpHoldForce = 5f;
    [SerializeField] private float jumpHoldTime = 0.18f;
    [SerializeField] private float jumpCutMultiplier = 0.2f;

    [Header("Shared Camera Clamp")]
    [SerializeField] private bool clampPlayersInsideCamera = true;
    [SerializeField] private float cameraEdgePadding = 0.35f;

    [Header("Checks")]
    [SerializeField] private Transform groundCheckLeft;
    [SerializeField] private Transform groundCheckRight;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float groundCheckDistance = 0.05f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] [Range(0.1f, 1f)] private float minGroundNormalY = 0.65f;
    [Header("Skill Modifiers")]
    private const int baseMaxJumps = 1;
    private int maxJumps = 1;
    private float speedBonus;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Animator animator;
    private bool isGrounded;
    private int jumpsLeft;
    private float jumpHoldTimer;
    private bool isWallSliding;
    private bool isKnockback;
    private bool canMove = true;
    private bool freezeMotion;
    private bool wasGrounded;
    private Vector3 lastPosition;
    private Vector2 remoteVelocity;
    private string currentAnimationState;
    private ContactFilter2D groundContactFilter;
    private float currentMoveInput;
    private bool jumpHeldInput;
    private bool jumpQueued;
    private bool jumpReleaseQueued;
    private float cameraClampSuspendTimer;

    public void ResetToBaseStats()
    {
        maxJumps = baseMaxJumps;
        speedBonus = 0f;
    }

    public void EnableDoubleJump()
    {
        maxJumps = 2;
    }

    public void AddSpeedBonus(float amount)
    {
        speedBonus += amount;
    }

    public void ResetForRespawn()
    {
        ResolveCheckReferences();
        jumpHoldTimer = 0f;
        jumpsLeft = maxJumps;
        isGrounded = false;
        wasGrounded = false;
        isWallSliding = false;
        isKnockback = false;
        freezeMotion = false;
        remoteVelocity = Vector2.zero;
        lastPosition = transform.position;
        currentMoveInput = 0f;
        jumpHeldInput = false;
        jumpQueued = false;
        jumpReleaseQueued = false;
        cameraClampSuspendTimer = 0f;
    }
   
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        ResolveCheckReferences();
        ConfigureContactFilters();
        jumpsLeft = maxJumps;
        lastPosition = transform.position;
    }

    private bool ShouldControlLocally()
    {
        return !PhotonNetwork.InRoom || photonView.IsMine;
    }

    public void HandleMovement(float moveInput, bool jumpDown, bool jumpHeld, bool jumpReleased)
    {
        if (!ShouldControlLocally())
        {
            return;
        }

        if (!canMove || isKnockback)
        {
            currentMoveInput = 0f;
            jumpHeldInput = false;
            jumpQueued = false;
            jumpReleaseQueued = false;
            return;
        }

        currentMoveInput = Mathf.Clamp(moveInput, -1f, 1f);
        jumpHeldInput = jumpHeld;

        if (jumpDown)
        {
            jumpQueued = true;
        }

        if (jumpReleased)
        {
            jumpReleaseQueued = true;
        }
    }

    private void FixedUpdate()
    {
        if (!ShouldControlLocally() || rb == null)
        {
            return;
        }

        if (!canMove || isKnockback)
        {
            currentMoveInput = 0f;
            jumpQueued = false;
            jumpReleaseQueued = false;
            jumpHeldInput = false;

            if (freezeMotion)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            return;
        }

        ApplyHorizontalMovement();
        ApplyJumpPhysics();
        ClampInsideSharedCamera();
    }

    private void UpdateAnimationState()
    {
        string targetAnimation;
        float horizontalMotion = GetHorizontalMotion();
        float verticalMotion = GetVerticalMotion();

        if (isGrounded)
        {
            targetAnimation = Mathf.Abs(horizontalMotion) > 0.05f
                ? ResolveAnimationState(RunAnimation, GirlRunAnimation)
                : ResolveAnimationState(IdleAnimation, GirlIdleAnimation);
        }
        else if (verticalMotion > 0.05f)
        {
            targetAnimation = ResolveAnimationState(JumpAnimation, GirlJumpAnimation);
        }
        else
        {
            targetAnimation = ResolveAnimationState(FallAnimation, GirlFallAnimation);
        }

        if (currentAnimationState == targetAnimation || animator == null)
        {
            return;
        }

        animator.Play(targetAnimation);
        currentAnimationState = targetAnimation;
    }

    private string ResolveAnimationState(string primaryState, string fallbackState)
    {
        if (animator == null)
        {
            return primaryState;
        }

        if (animator.HasState(BaseAnimationLayer, Animator.StringToHash(primaryState)))
        {
            return primaryState;
        }

        if (animator.HasState(BaseAnimationLayer, Animator.StringToHash(fallbackState)))
        {
            return fallbackState;
        }

        return primaryState;
    }

    private void HandleFacingDirection(float horizontalMotion)
    {
        Vector3 scale = transform.localScale;
        if (horizontalMotion > 0.05f) scale.x = Mathf.Abs(scale.x);
        else if (horizontalMotion < -0.05f) scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    void Update()
    {
        if (cameraClampSuspendTimer > 0f)
        {
            cameraClampSuspendTimer = Mathf.Max(0f, cameraClampSuspendTimer - Time.unscaledDeltaTime);
        }

        CheckGround();

        if (!ShouldControlLocally())
        {
            UpdateRemoteVelocity();
        }

        UpdateAnimationState();
        HandleFacingDirection(GetHorizontalMotion());
        lastPosition = transform.position;
    }

    public void EnableMovement(bool value, bool freezeVelocity = false)
    {
        canMove = value;
        freezeMotion = !value && freezeVelocity;

        if (value)
        {
            return;
        }

        currentMoveInput = 0f;
        jumpHeldInput = false;
        jumpQueued = false;
        jumpReleaseQueued = false;
        jumpHoldTimer = 0f;

        if (freezeMotion && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void SuspendCameraClamp(float duration)
    {
        cameraClampSuspendTimer = Mathf.Max(cameraClampSuspendTimer, duration);
    }

    private void ApplyHorizontalMovement()
    {
        float targetSpeed = currentMoveInput * Mathf.Max(0f, speed + speedBonus);
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }

    private void ApplyJumpPhysics()
    {
        if (jumpQueued)
        {
            if (jumpsLeft > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpHoldTimer = jumpHoldTime;
                jumpsLeft--;
            }

            jumpQueued = false;
        }

        if (jumpHeldInput && rb.linearVelocity.y > 0f && jumpHoldTimer > 0f)
        {
            rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
            jumpHoldTimer -= Time.fixedDeltaTime;
        }

        if (jumpReleaseQueued)
        {
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }

            jumpHoldTimer = 0f;
            jumpReleaseQueued = false;
        }
    }

    private void ClampInsideSharedCamera()
    {
        if (!clampPlayersInsideCamera || rb == null || cameraClampSuspendTimer > 0f)
        {
            return;
        }

        Camera activeCamera = Camera.main;
        if (activeCamera == null || !activeCamera.orthographic)
        {
            return;
        }

        float halfHeight = activeCamera.orthographicSize;
        float halfWidth = halfHeight * activeCamera.aspect;
        float playerHalfWidth = bodyCollider != null ? bodyCollider.bounds.extents.x : 0f;

        float minX = activeCamera.transform.position.x - halfWidth + playerHalfWidth + cameraEdgePadding;
        float maxX = activeCamera.transform.position.x + halfWidth - playerHalfWidth - cameraEdgePadding;

        if (minX > maxX)
        {
            float centerX = activeCamera.transform.position.x;
            minX = centerX;
            maxX = centerX;
        }

        Vector2 clampedPosition = rb.position;
        float originalX = clampedPosition.x;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);

        if (Mathf.Abs(clampedPosition.x - originalX) <= Mathf.Epsilon)
        {
            return;
        }

        rb.position = clampedPosition;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }


    private void CheckGround()
    {
        if (bodyCollider == null)
        {
            isGrounded = false;
            wasGrounded = false;
            return;
        }

        int hitCount = bodyCollider.Cast(Vector2.down, groundContactFilter, groundHits, groundCheckDistance);
        bool groundedNow = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = groundHits[i];

            if (hit.collider == null)
            {
                continue;
            }

            if (hit.normal.y < minGroundNormalY)
            {
                continue;
            }

            groundedNow = true;
            break;
        }

        if (groundedNow && !wasGrounded)
        {
            jumpsLeft = maxJumps;
        }

        isGrounded = groundedNow;
        wasGrounded = groundedNow;
    }

    private void ConfigureContactFilters()
    {
        groundContactFilter.useLayerMask = true;
        groundContactFilter.layerMask = groundLayer;
        groundContactFilter.useTriggers = false;
    }

    private void UpdateRemoteVelocity()
    {
        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        remoteVelocity = ((Vector2)(transform.position - lastPosition)) / deltaTime;
    }

    private float GetHorizontalMotion()
    {
        return ShouldControlLocally() ? rb.linearVelocity.x : remoteVelocity.x;
    }

    private float GetVerticalMotion()
    {
        return ShouldControlLocally() ? rb.linearVelocity.y : remoteVelocity.y;
    }

    private void ResolveCheckReferences()
    {
        if (groundCheckLeft == null)
        {
            groundCheckLeft = transform.Find("GroundCheckLeft");
        }

        if (groundCheckRight == null || groundCheckRight == groundCheckLeft)
        {
            groundCheckRight = transform.Find("GroundCheckRight");
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheckLeft != null)
        {
            Gizmos.DrawWireSphere(groundCheckLeft.position, groundCheckRadius);
            Gizmos.DrawLine(groundCheckLeft.position, groundCheckLeft.position + Vector3.down * groundCheckDistance);
        }

        if (groundCheckRight != null)
        {
            Gizmos.DrawWireSphere(groundCheckRight.position, groundCheckRadius);
            Gizmos.DrawLine(groundCheckRight.position, groundCheckRight.position + Vector3.down * groundCheckDistance);
        }
    }
}
