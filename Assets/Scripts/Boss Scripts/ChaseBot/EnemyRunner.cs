using System.Collections;
using UnityEngine;

public class EnemyRunner : MonoBehaviour
{
    public float speed = 10f;
    public float boostedSpeed = 12f;
    public float boostDuration = 1f;
    public float jumpForce = 11f;
    public float screenCheckInterval = 0.2f;

    [Header("Kill Settings")]
    public int killDamage = 9999;

    private Rigidbody2D rb;
    private Collider2D ownCollider;
    private Camera cachedMainCamera;
    private bool isBoosted;
    private bool hasTriggeredGameOver;
    private float nextScreenCheckTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();
        cachedMainCamera = Camera.main;
    }

    void Start()
    {
        if (rb != null)
        {
            rb.linearDamping = 0f;
        }
    }

    void Update()
    {
        if (hasTriggeredGameOver || Time.time < nextScreenCheckTime)
        {
            return;
        }

        nextScreenCheckTime = Time.time + Mathf.Max(0.05f, screenCheckInterval);

        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
            if (cachedMainCamera == null)
            {
                return;
            }
        }

        Vector3 screenPos = cachedMainCamera.WorldToViewportPoint(transform.position);
        if (screenPos.x > 1.5f || screenPos.x < -1f || screenPos.y > 1.5f || screenPos.y < -1f)
        {
            TriggerGameOver();
        }
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle")
            || collision.gameObject.CompareTag("Damage")
            || collision.gameObject.CompareTag("Enemy"))
        {
            if (ownCollider != null)
            {
                Physics2D.IgnoreCollision(collision.collider, ownCollider);
            }

            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            if (!isBoosted)
            {
                StartCoroutine(TemporarySpeedBoost());
            }
        }
    }

    void TriggerGameOver()
    {
        if (hasTriggeredGameOver)
        {
            return;
        }

        hasTriggeredGameOver = true;

        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(killDamage, transform.position);
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.Lose();
        }

        Time.timeScale = 0f;
    }

    private IEnumerator TemporarySpeedBoost()
    {
        isBoosted = true;
        float originalSpeed = speed;

        speed = boostedSpeed;
        yield return new WaitForSeconds(boostDuration);

        speed = originalSpeed;
        isBoosted = false;
    }
}
