using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BossController : MonoBehaviour
{
    public enum Phase
    {
        Phase1,
        Phase2,
        Phase3
    }

    [Header("References")]
    [SerializeField] private BossHealth health;
    [SerializeField] private BossChase bossChase;

    [Header("Attacks")]
    [SerializeField] private BossBulletAttack bulletAttack;
    [SerializeField] private BossRocketAttack rocketAttack;
    [SerializeField] private BossLaserAttack laserAttack;
    [SerializeField] private BossDroneAttack droneAttack;
    [SerializeField] private SkyBotMovement movement;

    [Header("Phase Thresholds")]
    [Range(0f, 1f)] public float phase2At = 0.7f;
    [Range(0f, 1f)] public float phase3At = 0.35f;

    [Header("Chase")]
    [SerializeField] private float chaseInterval = 30f;

    [Header("Death")]
    [SerializeField] private string sceneToLoadOnDeath;
    [SerializeField] private float loadSceneDelay = 1.5f;

    private Phase currentPhase = (Phase)(-1);
    private Coroutine phaseRoutine;
    private bool isDead;
    private bool isChasing;
    private float nextChaseTime;

    private void Awake()
    {
        if (bossChase == null)
            bossChase = GetComponent<BossChase>();

        if (bossChase == null)
            bossChase = gameObject.AddComponent<BossChase>();

        if (bossChase != null)
        {
            bossChase.Configure(health, movement);
            bossChase.enabled = false;
        }

        DisableAllAttacks();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath.AddListener(HandleBossDeath);

        if (bossChase != null)
            bossChase.ChaseBroken += HandleChaseBroken;
    }

    private void Start()
    {
        if (health == null)
            return;

        nextChaseTime = Time.time + chaseInterval;
        SetPhase(GetPhaseForHealth());
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath.RemoveListener(HandleBossDeath);

        if (bossChase != null)
            bossChase.ChaseBroken -= HandleChaseBroken;

        if (phaseRoutine != null)
        {
            StopCoroutine(phaseRoutine);
            phaseRoutine = null;
        }

        DisableAllAttacks();
    }

    private void Update()
    {
        if (health == null || isDead)
            return;

        if (!isChasing && Time.time >= nextChaseTime)
        {
            StartChase();
            return;
        }

        if (isChasing)
            return;

        Phase target = GetPhaseForHealth();

        if (target != currentPhase)
            SetPhase(target);
    }

    private Phase GetPhaseForHealth()
    {
        float hp = health.Normalized;

        return (hp <= phase3At) ? Phase.Phase3 :
               (hp <= phase2At) ? Phase.Phase2 :
               Phase.Phase1;
    }

    private void SetPhase(Phase newPhase)
    {
        if (isDead)
            return;

        if (phaseRoutine != null)
        {
            StopCoroutine(phaseRoutine);
            phaseRoutine = null;
        }

        currentPhase = newPhase;

        Debug.Log("Boss Phase: " + newPhase);

        DisableAllAttacks();

        switch (newPhase)
        {
            case Phase.Phase1:
                if (movement != null)
                {
                    movement.ResetToDefaultArea();
                    movement.BeginMovement();
                }

                BeginAutoAttack(bulletAttack);
                break;

            case Phase.Phase2:
                if (movement != null)
                {
                    movement.ResetToDefaultArea();
                    movement.BeginMovement();
                }

                BeginAutoAttack(rocketAttack);
                BeginAutoAttack(droneAttack);
                break;

            case Phase.Phase3:
                if (movement != null)
                {
                    movement.SwitchToPhase3Area();
                    movement.BeginMovement();
                }

                phaseRoutine = StartCoroutine(Phase3Loop());
                break;
        }
    }

    private IEnumerator Phase3Loop()
    {
        while (currentPhase == Phase.Phase3 && !isDead && !isChasing)
        {
            if (laserAttack != null)
                yield return StartCoroutine(laserAttack.ExecuteAttackCycle());

            if (currentPhase != Phase.Phase3 || isDead || isChasing)
                yield break;

            if (rocketAttack != null)
                yield return StartCoroutine(rocketAttack.ExecuteAttackCycle());

            if (currentPhase != Phase.Phase3 || isDead || isChasing)
                yield break;

            if (droneAttack != null)
                yield return StartCoroutine(droneAttack.ExecuteAttackCycle());

            if (laserAttack == null && rocketAttack == null && droneAttack == null)
                yield break;
        }
    }

    private void DisableAllAttacks()
    {
        StopAndDisable(bossChase);
        StopAndDisable(bulletAttack);
        StopAndDisable(rocketAttack);
        StopAndDisable(laserAttack);
        StopAndDisable(droneAttack);
    }

    private void DisableNormalAttacks()
    {
        StopAndDisable(bulletAttack);
        StopAndDisable(rocketAttack);
        StopAndDisable(laserAttack);
        StopAndDisable(droneAttack);
    }

    private void BeginAutoAttack(BossChase attack)
    {
        if (attack == null)
            return;

        attack.enabled = true;
        attack.BeginChase();
    }

    private void BeginAutoAttack(BossBulletAttack attack)
    {
        if (attack == null)
            return;

        attack.enabled = true;
        attack.BeginAutoLoop();
    }

    private void BeginAutoAttack(BossRocketAttack attack)
    {
        if (attack == null)
            return;

        attack.enabled = true;
        attack.BeginAutoLoop();
    }

    private void BeginAutoAttack(BossLaserAttack attack)
    {
        if (attack == null)
            return;

        attack.enabled = true;
        attack.BeginAutoLoop();
    }

    private void BeginAutoAttack(BossDroneAttack attack)
    {
        if (attack == null)
            return;

        attack.enabled = true;
        attack.BeginAutoLoop();
    }

    private void StopAndDisable(BossChase attack)
    {
        if (attack == null)
            return;

        attack.StopChase();
        attack.enabled = false;
    }

    private void StopAndDisable(BossBulletAttack attack)
    {
        if (attack == null)
            return;

        attack.StopAttack();
        attack.enabled = false;
    }

    private void StopAndDisable(BossRocketAttack attack)
    {
        if (attack == null)
            return;

        attack.StopAttack();
        attack.enabled = false;
    }

    private void StopAndDisable(BossLaserAttack attack)
    {
        if (attack == null)
            return;

        attack.StopAttack();
        attack.enabled = false;
    }

    private void StopAndDisable(BossDroneAttack attack)
    {
        if (attack == null)
            return;

        attack.StopAttack();
        attack.enabled = false;
    }

    private void HandleBossDeath()
    {
        if (isDead)
            return;

        isDead = true;

        if (phaseRoutine != null)
        {
            StopCoroutine(phaseRoutine);
            phaseRoutine = null;
        }

        DisableAllAttacks();
        StartCoroutine(LoadSceneAfterDeath());
    }

    private void HandleChaseBroken()
    {
        if (isDead || !isChasing)
            return;

        isChasing = false;
        nextChaseTime = Time.time + chaseInterval;
        SetPhase(GetPhaseForHealth());
    }

    private void StartChase()
    {
        if (isDead || bossChase == null)
            return;

        if (phaseRoutine != null)
        {
            StopCoroutine(phaseRoutine);
            phaseRoutine = null;
        }

        isChasing = true;
        DisableNormalAttacks();
        BeginAutoAttack(bossChase);
    }

    private IEnumerator LoadSceneAfterDeath()
    {
        if (loadSceneDelay > 0f)
            yield return new WaitForSeconds(loadSceneDelay);

        if (string.IsNullOrWhiteSpace(sceneToLoadOnDeath))
        {
            Debug.LogWarning("BossController has no death scene assigned.", this);
            yield break;
        }

        SinglePlayerSaveSystem.SaveCheckpoint();
        SceneManager.LoadScene(sceneToLoadOnDeath);
    }

    public Phase CurrentPhase => currentPhase;
}
