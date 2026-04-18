using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class SceneAudioManager : MonoBehaviour
{
    private const string CatalogResourcePath = "SceneAudioCatalog";

    private static SceneAudioManager instance;
    private static AudioClip defaultCoinPickupClip;
    private static AudioClip defaultStunClip;
    private static AudioClip defaultExplosionClip;
    private static AudioClip defaultTurretShotClip;
    private static AudioClip defaultStunTurretShotClip;

    [SerializeField] private SceneAudioCatalog catalog;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private bool hasLoggedMissingCatalog;
    private Coroutine delayedMusicCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject managerObject = new GameObject(nameof(SceneAudioManager));
        instance = managerObject.AddComponent<SceneAudioManager>();
        DontDestroyOnLoad(managerObject);
    }

    private void Reset()
    {
        EnsureAudioSource();
        EnsureSfxSource();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSource();
        EnsureSfxSource();
        LoadCatalogIfNeeded();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        ApplySceneAudio(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        CancelPendingTemporaryMusic();
        ApplySceneAudio(scene.name);
    }

    private void LoadCatalogIfNeeded()
    {
        if (catalog != null)
            return;

        catalog = Resources.Load<SceneAudioCatalog>(CatalogResourcePath);

        if (catalog == null && !hasLoggedMissingCatalog)
        {
            Debug.LogWarning("SceneAudioManager could not find Resources/SceneAudioCatalog. Scene music is set up, but no clips can play until that asset exists.", this);
            hasLoggedMissingCatalog = true;
        }
    }

    private void EnsureAudioSource()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
    }

    private void EnsureSfxSource()
    {
        if (sfxSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            foreach (AudioSource source in sources)
            {
                if (source != null && source != musicSource)
                {
                    sfxSource = source;
                    break;
                }
            }
        }

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void ApplySceneAudio(string sceneName)
    {
        EnsureAudioSource();
        LoadCatalogIfNeeded();

        if (catalog != null && catalog.TryGetSceneSettings(sceneName, out SceneAudioEntry sceneEntry))
        {
            if (sceneEntry.musicClip == null)
            {
                StopMusic();
                return;
            }

            Play(sceneEntry.musicClip, sceneEntry.volume, sceneEntry.loop);
            return;
        }

        if (catalog != null && catalog.TryGetFallback(out SceneAudioEntry fallbackEntry))
        {
            Play(fallbackEntry.musicClip, fallbackEntry.volume, fallbackEntry.loop);
            return;
        }

        if (catalog == null || catalog.StopWhenSceneHasNoEntry)
            StopMusic();
    }

    private void Play(AudioClip clip, float volume, bool loop)
    {
        if (musicSource.clip != clip)
        {
            musicSource.Stop();
            musicSource.clip = clip;
        }

        musicSource.volume = Mathf.Clamp01(volume);
        musicSource.loop = loop;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    private void StopMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
        musicSource.clip = null;
    }

    public static void PlayCoinPickupSfx(AudioClip clipOverride = null, float volume = 1f, float pitch = 1f)
    {
        EnsureInstance();
        instance.PlayOneShot(clipOverride != null ? clipOverride : GetDefaultCoinPickupClip(), volume, pitch);
    }

    public static void PlayStunSfx(AudioClip clipOverride = null, float volume = 1f, float pitch = 1f)
    {
        EnsureInstance();
        instance.PlayOneShot(clipOverride != null ? clipOverride : GetDefaultStunClip(), volume, pitch);
    }

    public static void PlayExplosionSfx(AudioClip clipOverride = null, float volume = 1f, float pitch = 1f)
    {
        EnsureInstance();
        instance.PlayOneShot(clipOverride != null ? clipOverride : GetDefaultExplosionClip(), volume, pitch);
    }

    public static void PlayTurretShotSfx(AudioClip clipOverride = null, float volume = 1f, float pitch = 1f)
    {
        EnsureInstance();
        instance.PlayOneShot(clipOverride != null ? clipOverride : GetDefaultTurretShotClip(), volume, pitch);
    }

    public static void PlayStunTurretShotSfx(AudioClip clipOverride = null, float volume = 1f, float pitch = 1f)
    {
        EnsureInstance();
        instance.PlayOneShot(clipOverride != null ? clipOverride : GetDefaultStunTurretShotClip(), volume, pitch);
    }

    public static void StopMusicPlayback()
    {
        EnsureInstance();
        instance.CancelPendingTemporaryMusic();
        instance.StopMusic();
    }

    public static void PlayTemporaryMusic(AudioClip clip, float volume = 1f, bool loop = true, float delay = 0f)
    {
        EnsureInstance();
        instance.PlayTemporaryMusicInternal(clip, volume, loop, delay);
    }

    public static void CancelTemporaryMusic(bool resumeSceneMusic = false)
    {
        EnsureInstance();
        instance.CancelTemporaryMusicInternal(resumeSceneMusic);
    }

    private static void EnsureInstance()
    {
        if (instance == null)
            Bootstrap();

        if (instance != null)
        {
            instance.EnsureAudioSource();
            instance.EnsureSfxSource();
            instance.LoadCatalogIfNeeded();
        }
    }

    private void PlayOneShot(AudioClip clip, float volume, float pitch)
    {
        if (clip == null)
            return;

        EnsureSfxSource();

        if (sfxSource == null)
            return;

        sfxSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void PlayTemporaryMusicInternal(AudioClip clip, float volume, bool loop, float delay)
    {
        CancelPendingTemporaryMusic();

        if (clip == null)
            return;

        if (delay <= 0f)
        {
            Play(clip, volume, loop);
            return;
        }

        delayedMusicCoroutine = StartCoroutine(PlayTemporaryMusicAfterDelay(clip, volume, loop, delay));
    }

    private void CancelTemporaryMusicInternal(bool resumeSceneMusic)
    {
        CancelPendingTemporaryMusic();

        if (resumeSceneMusic)
            ApplySceneAudio(SceneManager.GetActiveScene().name);
    }

    private void CancelPendingTemporaryMusic()
    {
        if (delayedMusicCoroutine == null)
            return;

        StopCoroutine(delayedMusicCoroutine);
        delayedMusicCoroutine = null;
    }

    private System.Collections.IEnumerator PlayTemporaryMusicAfterDelay(AudioClip clip, float volume, bool loop, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        delayedMusicCoroutine = null;
        Play(clip, volume, loop);
    }

    private static AudioClip GetDefaultCoinPickupClip()
    {
        AudioClip catalogClip = GetCatalogSfxClip(catalog => catalog.CoinPickupClip);
        if (catalogClip != null)
            return catalogClip;

        if (defaultCoinPickupClip == null)
            defaultCoinPickupClip = CreateCoinPickupClip();

        return defaultCoinPickupClip;
    }

    private static AudioClip GetDefaultStunClip()
    {
        AudioClip catalogClip = GetCatalogSfxClip(catalog => catalog.StunClip);
        if (catalogClip != null)
            return catalogClip;

        if (defaultStunClip == null)
            defaultStunClip = CreateStunClip();

        return defaultStunClip;
    }

    private static AudioClip GetDefaultExplosionClip()
    {
        AudioClip catalogClip = GetCatalogSfxClip(catalog => catalog.ExplosionClip);
        if (catalogClip != null)
            return catalogClip;

        if (defaultExplosionClip == null)
            defaultExplosionClip = CreateExplosionClip();

        return defaultExplosionClip;
    }

    private static AudioClip GetDefaultTurretShotClip()
    {
        AudioClip catalogClip = GetCatalogSfxClip(catalog => catalog.TurretShotClip);
        if (catalogClip != null)
            return catalogClip;

        if (defaultTurretShotClip == null)
            defaultTurretShotClip = CreateTurretShotClip();

        return defaultTurretShotClip;
    }

    private static AudioClip GetDefaultStunTurretShotClip()
    {
        AudioClip catalogClip = GetCatalogSfxClip(catalog => catalog.StunTurretShotClip);
        if (catalogClip != null)
            return catalogClip;

        if (defaultStunTurretShotClip == null)
            defaultStunTurretShotClip = CreateStunTurretShotClip();

        return defaultStunTurretShotClip;
    }

    private static AudioClip GetCatalogSfxClip(System.Func<SceneAudioCatalog, AudioClip> selector)
    {
        if (selector == null)
            return null;

        EnsureInstance();

        if (instance == null || instance.catalog == null)
            return null;

        return selector(instance.catalog);
    }

    private static AudioClip CreateCoinPickupClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.11f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phaseA = 0f;
        float phaseB = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float progress = i / (float)(sampleCount - 1);
            float envelope = Mathf.Exp(-16f * t) * (1f - 0.18f * progress);
            float freqA = Mathf.Lerp(1200f, 1900f, progress);
            float freqB = freqA * 1.9f;

            phaseA += 2f * Mathf.PI * freqA / sampleRate;
            phaseB += 2f * Mathf.PI * freqB / sampleRate;

            float tone = Mathf.Sin(phaseA) * 0.78f + Mathf.Sin(phaseB) * 0.22f;
            samples[i] = Mathf.Clamp(tone * envelope * 0.42f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("DefaultCoinPickupSfx", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateStunClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.24f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phaseA = 0f;
        float phaseB = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float progress = i / (float)(sampleCount - 1);
            float envelope = Mathf.Exp(-9.5f * t) * (1f - Mathf.SmoothStep(0.78f, 1f, progress));
            float wobble = Mathf.Sin(t * 34f * Mathf.PI * 2f) * 35f;
            float freqA = Mathf.Lerp(420f, 140f, progress) + wobble;
            float freqB = Mathf.Lerp(680f, 250f, progress * 0.9f);

            phaseA += 2f * Mathf.PI * Mathf.Max(40f, freqA) / sampleRate;
            phaseB += 2f * Mathf.PI * Mathf.Max(60f, freqB) / sampleRate;

            float buzz = Mathf.Sin(phaseA) * 0.62f + Mathf.Sin(phaseB) * 0.24f;
            float crackle = (Mathf.PerlinNoise(i * 0.08f, 0.37f) * 2f - 1f) * 0.14f;
            float tremolo = 0.65f + 0.35f * Mathf.Sin(t * 20f * Mathf.PI * 2f);
            samples[i] = Mathf.Clamp((buzz + crackle) * envelope * tremolo * 0.58f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("DefaultStunSfx", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateExplosionClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.4f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phaseA = 0f;
        float phaseB = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float progress = i / (float)(sampleCount - 1);
            float envelope = Mathf.Exp(-6.5f * t) * (1f - 0.2f * progress);
            float noise = Mathf.PerlinNoise(i * 0.045f, 0.17f) * 2f - 1f;
            float crackle = (Mathf.PerlinNoise(i * 0.14f, 0.73f) * 2f - 1f) * Mathf.Exp(-11f * t);
            float freqA = Mathf.Lerp(150f, 48f, progress);
            float freqB = Mathf.Lerp(240f, 78f, progress);

            phaseA += 2f * Mathf.PI * freqA / sampleRate;
            phaseB += 2f * Mathf.PI * freqB / sampleRate;

            float body = Mathf.Sin(phaseA) * 0.58f + Mathf.Sin(phaseB) * 0.22f;
            float impact = Mathf.Exp(-28f * t) * 0.8f;
            float rumble = (body * 0.72f + noise * 0.34f + crackle * 0.18f) * envelope;
            samples[i] = Mathf.Clamp((impact + rumble) * 0.52f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("DefaultExplosionSfx", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateTurretShotClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.12f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phaseA = 0f;
        float phaseB = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float progress = i / (float)(sampleCount - 1);
            float envelope = Mathf.Exp(-24f * t) * (1f - 0.12f * progress);
            float freqA = Mathf.Lerp(980f, 240f, progress);
            float freqB = Mathf.Lerp(1600f, 520f, progress);

            phaseA += 2f * Mathf.PI * freqA / sampleRate;
            phaseB += 2f * Mathf.PI * freqB / sampleRate;

            float tone = Mathf.Sin(phaseA) * 0.62f + Mathf.Sin(phaseB) * 0.23f;
            float click = (Mathf.PerlinNoise(i * 0.19f, 0.11f) * 2f - 1f) * Mathf.Exp(-42f * t) * 0.32f;
            samples[i] = Mathf.Clamp((tone + click) * envelope * 0.44f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("DefaultTurretShotSfx", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateStunTurretShotClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.18f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float phaseA = 0f;
        float phaseB = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float progress = i / (float)(sampleCount - 1);
            float envelope = Mathf.Exp(-14f * t) * (1f - Mathf.SmoothStep(0.8f, 1f, progress));
            float wobble = Mathf.Sin(t * 52f * Mathf.PI * 2f) * 85f;
            float freqA = Mathf.Lerp(720f, 280f, progress) + wobble;
            float freqB = Mathf.Lerp(1180f, 460f, progress * 0.9f);

            phaseA += 2f * Mathf.PI * Mathf.Max(60f, freqA) / sampleRate;
            phaseB += 2f * Mathf.PI * Mathf.Max(90f, freqB) / sampleRate;

            float zap = Mathf.Sin(phaseA) * 0.44f + Mathf.Sin(phaseB) * 0.24f;
            float sparkle = (Mathf.PerlinNoise(i * 0.13f, 0.59f) * 2f - 1f) * 0.24f;
            float shimmer = 0.72f + 0.28f * Mathf.Sin(t * 18f * Mathf.PI * 2f);
            samples[i] = Mathf.Clamp((zap + sparkle) * envelope * shimmer * 0.5f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("DefaultStunTurretShotSfx", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
