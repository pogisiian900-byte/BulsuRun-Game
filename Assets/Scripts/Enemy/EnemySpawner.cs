using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Settings")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxAlive = 5;

    private float timer;
    private List<GameObject> aliveEnemies = new List<GameObject>();

    void Update()
    {
        // Remove destroyed enemies from list
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null)
                aliveEnemies.RemoveAt(i);
        }

        if (aliveEnemies.Count >= maxAlive) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            Spawn();
            timer = 0f;
        }
    }

    void Spawn()
    {
        if (enemyPrefab == null || spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        aliveEnemies.Add(enemy);
    }
    private void OnDrawGizmosSelected()
{
    if (spawnPoints == null) return;

    Gizmos.color = Color.red;

    foreach (Transform point in spawnPoints)
    {
        if (point != null)
        {
            Gizmos.DrawSphere(point.position, 0.3f);
        }
    }
}

}
