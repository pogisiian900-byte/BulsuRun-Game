using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/New Weapon")]
public class WeaponData : ScriptableObject
{
    public string id;
    public string weaponName;
    public WeaponType weaponType;

    [Header("General")]
    public int damage;
    public float attackRate;

    [Header("Melee")]
    public float attackRange;
    public Vector2 meleeHitboxSize = new Vector2(1.5f, 0.8f);

    [Header("Projectile Weapons")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 2f;
    public bool rotateProjectileToDirection = true;

    [Header("Laser")]
    public GameObject laserPrefab;
    public float laserDistance = 6f;
    public float laserThickness = 0.6f;
    public float laserDuration = 0.25f;

    [Header("Rocket")]
    public float explosionRadius = 1.75f;
    public GameObject impactEffectPrefab;

    [Header("Spawner")]
    public GameObject spawnPrefab;
    public int spawnCount = 1;
    public float spawnLifetime = 10f;
    public Vector2 spawnOffset = new Vector2(1f, 0f);
    public Vector2 spawnStep = new Vector2(0.8f, 0f);

    [Header("Healing")]
    public bool healToFull = true;
    public int healAmount;
    public bool consumeOnUse = true;

    [Header("Audio")]
    public AudioClip useSfx;
    [Range(0f, 1f)] public float useSfxVolume = 1f;
    [Range(0.1f, 3f)] public float useSfxPitch = 1f;
    public bool loopUseSfxWhileActive;

    [Header("Visual")]
    public Sprite weaponSprite;
}
