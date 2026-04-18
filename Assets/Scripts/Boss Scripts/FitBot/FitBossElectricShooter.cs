using UnityEngine;

public class FitBossElectricShooter : MonoBehaviour
{
    [Header("Electric Ball")]
    [SerializeField] private ElectricBallProjectile electricBallPrefab;

    [Header("Fire Points")]
    [SerializeField] private Transform[] firePoints;

    [Header("Spawn Rate")]
    [SerializeField] private float dropCooldown = 1.2f;

    private float baseDropCooldown;
    private float timer;

    private void Awake()
    {
        baseDropCooldown = Mathf.Max(0.01f, dropCooldown);
        dropCooldown = baseDropCooldown;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    public void SetFireRateMultiplier(float multiplier)
    {
        if (baseDropCooldown <= 0f)
            baseDropCooldown = Mathf.Max(0.01f, dropCooldown);

        multiplier = Mathf.Max(0.01f, multiplier);
        dropCooldown = baseDropCooldown / multiplier;
        timer = Mathf.Min(timer, dropCooldown);
    }

    private void Update()
    {
        if (electricBallPrefab == null || firePoints.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= dropCooldown)
        {
            timer = 0f;
            ShootElectricBalls();
        }
    }

    private void ShootElectricBalls()
    {
        foreach (Transform firePoint in firePoints)
        {
            var proj = Instantiate(electricBallPrefab, firePoint.position, Quaternion.identity);

            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.down * 6f; // faster downward shot
        }
    }
}
