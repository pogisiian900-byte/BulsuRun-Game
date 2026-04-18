using UnityEngine;

public class HeartPickup : MonoBehaviour
{
    public int healAmount = 20;
    private string pickupId;

    private void Awake()
    {
        pickupId = LevelCollectibleProgress.BuildPickupId(this);
        if (LevelCollectibleProgress.IsCollectedInCompletedLevel(this, pickupId))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(other, out Transform localPlayer))
            return;

        PlayerHealth health = localPlayer.GetComponent<PlayerHealth>();
        if (health == null)
            return;

        health.Heal(healAmount);
        LevelCollectibleProgress.RegisterCollected(pickupId);
        Destroy(gameObject);
    }
}
