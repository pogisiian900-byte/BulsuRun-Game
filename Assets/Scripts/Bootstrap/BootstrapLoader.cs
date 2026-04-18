using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private string firstScene = "Start Screen";
    [Header("Startup Video")]
    [SerializeField] private bool playStartupCutscene = true;
    [SerializeField] private VideoClip startupCutsceneClip;

    private void Start()
    {
        if (ShouldPlayStartupCutscene())
        {
            VideoCutsceneState.Queue(startupCutsceneClip, firstScene);
            SceneManager.LoadScene(VideoCutsceneState.CutsceneSceneName);
            return;
        }

        SceneManager.LoadScene(firstScene);
    }

    private bool ShouldPlayStartupCutscene()
    {
        if (!playStartupCutscene)
            return false;

        if (string.IsNullOrWhiteSpace(firstScene))
            return false;

        if (string.Equals(firstScene, VideoCutsceneState.CutsceneSceneName, System.StringComparison.OrdinalIgnoreCase))
            return false;

        if (Application.CanStreamedLevelBeLoaded(VideoCutsceneState.CutsceneSceneName))
            return true;

        Debug.LogWarning($"Startup cutscene scene '{VideoCutsceneState.CutsceneSceneName}' is not in Build Settings. Loading '{firstScene}' directly instead.", this);
        return false;
    }
}
