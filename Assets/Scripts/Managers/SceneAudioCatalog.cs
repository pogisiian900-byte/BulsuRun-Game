using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Audio/Scene Audio Catalog", fileName = "SceneAudioCatalog")]
public class SceneAudioCatalog : ScriptableObject
{
    [Header("Fallback")]
    [SerializeField] private AudioClip fallbackClip;
    [SerializeField, Range(0f, 1f)] private float fallbackVolume = 1f;
    [SerializeField] private bool fallbackLoop = true;
    [SerializeField] private bool stopWhenSceneHasNoEntry = true;

    [Header("Shared SFX")]
    [SerializeField] private AudioClip coinPickupClip;
    [SerializeField] private AudioClip stunClip;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private AudioClip turretShotClip;
    [SerializeField] private AudioClip stunTurretShotClip;

    [Header("Per Scene Music")]
    [SerializeField] private List<SceneAudioEntry> scenes = new List<SceneAudioEntry>();

    public bool StopWhenSceneHasNoEntry => stopWhenSceneHasNoEntry;
    public AudioClip CoinPickupClip => coinPickupClip;
    public AudioClip StunClip => stunClip;
    public AudioClip ExplosionClip => explosionClip;
    public AudioClip TurretShotClip => turretShotClip;
    public AudioClip StunTurretShotClip => stunTurretShotClip;

    public bool TryGetSceneSettings(string sceneName, out SceneAudioEntry entry)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            SceneAudioEntry sceneEntry = scenes[i];
            if (string.Equals(sceneEntry.sceneName, sceneName, StringComparison.Ordinal))
            {
                entry = sceneEntry;
                return true;
            }
        }

        entry = null;
        return false;
    }

    public bool TryGetFallback(out SceneAudioEntry entry)
    {
        if (fallbackClip == null)
        {
            entry = null;
            return false;
        }

        entry = new SceneAudioEntry
        {
            sceneName = string.Empty,
            musicClip = fallbackClip,
            volume = fallbackVolume,
            loop = fallbackLoop
        };

        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Sync Scenes From Build Settings")]
    private void SyncScenesFromBuildSettings()
    {
        Dictionary<string, SceneAudioEntry> existingEntries = scenes
            .Where(scene => !string.IsNullOrWhiteSpace(scene.sceneName))
            .GroupBy(scene => scene.sceneName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        List<SceneAudioEntry> syncedEntries = new List<SceneAudioEntry>();

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
        {
            string sceneName = Path.GetFileNameWithoutExtension(buildScene.path);
            if (string.IsNullOrWhiteSpace(sceneName))
                continue;

            if (existingEntries.TryGetValue(sceneName, out SceneAudioEntry existingEntry))
            {
                syncedEntries.Add(existingEntry);
                continue;
            }

            syncedEntries.Add(new SceneAudioEntry
            {
                sceneName = sceneName,
                volume = 1f,
                loop = true
            });
        }

        scenes = syncedEntries;
        EditorUtility.SetDirty(this);
        Debug.Log($"Synced {scenes.Count} scene audio entries from Build Settings.", this);
    }
#endif
}

[Serializable]
public class SceneAudioEntry
{
    public string sceneName;
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = true;
}
