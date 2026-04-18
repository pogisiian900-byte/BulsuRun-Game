using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SinglePlayerSaveData
{
    public int coins;
    public int score;
    public string currentWeaponId;
    public int playerHealth;
    public int currentLevelIndex;
    public string currentLevelSelectScene;
    public int runStageIndex = 1;
    public string lastPlayedSceneName;
    public SlotSaveData[] inventorySlots;
    public List<string> skillIds = new List<string>();
    public List<LevelCollectibleSaveData> completedLevelCollectibles = new List<LevelCollectibleSaveData>();
    public int highestLevelUnlocked = 1;
    public int highestWorldUnlocked = 1;
}

public static class SinglePlayerSaveSystem
{
    private const string SaveFileName = "singleplayer_save.json";
    private static bool hasLoaded;
    private static SinglePlayerSaveData cachedData;
    private static SinglePlayerSaveData sessionCheckpointData;
    private static SinglePlayerSaveData persistentCachedDataBeforeMultiplayer;
    private static SinglePlayerSaveData persistentCheckpointDataBeforeMultiplayer;
    private static bool checkpointRestoreQueued;
    private static bool temporaryMultiplayerSessionActive;

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    public static bool IsCheckpointRestoreQueued => checkpointRestoreQueued;
    public static bool IsTemporaryMultiplayerSessionActive => temporaryMultiplayerSessionActive;

    public static bool HasSaveData()
    {
        if (!File.Exists(SavePath))
            return false;

        SinglePlayerSaveData saveData = hasLoaded ? cachedData : LoadFromDisk();
        return HasMeaningfulProgress(saveData);
    }

    public static void EnsureLoaded()
    {
        if (hasLoaded)
            return;

        cachedData = LoadFromDisk();
        sessionCheckpointData = CloneData(cachedData);
        ApplyToGameData(cachedData);
        hasLoaded = true;
    }

    public static void SaveNow(bool captureRuntimeState = true)
    {
        SaveCheckpoint(captureRuntimeState);
    }

    public static void SaveCheckpoint(bool captureRuntimeState = true)
    {
        EnsureLoaded();

        if (captureRuntimeState)
            CaptureRuntimeStateIntoGameData();

        SinglePlayerSaveData saveData = BuildSnapshot();
        sessionCheckpointData = CloneData(saveData);

        if (CanWriteToPersistentStorage())
            WriteToDisk(saveData);
        else
            cachedData = CloneData(saveData);
    }

    public static void BeginTemporaryMultiplayerSession()
    {
        EnsureLoaded();

        if (temporaryMultiplayerSessionActive)
            return;

        persistentCachedDataBeforeMultiplayer = CloneData(cachedData);
        persistentCheckpointDataBeforeMultiplayer = CloneData(
            sessionCheckpointData != null ? sessionCheckpointData : cachedData);

        temporaryMultiplayerSessionActive = true;
        checkpointRestoreQueued = false;
        LevelCollectibleProgress.ClearPendingProgress();
        ProgressStore.BeginTemporaryProgress();

        SinglePlayerSaveData temporaryData = CreateTemporarySessionData();
        cachedData = CloneData(temporaryData);
        sessionCheckpointData = CloneData(temporaryData);

        ApplyToGameData(temporaryData);
        ApplyGameDataToLiveObjects();
    }

    public static void EndTemporaryMultiplayerSession()
    {
        if (!temporaryMultiplayerSessionActive)
            return;

        SinglePlayerSaveData restoredCachedData = persistentCachedDataBeforeMultiplayer != null
            ? CloneData(persistentCachedDataBeforeMultiplayer)
            : LoadFromDisk();

        SinglePlayerSaveData restoredCheckpointData = persistentCheckpointDataBeforeMultiplayer != null
            ? CloneData(persistentCheckpointDataBeforeMultiplayer)
            : CloneData(restoredCachedData);

        temporaryMultiplayerSessionActive = false;
        checkpointRestoreQueued = false;
        LevelCollectibleProgress.ClearPendingProgress();
        ProgressStore.EndTemporaryProgress();

        cachedData = restoredCachedData;
        sessionCheckpointData = restoredCheckpointData;
        persistentCachedDataBeforeMultiplayer = null;
        persistentCheckpointDataBeforeMultiplayer = null;

        ApplyToGameData(restoredCheckpointData);
        ApplyGameDataToLiveObjects();
    }

