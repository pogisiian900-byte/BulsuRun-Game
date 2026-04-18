using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class InventorySkillsUI : MonoBehaviour
{
    [SerializeField] private PlayerSkillHolder playerSkills;

    [Header("UI Slots (size 3)")]
    [SerializeField] private Image[] icons;
    [SerializeField] private TMP_Text[] names;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayerSkillHolder.SkillsChanged += Refresh;

        BindPlayerSkills();
        Refresh();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        PlayerSkillHolder.SkillsChanged -= Refresh;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindPlayerSkills();
        Refresh();
    }

    private void BindPlayerSkills()
    {
        if (playerSkills == null)
            playerSkills = FindObjectOfType<PlayerSkillHolder>(true);

        if (playerSkills == null)
            Debug.LogWarning("InventorySkillsUI: PlayerSkillHolder not found in scene.");
    }

    public void Refresh()
    {
        if (playerSkills == null)
            return;

        var owned = playerSkills.Owned;

        for (int i = 0; i < icons.Length; i++)
        {
            if (i < owned.Count && owned[i] != null)
            {
                icons[i].enabled = true;
                icons[i].preserveAspect = true;
                icons[i].sprite = owned[i].icon;

                if (names != null && i < names.Length && names[i] != null)
                {
                    names[i].enabled = true;
                    names[i].text = owned[i].displayName;
                }
            }
            else
            {
                icons[i].enabled = false;

                if (names != null && i < names.Length && names[i] != null)
                    names[i].enabled = false;
            }
        }
    }
}
