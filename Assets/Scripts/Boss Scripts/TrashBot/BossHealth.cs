using UnityEngine;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour
{
    private const string DamageAudioObjectName = "BossDamageSfx";

    [SerializeField] private int maxHP = 100;
    public int CurrentHP { get; private set; }
    [SerializeField] private BossBattleUI bossBattleUI;

    [Header("Audio")]
    [SerializeField] private AudioSource damageAudioSource;
    [SerializeField] private AudioClip damageSfx;
    [SerializeField, Range(0f, 1f)] private float damageSfxVolume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float damageSfxPitch = 1f;

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged; // current, max
    public UnityEvent OnDeath;

    private BossKnockback knockback;

    private void Awake()
    {
        CurrentHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
        knockback = GetComponent<BossKnockback>();
        EnsureDamageAudioSource();
    }

    public void TakeDamage(int amount, Vector2 hitSource)
    {
        if (amount <= 0)
            return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        PlayDamageSfx();
        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if (knockback != null)
            knockback.ApplyKnockback(hitSource);

        if (CurrentHP == 0)
            OnDeath?.Invoke();
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

    public float Normalized => (maxHP <= 0) ? 0f : (float)CurrentHP / maxHP;

    void OnTriggerEnter2D(Collider2D other)
    {
        BossChase chase = GetComponent<BossChase>();
        if (chase != null && chase.IsChasing)
            return;

        if (other.CompareTag("Player"))
        {
            PlayerHealth player = other.GetComponent<PlayerHealth>();

            if (player != null)
            {
                player.TakeHeartDamage(1, transform.position);
            }
        }
    }
}
