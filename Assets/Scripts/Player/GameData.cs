using System.Collections.Generic;

[System.Serializable]
public struct SlotSaveData
{
    public string weaponId; // "" if none
    public string itemId;   // "" if none
    public int amount;
}

public static class GameData
{
    public static int Coins;
    public static int Score;

    public static string CurrentWeaponId;   // change from string name to ID
    public static int PlayerHealth;

    public static int CurrentLevelIndex;
    public static string CurrentLevelSelectScene;
    public static int RunStageIndex = 1;
    public static string LastPlayedSceneName;

    public static SlotSaveData[] InventorySlots; // <-- saved inventory
    public static List<string> SkillIds = new List<string>();

    public static void ResetAll()
    {
        Coins = 0;
        Score = 0;
        CurrentWeaponId = string.Empty;
        PlayerHealth = 0;
        CurrentLevelIndex = 0;
        CurrentLevelSelectScene = string.Empty;
        RunStageIndex = 1;
        LastPlayedSceneName = string.Empty;
        InventorySlots = null;
        SkillIds.Clear();
    }
}
