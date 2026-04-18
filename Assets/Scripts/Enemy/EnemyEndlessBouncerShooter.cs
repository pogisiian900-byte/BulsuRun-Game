using UnityEngine;

public class EnemyEndlessBouncerShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Fire Settings")]
    [SerializeField] private float fireCooldown = 1.5f;

    [Header("Shoot Direction")]
    [SerializeField] private Vector2 shootDirection = Vector2.left;

    [Header("Gizmo")]
    [SerializeField] private float gizmoLength = 2f;

    private float timer;

    private void Awake()
    {
        if (firePoint == null)
            firePoint = transform;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= fireCooldown)
        {
            timer = 0f;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null) return;

        GameObject obj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        var proj = obj.GetComponent<DodgeBallProjectile>();
        if (proj != null)
            proj.Launch(shootDirection);
    }

    // 👇 GIZMO DRAW
    private void OnDrawGizmosSelected()
    {
        if (firePoint == null)
            firePoint = transform;

        Gizmos.color = Color.red;

        Vector3 start = firePoint.position;
        Vector3 dir = new Vector3(shootDirection.x, shootDirection.y, 0f).normalized;
        Vector3 end = start + dir * gizmoLength;

        // draw direction line
        Gizmos.DrawLine(start, end);

        // draw firepoint sphere
        Gizmos.DrawSphere(start, 0.15f);
    }
}