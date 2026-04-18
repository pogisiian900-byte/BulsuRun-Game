using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class TrashLordPhases : MonoBehaviour // 169 check
{
    public enum Phase { Phase1, Phase2, Phase3 }

    [Header("Refs")]
    [SerializeField] private BossHealth health;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Phase thresholds (HP %)")]
    [Range(0f, 1f)] public float phase2At = 0.70f;
    [Range(0f, 1f)] public float phase3At = 0.35f;

    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 2f;
    [SerializeField] private float phase2SpeedMultiplier = 1.25f;
    [SerializeField] private float phase3SpeedMultiplier = 1.5f;
    public float leftX = -6f;
    public float rightX = 6f;

    [Header("Phase 2 Summon")]
    [SerializeField] private GameObject trashBotPrefab;
    [SerializeField] private Transform summonPoint;      // where it spawns
    [SerializeField] private float summonInterval = 3f;  

[SerializeField] private float throwAnimLength = 0.6f; // set to your Throw clip length

        [Header("Phase 3: Stand + Throw")]
        [SerializeField] private Transform phase3StandPoint; // where boss stands
        [SerializeField] private GameObject[] trashProjectiles; // ARRAY
        [SerializeField] private Transform throwPoint;        // spawn point (hand/mouth)
        [SerializeField] private float throwInterval = 0.5f;

        [SerializeField] private float throwForce = 12f;
        [SerializeField] private float burstDuration = 5f;
        [SerializeField] private float restDuration = 2f;   

[Header("Throw Randomness")]
[SerializeField] private float minForceMultiplier = 0.6f; // closer
[SerializeField] private float maxForceMultiplier = 1.4f; // farther
[SerializeField] private float minUpMultiplier = 0.4f;
[SerializeField] private float maxUpMultiplier = 0.8f;

        private bool isThrowing;


[Header("Multi-Throw")]
[SerializeField] private int minTrashPerAttack = 2;
[SerializeField] private int maxTrashPerAttack = 5;
[SerializeField] private float multiThrowSpreadDegrees = 12f;  // angle spread
[SerializeField] private float multiThrowGap = 0.05f;          // tiny delay between each spawn (optional)
[SerializeField] private float phase3SummonMultiplier = 0.6f; // 60% of interval

        private Coroutine phase3ThrowRoutine;

    private Phase currentPhase = Phase.Phase1;
    private int dir = 1;
    private Coroutine summonRoutine;
    public Phase CurrentPhase => currentPhase;


    private bool isTransitioning = false;

   private bool isDead = false;
    [SerializeField] Animator animator;
    [SerializeField] private BossExplosionDeath deathExplosion;

    [SerializeField] private BossBattleUI bossBattleUI;
private bool bossUIShown = false;
    [SerializeField] private float winPanelDelay = 1.7f;

[Header("Victory Cutscene")]
[SerializeField] private bool playVictoryCutscene;
[SerializeField] private VideoClip victoryCutscene;
[SerializeField] private string cutsceneReturnScene = VideoCutsceneState.DefaultReturnSceneName;

        private void Start()
        {
            if (bossBattleUI != null)
                bossBattleUI.StartBossBattle();
            bossUIShown = true;
        }


    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (health == null) health = GetComponent<BossHealth>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (deathExplosion == null) deathExplosion = GetComponent<BossExplosionDeath>();

    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath.AddListener(Die);
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath.RemoveListener(Die);
    }


    private void Update()
    {
        if (health == null) return;

        if (isTransitioning)
        {
            animator.SetBool("IsMoving", false);
            return;
        }

        // Decide phase
        float hp = health.Normalized;
        Phase target =
            (hp <= phase3At) ? Phase.Phase3 :
            (hp <= phase2At) ? Phase.Phase2 :
            Phase.Phase1;

        if (target != currentPhase) SetPhase(target);

        // Behaviors
        switch (currentPhase)
        {
            case Phase.Phase1:
                MoveLeftRight(GetMoveSpeed());
                break;

            case Phase.Phase2:
                MoveLeftRight(GetMoveSpeed());
                break;

            case Phase.Phase3:
                animator.SetBool("IsMoving", false);
                   if (phase3StandPoint != null)
                        {
                            Vector3 p = phase3StandPoint.position;
                            p.x = Mathf.Clamp(p.x, leftX, rightX);
                            transform.position = p; // lock position
                            spriteRenderer.flipX = false; // ALWAYS LEFT

                        }

                         if (summonRoutine == null)
                            summonRoutine = StartCoroutine(SummonLoop());

                            if (phase3ThrowRoutine == null)
                                phase3ThrowRoutine = StartCoroutine(Phase3ThrowLoop());
                  break;
        }
    }
 

