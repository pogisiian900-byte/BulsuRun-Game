using System;
using UnityEngine;

public class BossChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossHealth health;
    [SerializeField] private SkyBotMovement movement;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 8f;
    [SerializeField] private float stopDistance = 0.15f;
    [SerializeField] private int damageToBreak = 50;

    [Header("Player Contact")]
    [SerializeField] private int contactDamageHearts = 1;
    [SerializeField] private float contactDamageInterval = 0.5f;

    private Transform playerTarget;
    private bool isChasing;
    private int chaseStartHP;
    private float nextContactTime;

    public event Action ChaseBroken;

    public bool IsChasing => isChasing;

    public void Configure(BossHealth assignedHealth, SkyBotMovement assignedMovement)
    {
        health = assignedHealth;
        movement = assignedMovement;
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<BossHealth>();

        if (movement == null)
            movement = GetComponent<SkyBotMovement>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnHealthChanged.AddListener(HandleHealthChanged);
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged.RemoveListener(HandleHealthChanged);

        StopChase();
    }

    private void Update()
    {
        if (!isChasing)
            return;

        if (playerTarget == null)
            TryFindPlayer();

        if (playerTarget == null)
            return;

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = playerTarget.position;
        targetPosition.z = currentPosition.z;

        float distance = Vector2.Distance(currentPosition, targetPosition);
        if (distance <= stopDistance)
            return;

        transform.position = Vector3.MoveTowards(
            currentPosition,
            targetPosition,
            chaseSpeed * Time.deltaTime
        );
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isChasing || !other.CompareTag("Player"))
            return;

        if (Time.time < nextContactTime)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        playerHealth.TakeHeartDamage(contactDamageHearts, transform.position);
        nextContactTime = Time.time + contactDamageInterval;
    }

    public void BeginChase()
    {
        if (isChasing)
            return;

        if (health == null)
            health = GetComponent<BossHealth>();

        if (movement == null)
            movement = GetComponent<SkyBotMovement>();

        if (movement != null)
            movement.StopMovement();

        isChasing = true;
        nextContactTime = Time.time + contactDamageInterval;
        chaseStartHP = health != null ? health.CurrentHP : 0;

        TryFindPlayer();
    }

    public void StopChase()
    {
        isChasing = false;
        nextContactTime = 0f;
    }

    private void HandleHealthChanged(int currentHP, int maxHP)
    {
        if (!isChasing)
            return;

        if (currentHP <= 0)
            return;

        if (chaseStartHP - currentHP < damageToBreak)
            return;

        StopChase();

        if (movement != null)
            movement.ResetToDefaultArea();

        ChaseBroken?.Invoke();
    }

    private void TryFindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        playerTarget = playerObject != null ? playerObject.transform : null;
    }
}
