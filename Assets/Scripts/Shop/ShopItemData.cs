using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Item")]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int buyPrice;
    public int sellPrice;

    // Hook your real reward here:
    public WeaponData weapon;
     public SkillCard skill;
     public int amount = 1;

}
