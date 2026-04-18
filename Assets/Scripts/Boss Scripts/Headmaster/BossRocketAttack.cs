using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossRocketAttack : MonoBehaviour
{
    [Header("Setup")]
    public GameObject rocketPrefab;
    public Transform[] spawnPoints;
    public int startingSpawnIndex = 0;
    public bool flipSpawnedSprite = false;
    public bool spriteFacesRightByDefault = true;

    [Header("Timing")]
    public float spawnInterval = 3f;

    private Coroutine attackRoutine;
    private readonly List<GameObject> activeRockets = new List<GameObject>();
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
    public int SpawnPointCount => spawnPoints != null ? spawnPoints.Length : 0;
    public bool HasActiveRockets
    {
        get
        {
            CleanupDestroyedRockets();
            return activeRockets.Count > 0;
        }
    }

    public void LaunchNextRocket()
    {
        LaunchRockets();
    }

    public void LaunchRocketFromSpawnIndex(int spawnIndex)
    {
        LaunchRockets(spawnIndex, false);
    }

    public void SetNextSpawnIndex(int spawnIndex)
    {
        int availableSpawnCount = GetAvailableSpawnCount();
        if (availableSpawnCount <= 0)
        {
            nextSpawnIndex = 0;
            return;
        }

        nextSpawnIndex = Mathf.Clamp(spawnIndex, 0, availableSpawnCount - 1);
    }

    public void StopAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        DestroySpawnedRockets();
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
        LaunchRockets();

        if (spawnInterval > 0f)
            yield return new WaitForSeconds(spawnInterval);
    }

    private void LaunchRockets()
    {
        LaunchRockets(nextSpawnIndex, true);
    }

    private void LaunchRockets(int spawnIndex, bool advanceSequence)
    {
        int selectedSpawnIndex;
        Transform selectedSpawnPoint = GetSpawnPoint(spawnIndex, out selectedSpawnIndex, advanceSequence);

        if (rocketPrefab == null || selectedSpawnPoint == null)
            return;

        CleanupDestroyedRockets();

        GameObject spawnedRocket = Instantiate(rocketPrefab, selectedSpawnPoint.position, Quaternion.identity);
        ConfigureRocketMovement(spawnedRocket, selectedSpawnIndex, selectedSpawnPoint);
        if (flipSpawnedSprite)
            ApplyFacingToSprite(spawnedRocket, selectedSpawnIndex);
        activeRockets.Add(spawnedRocket);
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

    private Transform GetSpawnPoint(int requestedSpawnIndex, out int usedSpawnIndex, bool advanceSequence)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            usedSpawnIndex = -1;
            return null;
        }

        int availableSpawnCount = spawnPoints.Length;
        int startIndex = Mathf.Clamp(requestedSpawnIndex, 0, availableSpawnCount - 1);
        int attempts = availableSpawnCount;
        int currentIndex = startIndex;

        while (attempts-- > 0)
        {
            usedSpawnIndex = currentIndex;
            Transform selectedSpawnPoint = spawnPoints[currentIndex];

            if (selectedSpawnPoint != null)
            {
                if (advanceSequence)
                    nextSpawnIndex = (currentIndex + 1) % availableSpawnCount;

                return selectedSpawnPoint;
            }

            currentIndex = (currentIndex + 1) % availableSpawnCount;
        }

        usedSpawnIndex = -1;
        return null;
    }

    private int GetAvailableSpawnCount()
    {
        return spawnPoints != null ? spawnPoints.Length : 0;
    }

    private void ApplyFacingToSprite(GameObject spawnedRocket, int spawnIndex)
    {
        SpriteRenderer spriteRenderer = spawnedRocket.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        bool shouldFaceRight = GetRocketDirection(spawnIndex, spawnedRocket.transform).x > 0f;

        spriteRenderer.flipX = shouldFaceRight ? !spriteFacesRightByDefault : spriteFacesRightByDefault;
    }

    private void ConfigureRocketMovement(GameObject spawnedRocket, int spawnIndex, Transform selectedSpawnPoint)
    {
        SkyBotRocket rocket = spawnedRocket.GetComponent<SkyBotRocket>();
        if (rocket == null)
            return;

        rocket.ConfigureDirection(GetRocketDirection(spawnIndex, selectedSpawnPoint));
    }

    private Vector2 GetRocketDirection(int spawnIndex, Transform selectedSpawnPoint)
    {
        if (spawnPoints != null && spawnPoints.Length > 1 && selectedSpawnPoint != null)
        {
            float averageX = 0f;
            int count = 0;

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] == null)
                    continue;

                averageX += spawnPoints[i].position.x;
                count++;
            }

            if (count > 0)
            {
                averageX /= count;
                return selectedSpawnPoint.position.x <= averageX ? Vector2.right : Vector2.left;
            }
        }

        return spawnIndex == 0 ? Vector2.right : Vector2.left;
    }

    private void DestroySpawnedRockets()
    {
        for (int i = activeRockets.Count - 1; i >= 0; i--)
        {
            if (activeRockets[i] != null)
                Destroy(activeRockets[i]);
        }

        activeRockets.Clear();
    }

    private void CleanupDestroyedRockets()
    {
        for (int i = activeRockets.Count - 1; i >= 0; i--)
        {
            if (activeRockets[i] == null)
                activeRockets.RemoveAt(i);
        }
    }
}
