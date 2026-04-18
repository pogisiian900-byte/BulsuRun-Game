using UnityEngine;

public class PikachuSummon : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Enemy Detection")]
    [SerializeField] private float detectionRadius = 1.2f;

    [Header("Explosion")]
    [SerializeField] private int damage = 30;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float explosionEffectLifetime = 0.6f;
    [SerializeField] private Vector3 explosionEffectScale = Vector3.one;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 6f;

    private bool hasExploded;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (hasExploded)
        {
            return;
        }

        MoveForward();

        if (CanSeeEnemyAhead())
        {
            Explode();
        }
    }

    private void MoveForward()
    {
        float direction = GetFacingDirection();
        transform.position += Vector3.right * (direction * moveSpeed * Time.deltaTime);
    }

    private float GetFacingDirection()
    {
        if (transform.localScale.x < 0f)
        {
            return -1f;
        }

        return 1f;
    }

    private bool CanSeeEnemyAhead()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        float facingDirection = GetFacingDirection();

        foreach (Collider2D hit in hits)
        {
            if (!IsDamageableTarget(hit))
            {
                continue;
            }

            Vector2 targetOffset = (Vector2)(hit.bounds.center - transform.position);
            bool targetIsInFront = Mathf.Abs(targetOffset.x) < 0.15f || Mathf.Sign(targetOffset.x) == Mathf.Sign(facingDirection);

            if (targetIsInFront)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDamageableTarget(Collider2D hit)
    {
        if (hit == null)
        {
            return false;
        }

        return hit.GetComponentInParent<EnemyHealth>() != null ||
               hit.GetComponentInParent<BossHealth>() != null;
    }

    private void Explode()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            effect.transform.localScale = explosionEffectScale;
            Destroy(effect, explosionEffectLifetime);
        }

        float blastRadius = Mathf.Max(detectionRadius, explosionRadius);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, blastRadius);
        PlayerCombatDamage.DamageUniqueTargets(hits, damage, transform.position);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(detectionRadius, explosionRadius));
    }
}
