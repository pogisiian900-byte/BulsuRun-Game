using UnityEngine;

public class PlayerLaserBeam : MonoBehaviour
{
    private Vector3 baseScale = Vector3.one;
    private bool hasCachedScale;
    private SpriteRenderer spriteRenderer;
    private WeaponData currentWeapon;
    private Transform owner;
    private float nextDamageTime;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CacheBaseScale();
    }

    public void Initialize(Vector2 origin, Vector2 direction, WeaponData weapon, Transform owner)
    {
        if (weapon == null)
        {
            Destroy(gameObject);
            return;
        }

        currentWeapon = weapon;
        this.owner = owner;
        nextDamageTime = 0f;

        UpdateBeam(origin, direction);
        ApplyDamage(origin, direction, true);
    }

    public void UpdateBeam(Vector2 origin, Vector2 direction)
    {
        if (currentWeapon == null)
        {
            return;
        }

        CacheBaseScale();

        Vector2 normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        float laserDistance = Mathf.Max(0.1f, currentWeapon.laserDistance);
        float laserThickness = Mathf.Max(0.1f, currentWeapon.laserThickness);

        Vector2 beamCenter = origin + normalizedDirection * (laserDistance * 0.5f);
        float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.localScale = BuildLaserScale(laserDistance, laserThickness);
        transform.position = beamCenter;
    }

    public void TickDamage(Vector2 origin, Vector2 direction)
    {
        ApplyDamage(origin, direction, false);
    }

    private void ApplyDamage(Vector2 origin, Vector2 direction, bool force)
    {
        if (currentWeapon == null)
        {
            return;
        }

        float damageInterval = currentWeapon.attackRate > 0f
            ? currentWeapon.attackRate
            : 0.1f;

        if (!force && Time.time < nextDamageTime)
        {
            return;
        }

        Vector2 normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        float laserDistance = Mathf.Max(0.1f, currentWeapon.laserDistance);
        float laserThickness = Mathf.Max(0.1f, currentWeapon.laserThickness);
        Vector2 beamCenter = origin + normalizedDirection * (laserDistance * 0.5f);
        float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            beamCenter,
            new Vector2(laserDistance, laserThickness),
            angle);

        PlayerCombatDamage.DamageUniqueTargets(
            hits,
            currentWeapon.damage,
            origin,
            owner != null ? owner.root : null);

        nextDamageTime = Time.time + damageInterval;
    }

    private void CacheBaseScale()
    {
        if (hasCachedScale)
        {
            return;
        }

        baseScale = transform.localScale;
        hasCachedScale = true;
    }

    private Vector3 BuildLaserScale(float laserDistance, float laserThickness)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return new Vector3(
                baseScale.x * laserDistance,
                baseScale.y * laserThickness,
                baseScale.z);
        }

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float spriteWidth = Mathf.Max(0.001f, spriteSize.x);
        float spriteHeight = Mathf.Max(0.001f, spriteSize.y);

        float scaledX = Mathf.Sign(baseScale.x == 0f ? 1f : baseScale.x) * (laserDistance / spriteWidth);
        float scaledY = Mathf.Sign(baseScale.y == 0f ? 1f : baseScale.y) * (laserThickness / spriteHeight);

        return new Vector3(scaledX, scaledY, baseScale.z);
    }
}
