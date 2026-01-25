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

public enum SkillGrade
{
    G1 = 1,
    G2 = 2,
    G3 = 3,
    G4 = 4,
    G5 = 5,
    G6 = 6
}

[CreateAssetMenu(menuName = "Proto/Skill")]
public class SkillData : ScriptableObject
{
    [Header("Identity")]
    public string skillId;   // "fire_1" など（手動でOK）
    public string skillName;

    [Header("Stats")]
    public int attack;
    public int block;

    [Header("Meta")]
    public SkillType type;
    public SkillTag tag;

    [Header("Grade / Shop")]
    public SkillGrade grade = SkillGrade.G1;

    // ★A方針：ショップに出るのはベーススキルのみ（トリプル産は false）
    public bool shopOnlyBase = true;

    [Header("Evolve")]
    public int evolveTier = 1; // 1=通常、2=トリプル後…
}
