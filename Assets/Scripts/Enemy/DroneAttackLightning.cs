using UnityEngine;

public class DroneLightningAttack : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackInterval = 3f;
    [SerializeField] private LightningStrike lightningPrefab;

    private void OnEnable()
    {
        CancelInvoke(nameof(Strike));
        InvokeRepeating(nameof(Strike), attackInterval, attackInterval);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Strike));
    }

    void Strike()
    {
        if (firePoint == null || lightningPrefab == null) return;

        var lightning = Instantiate(lightningPrefab, firePoint.position, Quaternion.identity);
        lightning.Init(firePoint.position);
    }
}
