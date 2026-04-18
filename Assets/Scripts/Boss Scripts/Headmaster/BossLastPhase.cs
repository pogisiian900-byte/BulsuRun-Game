using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class BossLastPhase : MonoBehaviour
{
    public enum Phase
    {
        Phase1,
        Phase2,
        Phase3
    }

    [Header("Timer (Survive Boss)")]
    [SerializeField] private bool startOnStart = true;
    [SerializeField] private float phase1Seconds = 25f;
    [SerializeField] private float phase2Seconds = 25f;
    [SerializeField] private float phase3Seconds = 25f;
    [SerializeField] private float phase2RespawnDelay = 1.5f;
    [SerializeField] private int phase2RocketShotsPerCycle = 4;

    [Header("References")]
    [SerializeField] private BossChaserAttack chaserAttack;
    [SerializeField] private BossBulletAttack bulletAttack;
    [SerializeField] private BossLaserAttack laserAttack;
    [SerializeField] private BossClosingLaserAttack closingLaserAttack;
    [SerializeField] private BossDroneAttack droneAttack;
    [SerializeField] private BossRocketAttack rocketAttack;
    [SerializeField] private SkyBotMovement movement;

    [Header("Optional Animation / UI")]
    [SerializeField] private Animator animator;
    [SerializeField] private BossBattleUI bossBattleUI;
    [SerializeField] private BossExplosionDeath deathExplosion;

    [Header("Timer Death Sequence")]
    [SerializeField] private Transform landingPoint;
    [SerializeField] private float landingY = -47f;
    [SerializeField] private float startFallSpeed = 12f;
    [SerializeField] private float fallAcceleration = 55f;
    [SerializeField] private float dieDelayAfterLanding = 0.2f;
    [SerializeField] private string fallingTriggerName = "Falling";
    [SerializeField] private string fallingStateName = "HeadMaster Falling";
    [SerializeField] private string dieTriggerName = "Die";
    [SerializeField] private string dieStateName = "HeadMaster Die";

    [Header("Victory Cutscene")]
    [SerializeField] private bool playVictoryCutscene = true;
    [SerializeField] private VideoClip victoryCutscene;
    [SerializeField] private string cutsceneReturnScene = VideoCutsceneState.DefaultReturnSceneName;
    [SerializeField] private float victoryTransitionDelay = 1.7f;

    private Phase currentPhase = (Phase)(-1);
    private float elapsed;
    private bool running;
    private bool finished;
    private Coroutine phaseRoutine;
    private Coroutine deathRoutine;

    public Phase CurrentPhase => currentPhase;
    public float ElapsedTime => elapsed;
    public float RemainingTime => Mathf.Max(0f, GetTotalDuration() - elapsed);

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (deathExplosion == null)
            deathExplosion = GetComponent<BossExplosionDeath>();
    }

    private void Start()
    {
        if (startOnStart)
            StartBoss();
    }

    private void OnDisable()
    {
        if (phaseRoutine != null)
        {
            StopCoroutine(phaseRoutine);
            phaseRoutine = null;
        }

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        DisableAllAttacks();
    }

    public void StartBoss()
    {
        if (phaseRoutine != null)
        {
            StopCoroutine(phaseRoutine);
            phaseRoutine = null;
        }

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        DisableAllAttacks();

        if (movement != null)
        {
            movement.ResetToDefaultArea();
            movement.BeginMovement();
        }

        elapsed = 0f;
        finished = false;
        running = true;
        currentPhase = (Phase)(-1);

        if (bossBattleUI != null)
            bossBattleUI.StartBossBattle();

        SetPhase(Phase.Phase1);
    }

    private void Update()
    {
        if (!running || finished)
            return;

        elapsed += Time.deltaTime;

        float phase1End = phase1Seconds;
        float phase2End = phase1End + phase2Seconds;
        float totalDuration = phase2End + phase3Seconds;

        if (elapsed >= phase2End)
            SetPhase(Phase.Phase3);
        else if (elapsed >= phase1End)
            SetPhase(Phase.Phase2);
        else
            SetPhase(Phase.Phase1);

        if (elapsed >= totalDuration)
            FinishBoss();
    }

    private void SetPhase(Phase phase)
    {
        if (currentPhase == phase)
            return;

        currentPhase = phase;

        if (animator != null)
            animator.SetInteger("Phase", (int)phase + 1);

        ApplyPhaseSettings(phase);
        Debug.Log("Headmaster Time Phase -> " + phase);
    }

    private void ApplyPhaseSettings(Phase phase)
    {
        if (phaseRoutine != null)
        {
            StopCoroutine(phaseRoutine);
            phaseRoutine = null;
        }

        DisableAllAttacks();

        switch (phase)
        {
            case Phase.Phase1:
                BeginAutoAttack(droneAttack);
                BeginAutoAttack(bulletAttack);
        
                break;

            case Phase.Phase2:
                PrepareAttack(chaserAttack);
                PrepareAttack(rocketAttack);
                phaseRoutine = StartCoroutine(Phase2Loop());
                break;

            case Phase.Phase3:
                if (movement != null)
                {
                    movement.SwitchToPhase3Area();
                    movement.BeginMovement();
                }

                BeginAutoAttack(laserAttack);
                BeginAutoAttack(closingLaserAttack);
                break;
        }
    }

    private void FinishBoss()
    {
        if (finished)
            return;

        finished = true;
        running = false;

        if (phaseRoutine != null)
        {
            StopCoroutine(phaseRoutine);
            phaseRoutine = null;
        }

        DisableAllAttacks();

        if (movement != null)
            movement.StopMovement();

        if (bossBattleUI != null)
            bossBattleUI.EndBossBattle();

        if (deathRoutine != null)
            StopCoroutine(deathRoutine);

        deathRoutine = StartCoroutine(DeathSequenceRoutine());

        Debug.Log("Survived Headmaster for " + GetTotalDuration() + " seconds.");
    }

    private IEnumerator DeathSequenceRoutine()
    {
        PlayAnimation(fallingTriggerName, fallingStateName);

        float targetY = landingPoint != null ? landingPoint.position.y : landingY;
        float currentFallSpeed = startFallSpeed;

        while (transform.position.y > targetY)
        {
            currentFallSpeed += fallAcceleration * Time.deltaTime;

            Vector3 nextPosition = transform.position + Vector3.down * currentFallSpeed * Time.deltaTime;

            if (nextPosition.y < targetY)
                nextPosition.y = targetY;

            transform.position = new Vector3(transform.position.x, nextPosition.y, transform.position.z);
            yield return null;
        }

        PlayAnimation(dieTriggerName, dieStateName);

        if (dieDelayAfterLanding > 0f)
            yield return new WaitForSeconds(dieDelayAfterLanding);

        if (deathExplosion != null)
            deathExplosion.Explode();

        if (victoryTransitionDelay > 0f)
            yield return new WaitForSeconds(victoryTransitionDelay);

        LevelManager levelManager = LevelManager.Instance;
        if (levelManager == null)
        {
            Debug.LogWarning("LevelManager.Instance is null, so Headmaster cannot finish the level.", this);
            deathRoutine = null;
            yield break;
        }

        if (!levelManager.TryPlayVictoryCutscene(playVictoryCutscene, victoryCutscene, cutsceneReturnScene))
            levelManager.Win();

        deathRoutine = null;
    }

    private float GetTotalDuration()
    {
        return phase1Seconds + phase2Seconds + phase3Seconds;
    }

    private IEnumerator Phase2Loop()
    {
        if (chaserAttack != null)
            chaserAttack.ResetSequence();

        int rocketBurstSpawnIndex = 0;
        if (rocketAttack != null && rocketAttack.SpawnPointCount > 0)
            rocketBurstSpawnIndex = Mathf.Clamp(rocketAttack.startingSpawnIndex, 0, rocketAttack.SpawnPointCount - 1);

        while (currentPhase == Phase.Phase2 && running && !finished)
        {
            if (chaserAttack != null)
                chaserAttack.SpawnNextChaser();

            if (rocketAttack != null)
            {
                rocketAttack.SetNextSpawnIndex(rocketBurstSpawnIndex);

                int shotsThisCycle = Mathf.Max(0, phase2RocketShotsPerCycle);
                for (int i = 0; i < shotsThisCycle && currentPhase == Phase.Phase2 && running && !finished; i++)
                {
                    rocketAttack.LaunchRocketFromSpawnIndex(rocketBurstSpawnIndex);

                    if (i < shotsThisCycle - 1 && rocketAttack.SpawnInterval > 0f)
                        yield return new WaitForSeconds(rocketAttack.SpawnInterval);
                }
            }

            while (currentPhase == Phase.Phase2 &&
                   running &&
                   !finished &&
                   chaserAttack != null &&
                   chaserAttack.HasActiveChasers)
            {
                yield return null;
            }

            if (rocketAttack != null && rocketAttack.SpawnPointCount > 1)
                rocketBurstSpawnIndex = (rocketBurstSpawnIndex + 1) % rocketAttack.SpawnPointCount;

            while (currentPhase == Phase.Phase2 &&
                   running &&
                   !finished &&
                   rocketAttack != null &&
                   rocketAttack.HasActiveRockets)
            {
                yield return null;
            }

            if (phase2RespawnDelay > 0f)
                yield return new WaitForSeconds(phase2RespawnDelay);
            else
                yield return null;
        }
    }

    private void DisableAllAttacks()
    {
        StopAndDisable(chaserAttack);
        StopAndDisable(bulletAttack);
        StopAndDisable(rocketAttack);
        StopAndDisable(laserAttack);
        StopAndDisable(closingLaserAttack);
        StopAndDisable(droneAttack);
    }

    private void PlayAnimation(string triggerName, string stateName)
    {
        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);

        if (!string.IsNullOrEmpty(stateName))
            animator.Play(stateName, 0, 0f);
    }

    private void BeginAutoAttack(BossChaserAttack attack)
    {
        if (attack == null)
            return;

        attack.enabled = true;
        attack.BeginAutoLoop();
    }

    private void BeginAutoAttack(BossBulletAttack attack)
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

    private void BeginAutoAttack(BossClosingLaserAttack attack)
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

    private void BeginAutoAttack(BossRocketAttack attack)
    {
        if (attack == null)
            return;

        attack.enabled = true;
        attack.BeginAutoLoop();
    }

    private void PrepareAttack(BossChaserAttack attack)
    {
        if (attack == null)
            return;

        attack.enabled = true;
        attack.ResetSequence();
    }

    private void PrepareAttack(BossRocketAttack attack)
    {
        if (attack == null)
            return;

        attack.enabled = true;
        attack.ResetSequence();
    }

    private void StopAndDisable(BossChaserAttack attack)
    {
        if (attack == null)
            return;

        attack.StopAttack();
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

    private void StopAndDisable(BossClosingLaserAttack attack)
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
}
