using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    private const string DamageAudioObjectName = "EnemyDamageSfx";

    [Header("Health")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private bool showHealthBar = true;

    [Header("Death")]
    [SerializeField] private GameObject deathExplosionPrefab;

    [Header("Audio")]
    [SerializeField] private AudioSource damageAudioSource;
    [SerializeField] private AudioClip damageSfx;
    [SerializeField, Range(0f, 1f)] private float damageSfxVolume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float damageSfxPitch = 1f;

    [Header("Health Bar")]
    [SerializeField] private float healthBarPadding = 0.12f;
    [SerializeField] private float minimumHealthBarWidth = 0.6f;
    [SerializeField] private float maximumHealthBarWidth = 1.75f;
    [SerializeField] private float minimumHealthBarHeight = 0.08f;
    [SerializeField] private float maximumHealthBarHeight = 0.18f;

    private int currentHealth;
    private SpriteRenderer sr;
    private Color originalColor = Color.white;
    private EnemyHealthBar healthBar;
    private Coroutine hitFlashRoutine;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float NormalizedHealth => maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;

        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalColor = sr.color;
        }

        EnsureDamageAudioSource();
        CreateHealthBar();
        UpdateHealthBar();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        PlayDamageSfx();

        if (sr != null && isActiveAndEnabled)
        {
            if (hitFlashRoutine != null)
            {
                StopCoroutine(hitFlashRoutine);
                hitFlashRoutine = null;
            }

            hitFlashRoutine = StartCoroutine(HitFlash());
        }

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void EnsureDamageAudioSource()
    {
        if (damageAudioSource == null)
        {
            Transform existingChild = transform.Find(DamageAudioObjectName);
            if (existingChild != null)
                damageAudioSource = existingChild.GetComponent<AudioSource>();
        }

        if (damageAudioSource == null)
        {
            GameObject audioObject = new GameObject(DamageAudioObjectName);
            audioObject.transform.SetParent(transform, false);
            damageAudioSource = audioObject.AddComponent<AudioSource>();
        }

        damageAudioSource.playOnAwake = false;
        damageAudioSource.loop = false;
        damageAudioSource.spatialBlend = 0f;
    }

    private void PlayDamageSfx()
    {
        if (damageSfx == null)
            return;

        EnsureDamageAudioSource();

        if (damageAudioSource == null)
            return;

        damageAudioSource.pitch = Mathf.Clamp(damageSfxPitch, 0.1f, 3f);
        damageAudioSource.PlayOneShot(damageSfx, Mathf.Clamp01(damageSfxVolume));
    }

    private void PreservePlayingDamageAudio()
    {
        if (damageAudioSource == null || !damageAudioSource.isPlaying)
            return;

        Transform audioTransform = damageAudioSource.transform;
        audioTransform.SetParent(null, true);
        Destroy(audioTransform.gameObject, (damageSfx != null ? damageSfx.length : 0.2f) + 0.1f);
        damageAudioSource = null;
    }

    private IEnumerator HitFlash()
    {
        if (sr == null)
        {
            yield break;
        }

        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if (sr != null)
        {
            sr.color = originalColor;
        }

        hitFlashRoutine = null;
    }

    private void CreateHealthBar()
    {
        if (!showHealthBar)
        {
            return;
        }

        Bounds bounds = GetHealthBarBounds();
        float width = Mathf.Clamp(bounds.size.x * 0.9f, minimumHealthBarWidth, maximumHealthBarWidth);
        float height = Mathf.Clamp(bounds.size.y * 0.12f, minimumHealthBarHeight, maximumHealthBarHeight);
        Vector3 offset = new Vector3(0f, bounds.extents.y + height + healthBarPadding, 0f);

        GameObject healthBarObject = new GameObject($"{name}_HealthBar");
        healthBar = healthBarObject.AddComponent<EnemyHealthBar>();
        healthBar.Initialize(transform, sr, offset, width, height);
    }

    private Bounds GetHealthBarBounds()
    {
        if (sr != null)
        {
            return sr.bounds;
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D collider2D in colliders)
        {
            if (collider2D != null && collider2D.enabled)
            {
                return collider2D.bounds;
            }
        }

        return new Bounds(transform.position, Vector3.one);
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(NormalizedHealth);
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        PreservePlayingDamageAudio();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }
    }
}
