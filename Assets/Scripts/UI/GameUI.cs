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
    [SerializeField] private Text shopHeaderText;

    [Header("Shop - Offer List")]
    [SerializeField] private Transform offerRoot;
    [SerializeField] private Button offerButtonPrefab;

    [Header("Shop - Owned List")]
    [SerializeField] private Transform ownedRoot;
    [SerializeField] private Button ownedButtonPrefab;

    [Header("Shop - Buttons")]
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button freezeButton;
    [SerializeField] private Button endTurnButton;

    // ===== Battle =====
    [Header("Battle - Header")]
    [SerializeField] private Text battleHeaderText;
    [SerializeField] private Text hpText;
    [SerializeField] private Text pickedText;
    [SerializeField] private Text statText;
    [SerializeField] private Text logText;

    [Header("Battle - Picked Cards")]
[SerializeField] private Transform playerAllRoot;
[SerializeField] private Transform enemyAllRoot;
[SerializeField] private GameObject battleCardPrefab;

private readonly List<GameObject> playerCards = new();
private readonly List<GameObject> enemyCards = new();


    private readonly List<Button> offerButtons = new();
    private readonly List<Button> ownedButtons = new();

    private Action onNext;

    // ---------------- Public API ----------------

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
    /// BG風 酒場（全体Freeze / 購入しても補充しない / 補充はリロールのみ）
    /// </summary>
    public void ShowShop_BG_AllFreeze(
        int round,
        int tier,
        int coins,
        bool shopFrozen,
        int rerollCost,
        int buyCost,
        int sellGain,
        int upgradeCost,
        IReadOnlyList<SkillData> offers,
        IReadOnlyList<SkillInstance> owned,
        Action<int> onBuyOffer,
        Action onReroll,
        Action onUpgrade,
        Action<int> onSellOwned,
        Action onToggleFreezeAll,
        Action onEndTurn
    )
    {
        SetPanel(shop: true);

        if (shopHeaderText != null)
        {
            shopHeaderText.text =
                $"ROUND: {round}\n" +
                $"TIER: {tier}\n" +
                $"COINS: {coins}\n" +
                $"UPGRADE: {(tier >= 6 ? "-" : upgradeCost)}\n" +
                $"REROLL: -{rerollCost}  BUY:-{buyCost}  SELL:+{sellGain}\n" +
                $"FREEZE: {(shopFrozen ? "ON" : "OFF")}";
        }

        // Offer buttons（クリック=BUY）
        RebuildButtons(
            offerRoot,
            offerButtonPrefab,
            offerButtons,
            offers?.Count ?? 0,
            (i, btn) =>
            {
                var s = new SkillInstance(offers[i]);

                var view = btn.GetComponent<CardView>();
                if(view != null) view.Bind(s);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onBuyOffer?.Invoke(i));

                // 空枠 or コイン不足は買えない
                btn.interactable = (s != null) && coins >= buyCost;
            }
        );

        // Owned buttons（クリック=SELL）
        RebuildButtons(
            ownedRoot,
            ownedButtonPrefab,
            ownedButtons,
            owned?.Count ?? 0,
            (i, btn) =>
            {
                var s = owned[i];

                var view = btn.GetComponent<CardView>();
                if(view != null) view.Bind(s);


                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onSellOwned?.Invoke(i));
                btn.interactable = (s != null);
            }
        );

        // Reroll
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() => onReroll?.Invoke());

            // Freeze中はリロール無効（あなたの仕様）
            rerollButton.interactable = !shopFrozen && coins >= rerollCost;
            SetButtonLabel(rerollButton, $"REROLL (-{rerollCost})");
        }

        // Upgrade
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(() => onUpgrade?.Invoke());

            bool canUpgrade = tier < 6 && coins >= upgradeCost;
            upgradeButton.interactable = canUpgrade;
            SetButtonLabel(upgradeButton, tier >= 6 ? "UPGRADE (-)" : $"UPGRADE (-{upgradeCost})");
        }

        // Freeze (ALL)
        if (freezeButton != null)
        {
            freezeButton.onClick.RemoveAllListeners();
            freezeButton.onClick.AddListener(() => onToggleFreezeAll?.Invoke());
            freezeButton.interactable = true;
            SetButtonLabel(freezeButton, shopFrozen ? "UNFREEZE" : "FREEZE");
        }

        // End Round
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(() => onEndTurn?.Invoke());
            endTurnButton.interactable = true;
            SetButtonLabel(endTurnButton, "END ROUND");
        }

        // Nextはショップでは基本非表示
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

