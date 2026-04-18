[System.Serializable]
public class InventorySlot
{
    public Item item;
    public WeaponData weapon;
    public int amount;

    public bool IsEmpty()
    {
        return item == null && weapon == null;
    }

    public bool HasItem()
    {
        return item != null;
    }

    public bool HasWeapon()
    {
        return weapon != null;
    }

    public void AddItem(Item newItem, int value)
    {
        Clear();
        item = newItem;
        amount = value;
    }

    public void AddWeapon(WeaponData newWeapon)
    {
        Clear();
        weapon = newWeapon;
        amount = 1;
    }

    public void Clear()
    {
        item = null;
        weapon = null;
        amount = 0;
    }
}
