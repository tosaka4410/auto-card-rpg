using UnityEngine;

public static class SkillRuntimeFactory
{
    // 進化ID: "{baseId}_t{tier}" 例: "fire_1_t2"
    public static bool TryParseEvolvedId(string id, out string baseId, out int tier)
    {
        baseId = null;
        tier = 1;

        if (string.IsNullOrEmpty(id)) return false;

        int idx = id.LastIndexOf("_t");
        if (idx < 0) return false;

        baseId = id.Substring(0, idx);
        string tStr = id.Substring(idx + 2);

        return int.TryParse(tStr, out tier);
    }

    // baseSkill を元に、tier に応じて再生成
    public static SkillData CreateEvolvedFromBase(SkillData baseSkill, string evolvedId, int tier)
    {
        var s = ScriptableObject.CreateInstance<SkillData>();

        s.skillId = evolvedId;
        s.skillName = $"{baseSkill.skillName}+";
        s.type = baseSkill.type;
        s.tag = baseSkill.tag;

        // tier2で+1 grade, tier3で+2 grade…（上限6）
        int g = Mathf.Min(6, (int)baseSkill.grade + (tier - 1));
        s.grade = (SkillGrade)g;

        // tierごとに倍率を上げる（例：1.6^(tier-1)）
        float mul = Mathf.Pow(1.6f, tier - 1);
        s.attack = Mathf.CeilToInt(baseSkill.attack * mul);
        s.block  = Mathf.CeilToInt(baseSkill.block  * mul);

        s.evolveTier = tier;
        s.shopOnlyBase = false; // ★ショップに出ない

        return s;
    }

    // 見つからない時の最後の保険（IDだけで仮生成）
    public static SkillData CreateUnknownFallback(string id)
    {
        var s = ScriptableObject.CreateInstance<SkillData>();
        s.skillId = id;
        s.skillName = id;
        s.attack = 0;
        s.block = 0;
        s.type = SkillType.Machine;
        s.tag = SkillTag.None;
        s.grade = SkillGrade.G1;
        s.evolveTier = 1;
        s.shopOnlyBase = false;
        return s;
    }
}
