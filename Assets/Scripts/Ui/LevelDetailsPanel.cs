using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelDetailsPanel : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject normalDetailsPanel;
    [SerializeField] private GameObject bossDetailsPanel;

    [Header("Normal Level UI")]
    [SerializeField] private TextMeshProUGUI levelTextNumber;
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI objectiveText;

    private string sceneToLoad;
    private int selectedLevelNumber;
    private bool isLoading;

    private void Start()
    {
        HideAll();
    }

    public void ShowLevelDetails(string sceneName, int levelNumber, string levelName, string difficulty, string objective)
    {
        HideAll();

        levelTextNumber.text = "Level " + levelNumber;
        levelTitleText.text = levelName;
        difficultyText.text = "Difficulty: " + difficulty;
        objectiveText.text = "Objective: " + objective;

        sceneToLoad = sceneName;
        selectedLevelNumber = levelNumber;
        isLoading = false;

        normalDetailsPanel.SetActive(true);
        gameObject.SetActive(true);
    }

    public void ShowBossLevelDetails(string sceneName, int levelNumber)
    {
        HideAll();

        sceneToLoad = sceneName;
        selectedLevelNumber = levelNumber;
        isLoading = false;

        if (bossDetailsPanel != null)
        {
            bossDetailsPanel.SetActive(true);
        }

        gameObject.SetActive(true);
    }

    public void PlayLevel()
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("No scene selected to load.");
            return;
        }

        string targetScene = sceneToLoad;
        GameData.CurrentLevelSelectScene = SceneManager.GetActiveScene().name;

        if (RunManager.Instance != null)
        {
            RunManager.Instance.SetStage(selectedLevelNumber);
            targetScene = RunManager.Instance.ShouldShowCardsForNextStage(selectedLevelNumber)
                ? "Ability Selection"
                : sceneToLoad;
        }

        GameData.CurrentLevelIndex = selectedLevelNumber;
        SinglePlayerSaveSystem.PrepareFreshLevelStart();
        SinglePlayerSaveSystem.ClearQueuedCheckpointRestore();
        isLoading = true;
        SinglePlayerSaveSystem.SaveCheckpoint(false);

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(targetScene);
            }

            return;
        }

        SceneManager.LoadScene(targetScene);
    }

    public void HideAll()
    {
        isLoading = false;
        gameObject.SetActive(false);
        normalDetailsPanel.SetActive(false);
        bossDetailsPanel.SetActive(false);
    }

    public void BacktoWorldMap()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("Worlds");
            }

            return;
        }

        SceneManager.LoadScene("Worlds");
    }
}
