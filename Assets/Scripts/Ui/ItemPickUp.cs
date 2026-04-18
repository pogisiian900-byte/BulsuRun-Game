using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public Item item;
    public int amount = 1;

    public Inventory inventory;

    private string pickupId;

    private void Awake()
    {
        pickupId = LevelCollectibleProgress.BuildPickupId(this);
        if (LevelCollectibleProgress.IsCollectedInCompletedLevel(this, pickupId))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(collision, out _))
            return;

        Inventory targetInventory = inventory != null ? inventory : Inventory.Instance;
        if (targetInventory == null)
            return;

        if (!targetInventory.AddItem(item, amount))
            return;

        LevelCollectibleProgress.RegisterCollected(pickupId);
        Destroy(gameObject);
    }
}
