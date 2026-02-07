using UnityEngine;

[System.Serializable]
public class SkillInstance
{
    public SkillData data;

    // 永続の補正
    public int permAtkBonus;
    public int permBlkBonus;

    // 1バトル限りの補正
    public int battleAtkBonus;
    public int battleBlkBonus;

    public SkillInstance(SkillData data)
    {
        this.data = data;
    }

    public int Attack => (data != null ? data.attack : 0) + permAtkBonus + battleAtkBonus;
    public int Block  => (data != null ? data.block  : 0) + permBlkBonus + battleBlkBonus;

    public string SkillId => data != null ? data.skillId : "";
    public string Name => data != null ? data.skillName : "(null)";
    public SkillTag Tag => data != null ? data.tag : SkillTag.None;
    public SkillType Type => data != null ? data.type : SkillType.Machine;
    public SkillGrade Grade => data != null ? data.grade : SkillGrade.G1;

    public void ResetBattleBonuses()
    {
        battleAtkBonus = 0;
        battleBlkBonus = 0;
    }

    // 「このターン選ばれて使われた」タイミングで呼ぶ
    public void OnUse()
    {
        if (data == null) return;

        switch (data.effectType)
        {
            case SkillEffectType.GrowAttackPermanent:
                permAtkBonus += Mathf.Max(0, data.effectValue);
                break;

            case SkillEffectType.GrowAttackThisBattle:
                battleAtkBonus += Mathf.Max(0, data.effectValue);
                break;
        }
    }
}
