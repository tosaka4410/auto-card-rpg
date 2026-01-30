using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameUI ui;

    [Header("Pools")]
    public List<SkillData> skillPool;

    [Header("Enemy Presets (Training)")]
    public List<EnemyPreset> enemyPresets;

    [Header("Config")]
    [SerializeField] private int playerBaseHp = 40;

    private const int BuyCost = 3;
    private const int SellGain = 1;
    private const int RerollCost = 1;

    private Monster player;
    private readonly Shop shop = new();

    // BG風：ターン制酒場
    private int turn = 0;
    private int coins = 0;

    // 酒場Tier
    private int shopTier = 1;

    // ★全体Freeze
    private bool shopFrozen = false;

    // 提示スロット
    private List<SkillData> offers = new();

    // Tierごとの提示数（あなたのゲーム用に調整OK）
    private readonly int[] offerCountByTier = new int[] { 0, 3, 4, 4, 5, 5, 6 };

    // Tierアップコスト（BG代表値）
    private readonly int[] tierUpCost = new int[] { 0, 0, 5, 7, 8, 9, 10 };

    void Start()
    {
        player = new Monster(playerBaseHp);

        // バトル終了 → Next押下 → 次ターン開始
        ui.BindNextButton(() => StartNextTurn());

        StartNextTurn();
    }

    // ===== Turn Flow =====

    void StartNextTurn()
    {
        turn++;

        // コイン支給：Turn1=3, Turn2=4 ... Turn8以降=10
        coins = Mathf.Min(10, 2 + turn);

        // Tierに応じたスロット数
        EnsureOfferSlots(offerCountByTier[shopTier]);

        // ターン開始の無料更新（Freeze中は更新しない）
        shop.RefreshAll(skillPool, shopTier, offers, shopFrozen);

        ShowShop();
    }

    void EnsureOfferSlots(int count)
    {
        while (offers.Count < count) offers.Add(null);
        if (offers.Count > count) offers = offers.Take(count).ToList();
    }

    void ShowShop()
    {
        int upgradeCost = GetTierUpCost();

        // ★ここはあなたの GameUI の関数名に合わせてください
        // 例：ShowShop_BG_AllFreeze(...) を実装していない場合、既存のShowShopに寄せる必要があります
        ui.ShowShop_BG_AllFreeze(
            turn: turn,
            tier: shopTier,
            coins: coins,
            shopFrozen: shopFrozen,
            rerollCost: RerollCost,
            buyCost: BuyCost,
            sellGain: SellGain,
            upgradeCost: upgradeCost,
            offers: offers,
            owned: player.skills,
            onBuyOffer: (i) => TryBuy(i),
            onReroll: () => TryReroll(),
            onUpgrade: () => TryUpgrade(),
            onSellOwned: (i) => TrySell(i),
            onToggleFreezeAll: () => ToggleShopFreeze(),
            onEndTurn: () => EndTurn()
        );
    }

    void ToggleShopFreeze()
    {
        shopFrozen = !shopFrozen;
        ShowShop();
    }

    void TryReroll()
    {
        // Freeze中は更新できない（あなたの仕様）
        if (shopFrozen) return;

        if (coins < RerollCost) return;
        coins -= RerollCost;

        shop.RefreshAll(skillPool, shopTier, offers, shopFrozen);
        ShowShop();
    }

    void TryUpgrade()
    {
        if (shopTier >= 6) return;

        int cost = GetTierUpCost();
        if (coins < cost) return;

        coins -= cost;
        shopTier++;

        EnsureOfferSlots(offerCountByTier[shopTier]);

        // BGでは「上げた瞬間に店の内容は変わらない」扱いにしておく
        ShowShop();
    }

    int GetTierUpCost()
    {
        if (shopTier >= 6) return 999999;
        return tierUpCost[shopTier + 1];
    }

    void TryBuy(int offerIndex)
    {
        if (offerIndex < 0 || offerIndex >= offers.Count) return;

        var s = offers[offerIndex];
        if (s == null) return;

        if (coins < BuyCost) return;
        coins -= BuyCost;

        if (player.skills.Count >= 7) player.skills.RemoveAt(0);
        player.skills.Add(s);

        // トリプルがあるならここで
        TryTriple(player);

        // ★重要：購入した枠は補充しない（あなたの仕様）
        offers[offerIndex] = null;

        ShowShop();
    }

    void TrySell(int ownedIndex)
    {
        if (ownedIndex < 0 || ownedIndex >= player.skills.Count) return;

        player.skills.RemoveAt(ownedIndex);
        coins += SellGain;

        ShowShop();
    }

    void EndTurn()
    {
        StartTrainingBattle();
    }

    // ===== Battle =====

    void StartTrainingBattle()
    {
        if (enemyPresets == null || enemyPresets.Count == 0)
        {
            Debug.LogError("enemyPresets が空です。EnemyPreset を1つ以上アサインしてください。");
            return;
        }

        var preset = enemyPresets[Random.Range(0, enemyPresets.Count)];
        var enemy = Monster.FromPreset(preset);

        player.hp = player.maxHp;

        ui.ShowBattleStart(preset.enemyName, player.hp, enemy.hp);
        StartCoroutine(BattleLoop(player, enemy));
    }

    IEnumerator BattleLoop(Monster myMonster, Monster enemy)
    {
        int t = 0;

        var myCooldownBlocked = new HashSet<string>();
        var enemyCooldownBlocked = new HashSet<string>();

        while (myMonster.hp > 0 && enemy.hp > 0)
        {
            t++;

            var p = ResolveTurn(myMonster, enemy, t, myCooldownBlocked);
            var e = ResolveTurn(enemy, myMonster, t, enemyCooldownBlocked);

            ui.UpdateBattleTurn(
                t,
                myMonster.hp,
                enemy.hp,
                p.pickedNames, p.atk, p.def,
                e.pickedNames, e.atk, e.def,
                p.fatigue,
                p.damage,
                e.damage
            );

            yield return new WaitForSeconds(0.35f);
        }

        bool win = myMonster.hp > 0;
        ui.ShowBattleEnd(win); // Nextボタンで StartNextTurn
    }

    private (string pickedNames, int atk, int def, int fatigue, int damage) ResolveTurn(
        Monster atkM,
        Monster defM,
        int t,
        HashSet<string> cdBlockedNextTurn
    )
    {
        int n = atkM.skills.Count;
        int k = Mathf.CeilToInt(n / 2f);

        var pool = new List<SkillData>();
        foreach (var s in atkM.skills)
        {
            if (s == null) continue;

            // Cooldown(1)：前ターンに引いたら次ターン除外
            if (s.tag == SkillTag.Cooldown &&
                !string.IsNullOrEmpty(s.skillId) &&
                cdBlockedNextTurn.Contains(s.skillId))
                continue;

            pool.Add(s);
        }

        var picked = new List<SkillData>();

        // Stable優先
        foreach (var s in pool)
        {
            if (s.tag == SkillTag.Stable && picked.Count < k)
                picked.Add(s);
        }

        // 残りランダム
        var rest = pool.Except(picked).ToList();
        while (picked.Count < k && rest.Count > 0)
        {
            int idx = Random.Range(0, rest.Count);
            picked.Add(rest[idx]);
            rest.RemoveAt(idx);
        }

        int atk = 0, def = 0;
        foreach (var s in picked)
        {
            atk += s.attack;
            def += s.block;
        }

        int fatigue = Mathf.Max(0, t - 4);
        int damage = Mathf.Max(0, atk - def) + fatigue;

        defM.hp -= damage;

        // 次ターン除外更新
        cdBlockedNextTurn.Clear();
        foreach (var s in picked)
        {
            if (s.tag == SkillTag.Cooldown && !string.IsNullOrEmpty(s.skillId))
                cdBlockedNextTurn.Add(s.skillId);
        }

        return (string.Join(", ", picked.Select(x => x.skillName)), atk, def, fatigue, damage);
    }

    // ===== Triple (optional) =====
    // 既にあなたのTryTriple実装があるなら差し替えてOK
    private void TryTriple(Monster m)
    {
        // 未実装でも動くように何もしない
    }
}
