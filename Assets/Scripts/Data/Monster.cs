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
}
