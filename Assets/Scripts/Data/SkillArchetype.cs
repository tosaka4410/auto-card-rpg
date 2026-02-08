using UnityEngine;

[CreateAssetMenu(menuName = "Proto/Skill Archetype")]
public class SkillArchetype : ScriptableObject
{
    public string baseIdPrefix = "fire";     // fire, guard...
    public string namePrefix = "Fire";       // 表示名の頭

    public SkillType type = SkillType.Fire;

    [Header("Base Stats (for G1)")]
    public int baseAtk = 3;
    public int baseBlk = 0;

    [Header("Grade Range")]
    public int minGrade = 1;
    public int maxGrade = 6;

    [Header("Per Grade Growth")]
    public float atkMulPerGrade = 1.25f;
    public float blkMulPerGrade = 1.15f;

    [Header("Tag Prob")]
    [Range(0, 1)] public float stableProb = 0.15f;
    [Range(0, 1)] public float cooldownProb = 0.10f;

    [Header("Effect Prob")]
    [Range(0, 1)] public float growAtkPermProb = 0.10f;
    [Range(0, 1)] public float growAtkBattleProb = 0.10f;
    public int effectValueMin = 1;
    public int effectValueMax = 2;

    [Header("How many variants per grade")]
    public int variantsPerGrade = 3;
}
