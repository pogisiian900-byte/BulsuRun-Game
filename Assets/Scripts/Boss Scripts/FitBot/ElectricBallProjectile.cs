using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ElectricBallProjectile : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private LayerMask destroyLayer;

    [Header("Stun + Damage")]
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private int damage = 10;

    [Header("Effects")]
    [SerializeField] private GameObject electricEffect; // optional VFX
    [SerializeField] private float destroyDelay = 0.05f;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;

    private bool triggered;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // Call this from a shooter script
    public void Launch(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        // hit player
        if (other.CompareTag("Player"))
        {
            triggered = true;

            // Damage
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage,transform.position);

            // Stun
            PlayerHealth controller = other.GetComponent<PlayerHealth>();
            if (controller != null) controller.Stun(stunDuration);

            HitEffect();
            DisableProjectile();
            Destroy(gameObject, destroyDelay);
            return;
        }

        // hit environment
        if ((destroyLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            triggered = true;
            HitEffect();
            Destroy(gameObject, destroyDelay);
        }
    }

    private void HitEffect()
    {
        if (electricEffect != null)
        {
            GameObject vfx = Instantiate(electricEffect, transform.position, Quaternion.identity);
            Destroy(vfx, 1.5f);
        }
    }

    private void DisableProjectile()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (col != null) col.enabled = false;
        if (sr != null) sr.enabled = false;
    }
}