using UnityEngine;

public class Coins : MonoBehaviour
{
    [SerializeField] private int coinValue = 1;
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField, Range(0f, 1f)] private float pickupSfxVolume = 0.9f;
    [SerializeField, Range(0.1f, 3f)] private float pickupSfxPitch = 1.12f;
    private string pickupId;

    private void Awake()
    {
        pickupId = LevelCollectibleProgress.BuildPickupId(this);
        if (LevelCollectibleProgress.IsCollectedInCompletedLevel(this, pickupId))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(collision, out Transform localPlayer))
            return;

        PlayerInventory player = localPlayer.GetComponent<PlayerInventory>();
        if (player != null)
        {
            player.AddCoin(coinValue);
            LevelCollectibleProgress.RegisterCollected(pickupId);
            SceneAudioManager.PlayCoinPickupSfx(pickupSfx, pickupSfxVolume, pickupSfxPitch);
        }

        Destroy(gameObject);
    }
}