    public static void PrepareFreshLevelStart()
    {
        checkpointRestoreQueued = false;
        LevelCollectibleProgress.ClearPendingProgress();

        PlayerHealth playerHealth = FindLocalPlayerHealth();
        if (playerHealth != null)
        {
            playerHealth.currentHP = playerHealth.MaxHP;
            GameData.PlayerHealth = playerHealth.currentHP;
            return;
        }

        // Keep the sentinel fallback so PlayerHealth can still spawn full
        // when there is no persistent player in the current scene.
        GameData.PlayerHealth = 0;
    }

    public static void ResetProgress()
    {
        EnsureLoaded();
        checkpointRestoreQueued = false;
        LevelCollectibleProgress.ResetAll();

        GameData.ResetAll();

        Inventory inventory = Inventory.Instance != null ? Inventory.Instance : UnityEngine.Object.FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (inventory != null)
            inventory.ClearAll();

        PlayerSkillHolder playerSkillHolder = PlayerSkillHolder.Instance != null ? PlayerSkillHolder.Instance : UnityEngine.Object.FindFirstObjectByType<PlayerSkillHolder>(FindObjectsInactive.Include);
        if (playerSkillHolder != null)
            playerSkillHolder.ClearSkills(false);

        PlayerInventory playerInventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>(FindObjectsInactive.Include);
        if (playerInventory != null)
            playerInventory.SetCoins(0, false);

        RunManager runManager = RunManager.Instance != null ? RunManager.Instance : UnityEngine.Object.FindFirstObjectByType<RunManager>(FindObjectsInactive.Include);
        if (runManager != null)
            runManager.ResetToDefault(false);

        if (temporaryMultiplayerSessionActive)
        {
            ProgressStore.ResetTemporaryProgress();
            cachedData = CreateTemporarySessionData();
            sessionCheckpointData = CloneData(cachedData);
            ApplyToGameData(cachedData);
            ApplyGameDataToLiveObjects();
            return;
        }

        cachedData = CreateDefaultData();
        sessionCheckpointData = CloneData(cachedData);

        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"SinglePlayerSaveSystem: Failed to delete save file.\n{exception}");
        }
    }

    public static void CaptureRuntimeStateIntoGameData()
    {
        EnsureLoaded();

        PlayerInventory playerInventory = FindOwnedPlayerComponent<PlayerInventory>();
        if (playerInventory != null)
            GameData.Coins = playerInventory.coins;

        Inventory inventory = Inventory.Instance != null ? Inventory.Instance : UnityEngine.Object.FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (inventory != null)
        {
            GameData.CurrentWeaponId = inventory.equippedWeapon != null ? inventory.equippedWeapon.id : string.Empty;
            GameData.InventorySlots = inventory.ToSaveData();
        }

        PlayerHealth playerHealth = FindOwnedPlayerComponent<PlayerHealth>();
        if (playerHealth != null)
            GameData.PlayerHealth = playerHealth.currentHP;

        PlayerSkillHolder playerSkillHolder = PlayerSkillHolder.Instance != null ? PlayerSkillHolder.Instance : UnityEngine.Object.FindFirstObjectByType<PlayerSkillHolder>(FindObjectsInactive.Include);
        if (playerSkillHolder != null)
        {
            GameData.SkillIds.Clear();
            for (int i = 0; i < playerSkillHolder.Owned.Count; i++)
            {
                SkillCard skill = playerSkillHolder.Owned[i];
                if (skill != null && !string.IsNullOrEmpty(skill.id))
                    GameData.SkillIds.Add(skill.id);
            }
        }

        RunManager runManager = RunManager.Instance != null ? RunManager.Instance : UnityEngine.Object.FindFirstObjectByType<RunManager>(FindObjectsInactive.Include);
        if (runManager != null)
            GameData.RunStageIndex = runManager.StageIndex;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            GameData.LastPlayedSceneName = activeScene.name;
    }

    public static void RefreshRuntimeState()
    {
        CaptureRuntimeStateIntoGameData();
    }

    public static void QueueCheckpointRestore()
    {
        EnsureLoaded();
        checkpointRestoreQueued = true;
    }

    public static void ClearQueuedCheckpointRestore()
    {
        checkpointRestoreQueued = false;
    }

    public static void CommitCompletedLevelCollectibles()
    {
        EnsureLoaded();
        LevelCollectibleProgress.CommitCurrentLevel();
    }

    public static void RestoreCheckpoint()
    {
        EnsureLoaded();
        checkpointRestoreQueued = false;
        LevelCollectibleProgress.ClearPendingProgress();

        SinglePlayerSaveData restoreData = sessionCheckpointData != null
            ? CloneData(sessionCheckpointData)
            : CloneData(cachedData);

        ApplyToGameData(restoreData);
        ApplyGameDataToLiveObjects();
    }

    public static void HandleSceneLoaded()
    {
        EnsureLoaded();

        if (checkpointRestoreQueued)
        {
            RestoreCheckpoint();
            Scene activeRestoreScene = SceneManager.GetActiveScene();
            if (activeRestoreScene.IsValid())
                GameData.LastPlayedSceneName = activeRestoreScene.name;

            SaveCheckpoint(false);
            return;
        }

        ApplyGameDataToLiveObjects();
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            GameData.LastPlayedSceneName = activeScene.name;

        SaveCheckpoint(false);
    }

    public static void FlushCheckpointToDisk()
    {
        EnsureLoaded();

        if (!CanWriteToPersistentStorage())
            return;

        SinglePlayerSaveData saveData = sessionCheckpointData != null
            ? CloneData(sessionCheckpointData)
            : CloneData(cachedData);

        WriteToDisk(saveData);
    }

    private static PlayerHealth FindLocalPlayerHealth()
    {
        return FindOwnedPlayerComponent<PlayerHealth>();
    }

    private static SinglePlayerSaveData LoadFromDisk()
    {
        SinglePlayerSaveData fallback = CreatePersistentDefaultData();

        if (!File.Exists(SavePath))
            return fallback;

        try
        {
            string json = File.ReadAllText(SavePath);
            SinglePlayerSaveData loaded = JsonUtility.FromJson<SinglePlayerSaveData>(json);
            if (loaded == null)
                return fallback;

            loaded.skillIds ??= new List<string>();
            loaded.completedLevelCollectibles ??= new List<LevelCollectibleSaveData>();
            loaded.currentWeaponId ??= string.Empty;
            loaded.currentLevelSelectScene ??= string.Empty;
            loaded.lastPlayedSceneName ??= string.Empty;
            loaded.highestLevelUnlocked = Mathf.Max(1, loaded.highestLevelUnlocked);
            loaded.highestWorldUnlocked = Mathf.Max(1, loaded.highestWorldUnlocked);
            loaded.runStageIndex = Mathf.Max(1, loaded.runStageIndex);

            loaded.highestLevelUnlocked = Mathf.Max(loaded.highestLevelUnlocked, ProgressStore.GetPersistentHighestLevel());
            loaded.highestWorldUnlocked = Mathf.Max(loaded.highestWorldUnlocked, ProgressStore.GetPersistentHighestWorld());
            return loaded;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"SinglePlayerSaveSystem: Failed to read save file, using defaults.\n{exception}");
            return fallback;
        }
    }

    private static SinglePlayerSaveData BuildSnapshot()
    {
        return new SinglePlayerSaveData
        {
            coins = GameData.Coins,
            score = GameData.Score,
            currentWeaponId = GameData.CurrentWeaponId ?? string.Empty,
            playerHealth = GameData.PlayerHealth,
            currentLevelIndex = GameData.CurrentLevelIndex,
            currentLevelSelectScene = GameData.CurrentLevelSelectScene ?? string.Empty,
            runStageIndex = Mathf.Max(1, GameData.RunStageIndex),
            lastPlayedSceneName = GameData.LastPlayedSceneName ?? string.Empty,
            inventorySlots = CloneInventorySlots(GameData.InventorySlots),
            skillIds = new List<string>(GameData.SkillIds),
            completedLevelCollectibles = CloneLevelCollectibles(LevelCollectibleProgress.BuildSaveData()),
            highestLevelUnlocked = ProgressStore.GetHighestLevel(),
            highestWorldUnlocked = ProgressStore.GetHighestWorld()
        };
    }

    private static SinglePlayerSaveData CreateDefaultData()
    {
        return CreateDefaultData(ProgressStore.GetHighestLevel(), ProgressStore.GetHighestWorld());
    }

    private static SinglePlayerSaveData CreatePersistentDefaultData()
    {
        return CreateDefaultData(ProgressStore.GetPersistentHighestLevel(), ProgressStore.GetPersistentHighestWorld());
    }

    private static SinglePlayerSaveData CreateTemporarySessionData()
    {
        return CreateDefaultData(1, 1);
    }

    private static SinglePlayerSaveData CreateDefaultData(int highestLevelUnlocked, int highestWorldUnlocked)
    {
        return new SinglePlayerSaveData
        {
            currentWeaponId = string.Empty,
            currentLevelSelectScene = string.Empty,
            lastPlayedSceneName = string.Empty,
            runStageIndex = 1,
            completedLevelCollectibles = new List<LevelCollectibleSaveData>(),
            highestLevelUnlocked = Mathf.Max(1, highestLevelUnlocked),
            highestWorldUnlocked = Mathf.Max(1, highestWorldUnlocked)
        };
    }

    private static void ApplyToGameData(SinglePlayerSaveData saveData)
    {
        if (saveData == null)
            saveData = CreateDefaultData();

        GameData.Coins = saveData.coins;
        GameData.Score = saveData.score;
        GameData.CurrentWeaponId = saveData.currentWeaponId ?? string.Empty;
        GameData.PlayerHealth = saveData.playerHealth;
        GameData.CurrentLevelIndex = saveData.currentLevelIndex;
        GameData.CurrentLevelSelectScene = saveData.currentLevelSelectScene ?? string.Empty;
        GameData.RunStageIndex = Mathf.Max(1, saveData.runStageIndex);
        GameData.LastPlayedSceneName = saveData.lastPlayedSceneName ?? string.Empty;
        GameData.InventorySlots = CloneInventorySlots(saveData.inventorySlots);
        LevelCollectibleProgress.LoadFromSaveData(saveData.completedLevelCollectibles);

        GameData.SkillIds.Clear();
        if (saveData.skillIds != null)
            GameData.SkillIds.AddRange(saveData.skillIds);

        ProgressStore.SetHighestLevel(Mathf.Max(1, saveData.highestLevelUnlocked));
        ProgressStore.SetHighestWorld(Mathf.Max(1, saveData.highestWorldUnlocked));
        ProgressStore.Save();
    }

    private static bool HasMeaningfulProgress(SinglePlayerSaveData saveData)
    {
        if (saveData == null)
            return false;

        if (saveData.highestLevelUnlocked > 1 || saveData.highestWorldUnlocked > 1)
            return true;

        if (saveData.coins > 0 || saveData.score > 0 || saveData.playerHealth > 0 || saveData.currentLevelIndex > 0)
            return true;

        if (!string.IsNullOrWhiteSpace(saveData.currentWeaponId))
            return true;

        if (saveData.skillIds != null && saveData.skillIds.Count > 0)
            return true;

        if (saveData.completedLevelCollectibles != null && saveData.completedLevelCollectibles.Count > 0)
            return true;

        if (saveData.inventorySlots == null)
            return false;

        for (int i = 0; i < saveData.inventorySlots.Length; i++)
        {
            SlotSaveData slot = saveData.inventorySlots[i];
            if (!string.IsNullOrWhiteSpace(slot.weaponId) || !string.IsNullOrWhiteSpace(slot.itemId) || slot.amount > 0)
                return true;
        }

        return false;
    }

    private static void WriteToDisk(SinglePlayerSaveData saveData)
    {
        try
        {
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(SavePath, JsonUtility.ToJson(saveData, true));
            cachedData = CloneData(saveData);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"SinglePlayerSaveSystem: Failed to write save file.\n{exception}");
        }
    }

    private static bool CanWriteToPersistentStorage()
    {
        return !temporaryMultiplayerSessionActive && !PhotonNetwork.InRoom;
    }

    private static void ApplyGameDataToLiveObjects()
    {
        PlayerInventory playerInventory = FindOwnedPlayerComponent<PlayerInventory>();
        if (playerInventory != null)
            playerInventory.SetCoins(GameData.Coins, false);

        Inventory inventory = Inventory.Instance != null ? Inventory.Instance : UnityEngine.Object.FindFirstObjectByType<Inventory>(FindObjectsInactive.Include);
        if (inventory != null && ItemDatabase.Instance != null)
        {
            inventory.equippedWeapon = ItemDatabase.Instance.GetWeapon(GameData.CurrentWeaponId);
            inventory.LoadFromSaveData(GameData.InventorySlots);
            inventory.onEquippedWeaponChanged?.Invoke(inventory.equippedWeapon);
        }

        RunManager runManager = RunManager.Instance != null ? RunManager.Instance : UnityEngine.Object.FindFirstObjectByType<RunManager>(FindObjectsInactive.Include);
        if (runManager != null)
            runManager.LoadFromGameData();

        PlayerSkillHolder playerSkillHolder = PlayerSkillHolder.Instance != null ? PlayerSkillHolder.Instance : UnityEngine.Object.FindFirstObjectByType<PlayerSkillHolder>(FindObjectsInactive.Include);
        if (playerSkillHolder != null)
            playerSkillHolder.LoadFromGameData();

        PlayerHealth playerHealth = FindOwnedPlayerComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.LoadFromGameData();
    }

    private static T FindOwnedPlayerComponent<T>() where T : Component
    {
        if (!PhotonNetwork.InRoom)
            return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        T[] components = UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (T component in components)
        {
            if (component == null || !component.gameObject.scene.IsValid())
                continue;

            PhotonView view = component.GetComponent<PhotonView>();
            if (view == null)
                view = component.GetComponentInParent<PhotonView>();

            if (view != null && view.IsMine)
                return component;
        }

        return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    }

    private static SlotSaveData[] CloneInventorySlots(SlotSaveData[] source)
    {
        if (source == null)
            return null;

        SlotSaveData[] clone = new SlotSaveData[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }

    private static SinglePlayerSaveData CloneData(SinglePlayerSaveData source)
    {
        if (source == null)
            return CreateDefaultData();

        return new SinglePlayerSaveData
        {
            coins = source.coins,
            score = source.score,
            currentWeaponId = source.currentWeaponId ?? string.Empty,
            playerHealth = source.playerHealth,
            currentLevelIndex = source.currentLevelIndex,
            currentLevelSelectScene = source.currentLevelSelectScene ?? string.Empty,
            runStageIndex = Mathf.Max(1, source.runStageIndex),
            lastPlayedSceneName = source.lastPlayedSceneName ?? string.Empty,
            inventorySlots = CloneInventorySlots(source.inventorySlots),
            skillIds = source.skillIds != null ? new List<string>(source.skillIds) : new List<string>(),
            completedLevelCollectibles = CloneLevelCollectibles(source.completedLevelCollectibles),
            highestLevelUnlocked = Mathf.Max(1, source.highestLevelUnlocked),
            highestWorldUnlocked = Mathf.Max(1, source.highestWorldUnlocked)
        };
    }

    private static List<LevelCollectibleSaveData> CloneLevelCollectibles(List<LevelCollectibleSaveData> source)
    {
        List<LevelCollectibleSaveData> clone = new List<LevelCollectibleSaveData>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            LevelCollectibleSaveData entry = source[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.levelKey))
                continue;

            clone.Add(new LevelCollectibleSaveData
            {
                levelKey = entry.levelKey,
                pickupIds = entry.pickupIds != null ? new List<string>(entry.pickupIds) : new List<string>()
            });
        }

        return clone;
    }
}

public class SinglePlayerSaveDriver : MonoBehaviour
{
    private const float AutoSaveIntervalSeconds = 5f;
    private static SinglePlayerSaveDriver instance;
    private float autoSaveTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        SinglePlayerSaveSystem.EnsureLoaded();

        if (instance != null)
            return;

        GameObject driverObject = new GameObject(nameof(SinglePlayerSaveDriver));
        instance = driverObject.AddComponent<SinglePlayerSaveDriver>();
        DontDestroyOnLoad(driverObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting += OnApplicationQuitting;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= OnApplicationQuitting;
    }

    private void Update()
    {
        autoSaveTimer += Time.unscaledDeltaTime;
        if (autoSaveTimer < AutoSaveIntervalSeconds)
            return;

        autoSaveTimer = 0f;
        SinglePlayerSaveSystem.CaptureRuntimeStateIntoGameData();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SinglePlayerSaveSystem.FlushCheckpointToDisk();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SinglePlayerSaveSystem.FlushCheckpointToDisk();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        autoSaveTimer = 0f;
        SinglePlayerSaveSystem.HandleSceneLoaded();
    }

    private static void OnApplicationQuitting()
    {
        SinglePlayerSaveSystem.FlushCheckpointToDisk();
    }
}
