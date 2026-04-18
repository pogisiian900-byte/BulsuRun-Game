using Photon.Pun;
using UnityEngine;

public class PlayerWeapon : MonoBehaviourPun
{
    private const string SlashOnePrefabName = "BlueSlash1";
    private const string SlashTwoPrefabName = "BlueSlash2";
    private const float MobileInputRetryInterval = 0.5f;
    private const string HeldWeaponObjectName = "HeldWeaponVisual";
    private const string WeaponOneShotAudioObjectName = "WeaponSfxOneShot";
    private const string WeaponLoopAudioObjectName = "WeaponSfxLoop";

    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Held Weapon Visual")]
    [SerializeField] private Transform heldWeaponAnchor;
    [SerializeField] private SpriteRenderer heldWeaponRenderer;
    [SerializeField] private Vector3 heldWeaponLocalOffset = new Vector3(0.55f, -0.02f, 0f);
    [SerializeField] private Vector3 heldWeaponLocalScale = new Vector3(0.18f, 0.18f, 1f);
    [SerializeField] private Vector3 heldWeaponLocalEulerAngles;
    [SerializeField] private int heldWeaponSortingOrderOffset = 1;

    [Header("Slash Effects")]
    [SerializeField] private Vector2 attackSize = new Vector2(1.5f, 0.8f);
    private int comboStep = 0;

    [Header("Audio")]
    [SerializeField] private AudioSource weaponOneShotSource;
    [SerializeField] private AudioSource weaponLoopSource;

    private WeaponData currentWeapon;
    private float nextAttackTime;

    private MobileInput mobile;
    private GameObject slashOnePrefab;
    private GameObject slashTwoPrefab;
    private float nextMobileLookupTime;
    private SpriteRenderer playerSpriteRenderer;
    private PlayerLaserBeam activeLaserBeam;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        playerHealth = GetComponent<PlayerHealth>();
        EnsureHeldWeaponRenderer();
        EnsureWeaponAudioSources();
        RefreshHeldWeaponVisual();

        if (ShouldHandleLocalAttacks())
        {
            TryResolveMobileInput(force: true);
        }

