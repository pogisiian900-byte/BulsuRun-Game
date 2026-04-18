using UnityEngine;

public class PlayerProjectiles : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private bool rotateToDirection = true;

    private int damage;
    private float explosionRadius;
    private bool explosive;
    private bool usingRigidBodyMovement = true;
    private Vector2 direction = Vector2.right;
    private Transform ownerRoot;
    private GameObject impactEffectPrefab;
    private Rigidbody2D rb;
    private int wallLayer = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        wallLayer = LayerMask.NameToLayer("Wall");
    }

    private void Update()
    {
        if (!usingRigidBodyMovement)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }

    public void Fire(Vector2 dir, int dmg)
    {
        direction = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.right;
        damage = dmg;
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

    public void Fire(Vector2 dir, WeaponData weapon, Transform owner)
    {
        if (weapon != null)
        {
            damage = weapon.damage;
            speed = weapon.projectileSpeed > 0f ? weapon.projectileSpeed : speed;
            lifetime = weapon.projectileLifetime > 0f ? weapon.projectileLifetime : lifetime;
            rotateToDirection = weapon.rotateProjectileToDirection;
            explosive = weapon.weaponType == WeaponType.Rocket;
            explosionRadius = Mathf.Max(0f, weapon.explosionRadius);
            impactEffectPrefab = weapon.impactEffectPrefab;
        }

        ownerRoot = owner != null ? owner.root : null;
        Fire(dir, damage);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
        {
            return;
        }

        if (ownerRoot != null && collision.transform.IsChildOf(ownerRoot))
        {
            return;
        }

        bool damagedTarget = PlayerCombatDamage.TryDamage(
            collision,
            damage,
            transform.position,
            ownerRoot);

        bool hitEnvironment =
            collision.CompareTag("Ground") ||
            (wallLayer >= 0 && collision.gameObject.layer == wallLayer);

        if (damagedTarget || hitEnvironment)
        {
            if (explosive)
            {
                Explode();
                return;
            }

            SpawnImpactEffect();
            Destroy(gameObject);
        }
    }

    private void Explode()
    {
        bool spawnedImpactEffect = SpawnImpactEffect();

        if (!spawnedImpactEffect)
            SceneAudioManager.PlayExplosionSfx();

        if (explosionRadius > 0f)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            PlayerCombatDamage.DamageUniqueTargets(hits, damage, transform.position, ownerRoot);
        }

        Destroy(gameObject);
    }

    private bool SpawnImpactEffect()
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!explosive || explosionRadius <= 0f)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

}
