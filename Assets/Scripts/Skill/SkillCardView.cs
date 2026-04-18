using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillCardView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private Button button;

    public void Set(SkillCard card, Action onClick)
    {
        bool hasCard = card != null;

        if (icon != null)
        {
            icon.sprite = hasCard ? card.icon : null;
            icon.enabled = hasCard;
        }

        if (titleText != null)
            titleText.text = hasCard ? card.displayName : string.Empty;

        if (descText != null)
            descText.text = hasCard ? card.description : string.Empty;

        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.interactable = hasCard && onClick != null;

        if (hasCard && onClick != null)
            button.onClick.AddListener(() => onClick.Invoke());
    }
}
