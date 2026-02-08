using System.Collections.Generic;
using UnityEngine;

public static class SkillPoolGenerator
{
    public static List<SkillData> Generate(IReadOnlyList<SkillArchetype> archetypes)
    {
        var pool = new List<SkillData>();
        if (archetypes == null) return pool;

        foreach (var a in archetypes)
        {
            if (a == null) continue;

            for (int g = a.minGrade; g <= a.maxGrade; g++)
            {
                for (int v = 1; v <= Mathf.Max(1, a.variantsPerGrade); v++)
                {
                    var s = ScriptableObject.CreateInstance<SkillData>();

                    s.grade = (SkillGrade)g;
                    s.type = a.type;

                    // 例: fire_g3_v2
                    s.skillId = $"{a.baseIdPrefix}_g{g}_v{v}";
                    s.skillName = $"{a.namePrefix} {g}-{v}";

                    float gm = g - 1;
                    s.attack = Mathf.Max(0, Mathf.RoundToInt(a.baseAtk * Mathf.Pow(a.atkMulPerGrade, gm)));
                    s.block  = Mathf.Max(0, Mathf.RoundToInt(a.baseBlk * Mathf.Pow(a.blkMulPerGrade, gm)));

                    // tag
                    float r = Random.value;
                    if (r < a.cooldownProb) s.tag = SkillTag.Cooldown;
                    else if (r < a.cooldownProb + a.stableProb) s.tag = SkillTag.Stable;
                    else s.tag = SkillTag.None;

                    // effects
                    s.effectType = SkillEffectType.None;
                    s.effectValue = 1;

                    float e = Random.value;
                    if (e < a.growAtkPermProb)
                    {
                        s.effectType = SkillEffectType.GrowAttackPermanent;
                        s.effectValue = Random.Range(a.effectValueMin, a.effectValueMax + 1);
                    }
                    else if (e < a.growAtkPermProb + a.growAtkBattleProb)
                    {
                        s.effectType = SkillEffectType.GrowAttackThisBattle;
                        s.effectValue = Random.Range(a.effectValueMin, a.effectValueMax + 1);
                    }

                    s.shopOnlyBase = true;
                    s.evolveTier = 1;

                    pool.Add(s);
                }
            }
        }

        return pool;
    }
}
