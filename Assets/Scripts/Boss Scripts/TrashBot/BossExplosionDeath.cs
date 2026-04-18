using System.Collections;
using UnityEngine;

public class BossExplosionDeath : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionDelay = 0.2f;
    [SerializeField] private float destroyDelay = 1.5f;

    public void Explode()
    {
        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        yield return new WaitForSeconds(explosionDelay);

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}

