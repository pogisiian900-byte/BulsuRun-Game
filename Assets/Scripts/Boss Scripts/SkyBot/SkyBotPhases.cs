using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class SkyBotPhases : MonoBehaviour
{
    public enum Phase
    {
        Phase1,
        Phase2,
        Phase3
    }

    [Header("References")]
    [SerializeField] private BossHealth health;
    [SerializeField] private Animator animator;
    [SerializeField] private SkyBotLaserAttack laserAttack;
    [SerializeField] private SkyBotRocketAttack rocketAttack;
    [SerializeField] private SkyBotRocketSummonAttack rocketSummonAttack; // NEW script for Phase 3
    [SerializeField] private SkyBotMovement skybotMove;
    [SerializeField] private BossExplosionDeath deathExplosion;

    [Header("Phase Thresholds")]
    [Range(0f, 1f)] public float phase2At = 0.7f;
    [Range(0f, 1f)] public float phase3At = 0.35f;
    [SerializeField] private float winPanelDelay = 1.7f;

    [Header("Victory Cutscene")]
    [SerializeField] private bool playVictoryCutscene;
    [SerializeField] private VideoClip victoryCutscene;
    [SerializeField] private string cutsceneReturnScene = VideoCutsceneState.DefaultReturnSceneName;

    private Phase currentPhase = Phase.Phase1;
    private bool isDead;
    private Coroutine phase3TransitionRoutine;

    [SerializeField] private BossBattleUI bossBattleUI;

    void Awake()
    {
        if (health == null) health = GetComponent<BossHealth>();
        if (animator == null) animator = GetComponent<Animator>();
        if (laserAttack == null) laserAttack = GetComponent<SkyBotLaserAttack>();
        if (rocketAttack == null) rocketAttack = GetComponent<SkyBotRocketAttack>();
        if (rocketSummonAttack == null) rocketSummonAttack = GetComponent<SkyBotRocketSummonAttack>();
        if (skybotMove == null) skybotMove = GetComponent<SkyBotMovement>();
        if (deathExplosion == null) deathExplosion = GetComponent<BossExplosionDeath>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnDeath.AddListener(Die);
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDeath.RemoveListener(Die);

        if (phase3TransitionRoutine != null)
        {
            StopCoroutine(phase3TransitionRoutine);
            phase3TransitionRoutine = null;
        }
    }

    void Start()
    {
        if (bossBattleUI != null)
            bossBattleUI.StartBossBattle();

        if (rocketSummonAttack != null)
            rocketSummonAttack.ResetArenaState();

        SetPhase(Phase.Phase1);
    }

    void Update()
    {
        if (health == null || isDead) return;

        float hp = health.Normalized;

        Phase target =
            (hp <= phase3At) ? Phase.Phase3 :
            (hp <= phase2At) ? Phase.Phase2 :
            Phase.Phase1;

        if (target != currentPhase)
            SetPhase(target);
    }

    void SetPhase(Phase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log("SkyBot Phase: " + newPhase);

        if (animator != null)
            animator.SetInteger("Phase", (int)newPhase + 1);

        switch (newPhase)
        {
            case Phase.Phase1:
                if (skybotMove != null)
                {
                    skybotMove.ResetToDefaultArea();
                    skybotMove.BeginMovement();
                }
                if (rocketSummonAttack != null)
                {
                    rocketSummonAttack.ResetArenaState();
                    rocketSummonAttack.enabled = false;
                }
                if (laserAttack != null) laserAttack.enabled = true;
                if (rocketAttack != null) rocketAttack.enabled = false;
                break;

            case Phase.Phase2:
                if (skybotMove != null)
                {
                    skybotMove.ResetToDefaultArea();
                    skybotMove.BeginMovement();
                }
                if (rocketSummonAttack != null)
                {
                    rocketSummonAttack.ResetArenaState();
                    rocketSummonAttack.enabled = false;
                }
                if (laserAttack != null) laserAttack.enabled = true;
                if (rocketAttack != null) rocketAttack.enabled = true;
                break;

            case Phase.Phase3:
                if (laserAttack != null) laserAttack.enabled = false;
                if (rocketAttack != null) rocketAttack.enabled = false;
                if (rocketSummonAttack != null) rocketSummonAttack.enabled = false;
                if (phase3TransitionRoutine == null)
                    phase3TransitionRoutine = StartCoroutine(EnterPhase3LastStand());
                break;
        }
    }

    private IEnumerator EnterPhase3LastStand()
    {
        if (skybotMove != null)
        {
            skybotMove.EnterPhase3LastStand();

            while (!isDead && !skybotMove.IsAtPhase3LastStand())
                yield return null;
        }

        if (!isDead && rocketSummonAttack != null)
        {
            rocketSummonAttack.enabled = true;
            rocketSummonAttack.BeginPhase3Sequence();
        }

        phase3TransitionRoutine = null;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (phase3TransitionRoutine != null)
        {
            StopCoroutine(phase3TransitionRoutine);
            phase3TransitionRoutine = null;
        }

        if (bossBattleUI != null)
            bossBattleUI.EndBossBattle();

        if (laserAttack != null) laserAttack.enabled = false;
        if (rocketAttack != null) rocketAttack.enabled = false;
        if (rocketSummonAttack != null)
        {
            rocketSummonAttack.StopAttack();
            rocketSummonAttack.enabled = false;
        }
        if (skybotMove != null) skybotMove.enabled = false;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        if (deathExplosion != null)
            deathExplosion.Explode();

        LevelManager levelManager = LevelManager.Instance;
        if (levelManager == null)
        {
            Debug.LogWarning("LevelManager.Instance is null, so SkyBot cannot show the win result panel.", this);
            return;
        }

        if (string.IsNullOrEmpty(GameData.CurrentLevelSelectScene))
            GameData.CurrentLevelSelectScene = "Worlds";

        levelManager.StartCoroutine(ShowWinPanelAfterDeath(levelManager, winPanelDelay));
    }

    private IEnumerator ShowWinPanelAfterDeath(LevelManager levelManager, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (levelManager == null)
            yield break;

        if (levelManager.TryPlayVictoryCutscene(playVictoryCutscene, victoryCutscene, cutsceneReturnScene))
            yield break;

        levelManager.Win();
    }

    public Phase CurrentPhase => currentPhase;
}
