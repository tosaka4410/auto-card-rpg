using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Proto/Enemy Preset")]
public class EnemyPreset : ScriptableObject
{
    public string enemyName;
    public int recommendedRound = 1;

    [Header("Base Stats")]
    public int maxHp = 40;

    [Header("Skills (Fixed Build)")]
    public List<SkillData> skills = new();
}
