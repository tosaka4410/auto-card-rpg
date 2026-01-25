using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject battlePanel;

    [Header("Common")]
    [SerializeField] private Button nextButton;

    // ===== Shop =====
    [Header("Shop - Header")]
    [SerializeField] private Text shopHeaderText; // grade/coins/upgradeCostなど

    [Header("Shop - Offer List")]
    [SerializeField] private Transform offerRoot;
    [SerializeField] private Button offerButtonPrefab;

    [Header("Shop - Owned List")]
    [SerializeField] private Transform ownedRoot;
    [SerializeField] private Button ownedButtonPrefab;

    [Header("Shop - Buttons")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button startBattleButton;

    // ===== Battle =====
    [Header("Battle - Header")]
    [SerializeField] private Text battleHeaderText; // enemyName, turn etc
    [SerializeField] private Text hpText;           // P/E HP
    [SerializeField] private Text pickedText;       // picked skills names
    [SerializeField] private Text statText;         // atk/def/fatigue/damage
    [SerializeField] private Text logText;          // accumulated log (optional)

    private readonly List<Button> offerButtons = new();
    private readonly List<Button> ownedButtons = new();

    private Action onNext;

    // -------------- Public API --------------

    public void BindNextButton(Action onClick)
    {
        onNext = onClick;
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => onNext?.Invoke());
        }
    }

    /// <summary>
    /// Shop画面表示（コイン/アップグレード込み）
    /// </summary>
    public void ShowShop(
        int shopGrade,
        int coins,
        int upgradeCost,
        int buyCost,
        int sellGain,
        IReadOnlyList<SkillData> offers,
        IReadOnlyList<SkillData> owned,
        Action<int> onPickOffer,
        Action onReroll,
        Action onUpgrade,
        Action<int> onSellOwned,
        Action onStartBattle
    )
    {
        SetPanel(shop: true);

        // Header
        if (shopHeaderText != null)
        {
            shopHeaderText.text =
                $"SHOP GRADE: {shopGrade}\n" +
                $"COINS: {coins}\n" +
                $"UPGRADE COST: {(shopGrade >= 6 ? "-" : upgradeCost)}\n" +
                $"BUY: -{buyCost}  SELL: +{sellGain}";
        }

        // Offer list
        RebuildButtons(
            offerRoot,
            offerButtonPrefab,
            offerButtons,
            offers.Count,
            (i, btn) =>
            {
                var s = offers[i];
                btn.GetComponentInChildren<Text>().text = FormatSkillLine(s, prefix: $"BUY(-{buyCost}) ");
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onPickOffer?.Invoke(i));
                btn.interactable = (s != null); // coins条件はController側で弾く想定
            }
        );

        // Owned list (Sell)
        RebuildButtons(
            ownedRoot,
            ownedButtonPrefab,
            ownedButtons,
            owned.Count,
            (i, btn) =>
            {
                var s = owned[i];
                btn.GetComponentInChildren<Text>().text = FormatSkillLine(s, prefix: $"SELL(+{sellGain}) ");
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onSellOwned?.Invoke(i));
                btn.interactable = (s != null);
            }
        );

        // Buttons
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() => onReroll?.Invoke());
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => onUpgrade?.Invoke());
            upgradeButton.interactable = shopGrade < 6; // coins条件はController側で弾く想定
        }

        if (startBattleButton != null)
        {
            startBattleButton.onClick.RemoveAllListeners();
            startBattleButton.onClick.AddListener(() => onStartBattle?.Invoke());
        }

        // Nextボタンはショップでは基本不要（必要なら表示切替）
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// バトル開始表示
    /// </summary>
    public void ShowBattleStart(string enemyName, int playerHp, int enemyHp)
    {
        SetPanel(shop: false);

        if (battleHeaderText != null) battleHeaderText.text = $"BATTLE vs {enemyName}";
        if (hpText != null) hpText.text = $"P:{playerHp}  E:{enemyHp}";

        if (pickedText != null) pickedText.text = "";
        if (statText != null) statText.text = "";
        if (logText != null) logText.text = "";

        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 毎ターン更新
    /// </summary>
    public void UpdateBattleTurn(
        int turn,
        int playerHp,
        int enemyHp,
        string playerPickedNames,
        int playerAtk,
        int playerDef,
        string enemyPickedNames,
        int enemyAtk,
        int enemyDef,
        int fatigue,
        int damageToEnemy,
        int damageToPlayer
    )
    {
        if (battleHeaderText != null) battleHeaderText.text = $"TURN {turn}";
        if (hpText != null) hpText.text = $"P:{playerHp}  E:{enemyHp}";

        if (pickedText != null)
        {
            pickedText.text =
                $"PLAYER PICKED:\n{playerPickedNames}\n\n" +
                $"ENEMY PICKED:\n{enemyPickedNames}";
        }

        if (statText != null)
        {
            statText.text =
                $"P ATK:{playerAtk} DEF:{playerDef} -> DMG:{damageToEnemy}\n" +
                $"E ATK:{enemyAtk} DEF:{enemyDef} -> DMG:{damageToPlayer}\n" +
                $"FATIGUE:{fatigue}";
        }

        if (logText != null)
        {
            logText.text +=
                $"T{turn}: P({playerAtk}-{playerDef})=>{damageToEnemy} / " +
                $"E({enemyAtk}-{enemyDef})=>{damageToPlayer} / F={fatigue}\n";
        }
    }

    /// <summary>
    /// 終了表示（Nextボタンでショップへ戻す想定）
    /// </summary>
    public void ShowBattleEnd(bool win)
    {
        if (battleHeaderText != null)
            battleHeaderText.text = win ? "WIN!" : "LOSE...";

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            // BindNextButtonで設定済みの onNext が呼ばれる
        }
    }

    // -------------- Helpers --------------

    private void SetPanel(bool shop)
    {
        if (shopPanel != null) shopPanel.SetActive(shop);
        if (battlePanel != null) battlePanel.SetActive(!shop);
    }

    private void RebuildButtons(
        Transform root,
        Button prefab,
        List<Button> cache,
        int needed,
        Action<int, Button> bind
    )
    {
        if (root == null || prefab == null) return;

        // 足りない分生成
        while (cache.Count < needed)
        {
            var btn = Instantiate(prefab, root);
            cache.Add(btn);
        }

        // 余りは非表示
        for (int i = 0; i < cache.Count; i++)
        {
            bool active = i < needed;
            cache[i].gameObject.SetActive(active);
            if (active)
            {
                bind?.Invoke(i, cache[i]);
            }
        }
    }

    private string FormatSkillLine(SkillData s, string prefix)
    {
        if (s == null) return prefix + "(null)";

        // grade表示（導入済み想定）
        string g = s.grade.ToString(); // 例: G3
        string tag = s.tag != SkillTag.None ? $"[{s.tag}]" : "";
        string type = s.type.ToString();

        return $"{prefix}{s.skillName} ({g}) {tag}  A:{s.attack} B:{s.block}  <{type}>";
    }
}
