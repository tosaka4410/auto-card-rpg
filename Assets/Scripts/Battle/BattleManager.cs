using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public MonsterData playerData;
    public MonsterData enemyData;

    private Combatant player;
    private Combatant enemy;

    private int turn = 0;

    void Start()
    {
        player = new Combatant(playerData);
        enemy = new Combatant(enemyData);

        StartCoroutine(BattleLoop());
    }

    System.Collections.IEnumerator BattleLoop()
    {
        while (player.hp > 0 && enemy.hp > 0)
        {
            turn++;

            ResolveTurn(player, enemy);
            ResolveTurn(enemy, player);

            Debug.Log($"Turn {turn} | Player HP:{player.hp} Enemy HP:{enemy.hp}");

            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log(player.hp > 0 ? "PLAYER WIN" : "ENEMY WIN");
    }

    void ResolveTurn(Combatant attacker, Combatant defender)
    {
        var picked = PickRandomSkills(attacker.data.skills, 3);

        int atk = 0;
        int def = 0;

        foreach (var s in picked)
        {
            atk += s.attack;
            def += s.block;
        }

        int fatigue = FatigueSystem.GetFatigueDamage(turn);

        int damage = Mathf.Max(0, atk - def) + fatigue;
        defender.hp -= damage;
    }

    List<SkillData> PickRandomSkills(SkillData[] pool, int count)
    {
        List<SkillData> list = new(pool);
        List<SkillData> result = new();

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, list.Count);
            result.Add(list[index]);
            list.RemoveAt(index);
        }

        return result;
    }
}
