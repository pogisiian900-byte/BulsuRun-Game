using System.Collections;
using UnityEngine;

public class LevelButtons : MonoBehaviour
{
    [Header("Scene")]
    public string sceneName;

    [Header("Normal Level")]
    public int levelNumber;
    public string levelName;
    public string difficulty;
    public string objective;

    [Header("Lock System")]
    [SerializeField] private GameObject lockImg;
    [SerializeField] private GameObject levelImg;
    [SerializeField] private Animator lockAnimator;

    public LevelDetailsPanel detailsPanel;

    private void Start()
    {
        int highestLevel = ProgressStore.GetHighestLevel();
        int justUnlocked = ProgressStore.GetJustUnlockedLevel();

        if (levelNumber <= highestLevel && levelNumber != justUnlocked)
        {
            lockImg.SetActive(false);
            levelImg.SetActive(true);
        }
        else if (levelNumber == justUnlocked)
        {
            lockImg.SetActive(true);
            levelImg.SetActive(false);
            StartCoroutine(PlayUnlock());
        }
        else
        {
            lockImg.SetActive(true);
            levelImg.SetActive(false);
        }
    }

    public void ShowLevelDetails()
    {
        int highestLevel = ProgressStore.GetHighestLevel();

        if (levelNumber > highestLevel)
        {
            return;
        }

        detailsPanel.ShowLevelDetails(
            sceneName,
            levelNumber,
            levelName,
            difficulty,
            objective
        );
    }

    public void ShowBossLevelDetails()
    {
        int highestLevel = ProgressStore.GetHighestLevel();

        if (levelNumber > highestLevel)
        {
            return;
        }

        detailsPanel.ShowBossLevelDetails(sceneName, levelNumber);
    }

    private IEnumerator PlayUnlock()
    {
        if (lockAnimator != null)
        {
            lockAnimator.SetTrigger("Unlock");
        }

        yield return new WaitForSeconds(1.2f);

        lockImg.SetActive(false);
        levelImg.SetActive(true);

        ProgressStore.DeleteJustUnlockedLevel();
    }
}