        CacheSlashPrefabs();
    }

    void Update()
    {
        UpdateHeldWeaponAnchor();

        if (!ShouldHandleLocalAttacks())
            return;

        if (currentWeapon == null) return;

        TryResolveMobileInput();

        bool attackInput =
            Input.GetKey(KeyCode.L) ||
            (mobile != null && mobile.AttackHeld);

        if (currentWeapon.weaponType == WeaponType.Laser)
        {
            HandleLaserAttack(attackInput);
            return;
        }

        if (!attackInput && activeLaserBeam != null)
        {
            StopLaserAttack();
        }

        if (attackInput && Time.time >= nextAttackTime)
        {
            Attack();
            float attackCooldown = currentWeapon.attackRate > 0f
                ? currentWeapon.attackRate
                : 0.1f;

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private bool ShouldHandleLocalAttacks()
    {
        return !PhotonNetwork.InRoom || photonView.IsMine;
    }

    private void CacheSlashPrefabs()
    {
        slashOnePrefab = Resources.Load<GameObject>(SlashOnePrefabName);
        slashTwoPrefab = Resources.Load<GameObject>(SlashTwoPrefabName);
    }

    private void TryResolveMobileInput(bool force = false)
    {
        if (mobile != null)
        {
            return;
        }

        if (!force && Time.time < nextMobileLookupTime)
        {
            return;
        }

        mobile = FindFirstObjectByType<MobileInput>();
        nextMobileLookupTime = Time.time + MobileInputRetryInterval;
    }

    public void ResetAttackState()
    {
        nextAttackTime = 0f;
        comboStep = 0;
        StopLaserAttack();
        StopLoopingWeaponAudio();
    }

    public void EquipWeapon(WeaponData weapon)
    {
        if (currentWeapon == weapon)
        {
            RefreshHeldWeaponVisual();
            return;
        }

        StopLaserAttack();
        StopLoopingWeaponAudio();
        currentWeapon = weapon;
        RefreshHeldWeaponVisual();

        if (weapon == null)
        {
            Debug.Log("Unequipped weapon");
            return;
        }

        Debug.Log("Equipped: " + weapon.weaponName);
    }

    private void EnsureHeldWeaponRenderer()
    {
        if (heldWeaponRenderer != null)
        {
            return;
        }

        Transform existingChild = transform.Find(HeldWeaponObjectName);
        if (existingChild != null)
        {
            heldWeaponRenderer = existingChild.GetComponent<SpriteRenderer>();
        }

        if (heldWeaponRenderer == null)
        {
            GameObject heldWeaponObject = new GameObject(HeldWeaponObjectName);
            Transform parent = heldWeaponAnchor != null ? heldWeaponAnchor : transform;
            heldWeaponObject.transform.SetParent(parent, false);
            heldWeaponRenderer = heldWeaponObject.AddComponent<SpriteRenderer>();
        }

        ReparentHeldWeaponRenderer();
        UpdateHeldWeaponAnchor();
        ApplyHeldWeaponSorting();
        heldWeaponRenderer.enabled = false;
    }

    private void RefreshHeldWeaponVisual()
    {
        EnsureHeldWeaponRenderer();

        if (heldWeaponRenderer == null)
        {
            return;
        }

        ApplyHeldWeaponSorting();
        UpdateHeldWeaponAnchor();

        if (currentWeapon == null || currentWeapon.weaponSprite == null)
        {
            heldWeaponRenderer.sprite = null;
            heldWeaponRenderer.enabled = false;
            return;
        }

        heldWeaponRenderer.sprite = currentWeapon.weaponSprite;
        heldWeaponRenderer.enabled = true;
    }

    private void UpdateHeldWeaponAnchor()
    {
        if (heldWeaponRenderer == null)
        {
            return;
        }

        if (heldWeaponAnchor != null)
        {
            if (heldWeaponRenderer.transform.parent != heldWeaponAnchor)
            {
                heldWeaponRenderer.transform.SetParent(heldWeaponAnchor, false);
            }

            heldWeaponRenderer.transform.localPosition = Vector3.zero;
        }
        else
        {
            if (heldWeaponRenderer.transform.parent != transform)
            {
                heldWeaponRenderer.transform.SetParent(transform, false);
            }

            Vector3 localPosition = attackPoint != null
                ? attackPoint.localPosition
                : heldWeaponLocalOffset;

            heldWeaponRenderer.transform.localPosition = localPosition;
        }

        heldWeaponRenderer.transform.localRotation = Quaternion.Euler(heldWeaponLocalEulerAngles);
        heldWeaponRenderer.transform.localScale = heldWeaponLocalScale;
    }

    private void ApplyHeldWeaponSorting()
    {
        if (heldWeaponRenderer == null)
        {
            return;
        }

        if (playerSpriteRenderer == null)
        {
            playerSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (playerSpriteRenderer == null)
        {
            return;
        }

        heldWeaponRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
        heldWeaponRenderer.sortingOrder = playerSpriteRenderer.sortingOrder + heldWeaponSortingOrderOffset;
    }

    private void ReparentHeldWeaponRenderer()
    {
        if (heldWeaponRenderer == null)
        {
            return;
        }

        Transform desiredParent = heldWeaponAnchor != null ? heldWeaponAnchor : transform;

        if (heldWeaponRenderer.transform.parent != desiredParent)
        {
            heldWeaponRenderer.transform.SetParent(desiredParent, false);
        }
    }

    private void EnsureWeaponAudioSources()
    {
        weaponOneShotSource = EnsureAudioSource(weaponOneShotSource, WeaponOneShotAudioObjectName);
        weaponLoopSource = EnsureAudioSource(weaponLoopSource, WeaponLoopAudioObjectName);

        ConfigureAudioSource(weaponOneShotSource, false);
        ConfigureAudioSource(weaponLoopSource, true);
    }

    private AudioSource EnsureAudioSource(AudioSource existingSource, string objectName)
    {
        if (existingSource != null)
        {
            return existingSource;
        }

        Transform existingChild = transform.Find(objectName);
        if (existingChild != null)
        {
            AudioSource childSource = existingChild.GetComponent<AudioSource>();
            if (childSource != null)
            {
                return childSource;
            }
        }

        GameObject audioObject = new GameObject(objectName);
        audioObject.transform.SetParent(transform, false);
        return audioObject.AddComponent<AudioSource>();
    }

    private static void ConfigureAudioSource(AudioSource source, bool loop)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
    }

    void Attack()
    {
        if (currentWeapon == null)
        {
            return;
        }

        switch (currentWeapon.weaponType)
        {
            case WeaponType.Melee:
                MeleeAttack();
                break;

            case WeaponType.Ranged:
                ProjectileAttack();
                break;

            case WeaponType.Laser:
                LaserAttack();
                break;

            case WeaponType.Rocket:
                RocketAttack();
                break;

            case WeaponType.Spawner:
                SpawnerAttack();
                break;

            case WeaponType.Healing:
                HealingAttack();
                break;
        }
    }

    void MeleeAttack()
    {
        Vector2 attackOrigin = GetAttackOrigin();
        Vector2 meleeHitboxSize = GetMeleeHitboxSize();

        // SPAWN SLASH EFFECT
        Quaternion rot = transform.localScale.x >= 0
            ? Quaternion.identity
            : Quaternion.Euler(0, 180, 0);

        if (comboStep == 0)
        {
            SpawnSlashEffect(SlashOnePrefabName, slashOnePrefab, attackOrigin, rot);
            comboStep = 1;
        }
        else
        {
            SpawnSlashEffect(SlashTwoPrefabName, slashTwoPrefab, attackOrigin, rot);
            comboStep = 0;
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            attackOrigin,
            meleeHitboxSize,
            0f);

        PlayerCombatDamage.DamageUniqueTargets(
            hits,
            currentWeapon.damage,
            transform.position,
            transform.root);

        PlayWeaponUseSfx(currentWeapon);
    }

    private Vector2 GetMeleeHitboxSize()
    {
        Vector2 meleeHitboxSize = currentWeapon != null &&
                                  currentWeapon.meleeHitboxSize.x > 0f &&
                                  currentWeapon.meleeHitboxSize.y > 0f
            ? currentWeapon.meleeHitboxSize
            : attackSize;

        if (currentWeapon != null && currentWeapon.attackRange > 0f)
        {
            meleeHitboxSize.x = currentWeapon.attackRange;
        }

        return meleeHitboxSize;
    }

    private void SpawnSlashEffect(string prefabName, GameObject localPrefab, Vector3 position, Quaternion rotation)
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.Instantiate(prefabName, position, rotation);
            return;
        }

        if (localPrefab == null)
        {
            Debug.LogWarning($"Melee slash prefab '{prefabName}' was not found in Resources.");
            return;
        }

        Instantiate(localPrefab, position, rotation);
    }

    void ProjectileAttack()
    {
        if (currentWeapon.projectilePrefab == null)
        {
            Debug.LogWarning($"Weapon '{currentWeapon.weaponName}' is missing a projectile prefab.");
            return;
        }

        GameObject projectileObject = Instantiate(
            currentWeapon.projectilePrefab,
            GetAttackOrigin(),
            Quaternion.identity);

        PlayerProjectiles projectile = projectileObject.GetComponent<PlayerProjectiles>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<PlayerProjectiles>();
        }

        projectile.Fire(GetAttackDirection(), currentWeapon, transform);
        PlayWeaponUseSfx(currentWeapon);
    }

    void RocketAttack()
    {
        if (currentWeapon.projectilePrefab == null)
        {
            Debug.LogWarning($"Rocket weapon '{currentWeapon.weaponName}' is missing a projectile prefab.");
            return;
        }

        GameObject rocketObject = Instantiate(
            currentWeapon.projectilePrefab,
            GetAttackOrigin(),
            Quaternion.identity);

        PlayerRocketProjectile rocket = rocketObject.GetComponent<PlayerRocketProjectile>();
        if (rocket == null)
        {
            rocket = rocketObject.AddComponent<PlayerRocketProjectile>();
        }

        rocket.Initialize(GetAttackDirection(), currentWeapon, transform);
        PlayWeaponUseSfx(currentWeapon);
    }

    void LaserAttack()
    {
        if (activeLaserBeam != null)
        {
            return;
        }

        GameObject laserObject;

        if (currentWeapon.laserPrefab != null)
        {
            laserObject = Instantiate(
                currentWeapon.laserPrefab,
                GetAttackOrigin(),
                Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"Weapon '{currentWeapon.weaponName}' is missing a laser prefab. Spawning a logic-only laser.");
            laserObject = new GameObject($"{currentWeapon.weaponName}_Laser");
            laserObject.transform.position = GetAttackOrigin();
        }

        PlayerLaserBeam laserBeam = laserObject.GetComponent<PlayerLaserBeam>();
        if (laserBeam == null)
        {
            laserBeam = laserObject.AddComponent<PlayerLaserBeam>();
        }

        laserBeam.Initialize(GetAttackOrigin(), GetAttackDirection(), currentWeapon, transform);
        activeLaserBeam = laserBeam;
        BeginHeldWeaponUseSfx(currentWeapon);
    }

    private void HandleLaserAttack(bool attackHeld)
    {
        if (!attackHeld)
        {
            StopLaserAttack();
            return;
        }

        if (activeLaserBeam == null)
        {
            LaserAttack();
        }

        if (activeLaserBeam == null)
        {
            return;
        }

        Vector2 attackOrigin = GetAttackOrigin();
        Vector2 attackDirection = GetAttackDirection();

        activeLaserBeam.UpdateBeam(attackOrigin, attackDirection);
        activeLaserBeam.TickDamage(attackOrigin, attackDirection);
    }

    private void StopLaserAttack()
    {
        if (activeLaserBeam == null)
        {
            StopLoopingWeaponAudio();
            return;
        }

        if (activeLaserBeam.gameObject != null)
        {
            Destroy(activeLaserBeam.gameObject);
        }

        activeLaserBeam = null;
        StopLoopingWeaponAudio();
    }

    void SpawnerAttack()
    {
        if (currentWeapon.spawnPrefab == null)
        {
            Debug.LogWarning($"Weapon '{currentWeapon.weaponName}' is missing a spawn prefab.");
            return;
        }

        int count = Mathf.Max(1, currentWeapon.spawnCount);
        Vector2 direction = GetAttackDirection();
        Vector2 origin = GetAttackOrigin() + MirrorVectorForFacing(currentWeapon.spawnOffset, direction);
        Vector2 step = MirrorVectorForFacing(currentWeapon.spawnStep, direction);

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnPosition = origin + (step * i);
            GameObject spawnedObject = Instantiate(
                currentWeapon.spawnPrefab,
                spawnPosition,
                Quaternion.identity);

            Vector3 spawnedScale = spawnedObject.transform.localScale;
            spawnedScale.x = Mathf.Abs(spawnedScale.x) * Mathf.Sign(direction.x);
            spawnedObject.transform.localScale = spawnedScale;

            if (currentWeapon.spawnLifetime > 0f)
            {
                Destroy(spawnedObject, currentWeapon.spawnLifetime);
            }
        }

        PlayWeaponUseSfx(currentWeapon);
    }

    void HealingAttack()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning($"Healing weapon '{currentWeapon.weaponName}' could not find PlayerHealth.");
            return;
        }

        if (playerHealth.currentHP >= playerHealth.MaxHP)
        {
            return;
        }

        if (currentWeapon.healToFull)
        {
            playerHealth.HealToFull();
        }
        else
        {
            int healAmount = currentWeapon.healAmount > 0
                ? currentWeapon.healAmount
                : playerHealth.MaxHP;

            playerHealth.Heal(healAmount);
        }

        PlayWeaponUseSfx(currentWeapon);

        if (!currentWeapon.consumeOnUse)
        {
            return;
        }

        Inventory inventory = Inventory.Instance != null
            ? Inventory.Instance
            : FindFirstObjectByType<Inventory>();

        if (inventory == null || !inventory.ConsumeEquippedWeapon(currentWeapon))
        {
            Debug.LogWarning($"Healing weapon '{currentWeapon.weaponName}' healed the player but could not be consumed from the inventory.");
        }
    }

    private void PlayWeaponUseSfx(WeaponData weapon)
    {
        if (weapon == null || weapon.useSfx == null)
        {
            return;
        }

        EnsureWeaponAudioSources();

        if (weaponOneShotSource == null)
        {
            return;
        }

        weaponOneShotSource.pitch = Mathf.Clamp(weapon.useSfxPitch, 0.1f, 3f);
        weaponOneShotSource.PlayOneShot(weapon.useSfx, Mathf.Clamp01(weapon.useSfxVolume));
    }

    private void BeginHeldWeaponUseSfx(WeaponData weapon)
    {
        if (weapon == null || weapon.useSfx == null)
        {
            return;
        }

        if (!weapon.loopUseSfxWhileActive)
        {
            PlayWeaponUseSfx(weapon);
            return;
        }

        EnsureWeaponAudioSources();

        if (weaponLoopSource == null)
        {
            return;
        }

        weaponLoopSource.pitch = Mathf.Clamp(weapon.useSfxPitch, 0.1f, 3f);
        weaponLoopSource.volume = Mathf.Clamp01(weapon.useSfxVolume);
        weaponLoopSource.clip = weapon.useSfx;

        if (!weaponLoopSource.isPlaying)
        {
            weaponLoopSource.Play();
        }
    }

    private void StopLoopingWeaponAudio()
    {
        if (weaponLoopSource == null)
        {
            return;
        }

        weaponLoopSource.Stop();
        weaponLoopSource.clip = null;
    }

    private Vector2 GetAttackOrigin()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
    }

    private Vector2 GetAttackDirection()
    {
        return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
    }

    private Vector2 MirrorVectorForFacing(Vector2 vector, Vector2 direction)
    {
        return new Vector2(
            Mathf.Abs(vector.x) * Mathf.Sign(direction.x),
            vector.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackSize);
    }

    private void OnDisable()
    {
        StopLaserAttack();
        StopLoopingWeaponAudio();
    }
}
