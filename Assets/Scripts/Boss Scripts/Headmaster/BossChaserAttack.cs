using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossChaserAttack : MonoBehaviour
{
    [Header("Setup")]
    public GameObject chaserPrefab;
    public Transform spawnPoint;
    public Transform[] spawnPoints;
    public int startingSpawnIndex = 0;
    public Transform[] stopPoints;
    public bool useOppositeStopPoint = true;
    public bool stopAtAssignedStopPoint = true;
    public bool destroyAtAssignedStopPoint = false;
    public bool flipSpawnedSprite = false;
    public bool spriteFacesRightByDefault = true;

    [Header("Timing")]
    public float spawnInterval = 2f;

    private Coroutine attackRoutine;
    private readonly List<GameObject> activeChasers = new List<GameObject>();
    private int nextSpawnIndex;

    public void BeginAutoLoop()
    {
        if (!isActiveAndEnabled || attackRoutine != null)
            return;

        ResetSequenceState();
        attackRoutine = StartCoroutine(AttackLoop());
    }

    public void ResetSequence()
    {
        ResetSequenceState();
    }

    public float SpawnInterval => spawnInterval;

    public bool HasActiveChasers
    {
        get
        {
            CleanupDestroyedChasers();
            return activeChasers.Count > 0;
        }
    }

    public void SpawnNextChaser()
    {
        SpawnChaser();
    }

    public void StopAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        DestroySpawnedChasers();
    }

    private void OnDisable()
    {
        StopAttack();
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            SpawnChaser();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnChaser()
    {
        int selectedSpawnIndex;
        Transform selectedSpawnPoint = GetNextSpawnPoint(out selectedSpawnIndex);

        if (chaserPrefab == null || selectedSpawnPoint == null)
            return;

        GameObject spawnedChaser = Instantiate(chaserPrefab, selectedSpawnPoint.position, Quaternion.identity);
        Transform assignedStopPoint = GetAssignedStopPoint(selectedSpawnIndex);

        if (flipSpawnedSprite)
            ApplyFacingToSprite(spawnedChaser, assignedStopPoint != null ? assignedStopPoint.position.x : selectedSpawnPoint.position.x);

        EnemyChase enemyChase = spawnedChaser.GetComponent<EnemyChase>();
        if (enemyChase != null)
            enemyChase.ConfigureStopPoint(assignedStopPoint, stopAtAssignedStopPoint, destroyAtAssignedStopPoint);

        activeChasers.Add(spawnedChaser);
    }

    private void ResetSequenceState()
    {
        int availableSpawnCount = GetAvailableSpawnCount();
        if (availableSpawnCount <= 0)
        {
            nextSpawnIndex = 0;
            return;
        }

        nextSpawnIndex = Mathf.Clamp(startingSpawnIndex, 0, availableSpawnCount - 1);
    }

    private Transform GetNextSpawnPoint(out int usedSpawnIndex)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int attempts = spawnPoints.Length;

            while (attempts-- > 0)
            {
                usedSpawnIndex = nextSpawnIndex;
                Transform selectedSpawnPoint = spawnPoints[nextSpawnIndex];
                nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Length;

                if (selectedSpawnPoint != null)
                    return selectedSpawnPoint;
            }

            usedSpawnIndex = -1;
            return null;
        }

        usedSpawnIndex = 0;
        return spawnPoint;
    }

    private int GetAvailableSpawnCount()
    {
        return (spawnPoints != null && spawnPoints.Length > 0) ? spawnPoints.Length : (spawnPoint != null ? 1 : 0);
    }

    private Transform GetAssignedStopPoint(int spawnIndex)
    {
        if (stopPoints == null || stopPoints.Length == 0)
            return null;

        int stopPointIndex = spawnIndex;

        if (useOppositeStopPoint && stopPoints.Length > 1 && spawnIndex >= 0)
            stopPointIndex = (spawnIndex + 1) % stopPoints.Length;

        stopPointIndex = Mathf.Clamp(stopPointIndex, 0, stopPoints.Length - 1);
        return stopPoints[stopPointIndex];
    }

    private void ApplyFacingToSprite(GameObject spawnedChaser, float targetX)
    {
        SpriteRenderer spriteRenderer = spawnedChaser.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        bool shouldFaceRight = targetX >= spawnedChaser.transform.position.x;
        spriteRenderer.flipX = shouldFaceRight ? !spriteFacesRightByDefault : spriteFacesRightByDefault;
    }

    private void DestroySpawnedChasers()
    {
        for (int i = activeChasers.Count - 1; i >= 0; i--)
        {
            if (activeChasers[i] != null)
                Destroy(activeChasers[i]);
        }

        activeChasers.Clear();
    }

    private void CleanupDestroyedChasers()
    {
        for (int i = activeChasers.Count - 1; i >= 0; i--)
        {
            if (activeChasers[i] == null)
                activeChasers.RemoveAt(i);
        }
    }
}
