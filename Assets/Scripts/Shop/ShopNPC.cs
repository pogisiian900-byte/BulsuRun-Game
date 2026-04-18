using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class ShopNPC : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private ShopItemData[] itemsForSale;

    [Header("UI Button")]
    [SerializeField] private GameObject shopButtonObject;

    private Button shopButton;
    private bool playerInRange;
    private PlayerInventory player;
    private readonly HashSet<ShopItemData> purchasedItems = new();

    private void Awake()
    {
        if (shopButtonObject != null)
        {
            shopButtonObject.SetActive(false);
            shopButton = shopButtonObject.GetComponent<Button>();

            if (shopButton != null)
                shopButton.onClick.AddListener(OpenShop);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!TryGetLocalPlayer(other, out PlayerInventory localPlayer))
            return;

        playerInRange = true;
        player = localPlayer;
        RefreshShopButtonVisibility();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!TryGetLocalPlayer(other, out PlayerInventory localPlayer) || localPlayer != player)
            return;

        playerInRange = false;
        player = null;
        RefreshShopButtonVisibility();

        if (shopUI != null)
            shopUI.Close();
    }

    private void OpenShop()
    {
        if (!playerInRange || player == null || shopUI == null)
            return;

        ShopItemData[] availableItems = GetAvailableItems();
        if (availableItems.Length == 0)
        {
            RefreshShopButtonVisibility();
            shopUI.Close();
            return;
        }

        shopUI.Toggle(player, availableItems, HandleItemPurchased);
    }

    private void HandleItemPurchased(ShopItemData purchasedItem)
    {
        if (purchasedItem == null)
            return;

        purchasedItems.Add(purchasedItem);
        RefreshShopButtonVisibility();
    }

    private void RefreshShopButtonVisibility()
    {
        if (shopButtonObject == null)
            return;

        shopButtonObject.SetActive(playerInRange && GetAvailableItemCount() > 0);
    }

    private ShopItemData[] GetAvailableItems()
    {
        if (itemsForSale == null || itemsForSale.Length == 0)
            return System.Array.Empty<ShopItemData>();

        List<ShopItemData> availableItems = new(itemsForSale.Length);

        foreach (ShopItemData item in itemsForSale)
        {
            if (item == null || purchasedItems.Contains(item))
                continue;

            availableItems.Add(item);
        }

        return availableItems.ToArray();
    }

    private int GetAvailableItemCount()
    {
        if (itemsForSale == null)
            return 0;

        int count = 0;

        foreach (ShopItemData item in itemsForSale)
        {
            if (item != null && !purchasedItems.Contains(item))
                count++;
        }

        return count;
    }

    private static bool TryGetLocalPlayer(Collider2D other, out PlayerInventory localPlayer)
    {
        localPlayer = other != null ? other.GetComponentInParent<PlayerInventory>() : null;
        if (localPlayer == null || !localPlayer.CompareTag("Player"))
        {
            localPlayer = null;
            return false;
        }

        PhotonView playerView = localPlayer.GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && (playerView == null || !playerView.IsMine))
        {
            localPlayer = null;
            return false;
        }

        return true;
    }
}
