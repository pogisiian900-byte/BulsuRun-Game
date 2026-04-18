using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [SerializeField] private ResultScreenUI resultUI;

    private string nextSceneName;
    private int completedLevelIndex = -1;
    private bool levelCompletionHandled;


    private void Awake()
    {
        // Singleton protection
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetNextScene(string sceneName)
    {
        nextSceneName = sceneName;
    }

    public void Win()
    {
        FinalizeLevelCompletion();

        int coins = GameData.Coins;
        int score = GameData.Score;
        float time = Time.timeSinceLevelLoad;

        resultUI.ShowWin(coins, score, time, nextSceneName);
    }

    public bool TryPlayVictoryCutscene(bool shouldPlayCutscene, VideoClip cutsceneClip = null, string returnSceneName = VideoCutsceneState.DefaultReturnSceneName)
    {
        if (!shouldPlayCutscene)
            return false;

        FinalizeLevelCompletion();

        VideoCutsceneState.Queue(cutsceneClip, returnSceneName);

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel(VideoCutsceneState.CutsceneSceneName);

            return true;
        }

        SceneManager.LoadScene(VideoCutsceneState.CutsceneSceneName);
        return true;
    }

    public void Lose()
    {
        SinglePlayerSaveSystem.QueueCheckpointRestore();

        int coins = GameData.Coins;
        int score = GameData.Score;
        float time = Time.timeSinceLevelLoad;

        resultUI.ShowLose(coins, score, time);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        resultUI = FindObjectOfType<ResultScreenUI>(true);
        if (resultUI == null)
        {
            Debug.Log("ResultScreenUI not found in scene!");
        }

        levelCompletionHandled = false;
        completedLevelIndex = -1;

        if (TryGetLevelIndexForSceneName(scene.name, out int levelIndex))
        {
            GameData.CurrentLevelIndex = levelIndex;
            SinglePlayerSaveSystem.SaveCheckpoint(false);
        }
    }

    public void UnlockNextLevel(int currentLevel)
    {
        int highestLevel = ProgressStore.GetHighestLevel();

        if (currentLevel >= highestLevel)
        {
            int newLevel = currentLevel + 1;

            ProgressStore.SetHighestLevel(newLevel);
            ProgressStore.SetJustUnlockedLevel(newLevel);

            int previousWorld = WorldMapUnlockController.GetUnlockedWorldCount(highestLevel);
            int unlockedWorlds = WorldMapUnlockController.GetUnlockedWorldCount(newLevel);

            ProgressStore.SetHighestWorld(Mathf.Max(ProgressStore.GetHighestWorld(), unlockedWorlds));

            if (unlockedWorlds > previousWorld)
            {
                ProgressStore.SetJustUnlockedWorld(unlockedWorlds);
            }

            ProgressStore.Save();
            SinglePlayerSaveSystem.SaveCheckpoint(false);
        }
    }

    public static bool TryGetLevelIndexForSceneName(string sceneName, out int levelIndex)
    {
        switch (sceneName)
        {
            case "Level 1 CBA Classroom":
            case "Level 1 CBA Hallway Tutorial":
            case "Level 1 CBA Hallway":
                levelIndex = 1;
                return true;
            case "Level 2 CBA":
                levelIndex = 2;
                return true;
            case "Level 3 CBA":
                levelIndex = 3;
                return true;
            case "CBA Mini Boss":
                levelIndex = 4;
                return true;
            case "AC Level 1":
                levelIndex = 5;
                return true;
            case "AC Level 2":
                levelIndex = 6;
                return true;
            case "AC Mini Boss":
                levelIndex = 7;
                return true;
            case "Level 1 Admin Outside":
                levelIndex = 8;
                return true;
            case "Level 2nd Floor Admin":
                levelIndex = 9;
                return true;
            case "Level 3rd Floor Admin":
                levelIndex = 10;
                return true;
            case "Admin Mini Boss":
                levelIndex = 11;
                return true;
            case "Level 1 Pancho 1st Floor":
                levelIndex = 12;
                return true;
            case "Level 2 Pancho 2nd Floor":
                levelIndex = 13;
                return true;
            case "Pancho Mini Boss":
                levelIndex = 14;
                return true;
            case "Gate Final Boss":
                levelIndex = 15;
                return true;
            default:
                levelIndex = 0;
                return false;
        }
    }

    private void FinalizeLevelCompletion()
    {
        if (levelCompletionHandled && completedLevelIndex == GameData.CurrentLevelIndex)
            return;

        SinglePlayerSaveSystem.CommitCompletedLevelCollectibles();
        UnlockNextLevel(GameData.CurrentLevelIndex);
        SinglePlayerSaveSystem.SaveCheckpoint();

        levelCompletionHandled = true;
        completedLevelIndex = GameData.CurrentLevelIndex;
    }
}

