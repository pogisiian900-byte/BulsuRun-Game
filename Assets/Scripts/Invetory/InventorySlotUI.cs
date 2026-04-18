using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("Index (set in Inspector 0..7)")]
    public int slotIndex;

    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button button;

    private Inventory inventory;
    private InventorySlot slot;

    public void Init(Inventory inv, int idx)
    {
        inventory = inv;
        slotIndex = idx;

        if (button == null)
            button = GetComponent<Button>();

        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickSlot);
    }

    public void UpdateSlot(InventorySlot s)
    {
        slot = s;

        if (button != null)
            button.interactable = true;

        if (slot == null || slot.IsEmpty())
        {
            if (icon != null)
                icon.enabled = false;

            if (amountText != null)
                amountText.text = "";

            return;
        }

        if (icon != null)
        {
            icon.enabled = true;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
        }

        if (slot.HasItem())
        {
            icon.sprite = slot.item.icon;

            if (amountText != null)
            {
                amountText.text = (slot.item.isStackable && slot.amount > 1)
                    ? slot.amount.ToString()
                    : "";
            }

            return;
        }

        if (slot.HasWeapon())
        {
            icon.sprite = slot.weapon.weaponSprite;
            if (amountText != null)
                amountText.text = "";
        }
    }

    private void OnClickSlot()
    {
        if (inventory == null)
            return;

        inventory.HandleSlotClick(slotIndex);
    }
}
