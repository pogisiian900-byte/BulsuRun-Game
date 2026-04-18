using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    public WeaponData[] allWeapons;
    public Item[] allItems;

    private readonly Dictionary<string, WeaponData> weaponMap = new();
    private readonly Dictionary<string, Item> itemMap = new();
    private readonly HashSet<string> missingWeaponIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> missingItemIds = new(StringComparer.Ordinal);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        RebuildMaps();
    }

    private void RebuildMaps()
    {
        weaponMap.Clear();
        itemMap.Clear();
        missingWeaponIds.Clear();
        missingItemIds.Clear();

        RegisterWeapons(allWeapons);
        RegisterItems(allItems);

        // Also cache any weapon/item assets that are already loaded by scene or prefab references.
        RegisterWeapons(Resources.FindObjectsOfTypeAll<WeaponData>());
        RegisterItems(Resources.FindObjectsOfTypeAll<Item>());
    }

    private void RegisterWeapons(IEnumerable<WeaponData> weapons)
    {
        if (weapons == null)
            return;

        foreach (WeaponData weapon in weapons)
        {
            if (weapon == null || string.IsNullOrWhiteSpace(weapon.id))
                continue;

            weaponMap[weapon.id] = weapon;
        }
    }

    private void RegisterItems(IEnumerable<Item> items)
    {
        if (items == null)
            return;

        foreach (Item item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id))
                continue;

            itemMap[item.id] = item;
        }
    }

    public WeaponData GetWeapon(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (weaponMap.TryGetValue(id, out WeaponData weapon) && weapon != null)
            return weapon;

        weapon = FindLoadedWeapon(id);
        if (weapon != null)
        {
            weaponMap[id] = weapon;
            return weapon;
        }

        if (missingWeaponIds.Add(id))
            Debug.LogWarning($"ItemDatabase: Weapon id '{id}' is missing from the registry, so it cannot be restored.");

        return null;
    }

    public Item GetItem(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (itemMap.TryGetValue(id, out Item item) && item != null)
            return item;

        item = FindLoadedItem(id);
        if (item != null)
        {
            itemMap[id] = item;
            return item;
        }

        if (missingItemIds.Add(id))
            Debug.LogWarning($"ItemDatabase: Item id '{id}' is missing from the registry, so it cannot be restored.");

        return null;
    }

    private static WeaponData FindLoadedWeapon(string id)
    {
        WeaponData[] loadedWeapons = Resources.FindObjectsOfTypeAll<WeaponData>();
        foreach (WeaponData weapon in loadedWeapons)
        {
            if (weapon != null && string.Equals(weapon.id, id, StringComparison.Ordinal))
                return weapon;
        }

        return null;
    }

    private static Item FindLoadedItem(string id)
    {
        Item[] loadedItems = Resources.FindObjectsOfTypeAll<Item>();
        foreach (Item item in loadedItems)
        {
            if (item != null && string.Equals(item.id, id, StringComparison.Ordinal))
                return item;
        }

        return null;
    }
}
