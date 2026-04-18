using UnityEngine;

public class PlayerRocketProjectile : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField] private float defaultSpeed = 8f;
    [SerializeField] private float defaultLifetime = 3f;
    [SerializeField] private bool rotateToDirection = true;

    [Header("Explosion")]
    [SerializeField] private float defaultExplosionRadius = 1.75f;
    [SerializeField] private GameObject defaultExplosionEffect;

    private int damage;
    private float speed;
    private float lifetime;
    private float explosionRadius;
    private Vector2 direction = Vector2.right;
    private Transform ownerRoot;
    private GameObject explosionEffectPrefab;
    private Rigidbody2D rb;
    private bool usingRigidBodyMovement = true;
    private bool exploded;
    private int wallLayer = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        wallLayer = LayerMask.NameToLayer("Wall");
        ResetToDefaults();
    }

    private void Update()
    {
        if (exploded || usingRigidBodyMovement)
        {
            return;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    public void Initialize(Vector2 fireDirection, WeaponData weapon, Transform owner)
    {
        ResetToDefaults();

        if (weapon != null)
        {
            damage = weapon.damage;
            speed = weapon.projectileSpeed > 0f ? weapon.projectileSpeed : defaultSpeed;
            lifetime = weapon.projectileLifetime > 0f ? weapon.projectileLifetime : defaultLifetime;
            explosionRadius = weapon.explosionRadius > 0f ? weapon.explosionRadius : defaultExplosionRadius;
            explosionEffectPrefab = weapon.impactEffectPrefab != null
                ? weapon.impactEffectPrefab
                : defaultExplosionEffect;
        }

        ownerRoot = owner != null ? owner.root : null;
        direction = fireDirection.sqrMagnitude > 0f ? fireDirection.normalized : Vector2.right;
        usingRigidBodyMovement = rb != null;

        if (rotateToDirection)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (exploded || collision == null)
        {
            return;
        }

        if (ownerRoot != null && collision.transform.IsChildOf(ownerRoot))
        {
            return;
        }

        bool hitDamageable = PlayerCombatDamage.TryDamage(
            collision,
            damage,
            transform.position,
            ownerRoot);

        bool hitEnvironment =
            collision.CompareTag("Ground") ||
            (wallLayer >= 0 && collision.gameObject.layer == wallLayer);

        if (hitDamageable || hitEnvironment)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        if (explosionRadius > 0f)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            PlayerCombatDamage.DamageUniqueTargets(hits, damage, transform.position, ownerRoot);
        }

        Destroy(gameObject);
    }

    private void ResetToDefaults()
    {
        damage = 0;
        speed = defaultSpeed;
        lifetime = defaultLifetime;
        explosionRadius = defaultExplosionRadius;
        explosionEffectPrefab = defaultExplosionEffect;
        exploded = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, defaultExplosionRadius);
    }
}
