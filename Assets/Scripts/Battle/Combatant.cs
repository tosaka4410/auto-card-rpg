public class Combatant
{
    public int hp;
    public MonsterData data;

    public Combatant(MonsterData data)
    {
        this.data = data;
        hp = data.maxHp;
    }
}
