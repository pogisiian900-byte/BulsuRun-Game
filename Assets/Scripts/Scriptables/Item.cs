using UnityEngine;


[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;

    public bool isStackable = true;
}