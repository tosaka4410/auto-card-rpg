// Assets/Scripts/GameController.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameUI ui;

    // いまは無くても動く（後で繋ぐ）
    [SerializeField] private MonsterLibraryUI libraryUI;

    [Header("Pools")]
    public List<SkillData> skillPool;

    [Header("Enemy Presets (Training)")]
    public List<EnemyPreset> enemyPresets;

    [Header("Config")]
    [SerializeField] private int playerBaseHp = 40;
    [SerializeField] private int startingCoins = 10;

    private const int BuyCost = 3;
    private const int SellGain = 1;

    private Monster player;
    private readonly Shop shop = new();

    private int shopGrade = 1;
    private int coins;
    private int shopTurn = 0; // ショップに来た回数（アップグレード割引用）

    private List<SkillData> currentOffers = new();

    // Skill辞書（Record -> Monster変換用）
    private Dictionary<string, SkillData> skillDict;

    // 疑似PvP用の選択
    private MonsterRecord selectedMy;
    private MonsterRecord selectedEnemy;

    // 戦闘ログ用
    private int totalBattles;
    private int totalTurns;

    // upgrade base costs: index = nextGrade
    private readonly int[] upgradeBaseCosts = new int[]
    {
        0,  // 0 unused
        0,  // G1
        10, // ->G2
        12, // ->G3
        15, // ->G4
        18, // ->G5
        22  // ->G6
    };

    void Start()
    {
        player = new Monster(playerBaseHp);
        coins = startingCoins;

        skillDict = BuildSkillDict(skillPool);

        // 次へボタン：バトル終了→ショップへ
        ui.BindNextButton(() => OpenShop());

        if (libraryUI != null) libraryUI.Hide();

        OpenShop();
    }

    // ===== Shop =====

    void OpenShop()
    {
        shopTurn++;

        int offerCount = 2 + shopGrade;
        // もし SkillGradeフィルタ対応の Offer(pool,count,grade) を実装済みならそちらに差し替えてOK
        currentOffers = shop.Offer(skillPool, offerCount, shopGrade);

        int upgradeCost = GetUpgradeCost(shopGrade, shopTurn);

        ui.ShowShop(
            shopGrade: shopGrade,
            coins: coins,
            upgradeCost: upgradeCost,
            buyCost: BuyCost,
            sellGain: SellGain,
            offers: currentOffers,
            owned: player.skills,
            onPickOffer: (idx) => TryBuySkillAt(idx),
            onReroll: () => Reroll(offerCount),
            onUpgrade: () => TryUpgradeShop(),
            onSellOwned: (idx) => TrySellSkillAt(idx),
            onStartBattle: StartBattle
        );
    }

    void Reroll(int offerCount)
    {
        // リロールコストを付けたい場合はここで coins 減らす
        currentOffers = shop.Offer(skillPool, offerCount, shopGrade);
        OpenShop(); // 表示更新（shopTurnを増やしたくないなら OpenShopを呼ばずにShowShopを直接呼ぶ運用にする）
    }

    void TryBuySkillAt(int offerIndex)
    {
        if (offerIndex < 0 || offerIndex >= currentOffers.Count) return;
        var skill = currentOffers[offerIndex];
        if (skill == null) return;

        if (coins < BuyCost) return;
        coins -= BuyCost;

        if (player.skills.Count >= 7)
            player.skills.RemoveAt(0);

        player.skills.Add(skill);

        // トリプルが実装済みならここで
        TryTriple(player);

        OpenShop();
    }

    void TrySellSkillAt(int index)
    {
        if (index < 0 || index >= player.skills.Count) return;

        player.skills.RemoveAt(index);
        coins += SellGain;

        OpenShop();
    }

    void TryUpgradeShop()
    {
        if (shopGrade >= 6) return;

        int cost = GetUpgradeCost(shopGrade, shopTurn);
        if (coins < cost) return;

        coins -= cost;
        shopGrade = Mathf.Min(6, shopGrade + 1);

        OpenShop();
    }

    int GetUpgradeCost(int currentGrade, int shopTurn)
    {
        if (currentGrade >= 6) return 999999;

        int baseCost = upgradeBaseCosts[currentGrade + 1];

        int discounted = baseCost - shopTurn * 1;        // 1ターンごとに-1
        int floor = Mathf.CeilToInt(baseCost * 0.5f);    // 下限50%

        return Mathf.Max(discounted, floor);
    }

    // ===== Library (optional) =====

    public void OpenLibrary()
    {
        if (libraryUI == null)
        {
            Debug.LogWarning("libraryUI が未設定です。MonsterLibraryUI をアサインしてください。");
            return;
        }
        libraryUI.Show();
    }

    public void CloseLibraryAndBackToShop()
    {
        if (libraryUI != null) libraryUI.Hide();
        OpenShop();
    }

    // ===== Battle Entry =====

    void StartBattle()
    {
        PullSelectedRecordsFromLibrary();

        if (selectedMy != null && selectedEnemy != null)
        {
            StartPseudoPvp(selectedMy, selectedEnemy);
            return;
        }

        StartTrainingBattle();
    }

    void PullSelectedRecordsFromLibrary()
    {
        if (libraryUI == null) { selectedMy = null; selectedEnemy = null; return; }

        selectedMy = libraryUI.SelectedMy;
        selectedEnemy = libraryUI.SelectedEnemy;
    }

    void StartPseudoPvp(MonsterRecord my, MonsterRecord enemyRec)
    {
        var myMonster = Monster.CreateMonsterFromRecord(my, skillDict);
        var enemyMonster = Monster.CreateMonsterFromRecord(enemyRec, skillDict);

        ui.ShowBattleStart($"PVP: {GetDisplayName(enemyRec)}", myMonster.hp, enemyMonster.hp);
        StartCoroutine(BattleLoop(myMonster, enemyMonster, isPvp: true, myRecord: my, enemyRecord: enemyRec));
    }

    void StartTrainingBattle()
    {
        if (enemyPresets == null || enemyPresets.Count == 0)
        {
            Debug.LogError("enemyPresets が空です。EnemyPreset を1つ以上アサインしてください。");
            return;
        }

        var preset = enemyPresets[Random.Range(0, enemyPresets.Count)];
        var enemy = Monster.FromPreset(preset);

        // 毎戦リセット
        player.hp = player.maxHp;

        ui.ShowBattleStart(preset.enemyName, player.hp, enemy.hp);
        StartCoroutine(BattleLoop(player, enemy, isPvp: false, myRecord: null, enemyRecord: null));
    }

    // ===== Battle Loop =====

    IEnumerator BattleLoop(Monster myMonster, Monster enemy, bool isPvp, MonsterRecord myRecord, MonsterRecord enemyRecord)
    {
        totalBattles++;
        int turn = 0;

        var myCooldownBlocked = new HashSet<string>();
        var enemyCooldownBlocked = new HashSet<string>();

        while (myMonster.hp > 0 && enemy.hp > 0)
        {
            turn++;
            totalTurns++;

            var p = ResolveTurn(myMonster, enemy, turn, myCooldownBlocked);
            var e = ResolveTurn(enemy, myMonster, turn, enemyCooldownBlocked);

            ui.UpdateBattleTurn(
                turn,
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
        ui.ShowBattleEnd(win);

        if (!isPvp)
        {
            // 勝利時にコイン報酬（おすすめ：最低限の経済循環）
            if (win) coins += 3;

            // 勝利時保存（Repositoryがあれば）
            if (win)
            {
                var record = MonsterRecordFactory.CreateRecordFromMonster(
                    player,
                    finalShopGrade: shopGrade,
                    totalBattles: totalBattles,
                    totalTurns: totalTurns
                );

                if (MonsterRecordRepository.I != null)
                    MonsterRecordRepository.I.Add(record);
            }
        }
        else
        {
            if (myRecord != null)
            {
                if (win) myRecord.pvpWin++;
                else myRecord.pvpLose++;
            }
            if (enemyRecord != null)
            {
                if (win) enemyRecord.pvpLose++;
                else enemyRecord.pvpWin++;
            }
        }
    }

    // ===== Turn Resolution =====

    private (string pickedNames, int atk, int def, int fatigue, int damage) ResolveTurn(
        Monster atkM,
        Monster defM,
        int turn,
        HashSet<string> cooldownBlockedNextTurn
    )
    {
        int n = atkM.skills.Count;
        int k = Mathf.CeilToInt(n / 2f);

        var pool = new List<SkillData>();
        foreach (var s in atkM.skills)
        {
            if (s == null) continue;

            if (s.tag == SkillTag.Cooldown &&
                !string.IsNullOrEmpty(s.skillId) &&
                cooldownBlockedNextTurn.Contains(s.skillId))
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

        var rest = pool.Except(picked).ToList();
        while (picked.Count < k && rest.Count > 0)
        {
            int idx = Random.Range(0, rest.Count);
            picked.Add(rest[idx]);
            rest.RemoveAt(idx);
        }

        int atk = 0;
        int def = 0;
        foreach (var s in picked)
        {
            atk += s.attack;
            def += s.block;
        }

        int fatigue = Mathf.Max(0, turn - 4);
        int damage = Mathf.Max(0, atk - def) + fatigue;

        defM.hp -= damage;

        cooldownBlockedNextTurn.Clear();
        foreach (var s in picked)
        {
            if (s.tag == SkillTag.Cooldown && !string.IsNullOrEmpty(s.skillId))
                cooldownBlockedNextTurn.Add(s.skillId);
        }

        string pickedNames = string.Join(", ", picked.Where(x => x != null).Select(x => x.skillName));
        return (pickedNames, atk, def, fatigue, damage);
    }

    // ===== Triple (optional) =====
    // 既に実装済みなら、このメソッドを削除してあなたの版に置き換えてOK
    private void TryTriple(Monster m)
    {
        // 未実装でも動くように“何もしない”が安全
        // トリプル導入済みなら、ここにあなたのTryTripleを貼ってください
    }

    // ===== Helpers =====

    private Dictionary<string, SkillData> BuildSkillDict(List<SkillData> allSkills)
    {
        var dict = new Dictionary<string, SkillData>();

        foreach (var s in allSkills)
        {
            if (s == null) continue;

            if (string.IsNullOrEmpty(s.skillId))
            {
                Debug.LogWarning($"SkillData '{s.name}' の skillId が空です。Record保存/PvPで困ります。");
                continue;
            }

            dict[s.skillId] = s;
        }

        return dict;
    }

    private string GetDisplayName(MonsterRecord r)
    {
        if (r == null) return "(null)";
        return string.IsNullOrEmpty(r.displayName) ? $"Monster-{r.recordId.Substring(0, 4)}" : r.displayName;
    }
}
