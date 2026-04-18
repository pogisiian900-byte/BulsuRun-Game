using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossDroneAttack : MonoBehaviour
{
    [Header("Setup")]
    public GameObject dronePrefab;
    public Transform[] spawnPoints;

    [Header("Timing")]
    public float spawnInterval = 3f;

    private Coroutine attackRoutine;
    private readonly List<GameObject> activeDrones = new List<GameObject>();

    public void BeginAutoLoop()
    {
        if (!isActiveAndEnabled || attackRoutine != null)
            return;

        attackRoutine = StartCoroutine(AttackLoop());
    }

    public void StopAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        DestroySpawnedDrones();
    }

    private void OnDisable()
    {
        StopAttack();
    }

    private IEnumerator AttackLoop()
    {
        while (true)
            yield return ExecuteAttackCycle();
    }

    public IEnumerator ExecuteAttackCycle()
    {
        SpawnDrones();

        if (spawnInterval > 0f)
            yield return new WaitForSeconds(spawnInterval);
    }

    private void SpawnDrones()
    {
        if (dronePrefab == null || spawnPoints == null)
            return;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            GameObject spawnedDrone = Instantiate(dronePrefab, point.position, Quaternion.identity);
            activeDrones.Add(spawnedDrone);
        }
    }

    private void DestroySpawnedDrones()
    {
        for (int i = activeDrones.Count - 1; i >= 0; i--)
        {
            if (activeDrones[i] != null)
                Destroy(activeDrones[i]);
        }

        activeDrones.Clear();
    }
}
