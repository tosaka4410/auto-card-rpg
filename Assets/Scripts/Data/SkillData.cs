using UnityEngine;

public enum SkillType
{
    Fire,
    Lightning,
    Guard,
    Support,
    Machine
}
public enum SkillTag
{
    None,
    Stable,
    Cooldown
}
[CreateAssetMenu(menuName = "Proto/Skill")]
public class SkillData : ScriptableObject
{
    public string skillId;   // "fire_1" など（手動 or 自動）
    public string skillName;

    public int attack;
    public int block;

    public SkillType type;
    public SkillTag tag;
}