public void ShowBattleStart(string enemyName, int playerHp, int enemyHp)
{
    SetPanel(shop: false);

    if (battleHeaderText != null) battleHeaderText.text = $"BATTLE vs {enemyName}";
    if (hpText != null) hpText.text = $"P:{playerHp}  E:{enemyHp}";

    if (pickedText != null) pickedText.text = "";
    if (statText != null) statText.text = "";
    if (logText != null) logText.text = "";

    // ★カード表示を初期化

    if (nextButton != null) nextButton.gameObject.SetActive(false);
}


    public void UpdateBattleTurn(
        int round,
        int playerHp,
        int enemyHp,
        IReadOnlyList<SkillInstance> playerAll,
        IReadOnlyList<int> playerPickedIdx,
        int playerAtk,
        int playerDef,
        IReadOnlyList<SkillInstance> enemyAll,
        IReadOnlyList<int> enemyPickedIdx,
        int enemyAtk,
        int enemyDef,
        int fatigue,
        int damageToEnemy,
        int damageToPlayer
    )
    {
        if (battleHeaderText != null) battleHeaderText.text = $"TURN {round}";
        if (hpText != null) hpText.text = $"P:{playerHp}  E:{enemyHp}";

// HP/ステータス表示はそのまま（省略）

// 1) カード数を確保
EnsureCardList(playerAllRoot, battleCardPrefab, playerCards, playerAll?.Count ?? 0);
EnsureCardList(enemyAllRoot, battleCardPrefab, enemyCards, enemyAll?.Count ?? 0);

// 2) 全カードをBind（成長反映）
BindAllCards(playerCards, playerAll);
BindAllCards(enemyCards, enemyAll);

// 3) picked だけアニメ
AnimatePicked(playerCards, playerPickedIdx);
AnimatePicked(enemyCards, enemyPickedIdx);



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
                $"T{round}: P({playerAtk}-{playerDef})=>{damageToEnemy} / " +
                $"E({enemyAtk}-{enemyDef})=>{damageToPlayer} / F={fatigue}\n";
        }
    }

    public void ShowBattleEnd(bool win)
    {
        if (battleHeaderText != null)
            battleHeaderText.text = win ? "WIN!" : "LOSE...";

        // Next押下でGameControllerが次ターンへ
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            SetButtonLabel(nextButton, "NEXT");
        }
    }

    // ---------------- Helpers ----------------

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

        while (cache.Count < needed)
        {
            var btn = Instantiate(prefab, root);
            cache.Add(btn);
        }

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

    private void RebuildCards(
    Transform root,
    GameObject prefab,
    List<GameObject> cache,
    IReadOnlyList<SkillInstance> picked
)
{
    if (root == null || prefab == null) return;

    int needed = picked?.Count ?? 0;

    while (cache.Count < needed)
    {
        var go = Instantiate(prefab, root);
        cache.Add(go);
    }

    for (int i = 0; i < cache.Count; i++)
    {
        bool active = i < needed;
        cache[i].SetActive(active);

        if (!active) continue;

        var view = cache[i].GetComponent<CardView>();
        if (view != null) view.Bind(picked[i]);
    }
}


private string FormatSkillLine(SkillInstance s, string prefix)
{
    if (s == null || s.data == null) return prefix + "(EMPTY)";

    string g = ((int)s.Grade).ToString();
    string tag = (s.Tag != SkillTag.None) ? $"[{s.Tag}]" : "";
    string type = s.Type.ToString();

    // ★補正込みの値で表示
    return $"{prefix}{s.Name} (G{g}) {tag}  A:{s.Attack} B:{s.Block}  <{type}>";
}


    private void SetButtonLabel(Button btn, string label)
    {
        if (btn == null) return;
        var t = btn.GetComponentInChildren<Text>();
        if (t != null) t.text = label;
    }

    private void EnsureCardList(
    Transform root,
    GameObject prefab,
    List<GameObject> cache,
    int needed
)
{
    if (root == null || prefab == null) return;

    while (cache.Count < needed)
    {
        cache.Add(Instantiate(prefab, root));
    }

    for (int i = 0; i < cache.Count; i++)
        cache[i].SetActive(i < needed);
}

private void BindAllCards(List<GameObject> cache, IReadOnlyList<SkillInstance> skills)
{
    int n = skills?.Count ?? 0;
    for (int i = 0; i < n; i++)
    {
        var view = cache[i].GetComponent<CardView>();
        if (view != null) view.Bind(skills[i]);

        // picked 表示を一旦OFF
        var anim = cache[i].GetComponent<BattleCardAnim>();
        if (anim != null) anim.SetPicked(false);
    }
}

private void AnimatePicked(List<GameObject> cache, IReadOnlyList<int> pickedIdx)
{
    if (pickedIdx == null) return;

    foreach (var idx in pickedIdx)
    {
        if (idx < 0 || idx >= cache.Count) continue;
        var anim = cache[idx].GetComponent<BattleCardAnim>();
        if (anim != null) anim.PlayPicked();
    }
}

}
