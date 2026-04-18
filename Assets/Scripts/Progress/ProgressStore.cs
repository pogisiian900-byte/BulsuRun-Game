using UnityEngine;

public static class ProgressStore
{
    private const string HighestLevelKey = "HighestLevel";
    private const string HighestWorldKey = "HighestWorld";
    private const string JustUnlockedLevelKey = "JustUnlockedLevel";
    private const string JustUnlockedWorldKey = "JustUnlockedWorld";

    private static bool useTemporaryProgress;
    private static int temporaryHighestLevel = 1;
    private static int temporaryHighestWorld = 1;
    private static int temporaryJustUnlockedLevel = -1;
    private static int temporaryJustUnlockedWorld = -1;

    public static bool IsUsingTemporaryProgress => useTemporaryProgress;

    public static void BeginTemporaryProgress()
    {
        useTemporaryProgress = true;
        ResetTemporaryProgress();
    }

    public static void EndTemporaryProgress()
    {
        useTemporaryProgress = false;
        ResetTemporaryProgress();
        SyncGameProgressInstance();
    }

    public static void ResetTemporaryProgress()
    {
        temporaryHighestLevel = 1;
        temporaryHighestWorld = 1;
        temporaryJustUnlockedLevel = -1;
        temporaryJustUnlockedWorld = -1;
        SyncGameProgressInstance();
    }

    public static int GetHighestLevel()
    {
        return useTemporaryProgress
            ? temporaryHighestLevel
            : GetPersistentHighestLevel();
    }

    public static int GetHighestWorld()
    {
        return useTemporaryProgress
            ? temporaryHighestWorld
            : GetPersistentHighestWorld();
    }

    public static int GetPersistentHighestLevel()
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(HighestLevelKey, 1));
    }

    public static int GetPersistentHighestWorld()
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(HighestWorldKey, 1));
    }

    public static bool HasHighestWorld()
    {
        return useTemporaryProgress || PlayerPrefs.HasKey(HighestWorldKey);
    }

    public static bool HasPersistentHighestWorld()
    {
        return PlayerPrefs.HasKey(HighestWorldKey);
    }

    public static int GetJustUnlockedLevel()
    {
        return useTemporaryProgress
            ? temporaryJustUnlockedLevel
            : PlayerPrefs.GetInt(JustUnlockedLevelKey, -1);
    }

    public static int GetJustUnlockedWorld()
    {
        return useTemporaryProgress
            ? temporaryJustUnlockedWorld
            : PlayerPrefs.GetInt(JustUnlockedWorldKey, -1);
    }

    public static void SetHighestLevel(int value)
    {
        int sanitizedValue = Mathf.Max(1, value);

        if (useTemporaryProgress)
        {
            temporaryHighestLevel = sanitizedValue;
        }
        else
        {
            PlayerPrefs.SetInt(HighestLevelKey, sanitizedValue);
        }

        SyncGameProgressInstance();
    }

    public static void SetHighestWorld(int value)
    {
        int sanitizedValue = Mathf.Max(1, value);

        if (useTemporaryProgress)
        {
            temporaryHighestWorld = sanitizedValue;
        }
        else
        {
            PlayerPrefs.SetInt(HighestWorldKey, sanitizedValue);
        }

        SyncGameProgressInstance();
    }

    public static void SetJustUnlockedLevel(int value)
    {
        if (useTemporaryProgress)
        {
            temporaryJustUnlockedLevel = value;
            return;
        }

        PlayerPrefs.SetInt(JustUnlockedLevelKey, value);
    }

    public static void SetJustUnlockedWorld(int value)
    {
        if (useTemporaryProgress)
        {
            temporaryJustUnlockedWorld = value;
            return;
        }

        PlayerPrefs.SetInt(JustUnlockedWorldKey, value);
    }

    public static void DeleteJustUnlockedLevel()
    {
        if (useTemporaryProgress)
        {
            temporaryJustUnlockedLevel = -1;
            return;
        }

        PlayerPrefs.DeleteKey(JustUnlockedLevelKey);
    }

    public static void DeleteJustUnlockedWorld()
    {
        if (useTemporaryProgress)
        {
            temporaryJustUnlockedWorld = -1;
            return;
        }

        PlayerPrefs.DeleteKey(JustUnlockedWorldKey);
    }

    public static void Save()
    {
        if (!useTemporaryProgress)
            PlayerPrefs.Save();

        SyncGameProgressInstance();
    }

    private static void SyncGameProgressInstance()
    {
        if (GameProgress.Instance == null)
            return;

        GameProgress.Instance.highestLevelUnlocked = GetHighestLevel();
        GameProgress.Instance.highestWorldUnlocked = GetHighestWorld();
    }
}
