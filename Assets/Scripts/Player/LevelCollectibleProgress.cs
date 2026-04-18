using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class LevelCollectibleSaveData
{
    public string levelKey;
    public List<string> pickupIds = new List<string>();
}

public static class LevelCollectibleProgress
{
    private static readonly Dictionary<string, HashSet<string>> committedPickupIdsByLevel = new Dictionary<string, HashSet<string>>();
    private static readonly Dictionary<string, HashSet<string>> pendingPickupIdsByLevel = new Dictionary<string, HashSet<string>>();

    public static void LoadFromSaveData(List<LevelCollectibleSaveData> saveData)
    {
        committedPickupIdsByLevel.Clear();
        pendingPickupIdsByLevel.Clear();

        if (saveData == null)
            return;

        foreach (LevelCollectibleSaveData entry in saveData)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.levelKey))
                continue;

            HashSet<string> pickupIds = GetOrCreateSet(committedPickupIdsByLevel, entry.levelKey);
            if (entry.pickupIds == null)
                continue;

            for (int i = 0; i < entry.pickupIds.Count; i++)
            {
                string pickupId = entry.pickupIds[i];
                if (!string.IsNullOrWhiteSpace(pickupId))
                    pickupIds.Add(pickupId);
            }
        }
    }

    public static List<LevelCollectibleSaveData> BuildSaveData()
    {
        List<LevelCollectibleSaveData> data = new List<LevelCollectibleSaveData>();

        foreach (KeyValuePair<string, HashSet<string>> pair in committedPickupIdsByLevel)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null || pair.Value.Count == 0)
                continue;

            List<string> pickupIds = new List<string>(pair.Value);
            pickupIds.Sort(StringComparer.Ordinal);

            data.Add(new LevelCollectibleSaveData
            {
                levelKey = pair.Key,
                pickupIds = pickupIds
            });
        }

        data.Sort((a, b) => string.CompareOrdinal(a.levelKey, b.levelKey));
        return data;
    }

    public static void RegisterCollected(string pickupId)
    {
        if (string.IsNullOrWhiteSpace(pickupId))
            return;

        if (!TryGetCurrentLevelKey(out string levelKey))
            return;

        GetOrCreateSet(pendingPickupIdsByLevel, levelKey).Add(pickupId);
    }

    public static bool IsCollectedInCompletedLevel(string pickupId)
    {
        return IsCollectedInCompletedLevel(null, pickupId);
    }

    public static bool IsCollectedInCompletedLevel(Component component, string pickupId)
    {
        if (string.IsNullOrWhiteSpace(pickupId))
            return false;

        Scene scene = component != null ? component.gameObject.scene : SceneManager.GetActiveScene();
        if (!TryGetLevelKeyForScene(scene, out string levelKey))
            return false;

        return committedPickupIdsByLevel.TryGetValue(levelKey, out HashSet<string> pickupIds) &&
               pickupIds.Contains(pickupId);
    }

    public static void CommitCurrentLevel()
    {
        if (!TryGetCurrentLevelKey(out string levelKey))
            return;

        if (!pendingPickupIdsByLevel.TryGetValue(levelKey, out HashSet<string> pendingPickupIds) ||
            pendingPickupIds == null ||
            pendingPickupIds.Count == 0)
        {
            return;
        }

        HashSet<string> committedPickupIds = GetOrCreateSet(committedPickupIdsByLevel, levelKey);
        foreach (string pickupId in pendingPickupIds)
            committedPickupIds.Add(pickupId);

        pendingPickupIdsByLevel.Remove(levelKey);
    }

    public static void ClearPendingProgress()
    {
        pendingPickupIdsByLevel.Clear();
    }

    public static void ResetAll()
    {
        committedPickupIdsByLevel.Clear();
        pendingPickupIdsByLevel.Clear();
    }

    public static string BuildPickupId(Component component)
    {
        if (component == null)
            return string.Empty;

        Scene scene = component.gameObject.scene;
        if (!scene.IsValid())
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        builder.Append(component.GetType().Name);
        builder.Append('|');
        builder.Append(scene.name);
        builder.Append('|');
        AppendHierarchyPath(builder, component.transform, scene);

        Vector3 position = component.transform.position;
        builder.Append('|');
        builder.Append(position.x.ToString("0.###", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(position.y.ToString("0.###", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(position.z.ToString("0.###", CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    public static bool TryGetCurrentLevelKey(out string levelKey)
    {
        return TryGetLevelKeyForScene(SceneManager.GetActiveScene(), out levelKey);
    }

    private static bool TryGetLevelKeyForScene(Scene scene, out string levelKey)
    {
        if (scene.IsValid() &&
            LevelManager.TryGetLevelIndexForSceneName(scene.name, out int levelIndex))
        {
            levelKey = "Level_" + levelIndex;
            return true;
        }

        if (GameData.CurrentLevelIndex > 0)
        {
            levelKey = "Level_" + GameData.CurrentLevelIndex;
            return true;
        }

        if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.name))
        {
            levelKey = string.Empty;
            return false;
        }

        levelKey = "Scene_" + scene.name;
        return true;
    }

    private static HashSet<string> GetOrCreateSet(Dictionary<string, HashSet<string>> map, string key)
    {
        if (!map.TryGetValue(key, out HashSet<string> values) || values == null)
        {
            values = new HashSet<string>(StringComparer.Ordinal);
            map[key] = values;
        }

        return values;
    }

    private static void AppendHierarchyPath(StringBuilder builder, Transform transform, Scene scene)
    {
        List<string> segments = new List<string>();
        Transform current = transform;

        while (current != null && current.gameObject.scene == scene)
        {
            segments.Add(current.name + "[" + current.GetSiblingIndex() + "]");
            current = current.parent;
        }

        for (int i = segments.Count - 1; i >= 0; i--)
        {
            builder.Append(segments[i]);

            if (i > 0)
                builder.Append('/');
        }
    }
}
