using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataManager : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private PlayerHealth playerHealth; // <-- use PlayerHealth

    

    [SerializeField] private PlayerSkillHolder playerSkills;
    [SerializeField] private List<SkillCard> allSkillCards; // drag all cards here


    void Start()
    {
        LoadAll();
    }


    public void SaveAll()
    {
        // // HP
        // if (playerHealth != null)
        //     GameData.PlayerHealth = playerHealth.currentHP;

        // equipped weapon
        GameData.CurrentWeaponId = (inventory != null && inventory.equippedWeapon != null)
            ? inventory.equippedWeapon.id
            : "";

        // inventory slots
        if (inventory != null)
            GameData.InventorySlots = inventory.ToSaveData();

                        // SKILLS
            if (playerSkills != null)
            {
                GameData.SkillIds.Clear();
                foreach (var s in playerSkills.Owned)
                    GameData.SkillIds.Add(s.id);
            }

        SinglePlayerSaveSystem.SaveCheckpoint(false);

    }

    public void LoadAll()
    {
        // // HP
        // if (playerHealth != null)
        // {
        //     // if no saved hp yet, start full
        //     int hpToUse = (GameData.PlayerHealth > 0) ? GameData.PlayerHealth : playerHealth.MaxHP;
        //     playerHealth.currentHP = Mathf.Clamp(hpToUse, 0, playerHealth.MaxHP);

        //     // update UI + keep GameData synced
        //     GameData.PlayerHealth = playerHealth.currentHP;
        //     // call a public refresh if you have one, otherwise PlayerHealth Start() already updates.
        //     // If your PlayerHealth has UpdateHealthBar() private, just let Start handle UI.
        // }

        // equipped weapon
        if (inventory != null && ItemDatabase.Instance != null)
        {
            inventory.equippedWeapon = ItemDatabase.Instance.GetWeapon(GameData.CurrentWeaponId);
            inventory.onEquippedWeaponChanged?.Invoke(inventory.equippedWeapon);

            // inventory slots
            inventory.LoadFromSaveData(GameData.InventorySlots);
        }
        if (playerSkills != null)
            playerSkills.LoadFromGameData();

        FindObjectOfType<InventorySkillsUI>()?.Refresh();


    }

    public void LoadNextLevel(string sceneName)
    {
        SaveAll();
        SinglePlayerSaveSystem.SaveCheckpoint(false);
        SceneManager.LoadScene(sceneName);
    }
}
