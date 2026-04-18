using UnityEngine;

public enum SkillType
{
    ExtraHP,
    DoubleJump,
    MoveSpeed
}

[CreateAssetMenu(menuName = "Skills/Skill Card")]
public class SkillCard : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    public SkillType type;
    public float value; // e.g. 20 HP, 0.2 speed bonus, etc.
}
