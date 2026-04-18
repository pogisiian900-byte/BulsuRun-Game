using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform content;
    [SerializeField] private ShopRow rowPrefab;

    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private Button closeButton;

    [Header("Purchase Feedback")]
    [SerializeField] private GameObject shopComponent;
    [SerializeField] private GameObject purchasePanel;
    [SerializeField] private float purchasePanelDuration = 2f;

    private PlayerInventory player;
    private PlayerSkillHolder playerSkill;
    private Action<ShopItemData> onPurchaseCompleted;
    private Coroutine purchaseRoutine;
    private bool purchaseInProgress;

    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        CacheOptionalReferences();
        RestorePanelsToDefaultState();

        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Open(PlayerInventory player, ShopItemData[] items, Action<ShopItemData> onPurchaseCompleted = null)
    {
        CacheOptionalReferences();

        if (root == null || content == null || rowPrefab == null)
        {
            Debug.LogError("ShopUI is missing required references.");
            return;
        }

        this.player = player;
        this.onPurchaseCompleted = onPurchaseCompleted;
        purchaseInProgress = false;

        if (purchaseRoutine != null)
        {
            StopCoroutine(purchaseRoutine);
            purchaseRoutine = null;
        }

        if (playerSkill == null)
        {
            playerSkill = PlayerSkillHolder.Instance != null
                ? PlayerSkillHolder.Instance
                : FindFirstObjectByType<PlayerSkillHolder>();
        }

        Time.timeScale = 0f;
        root.SetActive(true);
        RestorePanelsToDefaultState();

        foreach (Transform child in content)
            Destroy(child.gameObject);

        if (items != null)
        {
            foreach (ShopItemData item in items)
            {
                if (item == null)
                    continue;

                ShopRow row = Instantiate(rowPrefab, content);
                row.Set(item, OnBuyClicked);
            }
        }

        if (closeButton != null)
            closeButton.interactable = true;

        RefreshCoins();
    }

    public void Close()
    {
        if (purchaseRoutine != null)
        {
            StopCoroutine(purchaseRoutine);
            purchaseRoutine = null;
        }

        purchaseInProgress = false;
        Time.timeScale = 1f;
        RestorePanelsToDefaultState();

        if (root != null)
            root.SetActive(false);

        player = null;
        onPurchaseCompleted = null;
    }

    public void Toggle(PlayerInventory player, ShopItemData[] items, Action<ShopItemData> onPurchaseCompleted = null)
    {
        if (IsOpen) Close();
        else Open(player, items, onPurchaseCompleted);
    }

    private void OnBuyClicked(ShopItemData shopItem)
    {
        if (player == null || shopItem == null || purchaseInProgress)
            return;

        if (player.coins < shopItem.buyPrice)
        {
            Debug.Log("Not enough coins!");
            return;
        }

        bool added = false;

        if (shopItem.weapon != null)
        {
            added = Inventory.Instance != null && Inventory.Instance.AddWeapon(shopItem.weapon);
        }
        else if (shopItem.skill != null)
        {
            if (playerSkill == null)
            {
                playerSkill = PlayerSkillHolder.Instance != null
                    ? PlayerSkillHolder.Instance
                    : FindFirstObjectByType<PlayerSkillHolder>();
            }

            if (playerSkill != null && !playerSkill.IsFull)
            {
                playerSkill.AddSkill(shopItem.skill);
                added = true;
            }
        }

        if (!added)
            return;

        purchaseInProgress = true;
        player.AddCoin(-shopItem.buyPrice);
        RefreshCoins();
        onPurchaseCompleted?.Invoke(shopItem);

        if (closeButton != null)
            closeButton.interactable = false;

        SetPurchasePanelVisible(true);
        purchaseRoutine = StartCoroutine(CloseAfterPurchaseDelay());
    }

    private void RefreshCoins()
    {
        if (coinsText != null && player != null)
            coinsText.text = $"Coins: {player.coins}";
    }

    private IEnumerator CloseAfterPurchaseDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, purchasePanelDuration));
        purchaseRoutine = null;
        Close();
    }

    private void CacheOptionalReferences()
    {
        if (root == null)
        {
            Transform rootTransform = transform.Find("ShopContent");
            if (rootTransform != null)
                root = rootTransform.gameObject;
        }

        if (root == null)
            return;

        if (shopComponent == null)
        {
            Transform shopComponentTransform = root.transform.Find("ShopComponent");
            if (shopComponentTransform != null)
                shopComponent = shopComponentTransform.gameObject;
        }

        if (purchasePanel == null)
        {
            Transform purchasePanelTransform = root.transform.Find("Item Purchase Panel");
            if (purchasePanelTransform != null)
                purchasePanel = purchasePanelTransform.gameObject;
        }
    }

    private void RestorePanelsToDefaultState()
    {
        if (closeButton != null)
            closeButton.interactable = true;

        if (shopComponent != null)
            shopComponent.SetActive(true);

        if (purchasePanel != null)
            purchasePanel.SetActive(false);
    }

    private void SetPurchasePanelVisible(bool visible)
    {
        if (shopComponent != null)
            shopComponent.SetActive(!visible);

        if (purchasePanel != null)
            purchasePanel.SetActive(visible);
    }
}
