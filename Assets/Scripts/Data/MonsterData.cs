using UnityEngine;

[CreateAssetMenu(menuName = "Game/Monster")]
public class MonsterData : ScriptableObject
{
    public string monsterName;
    public int maxHp;
    public SkillData[] skills; // 7個
}
