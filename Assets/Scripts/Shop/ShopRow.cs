using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ShopRow : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    private ShopItemData item;

    public void Set(ShopItemData item, Action<ShopItemData> onBuy)
    {
        this.item = item;

        icon.sprite = item.icon;
        nameText.text = item.itemName;
        priceText.text = item.buyPrice.ToString();

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => onBuy?.Invoke(this.item));
    }
}