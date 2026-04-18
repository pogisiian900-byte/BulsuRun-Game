using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossBulletAttack : MonoBehaviour
{
    private enum SpawnSide
    {
        Left,
        Right
    }

    [Header("Setup")]
    public GameObject bulletEnemyPrefab;
    public Transform[] leftSpawnPoints;
    public Transform[] rightSpawnPoints;

    [Header("Timing")]
    public float spawnInterval = 2.5f;
    public float sideDuration = 5f;

    private Coroutine attackRoutine;
    private readonly List<GameObject> activeBulletEnemies = new List<GameObject>();

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

        DestroySpawnedBulletEnemies();
    }

    private void OnDisable()
    {
        StopAttack();
    }

    private IEnumerator AttackLoop()
    {
        float safeSpawnInterval = Mathf.Max(0.01f, spawnInterval);
        float safeSideDuration = Mathf.Max(0.01f, sideDuration);
        SpawnSide currentSide = SpawnSide.Left;
        float sideStartTime = Time.time;
        float nextSpawnTime = Time.time;

        while (true)
        {
            if (Time.time - sideStartTime >= safeSideDuration)
            {
                currentSide = currentSide == SpawnSide.Left ? SpawnSide.Right : SpawnSide.Left;
                sideStartTime = Time.time;
                nextSpawnTime = Time.time;
            }

            if (Time.time >= nextSpawnTime)
            {
                SpawnBulletEnemies(currentSide);
                nextSpawnTime += safeSpawnInterval;
            }

            yield return null;
        }
    }

    private void SpawnBulletEnemies(SpawnSide side)
    {
        if (bulletEnemyPrefab == null)
            return;

        Transform[] activeSpawnPoints = side == SpawnSide.Left ? leftSpawnPoints : rightSpawnPoints;
        int moveDirection = side == SpawnSide.Left ? 1 : -1;

        if (activeSpawnPoints == null || activeSpawnPoints.Length == 0)
            return;

        foreach (Transform point in activeSpawnPoints)
        {
            if (point == null)
                continue;

            GameObject spawnedBulletEnemy = Instantiate(bulletEnemyPrefab, point.position, Quaternion.identity);
            activeBulletEnemies.Add(spawnedBulletEnemy);
            FloatingEnemy floatingEnemy = spawnedBulletEnemy.GetComponent<FloatingEnemy>();

            if (floatingEnemy != null)
                floatingEnemy.SetDirection(moveDirection);
        }
    }

    private void DestroySpawnedBulletEnemies()
    {
        for (int i = activeBulletEnemies.Count - 1; i >= 0; i--)
        {
            if (activeBulletEnemies[i] != null)
                Destroy(activeBulletEnemies[i]);
        }

        activeBulletEnemies.Clear();
    }
}
