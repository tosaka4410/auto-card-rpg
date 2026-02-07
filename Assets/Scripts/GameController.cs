using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameUI ui;

    [Header("Pools")]
    public List<SkillData> skillPool;

    [Header("Enemy Presets (Training)")]
    public List<EnemyPreset> enemyPresets;

    [Header("Config")]
    [SerializeField]
    private int playerBaseHp = 40;

    // ===== Tier Upgrade Discount (per turn) =====
    [Header("Tier Upgrade Discount")]
    [SerializeField]
    private int tierUpDiscountPerTurn = 1; // 1ターンごとに何コスト下がるか

    [SerializeField]
    private int tierUpMinCost = 0; // 下限（0 or 1推奨）

    private const int BuyCost = 3;
    private const int SellGain = 1;
    private const int RerollCost = 1;

    private Monster player;
    private readonly Shop shop = new();
    private Dictionary<string, SkillData> skillDict;


    // BG風：ターン制酒場
    private int turn = 0;
    private int coins = 0;

    // 最後にアップグレードしたターン（割引リセット基準）
    private int lastUpgradeTurn = 0;

    // 酒場Tier
    private int shopTier = 1;

    // ★全体Freeze
    private bool shopFrozen = false;

    // 提示スロット
    private List<SkillData> offers = new();

    // Tierごとの提示数（あなたのゲーム用に調整OK）
    private readonly int[] offerCountByTier = new int[] { 0, 3, 4, 4, 5, 5, 6 };

    // Tierアップコスト（BG代表値）
    private readonly int[] tierUpCost = new int[] { 0, 0, 6, 7, 8, 10, 11 };

    void Start()
    {
        player = new Monster(playerBaseHp);
        skillDict = skillPool
    .Where(s => s != null && !string.IsNullOrEmpty(s.skillId))
    .GroupBy(s => s.skillId)
    .ToDictionary(g => g.Key, g => g.First());


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
        while (offers.Count < count)
            offers.Add(null);
        if (offers.Count > count)
            offers = offers.Take(count).ToList();
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
        if (shopFrozen)
            return;

        if (coins < RerollCost)
            return;
        coins -= RerollCost;

        shop.RefreshAll(skillPool, shopTier, offers, shopFrozen);
        ShowShop();
    }

    void TryUpgrade()
    {
        if (shopTier >= 6)
            return;

        int cost = GetTierUpCost();
        if (coins < cost)
            return;

        coins -= cost;
        shopTier++;

        // ★割引リセット：このターンを基準にする
        lastUpgradeTurn = turn;

        EnsureOfferSlots(offerCountByTier[shopTier]);

        // BGでは「上げた瞬間に店の内容は変わらない」扱いにしておく
        ShowShop();
    }

    int GetTierUpCost()
    {
        if (shopTier >= 6)
            return 999999;

        int baseCost = tierUpCost[shopTier + 1];

        // 「最後のアップグレード」以降の経過ターンで割引が増える
        // 例：アップグレード直後の同ターンは0、次ターンから1…
        int turnsSinceUpgrade = Mathf.Max(0, turn - lastUpgradeTurn);

        int discount = turnsSinceUpgrade * tierUpDiscountPerTurn;

        int finalCost = baseCost - discount;
        finalCost = Mathf.Max(tierUpMinCost, finalCost);

        return finalCost;
    }

    void TryBuy(int offerIndex)
    {
        if (offerIndex < 0 || offerIndex >= offers.Count)
            return;

        var s = offers[offerIndex];
        if (s == null)
            return;

        if (coins < BuyCost)
            return;
        coins -= BuyCost;

        if (player.skills.Count >= 7)
            player.skills.RemoveAt(0);
        player.skills.Add(new SkillInstance(s));

        // トリプルがあるならここで
        TryTriple(player);

        // ★重要：購入した枠は補充しない（あなたの仕様）
        offers[offerIndex] = null;

        ShowShop();
    }

    void TrySell(int ownedIndex)
    {
        if (ownedIndex < 0 || ownedIndex >= player.skills.Count)
            return;

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
        player.ResetBattleState();
        enemy.ResetBattleState();

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
                p.pickedNames,
                p.atk,
                p.def,
                e.pickedNames,
                e.atk,
                e.def,
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

        var pool = new List<SkillInstance>();
        foreach (var inst in atkM.skills)
        {
            if (inst == null || inst.data == null)
                continue;

            // Cooldown(1)：前ターンに引いたら次ターン除外
            if (
                inst.Tag == SkillTag.Cooldown
                && !string.IsNullOrEmpty(inst.SkillId)
                && cdBlockedNextTurn.Contains(inst.SkillId)
            )
                continue;

            pool.Add(inst);
        }

        var picked = new List<SkillInstance>();

        // Stable優先
        foreach (var s in pool)
        {
            if (s.Tag == SkillTag.Stable && picked.Count < k)
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

        int atk = 0,
            def = 0;
        foreach (var s in picked)
        {
            atk += s.Attack;
            def += s.Block;
        }

        foreach (var s in picked)
            s.OnUse();

        int fatigue = Mathf.Max(0, t - 4);
        int damage = Mathf.Max(0, atk - def) + fatigue;

        defM.hp -= damage;

        // 次ターン除外更新
        cdBlockedNextTurn.Clear();
        foreach (var s in picked)
        {
            if (s.Tag == SkillTag.Cooldown && !string.IsNullOrEmpty(s.SkillId))
                cdBlockedNextTurn.Add(s.SkillId);
        }

        return (string.Join(", ", picked.Select(x => x.Name)), atk, def, fatigue, damage);
    }

// ===== Triple =====
// 同一(baseId + tier)が3枚 → tier+1 を1枚生成して置き換える（連鎖あり）
private void TryTriple(Monster m)
{
    if (m == null || m.skills == null || m.skills.Count == 0) return;

    // 連鎖するので while で回す
    while (true)
    {
        // key = $"{baseId}|{tier}"
        var buckets = new Dictionary<string, List<int>>();

        for (int i = 0; i < m.skills.Count; i++)
        {
            var s = m.skills[i];
            if (s == null || string.IsNullOrEmpty(s.SkillId)) continue;

            GetBaseAndTier(s.SkillId, out var baseId, out var tier);

            string key = $"{baseId}|{tier}";
            if (!buckets.TryGetValue(key, out var list))
            {
                list = new List<int>();
                buckets[key] = list;
            }
            list.Add(i);
        }

        // どれか1つでも3枚以上あるか？
        string foundKey = null;
        List<int> idxs = null;

        foreach (var kv in buckets)
        {
            if (kv.Value.Count >= 3)
            {
                foundKey = kv.Key;
                idxs = kv.Value;
                break;
            }
        }

        if (foundKey == null) break; // もうトリプルなし

        // foundKey を分解
        var parts = foundKey.Split('|');
        string baseIdFound = parts[0];
        int tierFound = int.Parse(parts[1]);

        int nextTier = tierFound + 1;
        string evolvedId = $"{baseIdFound}_t{nextTier}";

        // ベース参照（進化生成は baseSkill が必要）
        if (skillDict == null || !skillDict.TryGetValue(baseIdFound, out var baseSkill) || baseSkill == null)
        {
            Debug.LogWarning($"TryTriple: base skill not found: {baseIdFound}");
            break;
        }

        // 進化スキル生成
        var evolved = SkillRuntimeFactory.CreateEvolvedFromBase(baseSkill, evolvedId, nextTier);

        // 3枚消す（インデックスがズレないように降順）
        idxs.Sort();
        int a = idxs[0];
        int b = idxs[1];
        int c = idxs[2];

        m.skills.RemoveAt(c);
        m.skills.RemoveAt(b);
        m.skills.RemoveAt(a);

        // 進化を追加（位置は末尾でOK。位置を維持したいなら a に Insert してもよい）
        m.skills.Add(new SkillInstance(evolved));

        // ここで次の while 周回で連鎖チェック
    }
}

// skillId から baseId/tier を取り出す（ベースは tier=1 扱い）
private static void GetBaseAndTier(string skillId, out string baseId, out int tier)
{
    baseId = skillId;
    tier = 1;

    if (SkillRuntimeFactory.TryParseEvolvedId(skillId, out var b, out var t))
    {
        baseId = b;
        tier = Mathf.Max(1, t);
    }
}

}
