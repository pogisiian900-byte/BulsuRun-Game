using UnityEngine;
using System.Collections;

public class FitBossSkyDropper : MonoBehaviour
{
    [Header("Ball")]
    [SerializeField] private DodgeBallProjectile regualarballPrefab;

    [Header("Drop Points")]
    [SerializeField] private Transform[] dropPoints;

    [Header("Spawn Rate")]
    [SerializeField] private float patternCooldown = 2f;

    private void OnEnable()
    {
        StartCoroutine(PatternLoop());
    }

   IEnumerator PatternLoop()
{
    while (true)
    {
        int pattern = Random.Range(0, 3);

        if (pattern == 0)
            yield return StartCoroutine(PatternLine());

        if (pattern == 1)
            yield return StartCoroutine(PatternAlternate());

        if (pattern == 2)
            yield return StartCoroutine(PatternWave());

        yield return new WaitForSeconds(patternCooldown);
    }
}
   IEnumerator PatternLine()
{
    foreach (Transform p in dropPoints)
    {
        SpawnBall(p);
        yield return new WaitForSeconds(0.1f); // spacing
    }
}

    IEnumerator PatternAlternate()
    {
        for (int i = 0; i < dropPoints.Length; i += 2)
        {
            SpawnBall(dropPoints[i]);
        }

        yield return null;
    }

    IEnumerator PatternWave()
    {
        foreach (Transform p in dropPoints)
        {
            SpawnBall(p);
            yield return new WaitForSeconds(0.2f);
        }
    }

    void SpawnBall(Transform point)
    {
        var proj = Instantiate(regualarballPrefab, point.position, Quaternion.identity);

        Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.down * 2f;
    }

    private void OnDrawGizmosSelected()
    {
        if (dropPoints == null) return;

        Gizmos.color = Color.cyan;

        foreach (Transform p in dropPoints)
        {
            if (p == null) continue;

            Gizmos.DrawSphere(p.position, 0.2f);
            Gizmos.DrawLine(p.position, p.position + Vector3.down * 1.5f);
        }
    }
}