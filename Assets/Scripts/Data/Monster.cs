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
        if (preset.skills != null)
            m.skills.AddRange(preset.skills);
        return m;
    }

    /// <summary>
    /// Record から Monster を復元。
    /// - 通常スキルは skillDict から
    /// - 進化ID（*_t2 等）は baseId を引いてランタイム生成
    /// - それも無理なら UnknownFallback を生成（落とさない）
    /// </summary>
    public static Monster CreateMonsterFromRecord(
        MonsterRecord record,
        Dictionary<string, SkillData> skillDict
    )
    {
        var m = new Monster(record.maxHp);

        if (record == null || record.skillIds == null)
            return m;

        foreach (var id in record.skillIds)
        {
            if (string.IsNullOrEmpty(id)) continue;

            // 1) まず辞書直引き（ベーススキルが入っている想定）
            if (skillDict != null && skillDict.TryGetValue(id, out var skill))
            {
                m.skills.Add(skill);
                continue;
            }

            // 2) 進化IDなら baseId/tier を解析して生成
            if (SkillRuntimeFactory.TryParseEvolvedId(id, out var baseId, out var tier))
            {
                if (skillDict != null && skillDict.TryGetValue(baseId, out var baseSkill))
                {
                    var evolved = SkillRuntimeFactory.CreateEvolvedFromBase(baseSkill, id, tier);
                    m.skills.Add(evolved);
                    continue;
                }

                Debug.LogWarning($"Evolved skill base not found: evolved={id} base={baseId}");
            }
            else
            {
                Debug.LogWarning($"Skill not found: {id}");
            }

            // 3) 最後の保険：Unknownとして生成
            m.skills.Add(SkillRuntimeFactory.CreateUnknownFallback(id));
        }

        return m;
    }
}
