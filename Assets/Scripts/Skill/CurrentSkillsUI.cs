using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentSkillsUI : MonoBehaviour
{
    [SerializeField] private PlayerSkillHolder playerSkill;
    [SerializeField] private Image[] skillIcons;     // size 3 (ICON images)
    [SerializeField] private TMP_Text[] skillTexts;  // size 3

    private void OnEnable()
    {
        if (playerSkill == null)
        {
            playerSkill = PlayerSkillHolder.Instance != null
                ? PlayerSkillHolder.Instance
                : FindFirstObjectByType<PlayerSkillHolder>();
        }

        PlayerSkillHolder.SkillsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        PlayerSkillHolder.SkillsChanged -= Refresh;
    }

    public void Refresh()
    {
        if (playerSkill == null)
        {
            Debug.LogWarning("CurrentSkillsUI: PlayerSkillHolder not found.");
            return;
        }

        var skills = playerSkill.Owned;

        for (int i = 0; i < skillIcons.Length; i++)
        {
            if (i < skills.Count && skills[i] != null)
            {
                skillIcons[i].sprite = skills[i].icon;
                skillIcons[i].enabled = true;

                skillTexts[i].text = skills[i].displayName;
                skillTexts[i].enabled = true;
            }
            else
            {
                skillIcons[i].enabled = false;
                skillTexts[i].enabled = false;
            }
        }
    }
}
