using UnityEngine;
using System.Collections;

public class SkyBotRocketSummonAttack : MonoBehaviour
{
    [Header("Rocket Settings")]
    [SerializeField] private GameObject rocketPrefab;
    [SerializeField] private Transform[] firePoints; // [0]=TopLeft, [1]=TopRight, [2]=BottomLeft, [3]=BottomRight
    [SerializeField] private float fireDelay = 1.5f;

    [Header("Ground")]
    [SerializeField] private GameObject[] normalGrounds;   // the intact platforms
    [SerializeField] private GameObject[] brokenGrounds;   // the broken versions
    [SerializeField] private GameObject groundExplosionPrefab;

    [Header("Warnings")]
    [SerializeField] private GameObject[] warningMessages; // two warnings in Canvas
    [SerializeField] private float warningTime = 2f;
    [SerializeField] private GameObject warningExplosionPrefab;

    [Header("Boss Movement")]
    [SerializeField] private SkyBotMovement movement;
    
    private Coroutine phase3SequenceRoutine;
    private Coroutine rocketLoopRoutine;
    private int nextFirePointIndex;

    private void Awake()
    {
        ResetArenaState();
    }

    private void OnDisable()
    {
        StopAttack();
        SetWarningsActive(false);
    }

    public void BeginPhase3Sequence()
    {
        StopAttack();
        nextFirePointIndex = 0;

        if (movement != null)
            movement.EnterPhase3LastStand();

        phase3SequenceRoutine = StartCoroutine(Phase3Sequence());
    }

    public void StopAttack()
    {
        if (phase3SequenceRoutine != null)
        {
            StopCoroutine(phase3SequenceRoutine);
            phase3SequenceRoutine = null;
        }

        if (rocketLoopRoutine != null)
        {
            StopCoroutine(rocketLoopRoutine);
            rocketLoopRoutine = null;
        }
    }

    public void ResetArenaState()
    {
        StopAttack();
        SetWarningsActive(false);
        SetGroundState(showNormalGrounds: true, showBrokenGrounds: false);
    }

    IEnumerator Phase3Sequence()
    {
        SetWarningsActive(false);

        // Show warnings
        foreach (GameObject warning in warningMessages)
        {
            if (warning != null) warning.SetActive(true);

            if (warningExplosionPrefab != null && warning != null)
                Instantiate(warningExplosionPrefab, warning.transform.position, Quaternion.identity);
        }

        if (warningTime > 0f)
            yield return new WaitForSeconds(warningTime);

        // Hide warnings
        SetWarningsActive(false);

        // Destroy normal ground and activate broken ground
        TriggerGroundSwap();

        yield return new WaitForSeconds(1f);

        // Start rocket firing loop
        rocketLoopRoutine = StartCoroutine(RocketSummonLoop());
        phase3SequenceRoutine = null;
    }

    IEnumerator RocketSummonLoop()
    {
        if (firePoints == null || firePoints.Length == 0)
            yield break;

        while (true)
        {
            FireRocket(firePoints[nextFirePointIndex]);
            nextFirePointIndex = (nextFirePointIndex + 1) % firePoints.Length;

            if (fireDelay > 0f)
                yield return new WaitForSeconds(fireDelay);
            else
                yield return null;
        }
    }

    void FireRocket(Transform firePoint)
    {
        if (rocketPrefab != null && firePoint != null)
        {
            Instantiate(rocketPrefab, firePoint.position, firePoint.rotation);
        }
    }

    private void TriggerGroundSwap()
    {
        if (normalGrounds != null)
        {
            foreach (GameObject ground in normalGrounds)
            {
                if (ground == null || !ground.activeSelf)
                    continue;

                if (groundExplosionPrefab != null)
                    Instantiate(groundExplosionPrefab, ground.transform.position, Quaternion.identity);

                ground.SetActive(false);
            }
        }

        if (brokenGrounds != null)
        {
            foreach (GameObject broken in brokenGrounds)
            {
                if (broken != null)
                    broken.SetActive(true);
            }
        }
    }

    private void SetGroundState(bool showNormalGrounds, bool showBrokenGrounds)
    {
        if (normalGrounds != null)
        {
            foreach (GameObject ground in normalGrounds)
            {
                if (ground != null)
                    ground.SetActive(showNormalGrounds);
            }
        }

        if (brokenGrounds != null)
        {
            foreach (GameObject broken in brokenGrounds)
            {
                if (broken != null)
                    broken.SetActive(showBrokenGrounds);
            }
        }
    }

    private void SetWarningsActive(bool isActive)
    {
        if (warningMessages == null)
            return;

        foreach (GameObject warning in warningMessages)
        {
            if (warning != null)
                warning.SetActive(isActive);
        }
    }
}
