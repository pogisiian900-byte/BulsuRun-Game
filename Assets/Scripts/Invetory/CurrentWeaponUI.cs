using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentWeaponUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerWeapon playerWeapon;

    [Header("UI")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TMP_Text weaponName;
    [SerializeField] private TMP_Text weaponDamage;

    private void OnEnable()
    {
        if (inventory == null)
            inventory = FindObjectOfType<Inventory>();

        if (inventory == null)
        {
            Debug.LogError("CurrentWeaponUI: Inventory not found in scene.");
            return;
        }

        inventory.onEquippedWeaponChanged += OnWeaponChanged;
        OnWeaponChanged(inventory.equippedWeapon);
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.onEquippedWeaponChanged -= OnWeaponChanged;
    }

    private void OnWeaponChanged(WeaponData weapon)
    {
        if (weaponIcon == null || weaponName == null|| weaponDamage == null)
            return;

        if (weapon == null || weapon.weaponSprite == null)
        {
            weaponIcon.enabled = false;
            weaponName.text ="";
            weaponDamage.text = "";
            return;
        }

        weaponIcon.enabled = true;
        weaponIcon.type = Image.Type.Simple;
        weaponIcon.preserveAspect = true;
        weaponIcon.sprite = weapon.weaponSprite;
        weaponName.text = weapon.weaponName;
        weaponDamage.text = weapon.weaponType == WeaponType.Healing
            ? (weapon.healToFull ? "HEAL: FULL" : "HEAL: " + Mathf.Max(0, weapon.healAmount))
            : "DMG: " + weapon.damage;
    }
}
