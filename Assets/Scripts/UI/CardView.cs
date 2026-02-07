using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text attackText;
    [SerializeField] private Text blockText;
    [SerializeField] private Text effectText;

    public void Bind(SkillInstance skill)
    {
        if (skill == null || skill.data == null)
        {
            nameText.text = "-";
            attackText.text = "0";
            blockText.text = "0";
            effectText.text = "";
            return;
        }

        nameText.text = skill.Name;
        attackText.text = skill.Attack.ToString();
        blockText.text = skill.Block.ToString();
        effectText.text = BuildEffectText(skill);
    }

    private string BuildEffectText(SkillInstance s)
    {
        switch (s.data.effectType)
        {
            case SkillEffectType.GrowAttackPermanent:
                return "使用するたびに攻撃力+"
                     + s.data.effectValue + "（永続）";

            case SkillEffectType.GrowAttackThisBattle:
                return "使用するたびに攻撃力+"
                     + s.data.effectValue + "（このバトル）";

            default:
                return "";
        }
    }
}
