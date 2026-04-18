using UnityEngine;
using UnityEngine.Video;

public class FitBossPhases_Time : MonoBehaviour
{
    public enum Phase { Phase1, Phase2, Phase3 }

    [Header("Timer (Survive Boss)")]
    [SerializeField] private float phase1Seconds = 15f;
    [SerializeField] private float phase2Seconds = 20f;
    [SerializeField] private float phase3Seconds = 25f;

    [Header("Refs (attacks)")]
    [SerializeField] private FitBossSideShooter sideShooter;
    [SerializeField] private FitBossSkyDropper skyDropper;
    [SerializeField] private FitBossElectricShooter electricShooterSky;

    [Header("Attack Speed")]
    [SerializeField] private float phase2SideFireRateMultiplier = 1f;
    [SerializeField] private float phase3SideFireRateMultiplier = 2f;
    [SerializeField] private float phase3ElectricFireRateMultiplier = 1.75f;

    [Header("Optional Animation/UI")]
    [SerializeField] private Animator animator;
    [SerializeField] private BossBattleUI bossBattleUI;
    [SerializeField] private BossExplosionDeath deathExplosion;
    [SerializeField] private float winPanelDelay = 1.7f;

    [Header("Victory Cutscene")]
    [SerializeField] private bool playVictoryCutscene;
    [SerializeField] private VideoClip victoryCutscene;
    [SerializeField] private string cutsceneReturnScene = VideoCutsceneState.DefaultReturnSceneName;

    public Phase CurrentPhase { get; private set; }

    private float elapsed;
    private float total;
    private bool running;
    private bool finished;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (deathExplosion == null) deathExplosion = GetComponent<BossExplosionDeath>();

        total = phase1Seconds + phase2Seconds + phase3Seconds;
    }

    private void Start()
    {
        if (bossBattleUI != null)
            bossBattleUI.StartBossBattle();

        StartBoss();
    }

    public void StartBoss()
    {
        elapsed = 0f;
        finished = false;
        running = true;

        CurrentPhase = (Phase)(-1);
        SetPhase(Phase.Phase1);
    }

    private void Update()
    {
        if (!running || finished) return;

        elapsed += Time.deltaTime;

        if (elapsed >= phase1Seconds + phase2Seconds)
            SetPhase(Phase.Phase3);
        else if (elapsed >= phase1Seconds)
            SetPhase(Phase.Phase2);
        else
            SetPhase(Phase.Phase1);

        if (elapsed >= total)
            FinishBoss();
    }

    private void SetPhase(Phase p)
    {
        if (CurrentPhase == p) return;

        CurrentPhase = p;

        if (animator != null)
            animator.SetInteger("Phase", (int)p + 1);

        ApplyPhaseSettings(p);

        Debug.Log("FitBoss Phase -> " + p);
    }

    private void ApplyPhaseSettings(Phase p)
    {
        if (sideShooter != null)
        {
            sideShooter.SetFireRateMultiplier(1f);
            sideShooter.enabled = false;
        }
        if (skyDropper != null) skyDropper.enabled = false;
        if (electricShooterSky != null)
        {
            electricShooterSky.SetFireRateMultiplier(1f);
            electricShooterSky.enabled = false;
        }

        if (p == Phase.Phase1)
        {
            if (skyDropper != null)
                skyDropper.enabled = true;
        }

        if (p == Phase.Phase2)
        {
            if (sideShooter != null)
            {
                sideShooter.SetFireRateMultiplier(phase2SideFireRateMultiplier);
                sideShooter.enabled = true;
            }
        }

        if (p == Phase.Phase3)
        {
            if (sideShooter != null)
            {
                sideShooter.enabled = true;
            }

            if (electricShooterSky != null)
            {
                electricShooterSky.SetFireRateMultiplier(phase3ElectricFireRateMultiplier);
                electricShooterSky.enabled = true;
            }
        }
    }

    private void FinishBoss()
    {
        finished = true;
        running = false;

        if (bossBattleUI != null)
            bossBattleUI.EndBossBattle();

        if (sideShooter != null) sideShooter.enabled = false;
        if (skyDropper != null) skyDropper.enabled = false;
        if (electricShooterSky != null) electricShooterSky.enabled = false;

        if (animator != null)
            animator.SetTrigger("Die");

        if (deathExplosion != null)
            deathExplosion.Explode();

        LevelManager levelManager = LevelManager.Instance;
        if (levelManager == null)
        {
            Debug.LogWarning("LevelManager.Instance is null, so FitBot cannot show the win result panel.", this);
            return;
        }

        if (string.IsNullOrEmpty(GameData.CurrentLevelSelectScene))
            GameData.CurrentLevelSelectScene = "Worlds";

        levelManager.StartCoroutine(ShowWinPanelAfterDeath(levelManager, winPanelDelay));

        Debug.Log("Survived FitBoss for " + total + " seconds.");
    }

    private System.Collections.IEnumerator ShowWinPanelAfterDeath(LevelManager levelManager, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (levelManager == null)
            yield break;

        if (levelManager.TryPlayVictoryCutscene(playVictoryCutscene, victoryCutscene, cutsceneReturnScene))
            yield break;

        levelManager.Win();
    }
}
