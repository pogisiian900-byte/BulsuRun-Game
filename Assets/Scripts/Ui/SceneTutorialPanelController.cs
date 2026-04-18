using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class SceneTutorialEntry
{
    public string sceneName;
    public Sprite tutorialSprite;
}

public class SceneTutorialPanelController : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private bool preserveCurrentSpriteWhenUnassigned = true;
    [SerializeField] private List<SceneTutorialEntry> sceneTutorials = new()
    {
        new SceneTutorialEntry { sceneName = "AC Level 1" },
        new SceneTutorialEntry { sceneName = "AC Mini Boss" },
        new SceneTutorialEntry { sceneName = "Level 1 Admin Inside" },
        new SceneTutorialEntry { sceneName = "Level 1 Pancho 1st Floor" },
        new SceneTutorialEntry { sceneName = "Pancho Mini Boss" }
    };

    private bool pauseApplied;
    private float previousTimeScale = 1f;

    private IEnumerator Start()
    {
        BindCloseButton();
        yield return null;
        TryShowTutorialForActiveScene();
    }

    private void OnEnable()
    {
        BindCloseButton();
    }

    private void OnDisable()
    {
        UnbindCloseButton();
        HideTutorialPanel();
        ReleasePause();
    }

    public void CloseTutorial()
    {
        HideTutorialPanel();
        ReleasePause();
    }

    private void TryShowTutorialForActiveScene()
    {
        if (!TryGetTutorialForScene(SceneManager.GetActiveScene().name, out SceneTutorialEntry tutorial))
        {
            HideTutorialPanel();
            ReleasePause();
            return;
        }

        ApplyTutorialSprite(tutorial);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        ApplyPause();
    }

    private bool TryGetTutorialForScene(string sceneName, out SceneTutorialEntry tutorial)
    {
        if (sceneTutorials != null)
        {
            for (int i = 0; i < sceneTutorials.Count; i++)
            {
                SceneTutorialEntry entry = sceneTutorials[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName))
                    continue;

                if (string.Equals(entry.sceneName.Trim(), sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    tutorial = entry;
                    return true;
                }
            }
        }

        tutorial = null;
        return false;
    }

    private void ApplyTutorialSprite(SceneTutorialEntry tutorial)
    {
        if (tutorialImage == null)
            return;

        if (tutorial?.tutorialSprite != null)
        {
            tutorialImage.sprite = tutorial.tutorialSprite;
            tutorialImage.preserveAspect = true;
            tutorialImage.enabled = true;
            return;
        }

        if (!preserveCurrentSpriteWhenUnassigned)
            tutorialImage.sprite = null;
    }

    private void ApplyPause()
    {
        if (pauseApplied)
            return;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pauseApplied = true;

        if (pauseMenu != null)
            pauseMenu.SetPauseInputBlocked(true);
    }

    private void ReleasePause()
    {
        if (!pauseApplied)
            return;

        pauseApplied = false;
        Time.timeScale = previousTimeScale;

        if (pauseMenu != null)
            pauseMenu.SetPauseInputBlocked(false);
    }

    private void HideTutorialPanel()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(CloseTutorial);
        closeButton.onClick.AddListener(CloseTutorial);
    }

    private void UnbindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(CloseTutorial);
    }
}
