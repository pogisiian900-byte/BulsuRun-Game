using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultScreenUI : MonoBehaviour
{
    private const string ResultAudioObjectName = "ResultScreenSfx";

    [Header("Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    [Header("Shared UI")]
    [SerializeField] private TMP_Text statsText;

    [Header("Audio")]
    [SerializeField] private AudioSource resultAudioSource;
    [SerializeField] private AudioClip winSfx;
    [SerializeField, Range(0f, 1f)] private float winSfxVolume = 1f;
    [SerializeField] private AudioClip loseSfx;
    [SerializeField, Range(0f, 1f)] private float loseSfxVolume = 1f;
    [SerializeField] private AudioClip loseMusic;
    [SerializeField, Range(0f, 1f)] private float loseMusicVolume = 0.85f;
    [SerializeField] private bool loseMusicLoop = true;
    [SerializeField] private float loseMusicDelay;

    private string nextScene;

    private void Awake()
    {
        EnsureResultAudioSource();
        Hide();
    }

    public void ShowWin(int coins, int score, float time, string sceneToLoad)
    {
        SceneAudioManager.CancelTemporaryMusic();
        nextScene = sceneToLoad;
        Show(coins, score, time);
        winPanel.SetActive(true);
        losePanel.SetActive(false);
        PlayResultSfx(winSfx, winSfxVolume);
    }

    public void ShowLose(int coins, int score, float time)
    {
        SceneAudioManager.StopMusicPlayback();
        Show(coins, score, time);
        winPanel.SetActive(false);
        losePanel.SetActive(true);
        PlayResultSfx(loseSfx, loseSfxVolume);
        SceneAudioManager.PlayTemporaryMusic(loseMusic, loseMusicVolume, loseMusicLoop, GetLoseMusicDelay());
    }

    private void Show(int coins, int score, float time)
    {
        gameObject.SetActive(true);
        statsText.text = $"Coins: {coins}\nScore: {score}\nTime: {time:0.0}s";
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnRetry()
    {
        SceneAudioManager.CancelTemporaryMusic(true);
        Time.timeScale = 1f;
        SinglePlayerSaveSystem.RestoreCheckpoint();

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void OnRetrySpecific(string firstSceneName)
    {
        SceneAudioManager.CancelTemporaryMusic(true);
        Time.timeScale = 1f;
        SinglePlayerSaveSystem.RestoreCheckpoint();

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene(firstSceneName);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(firstSceneName);
        }
    }

    public void OnNextLevel()
    {
        SceneAudioManager.CancelTemporaryMusic(true);
        Time.timeScale = 1f;
        string targetScene = ResolveLevelSelectScene();

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("Next level target scene not set!");
            return;
        }

        SinglePlayerSaveSystem.ClearQueuedCheckpointRestore();
        SinglePlayerSaveSystem.SaveCheckpoint();

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene(targetScene);
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(targetScene);
        }
    }

    public void OnWorldsLevel()
    {
        SceneAudioManager.CancelTemporaryMusic(true);
        Time.timeScale = 1f;
        const string worldsSceneName = "Worlds";

        if (SinglePlayerSaveSystem.IsCheckpointRestoreQueued)
            SinglePlayerSaveSystem.RestoreCheckpoint();
        else
            SinglePlayerSaveSystem.SaveCheckpoint();

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene(worldsSceneName);
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(worldsSceneName);
        }
    }

    private string ResolveLevelSelectScene()
    {
        if (TryGetLevelSelectSceneForScene(SceneManager.GetActiveScene().name, out string targetScene))
        {
            GameData.CurrentLevelSelectScene = targetScene;
            return targetScene;
        }

        if (TryGetLevelSelectSceneForIndex(GameData.CurrentLevelIndex, out targetScene))
        {
            GameData.CurrentLevelSelectScene = targetScene;
            return targetScene;
        }

        if (!string.IsNullOrEmpty(GameData.CurrentLevelSelectScene))
        {
            return GameData.CurrentLevelSelectScene;
        }

        return nextScene;
    }

    private bool TryGetLevelSelectSceneForScene(string sceneName, out string targetScene)
    {
        switch (sceneName)
        {
            case "Level 1 CBA Classroom":
            case "Level 1 CBA Hallway Tutorial":
            case "Level 1 CBA Hallway":
            case "Level 2 CBA":
            case "Level 3 CBA":
            case "CBA Mini Boss":
                targetScene = "CBA LEVELS";
                return true;
            case "AC Level 1":
            case "AC Level 2":
            case "AC Mini Boss":
                targetScene = "AC LEVELS";
                return true;
            case "Level 1 Admin Outside":
            case "Level 1 Admin Inside":
            case "Level 2nd Floor Admin":
            case "Level 3rd Floor Admin":
            case "Admin Mini Boss":
                targetScene = "Admin LEVELS";
                return true;
            case "Level 1 Pancho 1st Floor":
            case "Level 2 Pancho 2nd Floor":
            case "Pancho Mini Boss":
            case "Pancho 1st Floor Back":
                targetScene = "Pancho LEVELS";
                return true;
            case "Gate 1":
            case "Gate 2":
            case "Gate Final Boss":
                targetScene = "Gate LEVELS";
                return true;
            default:
                targetScene = null;
                return false;
        }
    }

    private bool TryGetLevelSelectSceneForIndex(int levelIndex, out string targetScene)
    {
        if (levelIndex >= 1 && levelIndex <= 4)
        {
            targetScene = "CBA LEVELS";
            return true;
        }

        if (levelIndex >= 5 && levelIndex <= 7)
        {
            targetScene = "AC LEVELS";
            return true;
        }

        if (levelIndex >= 8 && levelIndex <= 11)
        {
            targetScene = "Admin LEVELS";
            return true;
        }

        if (levelIndex >= 12 && levelIndex <= 14)
        {
            targetScene = "Pancho LEVELS";
            return true;
        }

        if (levelIndex == 15)
        {
            targetScene = "Gate LEVELS";
            return true;
        }

        targetScene = null;
        return false;
    }

    private void EnsureResultAudioSource()
    {
        if (resultAudioSource == null)
        {
            Transform existingChild = transform.Find(ResultAudioObjectName);
            if (existingChild != null)
                resultAudioSource = existingChild.GetComponent<AudioSource>();
        }

        if (resultAudioSource == null)
        {
            GameObject audioObject = new GameObject(ResultAudioObjectName);
            audioObject.transform.SetParent(transform, false);
            resultAudioSource = audioObject.AddComponent<AudioSource>();
        }

        resultAudioSource.playOnAwake = false;
        resultAudioSource.loop = false;
        resultAudioSource.spatialBlend = 0f;
    }

    private void PlayResultSfx(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        EnsureResultAudioSource();

        if (resultAudioSource == null)
            return;

        resultAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private float GetLoseMusicDelay()
    {
        if (loseMusicDelay > 0f)
            return loseMusicDelay;

        return loseSfx != null ? loseSfx.length : 0f;
    }
}
