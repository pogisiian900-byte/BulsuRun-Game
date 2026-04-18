using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviourPun
{
    public const int HP_PER_HEART = 20;
    private const string DamageAudioObjectName = "PlayerDamageSfx";

    [Header("Health Settings")]
    public int baseMaxHP = 100;
    public int currentHP;

    private int bonusHP;

    [Header("UI")]
    [SerializeField] private HeartUI heartUI;
    [SerializeField] private Color stunOverlayTopColor = new Color(1f, 0.92f, 0.35f, 0.16f);
    [SerializeField] private Color stunOverlayBottomColor = new Color(1f, 0.98f, 0.75f, 0.04f);
    [SerializeField] private int lowHealthOverlayThreshold = 25;
    [SerializeField] private Color lowHealthOverlayTopColor = new Color(1f, 0.1f, 0.1f, 0.6f);
    [SerializeField] private Color lowHealthOverlayBottomColor = new Color(0.75f, 0f, 0f, 0.12f);
    [SerializeField] private float lowHealthOverlayBlinkSpeed = 5f;
    [SerializeField] private float lowHealthOverlayMinAlpha = 0.22f;
    [SerializeField] private float lowHealthOverlayMaxAlpha = 0.55f;
    [SerializeField] private int stunOverlaySortingOrder = 250;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 6f;
    [SerializeField] private float knockbackForceY = 4f;

    [Header("Audio")]
    [SerializeField] private AudioSource damageAudioSource;
    [SerializeField] private AudioClip damageSfx;
    [SerializeField, Range(0f, 1f)] private float damageSfxVolume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float damageSfxPitch = 1f;
    [SerializeField] private AudioClip stunSfx;
    [SerializeField, Range(0f, 1f)] private float stunSfxVolume = 0.85f;
    [SerializeField, Range(0.1f, 3f)] private float stunSfxPitch = 1f;
    [SerializeField] private AudioClip deathExplosionSfx;
    [SerializeField, Range(0f, 1f)] private float deathExplosionSfxVolume = 1f;
    [SerializeField, Range(0.1f, 3f)] private float deathExplosionSfxPitch = 1f;

    private bool isInvincible;
    private bool isKnockback;
    private bool canTakeDamage = true;
    private bool isStunned;
    private Rigidbody2D rb;
    private PlayerMovement movement;
    private PlayerInput playerInput;
    private SpriteRenderer spriteRenderer;
    private CanvasGroup stunOverlayCanvasGroup;
    private Image stunOverlayImage;
    private static Sprite stunOverlaySprite;
    private static Sprite lowHealthOverlaySprite;

    public int MaxHP => baseMaxHP + bonusHP;
    public int BaseMaxHP => baseMaxHP;
    public int BonusHP => bonusHP;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>();
        EnsureDamageAudioSource();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ResetRuntimeState();
        ApplySavedHealth();

        if (!ShouldHandleLocalState())
            return;

        StartCoroutine(RefreshSceneState(false));
    }

    private void Update()
    {
        if (!ShouldHandleLocalState())
            return;

        UpdateLowHealthOverlay();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!ShouldHandleLocalState())
            return;

        StopAllCoroutines();
        StartCoroutine(RefreshSceneState(true));
    }

    private bool ShouldHandleLocalState()
    {
        return !PhotonNetwork.InRoom || photonView.IsMine;
    }

    private void ApplySavedHealth()
    {
        if (GameData.PlayerHealth <= 0)
        {
            currentHP = MaxHP;
        }
        else
        {
            currentHP = Mathf.Clamp(GameData.PlayerHealth, 0, MaxHP);
        }

        GameData.PlayerHealth = currentHP;
    }

    public void LoadFromGameData()
    {
        if (!ShouldHandleLocalState())
            return;

        StopAllCoroutines();
        StartCoroutine(RefreshSceneState(false));
    }

    private void ResetRuntimeState()
    {
        canTakeDamage = true;
        isInvincible = false;
        isKnockback = false;
        isStunned = false;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        HideStunOverlayImmediate();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (movement != null && ShouldHandleLocalState())
            movement.EnableMovement(true);

        if (playerInput != null && ShouldHandleLocalState())
            playerInput.SetMovementInputEnabled(true);
    }

    private IEnumerator RefreshSceneState(bool repositionPlayer)
    {
        yield return null;

        bool shouldGrantRespawnInvincibility = GameData.PlayerHealth <= 0;

        ResetRuntimeState();
        ApplySavedHealth();

        if (repositionPlayer)
            RespawnToSceneSpawn(shouldGrantRespawnInvincibility);

        heartUI = null;

        int waitFrames = 0;
        while (heartUI == null && waitFrames < 30)
        {
            heartUI = FindObjectOfType<HeartUI>(true);
            if (heartUI != null)
            {
                break;
            }

            waitFrames++;
            yield return null;
        }

        EnsureStunOverlay();

        if (heartUI == null)
        {
            // Debug.LogError("HeartUI not found!");
            yield break;
        }

        RebuildHeartsUI();
        UpdateUI();
    }

    public void ResetBonusHP()
    {
        bonusHP = 0;
        currentHP = Mathf.Clamp(currentHP, 0, MaxHP);
        GameData.PlayerHealth = currentHP;
        RebuildHeartsUI();
        UpdateUI();
    }

    public void Stun(float duration)
    {
        if (isStunned)
            return;

        if (ShouldHandleLocalState())
            SceneAudioManager.PlayStunSfx(stunSfx, stunSfxVolume, stunSfxPitch);

        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        EnsureStunOverlay();
        SetOverlaySprite(GetOrCreateStunOverlaySprite());

        if (movement != null)
            movement.EnableMovement(false, true);

        if (playerInput != null && ShouldHandleLocalState())
            playerInput.SetMovementInputEnabled(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        float timer = 0f;

        while (timer < duration)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (spriteRenderer != null)
                spriteRenderer.color = Color.yellow;

            SetStunOverlayAlpha(1f);
            yield return new WaitForSeconds(0.1f);

            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;

            SetStunOverlayAlpha(0.45f);
            yield return new WaitForSeconds(0.1f);

            timer += 0.2f;
        }

        HideStunOverlayImmediate();

        if (movement != null)
            movement.EnableMovement(true);

        if (playerInput != null && ShouldHandleLocalState())
            playerInput.SetMovementInputEnabled(true);

        isStunned = false;
    }

    public void TakeHeartDamage(int hearts, Vector2 hitSource)
    {
        TakeDamage(hearts * HP_PER_HEART, hitSource);
    }

    public void TakeDamage(int damage, Vector2 hitSource)
    {
        if (!canTakeDamage)
            return;

        canTakeDamage = false;

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            TakeDamageRPC(damage, hitSource);
        }
        else if (photonView.IsMine)
        {
            photonView.RPC(nameof(TakeDamageRPC), RpcTarget.All, damage, hitSource);
        }

        StartCoroutine(DamageCooldown());
    }

    [PunRPC]
    private void TakeDamageRPC(int damage, Vector2 hitSource)
    {
        if (isInvincible)
            return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, MaxHP);
        GameData.PlayerHealth = currentHP;

        if (ShouldHandleLocalState())
            PlayDamageSfx();

        if (ShouldHandleLocalState())
            UpdateUI();

        StartCoroutine(Invincibility());

        Vector2 hitDir = (transform.position - (Vector3)hitSource).normalized;
        StartCoroutine(Knockback(hitDir));

        if (currentHP <= 0)
            Die();
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

    private IEnumerator Knockback(Vector2 direction)
    {
        isKnockback = true;

        if (movement != null)
            movement.EnableMovement(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(
                new Vector2(direction.x * knockbackForceX, knockbackForceY),
                ForceMode2D.Impulse
            );
        }

        float timer = 0f;

        while (timer < 0.2f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isKnockback = false;

        if (movement != null)
            movement.EnableMovement(true);
    }

    private IEnumerator DamageCooldown()
    {
        yield return new WaitForSeconds(0.5f);
        canTakeDamage = true;
    }

    public void Heal(int amount)
    {
        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, MaxHP);

        GameData.PlayerHealth = currentHP;
        UpdateUI();
    }

    public void HealToFull()
    {
        currentHP = MaxHP;
        GameData.PlayerHealth = currentHP;
        UpdateUI();
    }

    public void HealHearts(int hearts) => Heal(hearts * HP_PER_HEART);

    public void AddMaxHP(int amount)
    {
        if (amount <= 0)
            return;

        bonusHP += amount;
        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, MaxHP);
        GameData.PlayerHealth = currentHP;

        RebuildHeartsUI();
        UpdateUI();

        Debug.Log($"[HP SKILL] bonusHP={bonusHP} MaxHP={MaxHP} currentHP={currentHP}");
    }

    private void RebuildHeartsUI()
    {
        if (heartUI == null)
            return;

        int baseHearts = Mathf.CeilToInt(baseMaxHP / (float)HP_PER_HEART);
        int totalHearts = Mathf.CeilToInt(MaxHP / (float)HP_PER_HEART);

        heartUI.BuildHearts(totalHearts, baseHearts);
    }

    private void UpdateUI()
    {
        if (heartUI == null)
            return;

        heartUI.UpdateHearts(currentHP, HP_PER_HEART);
    }

    private IEnumerator Invincibility()
    {
        isInvincible = true;

        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            timer += 0.2f;
        }

        isInvincible = false;
    }

    private void EnsureStunOverlay()
    {
        if (!ShouldHandleLocalState())
            return;

        if (stunOverlayCanvasGroup != null && stunOverlayImage != null)
            return;

        GameObject overlayRoot = GameObject.Find("PlayerStunOverlayCanvas");
        if (overlayRoot == null)
        {
            overlayRoot = new GameObject(
                "PlayerStunOverlayCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup)
            );
        }

        Canvas overlayCanvas = overlayRoot.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = stunOverlaySortingOrder;

        CanvasScaler canvasScaler = overlayRoot.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = overlayRoot.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        stunOverlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
        stunOverlayCanvasGroup.alpha = 0f;
        stunOverlayCanvasGroup.interactable = false;
        stunOverlayCanvasGroup.blocksRaycasts = false;

        Transform overlayTransform = overlayRoot.transform.Find("PlayerStunOverlay");
        if (overlayTransform == null)
        {
            GameObject overlayImageObject = new GameObject("PlayerStunOverlay", typeof(RectTransform), typeof(Image));
            overlayTransform = overlayImageObject.transform;
            overlayTransform.SetParent(overlayRoot.transform, false);
        }

        RectTransform rectTransform = overlayTransform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        stunOverlayImage = overlayTransform.GetComponent<Image>();
        stunOverlayImage.sprite = GetOrCreateStunOverlaySprite();
        stunOverlayImage.type = Image.Type.Simple;
        stunOverlayImage.preserveAspect = false;
        stunOverlayImage.raycastTarget = false;
        stunOverlayImage.color = Color.white;
    }

    private void UpdateLowHealthOverlay()
    {
        if (isStunned)
            return;

        if (currentHP <= 0 || currentHP > lowHealthOverlayThreshold)
        {
            HideStunOverlayImmediate();
            return;
        }

        EnsureStunOverlay();
        SetOverlaySprite(GetOrCreateLowHealthOverlaySprite());

        float pulse = (Mathf.Sin(Time.unscaledTime * lowHealthOverlayBlinkSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(lowHealthOverlayMinAlpha, lowHealthOverlayMaxAlpha, pulse);
        SetStunOverlayAlpha(alpha);
    }

    private void SetOverlaySprite(Sprite sprite)
    {
        if (stunOverlayImage == null || sprite == null)
            return;

        if (stunOverlayImage.sprite != sprite)
            stunOverlayImage.sprite = sprite;
    }

    private void SetStunOverlayAlpha(float normalizedAlpha)
    {
        EnsureStunOverlay();

        if (stunOverlayCanvasGroup == null)
            return;

        stunOverlayCanvasGroup.alpha = Mathf.Clamp01(normalizedAlpha);
    }

    private void HideStunOverlayImmediate()
    {
        if (stunOverlayCanvasGroup != null)
            stunOverlayCanvasGroup.alpha = 0f;
    }

    private Sprite GetOrCreateStunOverlaySprite()
    {
        if (stunOverlaySprite != null)
            return stunOverlaySprite;

        stunOverlaySprite = CreateOverlaySprite(
            "PlayerStunOverlayGradient",
            stunOverlayBottomColor,
            stunOverlayTopColor
        );
        return stunOverlaySprite;
    }

    private Sprite GetOrCreateLowHealthOverlaySprite()
    {
        if (lowHealthOverlaySprite != null)
            return lowHealthOverlaySprite;

        lowHealthOverlaySprite = CreateOverlaySprite(
            "PlayerLowHealthOverlayGradient",
            lowHealthOverlayBottomColor,
            lowHealthOverlayTopColor
        );
        return lowHealthOverlaySprite;
    }

    private static Sprite CreateOverlaySprite(string textureName, Color bottomColor, Color topColor)
    {
        const int gradientHeight = 64;
        Texture2D texture = new Texture2D(1, gradientHeight, TextureFormat.RGBA32, false);
        texture.name = textureName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < gradientHeight; y++)
        {
            float t = y / (gradientHeight - 1f);
            texture.SetPixel(0, y, Color.Lerp(bottomColor, topColor, t));
        }

        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    private void Die()
    {
        canTakeDamage = true;
        isInvincible = false;

        if (ShouldHandleLocalState())
            SceneAudioManager.PlayExplosionSfx(deathExplosionSfx, deathExplosionSfxVolume, deathExplosionSfxPitch);

        if (movement != null)
            movement.EnableMovement(false, true);

        if (playerInput != null && ShouldHandleLocalState())
            playerInput.SetMovementInputEnabled(false);

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.Lose();
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(GameOverRPC), RpcTarget.All);
        }
    }

    [PunRPC]
    private void GameOverRPC()
    {
        if (movement != null)
            movement.EnableMovement(false, true);

        if (playerInput != null && ShouldHandleLocalState())
            playerInput.SetMovementInputEnabled(false);

        if (LevelManager.Instance != null)
            LevelManager.Instance.Lose();
    }

    private void RespawnToSceneSpawn(bool grantInvincibility)
    {
        SpawnPlayer spawnPoint = SpawnPlayer.Instance != null
            ? SpawnPlayer.Instance
            : FindFirstObjectByType<SpawnPlayer>();

        if (spawnPoint != null)
        {
            spawnPoint.RespawnPlayer(transform);
        }
        else
        {
            GameObject spawnObj = GameObject.FindGameObjectWithTag("SpawnPoint");
            if (spawnObj == null)
            {
                Debug.LogWarning("No SpawnPoint found in this scene.");
                return;
            }

            transform.position = spawnObj.transform.position;
        }

        if (grantInvincibility)
            StartCoroutine(Invincibility());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Damage"))
            return;

        if (!canTakeDamage)
            return;

        Vector2 hitSource = collision.transform.position;
        TakeDamage(10, hitSource);
    }

    [PunRPC]
    public void RequestLoadLevelRpc(string sceneName)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        PhotonNetwork.LoadLevel(sceneName);
    }

    [PunRPC]
    public void SynchronizeDoorEntryRpc(string doorIdentifier)
    {
        if (string.IsNullOrWhiteSpace(doorIdentifier))
        {
            return;
        }

        Door targetDoor = Door.FindDoorByIdentifier(doorIdentifier);
        if (targetDoor == null)
        {
            Debug.LogWarning($"Door '{doorIdentifier}' could not be found for synchronized entry.");
            return;
        }

        targetDoor.SynchronizeLocalPlayerEntry();
    }
}
