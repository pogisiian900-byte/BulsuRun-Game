using System.Collections;
using UnityEngine;

public class SkyBotRocketAttack : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject rocketPrefab;

    [SerializeField] private float fireInterval = 2f;

    private float timer;

    private void OnEnable()
    {
        timer = Mathf.Max(0f, fireInterval);
    }

    private void OnDisable()
    {
        StopAttack();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            FireRocket();
            timer = fireInterval;
        }
    }

    void FireRocket()
    {
        if (rocketPrefab == null || firePoint == null)
            return;

        Instantiate(rocketPrefab, firePoint.position, firePoint.rotation);
    }

    public void StopAttack()
    {
        timer = Mathf.Max(0f, fireInterval);
    }

private void OnDrawGizmosSelected()
{
    if (firePoint == null) return;

    Gizmos.color = Color.yellow;

    // draw fire point
    Gizmos.DrawSphere(firePoint.position, 0.15f);

    // draw rocket direction
    Vector3 dir = firePoint.up * 2f;

    Gizmos.DrawLine(firePoint.position, firePoint.position + dir);
}
}
