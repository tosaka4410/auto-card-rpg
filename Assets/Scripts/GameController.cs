using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public List<SkillData> skillPool;
    private Monster player;
    private Shop shop = new();

    [Header("Enemy Presets")]
    public EnemyPreset attackEnemy;
    public EnemyPreset defenseEnemy;
    public EnemyPreset balancedEnemy;

    void Start()
    {
        player = new Monster(40);
        OpenShop();
    }

    void OpenShop()
    {
        var offers = shop.Offer(skillPool, 3);

        Debug.Log("=== SHOP ===");
        for (int i = 0; i < offers.Count; i++)
        {
            Debug.Log($"{i}: {offers[i].skillName}");
        }

        // 仮：ランダムに1つ取る（後でボタンにする）
        TakeSkill(offers[Random.Range(0, offers.Count)]);
    }

    void TakeSkill(SkillData skill)
    {
        if (player.skills.Count >= 7)
            player.skills.RemoveAt(0);

        player.skills.Add(skill);

        Debug.Log($"TAKE: {skill.skillName}");
        StartBattle();
    }

    void StartBattle()
    {
        StartCoroutine(Battle());
    }

    IEnumerator Battle()
    {
        EnemyPreset preset = PickEnemyPreset();

        Debug.Log($"ENEMY TYPE: {preset.enemyName}");

        var enemy = Monster.FromPreset(preset);

        int turn = 0;

        while (player.hp > 0 && enemy.hp > 0)
        {
            turn++;

            ResolveTurn(player, enemy, turn);
            ResolveTurn(enemy, player, turn);

            Debug.Log($"Turn {turn} | P:{player.hp} E:{enemy.hp}");
            yield return new WaitForSeconds(0.3f);
        }

        Debug.Log(player.hp > 0 ? "WIN" : "LOSE");
        player.hp = player.maxHp;
        OpenShop();
    }

    EnemyPreset PickEnemyPreset()
    {
        int r = Random.Range(0, 3);
        if (r == 0)
            return attackEnemy;
        if (r == 1)
            return defenseEnemy;
        return balancedEnemy;
    }

    void ResolveTurn(Monster atk, Monster def, int turn)
    {
        int n = atk.skills.Count;
        int k = Mathf.CeilToInt(n / 2f);

        var pool = new List<SkillData>(atk.skills);

        // Stable 優先
        var picked = new List<SkillData>();
        foreach (var s in pool)
        {
            if (s.tag == SkillTag.Stable && picked.Count < k)
                picked.Add(s);
        }

        // 残りをランダム
        var rest = pool.Except(picked).ToList();
        while (picked.Count < k && rest.Count > 0)
        {
            int idx = Random.Range(0, rest.Count);
            picked.Add(rest[idx]);
            rest.RemoveAt(idx);
        }

        int atkVal = 0;
        int defVal = 0;

        foreach (var s in picked)
        {
            atkVal += s.attack;
            defVal += s.block;
        }

        int fatigue = Mathf.Max(0, turn - 4);
        int damage = Mathf.Max(0, atkVal - defVal) + fatigue;

        def.hp -= damage;

        string pickedNames = string.Join(", ", picked.Select(s => s.skillName));

        Debug.Log(
            $"[{(atk == player ? "PLAYER" : "ENEMY")}] "
                + $"Picked: {pickedNames} | "
                + $"ATK:{atkVal} DEF:{defVal} FAT:{fatigue}"
        );
    }

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
            if (s == null)
                continue;

            record.skillIds.Add(s.skillId);

            if (!record.typeCount.ContainsKey(s.type))
                record.typeCount[s.type] = 0;
            record.typeCount[s.type]++;

            if (s.tag == SkillTag.Stable)
                record.stableCount++;
            if (s.tag == SkillTag.Cooldown)
                record.cooldownCount++;
        }

        return record;
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

    Dictionary<string, SkillData> BuildSkillDict(List<SkillData> allSkills)
    {
        var dict = new Dictionary<string, SkillData>();
        foreach (var s in allSkills)
        {
            if (!string.IsNullOrEmpty(s.skillId))
                dict[s.skillId] = s;
        }
        return dict;
    }
}
