using System.Collections;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float radius = 1.5f;

    [Header("Timing")]
    [SerializeField] private float delayBeforeExplosion = 3f;
    [SerializeField] private float destroyDelay = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip explosionSfx;
    [SerializeField, Range(0f, 1f)] private float explosionSfxVolume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float explosionSfxPitch = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackUpForce = 3f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (animator != null)
            animator.enabled = false;

        StartCoroutine(ExplosionSequence());
    }

    private IEnumerator ExplosionSequence()
    {
        yield return new WaitForSeconds(delayBeforeExplosion);

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        if (animator != null)
            animator.enabled = true;

        SceneAudioManager.PlayExplosionSfx(explosionSfx, explosionSfxVolume, explosionSfxPitch);
        DealDamage();

        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    private void DealDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
            Rigidbody2D rb = hit.GetComponentInParent<Rigidbody2D>();

            if (health != null)
                health.TakeDamage(damage, transform.position);

            if (rb == null)
                continue;

            Vector2 direction = (hit.transform.position - transform.position).normalized;
            direction.y += 0.5f;

            rb.linearVelocity = Vector2.zero;
            rb.AddForce(
                new Vector2(direction.x * knockbackForce, knockbackUpForce),
                ForceMode2D.Impulse
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
