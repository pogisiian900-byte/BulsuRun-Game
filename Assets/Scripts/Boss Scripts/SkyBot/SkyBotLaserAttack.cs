using System.Collections;
using UnityEngine;

public class SkyBotLaserAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private SkyBotMovement movement;
    private Transform player;

    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject laserPrefab;

    [Header("Attack Settings")]
    [SerializeField] private float hoverHeight = 1.5f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float aimDelay = 0.5f;
    [SerializeField] private float laserLife = 1.5f;
    [SerializeField] private float attackCooldown = 4f;

    [Header("Player Search")]
    [SerializeField] private float playerSearchInterval = 5f;

    private float playerSearchTimer;
    private float attackTimer;
    private bool attacking;
    private Coroutine attackRoutine;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<SkyBotMovement>();
    }

    private void OnEnable()
    {
        FindPlayer();
        playerSearchTimer = 0f;
        attackTimer = attackCooldown;
    }

    private void OnDisable()
    {
        StopAttackInternal(resumeMovement: false);
    }

    void Update()
    {
        if (player == null)
        {
            playerSearchTimer -= Time.deltaTime;

            if (playerSearchTimer <= 0f)
            {
                FindPlayer();
                playerSearchTimer = playerSearchInterval;
            }

            return;
        }

        if (!player.gameObject.activeInHierarchy)
        {
            player = null;
            return;
        }

        // attack cooldown
        attackTimer -= Time.deltaTime;

        if (!attacking && attackTimer <= 0f)
        {
            StartLaserAttack();
            attackTimer = attackCooldown;
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.L))
            StartLaserAttack();
#endif
    }

    void FindPlayer()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            player = taggedPlayer.transform;
            return;
        }

        GameObject[] objs = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in objs)
        {
            if (((1 << obj.layer) & playerLayer) != 0)
            {
                player = obj.transform;
                return;
            }
        }
    }

    public void StartLaserAttack()
    {
        if (attacking)
            return;

        if (player == null)
            FindPlayer();

        if (player == null || firePoint == null || laserPrefab == null)
            return;

        attackRoutine = StartCoroutine(LaserRoutine());
    }

    public void StopAttack()
    {
        StopAttackInternal(resumeMovement: true);
    }

    private void StopAttackInternal(bool resumeMovement)
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        attacking = false;

        if (resumeMovement && movement != null && movement.isActiveAndEnabled)
            movement.BeginMovement();
    }

    IEnumerator LaserRoutine()
    {
        attacking = true;

        if (movement != null)
            movement.StopMovement();

        Vector3 originalPos = transform.position;

        Vector3 target = new Vector3(
            player.position.x,
            player.position.y + hoverHeight,
            transform.position.z
        );

        // Move above player
        while (Vector2.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        // Wait before firing
        yield return new WaitForSeconds(aimDelay);

        Vector3 spawnPos = firePoint.position;
        spawnPos.y -= 0.2f;

        GameObject laser = Instantiate(
            laserPrefab,
            spawnPos,
            Quaternion.Euler(0,0,90)
        );

        // destroy laser after duration
        Destroy(laser, laserLife);

        // wait until laser disappears
        if (laserLife > 0f)
            yield return new WaitForSeconds(laserLife);

        // return to original position
        while (Vector2.Distance(transform.position, originalPos) > 0.1f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                originalPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (movement != null && movement.isActiveAndEnabled)
            movement.BeginMovement();

        attackRoutine = null;
        attacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        Gizmos.color = Color.red;

        float laserLength = 10f;

        Vector3 start = firePoint.position;
        Vector3 end = firePoint.position + Vector3.down * laserLength;

        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.15f);
    }
}
