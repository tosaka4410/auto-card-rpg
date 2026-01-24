using UnityEngine;

public static class MonsterRecordFactory
{
    public static MonsterRecord CreateRecordFromMonster(
        Monster monster,
        int finalShopGrade,
        int totalBattles,
        int totalTurns
    )
    {
        var record = new MonsterRecord
        {
            maxHp = monster.maxHp,
            finalShopGrade = finalShopGrade,
            totalBattles = totalBattles,
            totalTurns = totalTurns,
        };

        foreach (var s in monster.skills)
        {
            if (s == null) continue;
            if (string.IsNullOrEmpty(s.skillId)) continue;

            record.skillIds.Add(s.skillId);

            if (!record.typeCount.ContainsKey(s.type))
                record.typeCount[s.type] = 0;
            record.typeCount[s.type]++;

            if (s.tag == SkillTag.Stable) record.stableCount++;
            if (s.tag == SkillTag.Cooldown) record.cooldownCount++;
        }

        return record;
    }
}
