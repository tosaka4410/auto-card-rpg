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
            if (string.IsNullOrEmpty(s.SkillId)) continue;

            record.skillIds.Add(s.SkillId);

            if (!record.typeCount.ContainsKey(s.Type))
                record.typeCount[s.Type] = 0;
            record.typeCount[s.Type]++;

            if (s.Tag == SkillTag.Stable) record.stableCount++;
            if (s.Tag == SkillTag.Cooldown) record.cooldownCount++;
        }

        return record;
    }
}
