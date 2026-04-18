using UnityEngine;
using System.Collections;

public class BossLaserAttack : MonoBehaviour
{
    [Header("Laser Parts (assign 5 objects)")]
    public GameObject[] laserParts;

    [Header("Warning Countdown")]
    [SerializeField] private GameObject[] warningObjects;
    [SerializeField] private GameObject warningObject;
    [SerializeField] private float warningStepDelay = 0.3f;
    [SerializeField] private float warningTime = 1.5f;

    [Header("Laser Timing")]
    [SerializeField] private float partDelay = 0.1f;
    [SerializeField] private float laserDuration = 2f;
    [SerializeField] private float cooldown = 3f;

    private Coroutine attackRoutine;

    public void BeginAutoLoop()
    {
        if (!isActiveAndEnabled || attackRoutine != null)
            return;

        attackRoutine = StartCoroutine(LaserLoop());
    }

    public void StopAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        TurnOffAllLasers();
        TurnOffAllWarnings();
    }

    private void OnDisable()
    {
        StopAttack();
    }

    private IEnumerator LaserLoop()
    {
        while (true)
            yield return ExecuteAttackCycle();
    }

    public IEnumerator ExecuteAttackCycle()
    {
        TurnOffAllLasers();
        TurnOffAllWarnings();

        yield return PlayWarningCountdown();
        yield return FireLaserSequence();
        yield return new WaitForSeconds(laserDuration);

        TurnOffAllLasers();
        TurnOffAllWarnings();

        if (cooldown > 0f)
            yield return new WaitForSeconds(cooldown);
    }

    private IEnumerator PlayWarningCountdown()
    {
        if (warningObject != null)
            warningObject.SetActive(true);

        if (warningObjects != null && warningObjects.Length > 0)
        {
            for (int i = 0; i < warningObjects.Length; i++)
            {
                if (warningObjects[i] != null)
                    warningObjects[i].SetActive(true);

                if (warningStepDelay > 0f)
                    yield return new WaitForSeconds(warningStepDelay);
            }

            yield break;
        }

        if (warningObject != null)
        {
            warningObject.SetActive(true);

            if (warningTime > 0f)
                yield return new WaitForSeconds(warningTime);
        }
    }

    private IEnumerator FireLaserSequence()
    {
        if (laserParts == null)
            yield break;

        for (int i = 0; i < laserParts.Length; i++)
        {
            if (laserParts[i] != null)
                laserParts[i].SetActive(true);

            if (partDelay > 0f)
                yield return new WaitForSeconds(partDelay);
        }
    }

    private void TurnOffAllLasers()
    {
        if (laserParts == null)
            return;

        foreach (GameObject part in laserParts)
        {
            if (part != null)
                part.SetActive(false);
        }
    }

    private void TurnOffAllWarnings()
    {
        if (warningObjects != null)
        {
            foreach (GameObject warning in warningObjects)
            {
                if (warning != null)
                    warning.SetActive(false);
            }
        }

        if (warningObject != null)
            warningObject.SetActive(false);
    }
}
