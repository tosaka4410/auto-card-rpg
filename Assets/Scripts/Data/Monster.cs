using UnityEngine;
using System.Collections.Generic;

public class Monster
{
    public int maxHp;
    public int hp;
    public List<SkillData> skills = new();

    public Monster(int maxHp)
    {
        this.maxHp = maxHp;
        hp = maxHp;
    }

    public static Monster FromPreset(EnemyPreset preset)
    {
        var m = new Monster(preset.maxHp);
        m.skills.AddRange(preset.skills);
        return m;
    }

    public static Monster CreateMonsterFromRecord(
        MonsterRecord record,
        Dictionary<string, SkillData> skillDict
    )
    {
        var m = new Monster(record.maxHp);

        foreach (var id in record.skillIds)
        {
            if (skillDict.TryGetValue(id, out var skill))
            {
                m.skills.Add(skill);
            }
            else
            {
                Debug.LogWarning($"Skill not found: {id}");
            }
        }

        return m;
    }
}
