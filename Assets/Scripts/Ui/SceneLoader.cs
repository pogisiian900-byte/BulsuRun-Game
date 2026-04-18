using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SceneLoader : MonoBehaviour
{
    private const string StartScreenSceneName = "Start Screen";
    private const string SingleplayerSceneName = "Singleplayer";
    private const string WorldsSceneName = "Worlds";

    [Header("Intro Video")]
    [SerializeField] private bool playSingleplayerIntroCutscene = true;
    [SerializeField] private VideoClip singleplayerIntroCutscene;

    public void LoadScene(string sceneName)
    {
        PersistLocalPlayerNameIfPresent();

        string resolvedSceneName = ResolveTargetSceneName(sceneName);

        if (ShouldResetProgress(resolvedSceneName))
        {
            ResetProgress();
        }
        else
        {
            if (SinglePlayerSaveSystem.IsCheckpointRestoreQueued)
                SinglePlayerSaveSystem.RestoreCheckpoint();
            else
                SinglePlayerSaveSystem.SaveCheckpoint();
        }

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(resolvedSceneName);
            }

            return;
        }

        if (TryPlayIntroCutscene(resolvedSceneName))
            return;

        SceneManager.LoadScene(resolvedSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit pressed!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ResetProgress()
    {
        SinglePlayerSaveSystem.ResetProgress();

        ProgressStore.SetHighestLevel(1);
        ProgressStore.SetHighestWorld(1);
        ProgressStore.DeleteJustUnlockedLevel();
        ProgressStore.DeleteJustUnlockedWorld();
        ProgressStore.Save();

        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.highestLevelUnlocked = 1;
            GameProgress.Instance.highestWorldUnlocked = 1;
        }
    }

    private bool ShouldResetProgress(string targetSceneName)
    {
        if (targetSceneName != WorldsSceneName)
        {
            return false;
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isSinglePlayerMenuFlow = currentSceneName == StartScreenSceneName || currentSceneName == SingleplayerSceneName;
        return isSinglePlayerMenuFlow && !SinglePlayerSaveSystem.HasSaveData();
    }

    private bool TryPlayIntroCutscene(string targetSceneName)
    {
        if (!playSingleplayerIntroCutscene)
            return false;

        if (targetSceneName != SingleplayerSceneName)
            return false;

        VideoCutsceneState.Queue(singleplayerIntroCutscene, targetSceneName);
        SceneManager.LoadScene(VideoCutsceneState.CutsceneSceneName);
        return true;
    }

    private static void PersistLocalPlayerNameIfPresent()
    {
        PlayerNameInputUI playerNameInput = Object.FindFirstObjectByType<PlayerNameInputUI>(FindObjectsInactive.Include);
        if (playerNameInput != null)
            playerNameInput.SaveNameFromInput();
    }

    private static string ResolveTargetSceneName(string targetSceneName)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool shouldSkipNameCheck =
            currentSceneName == StartScreenSceneName &&
            targetSceneName == SingleplayerSceneName &&
            PlayerNameStore.HasSavedName();

        if (!shouldSkipNameCheck)
            return targetSceneName;

        PlayerNameStore.ApplySavedName();
        return WorldsSceneName;
    }
}