public void Die()
{
    if (isDead) return;
    isDead = true;
    
     if (bossBattleUI != null)
        bossBattleUI.EndBossBattle();

    // Stop everything
    StopAllCoroutines();
    animator.SetBool("IsMoving", false);

    // Optional: disable collisions so player can't get stuck
    foreach (var col in GetComponentsInChildren<Collider2D>())
        col.enabled = false;

    // Trigger death animation
    animator.SetTrigger("Die");

    if (deathExplosion != null)
        deathExplosion.Explode();

    LevelManager levelManager = LevelManager.Instance;
    if (levelManager == null)
    {
        Debug.LogWarning("LevelManager.Instance is null, so Trash Lord cannot show the win result panel.", this);
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

        public void OnPhaseTransitionComplete()
        {
            isTransitioning = false;

            // Phase 2: start summoning
            if (currentPhase == Phase.Phase2 && summonRoutine == null)
                summonRoutine = StartCoroutine(SummonLoop());

            // Phase 3: snap to stand point SAFELY + start throwing
            if (currentPhase == Phase.Phase3)
            {
                if (phase3StandPoint != null)
                {
                    Vector3 p = phase3StandPoint.position;
                    p.x = Mathf.Clamp(p.x, leftX, rightX); // stay inside arena bounds
                    transform.position = p;
                }

                if (phase3ThrowRoutine == null)
                    phase3ThrowRoutine = StartCoroutine(Phase3ThrowLoop());
            }
        }




    private float GetMoveSpeed()
    {
        return currentPhase switch
        {
            Phase.Phase2 => baseMoveSpeed * phase2SpeedMultiplier,
            Phase.Phase3 => baseMoveSpeed * phase3SpeedMultiplier,
            _ => baseMoveSpeed,
        };
    }
private void SetPhase(Phase p)
{
    if (currentPhase == p) return;

    currentPhase = p;

    Debug.Log("SET PHASE: " + p);
    // stop movement + actions during transition
    isTransitioning = true;
    animator.SetBool("IsMoving", false);

    // Stop Phase 2 summoning when leaving Phase 2
    if (summonRoutine != null)
    {
        StopCoroutine(summonRoutine);
        summonRoutine = null;
    }

    // Stop Phase 3 throwing when leaving Phase 3
    if (phase3ThrowRoutine != null)
    {
        StopCoroutine(phase3ThrowRoutine);
        phase3ThrowRoutine = null;
    }

    // Trigger transition animations
    if (p == Phase.Phase2)
        {
        animator.SetTrigger("Phase2Enter");
        }
    else if (p == Phase.Phase3)
        {
        animator.SetTrigger("Phase3Enter");
            
        if (spriteRenderer != null)
            spriteRenderer.flipX = true; 
        }
       
}


private IEnumerator Phase3ThrowLoop()
{
    yield return new WaitForSeconds(0.5f);

    while (currentPhase == Phase.Phase3 && health != null && health.CurrentHP > 0)
    {
        // THROW BURST
        float t = 0f;
        while (t < burstDuration && currentPhase == Phase.Phase3 && health.CurrentHP > 0)
        {
            animator.SetBool("IsThrowing", true); // <--- ADD
            animator.SetTrigger("Throw");
            yield return StartCoroutine(ThrowMultipleTrash());
            yield return new WaitForSeconds(throwInterval);
            t += throwInterval;

            yield return new WaitForSeconds(throwAnimLength); // let the animation play
            animator.SetBool("IsThrowing", false); // <--- ADD

            yield return new WaitForSeconds(throwInterval);

            t += (throwAnimLength + throwInterval);
        }

        // REST
        if (currentPhase != Phase.Phase3 || health.CurrentHP <= 0) break;
        yield return new WaitForSeconds(restDuration);
    }

    phase3ThrowRoutine = null;
}


private IEnumerator ThrowMultipleTrash()
{
    int count = Random.Range(minTrashPerAttack, maxTrashPerAttack + 1);

    for (int i = 0; i < count; i++)
    {
        ThrowTrash(multiThrowSpreadDegrees); // uses spread
        if (multiThrowGap > 0f)
            yield return new WaitForSeconds(multiThrowGap);
    }
}

   public void ThrowTrash(float spreadDegrees = 0f)

{
    if (trashProjectiles == null || trashProjectiles.Length == 0)
    {
        Debug.LogError("No trashProjectiles assigned!");
        return;
    }

    if (throwPoint == null)
    {
        Debug.LogError("throwPoint is NULL (assign it in Inspector)!");
        return;
    }

    GameObject prefab = trashProjectiles[Random.Range(0, trashProjectiles.Length)];

    if (prefab == null)
    {
        Debug.LogError("One of the trashProjectiles array elements is NULL!");
        return;
    }

    GameObject proj = Instantiate(prefab, throwPoint.position, Quaternion.identity);

        Collider2D projCol = proj.GetComponent<Collider2D>();
        if (projCol != null)
        {
            // ignore ALL boss colliders
            Collider2D[] bossCols = GetComponentsInChildren<Collider2D>();
            foreach (var c in bossCols)
                Physics2D.IgnoreCollision(projCol, c, true);
        }

   Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
if (rb != null)
{
    Vector2 throwDir = Vector2.left; // or your player-based direction

            float distMul = Random.Range(minForceMultiplier, maxForceMultiplier);
            float upMul   = Random.Range(minUpMultiplier, maxUpMultiplier);

            Vector2 force = new Vector2(throwDir.x * throwForce * distMul, throwForce * upMul);

            // angle spread
            if (spreadDegrees > 0f)
            {
                float ang = Random.Range(-spreadDegrees, spreadDegrees);
                force = (Vector2)(Quaternion.Euler(0, 0, ang) * force);
            }

            rb.linearVelocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);

}

    else
    {
        Debug.LogWarning("Projectile has no Rigidbody2D.");
    }
}



public void ThrowEnd()
{
    isThrowing = false;
}



    private void MoveLeftRight(float moveSpeed)
    {
        Vector3 pos = transform.position;
        pos.x += dir * moveSpeed * Time.deltaTime;

        if (pos.x >= rightX)
        {
            pos.x = rightX;
            dir = -1;
        }
        else if (pos.x <= leftX)
        {
            pos.x = leftX;
            dir = 1;
        }

        // Flip sprite based on direction (dir = -1 means facing left)
        if (spriteRenderer != null)
            spriteRenderer.flipX = (dir == 1);

        transform.position = pos;
    }

private IEnumerator SummonLoop()
{
    // initial delay before first summon
    yield return new WaitForSeconds(summonInterval);

    while (currentPhase == Phase.Phase2 || currentPhase == Phase.Phase3)
    {
        SummonTrashBot();

        float interval = (currentPhase == Phase.Phase3)
            ? summonInterval * phase3SummonMultiplier
            : summonInterval;

        yield return new WaitForSeconds(interval);
    }

    summonRoutine = null;
}


    private void SummonTrashBot()
    {
        if (trashBotPrefab == null || summonPoint == null) return;

        Instantiate(trashBotPrefab, summonPoint.position, Quaternion.identity);
    }
    private void OnDrawGizmosSelected()
{
    if (phase3StandPoint == null) return;

    Gizmos.color = Color.magenta;

    // Draw a circle at the stand point
    Gizmos.DrawWireSphere(phase3StandPoint.position, 0.4f);

    // Draw a vertical line so it's easy to spot
    Gizmos.DrawLine(
        phase3StandPoint.position + Vector3.up * 0.6f,
        phase3StandPoint.position + Vector3.down * 0.6f
    );
}

}
